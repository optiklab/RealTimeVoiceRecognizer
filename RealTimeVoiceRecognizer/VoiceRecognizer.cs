/*****************************************************
************* Copyright OptikLab 2011 ****************
************* http://www.ayarkov.com  ****************
******************************************************/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Documents;
using System.Windows.Threading;
using CUETools.Codecs;
using CUETools.Codecs.FLAKE;

namespace RealTimeVoiceRecognizer
{
    /// <summary>
    /// Handler for events when buffer is done.
    /// </summary>
    /// <param name="data">Data to fill in.</param>
    /// <param name="size">Size.</param>
    public delegate void BufferDoneEventHandler(IntPtr data, uint size);

    /// <summary>
    /// 
    /// </summary>
    internal sealed class VoiceRecognizer
    {
        #region Public events

        /// <summary>
        /// Raises when some peace of sound is recognized and result is received from the server.
        /// </summary>
        public event VoiceRecognizedEventHandler VoiceRecognized;

        #endregion

        #region Public properties

        /// <summary>
        /// Current application state.</summary>
        public ApplicationState State
        {
            get;
            private set;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Starts recording of sound with using specified audio settings.
        /// </summary>
        /// <param name="settings">Audio settings.</param>
        /// <param name="equalizer">Equalizer.</param>
        /// <returns>Changed state of application.</returns>
        public ApplicationState Start(AudioSettings settings,
            Action<List<byte>, IntPtr> soundProcessing,
            string shortLanguageName,
            string temporaryFolder)
        {
            _shortLanguageName = shortLanguageName;
            _tempWavFilePath = _GetTemporaryFilePath(temporaryFolder, "wav");
            _tempFlacFilePath = _GetTemporaryFilePath(temporaryFolder, "flac");
            _soundProcessing = soundProcessing;

            if (State == ApplicationState.Idle)
            {
                _RecorderLoad(settings);

                _timer.Start();

                _RecordWave();
            }
            else
            {
                Debug.Assert(false);
            }

            return State;
        }

        /// <summary>
        /// Stops recording process.
        /// </summary>
        public void Stop()
        {
            if (_isRecorderLoaded)
                _RecordingStop();
        }

        #endregion

        #region Private static methods

        /// <summary>
        /// Copies header to a stream.
        /// </summary>
        /// <param name="waveData">Wav data stream.</param>
        /// <param name="format">WAVEFORMATEX wav format.</param>
        /// <returns>Stream.</returns>
        private static Stream _CreateStream(Stream waveData, WaveFormatEx format)
        {
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF".ToCharArray()));
            writer.Write((Int32)(waveData.Length + 36)); //File length minus first 8 bytes of RIFF description
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ".ToCharArray()));
            writer.Write((Int32)16); //length of following chunk: 16
            writer.Write((Int16)format.wFormatTag);
            writer.Write((Int16)format.nChannels);
            writer.Write((Int32)format.nSamplesPerSec);
            writer.Write((Int32)format.nAvgBytesPerSec);
            writer.Write((Int16)format.nBlockAlign);
            writer.Write((Int16)format.wBitsPerSample);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data".ToCharArray()));
            writer.Write((Int32)waveData.Length);

            waveData.Seek(0, SeekOrigin.Begin);
            byte[] b = new byte[waveData.Length];
            waveData.Read(b, 0, (int)waveData.Length);
            writer.Write(b);
            writer.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        /// <summary>Конвертирование wav-файла во flac</summary>
        /// <returns>Частота дискретизации</returns>
        private static void _Wav2Flac(String wavName, string flacName)
        {
            Debug.Assert(wavName != null);
            Debug.Assert(!string.IsNullOrEmpty(flacName));

            IAudioSource audioSource = new WAVReader(wavName, null);
            AudioBuffer buff = new AudioBuffer(audioSource, 0x10000);

            FlakeWriter flakewriter = new FlakeWriter(flacName, audioSource.PCM);

            FlakeWriter audioDest = flakewriter;
            while (audioSource.Read(buff, -1) != 0)
            {
                audioDest.Write(buff);
            }

            audioDest.Close();
            audioSource.Close();
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Record a wave file.
        /// </summary>
        private void _RecordWave()
        {
            if (_recorder != null && _recorder.Record() == MMSYSERR.NOERROR)
            {
                State = ApplicationState.Recording;
            }
            else
                throw new Exception(Properties.Resources.DeviceNotFoundError);
        }

        /// <summary>
        /// Loads recorder.
        /// </summary>
        /// <param name="settings">Audio settings.</param>
        /// <param name="equalizer">Equalizer.</param>
        private void _RecorderLoad(AudioSettings settings)
        {
            try
            {
                if (_isRecorderLoaded)
                    _RecorderUnLoad();

                // Prepare destination stream.
                if (_streamMemory != null)
                    _streamMemory.Dispose();
                _streamMemory = new MemoryStream();

                // Create wave format and load recorder.
                _waveFormat = _CreateWaveFormat((uint)settings.SamplesPerSecond, settings.isEightBitSample, settings.Channels);

                _recorder = new WaveInRecorder(settings.AudioInputDevice, new BufferDoneEventHandler(_RecordData), _waveFormat);

                _isRecorderLoaded = true;
            }
            catch (Exception)
            {
                throw new Exception(Properties.Resources.DeviceNotFoundError);
            }
        }

        /// <summary>
        /// Creates a WAVEFORMATEX structure.
        /// </summary>
        /// <param name="rate">Bit rate.</param>
        /// <param name="isEightBits">Is 8bits per sample, or 16th.</param>
        /// <param name="channels">Number of channels.</param>
        /// <returns>Created wave format object.</returns>
        private WaveFormatEx _CreateWaveFormat(uint rate, bool isEightBits, ushort channels)
        {
            WaveFormatEx wfx = new WaveFormatEx();
            wfx.wFormatTag = WAVE_FORMAT_PCM;
            wfx.nChannels = channels;
            wfx.nSamplesPerSec = rate;

            ushort bits = isEightBits ? (ushort)8 : (ushort)16;

            wfx.wBitsPerSample = bits;
            wfx.nBlockAlign = (ushort)(channels * (bits / 8));
            wfx.nAvgBytesPerSec = wfx.nSamplesPerSec * wfx.nBlockAlign;
            wfx.cbSize = 0;

            return wfx;
        }

        /// <summary>
        /// Recorder callback.
        /// </summary>
        /// <param name="data">Data to fill in.</param>
        /// <param name="size">Buffer size.</param>
        private void _RecordData(IntPtr data, uint size)
        {
            if (_recodingBuffer == null)
                _recodingBuffer = new byte[size];
            else
                Array.Resize(ref _recodingBuffer, (int)size);

            Marshal.Copy(data, _recodingBuffer, 0, (int)size);

            // Process byte stream.
            ProcessCallback ps = new ProcessCallback(_soundProcessing);
                Dispatcher.CurrentDispatcher.Invoke(ps, new object[] { _recodingBuffer.ToList(), data });

            _streamMemory.Write(_recodingBuffer, 0, _recodingBuffer.Length);

            // Update labels.
            PositionChangedCallback pc = new PositionChangedCallback(_PositionChange);
            Dispatcher.CurrentDispatcher.Invoke(pc, new object[] { });
        }

        /// <summary>
        /// Player position slider change.
        /// </summary>
        private void _PositionChange()
        {
            if (State == ApplicationState.Recording && _timer.ElapsedMilliseconds > 5000)
            {
                _RecordingSave();

                _timer.Stop();
                _timer.Reset();
                _timer.Start();
            }
        }

        /// <summary>
        /// Save recording.
        /// </summary>
        private void _RecordingSave()
        {
            MemoryStream newTempStream = new MemoryStream();
            _streamMemory.WriteTo(newTempStream);
            _streamMemory.Dispose();
            _streamMemory = new MemoryStream();

            Stream sw = _CreateStream(newTempStream, _waveFormat);
            byte[] bf = new byte[sw.Length - sw.Position];
            sw.Read(bf, 0, bf.Length);
            sw.Dispose();

            FileStream fs = new FileStream(_tempWavFilePath, FileMode.Create);
            fs.Write(bf, 0, bf.Length);
            fs.Close();
            fs.Dispose();

            _Wav2Flac(_tempWavFilePath, _tempFlacFilePath);

            // Send data in another thread.
            ThreadStart ts = new ThreadStart(SendData);
            Thread t = new Thread(ts);
            t.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        private void SendData()
        {
            var webRecognizer = new VoiceWebService();
            var data = webRecognizer.RecognizeSoundFile(_tempFlacFilePath, _shortLanguageName);
            string text = string.Empty;

            if (data != null && data.Hypotheses != null && data.Hypotheses.Length > 0)
                text = data.Hypotheses[0].Utterance;
            else
                text = (string)App.Current.FindResource("CanNotParse");

            // Raise event to show data.
            if (VoiceRecognized != null && !string.IsNullOrEmpty(text))
                VoiceRecognized(this, new VoiceRecognizedEventArgs(text));
        }

        /// <summary>
        /// 
        /// </summary>
        private string _GetTemporaryFilePath(string folderTempPath, string extension)
        {
            string fileName = Path.GetRandomFileName();

            if (Path.IsPathRooted(folderTempPath))
            {
                var di = new DirectoryInfo(folderTempPath);
                if (!di.Exists)
                    di.Create();
                string path = Path.Combine(folderTempPath, fileName);

                return Path.ChangeExtension(path, extension);
            }
            else
            {
                string path = Path.Combine(Environment.CurrentDirectory, folderTempPath, fileName);

                var di = Directory.CreateDirectory(path);

                return Path.ChangeExtension(di.FullName, extension);
            }
        }

        /// <summary>
        /// Stop recording.
        /// </summary>
        private void _RecordingStop()
        {
            if (_recorder != null)
            {
                _recorder.Stop();
                _RecordingSave();

                _timer.Stop();
                _timer.Reset();

                _RecorderUnLoad();
            }

            State = ApplicationState.Idle;
        }

        /// <summary>
        /// Unload recorder.
        /// </summary>
        private void _RecorderUnLoad()
        {
            try
            {
                if (_recorder != null)
                {
                    if (State == ApplicationState.Recording)
                        _recorder.Stop();

                    _recorder.Dispose();
                    _recorder = null;
                }

                if (_streamMemory != null)
                {
                    _streamMemory.Dispose();
                    _streamMemory = null;
                }
            }
            finally
            {
                _isRecorderLoaded = false;
            }
        }

        #endregion

        #region Delegates

        //private delegate void SetDrawCallback(byte[] data);
        //private delegate void ResetCallback();

        /// <summary>
        /// Callback for changing position during recording.
        /// </summary>
        private delegate void PositionChangedCallback();

        /// <summary>
        /// Callback for processing recording.
        /// </summary>
        /// <param name="buffer">Data buffer.</param>
        /// <param name="data">Data to fill in.</param>
        private delegate void ProcessCallback(List<byte> buffer, IntPtr data);//ref byte[] buffer, IntPtr data);

        #endregion

        #region Private constants

        /// <summary>
        /// Wave format index.
        /// </summary>
        private const int WAVE_FORMAT_PCM = 1;

        #endregion

        #region Private fields

        private Action<List<byte>, IntPtr> _soundProcessing;

        /// <summary>
        /// Low-level wave recorder.
        /// </summary>
        private WaveInRecorder _recorder;

        /// <summary>
        /// Is recorded loaded.</summary>
        private bool _isRecorderLoaded;

        /// <summary>
        /// Wave format structure.
        /// </summary>
        private WaveFormatEx _waveFormat;

        /// <summary>
        /// Memory stream.
        /// </summary>
        private MemoryStream _streamMemory;

        /// <summary>
        /// Buffer for recording.
        /// </summary>
        private byte[] _recodingBuffer;

        /// <summary>
        /// 
        /// </summary>
        private Stopwatch _timer = new Stopwatch();

        /// <summary>
        /// 
        /// </summary>
        private string _shortLanguageName = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        private string _tempWavFilePath = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        private string _tempFlacFilePath = string.Empty;

        #endregion
    }
}
