/*****************************************************
************* Copyright OptikLab 2011 ****************
************* http://www.ayarkov.com  ****************
******************************************************/
using System;
using System.Threading;
using System.Runtime.InteropServices;

namespace RealTimeVoiceRecognizer
{
    /// <summary>
    /// Wave sound recorder, which do the main system work to catch sound.
    /// </summary>
    internal class WaveInRecorder : IDisposable
    {
        #region Constructor

        /// <summary>Initialze class and set defaults.</summary>
        /// <param name="device">Current device id.</param>
        /// <param name="doneProc">data callback</param>
        /// <param name="format">WAVEFORMATEX format description</param>
        public WaveInRecorder(int device, BufferDoneEventHandler doneProc, WaveFormatEx format)
        {
            _device = device;
            _eDoneProc = doneProc;
            _waveFormat = format;
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~WaveInRecorder()
        {
            Dispose();
        }

        /// <summary>
        /// Dispose and release resources
        /// </summary>
        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Imported functions

        /// <summary>
        /// Opens audio input device.
        /// </summary>
        /// <param name="phwi">Output: address of variable with hander of audio input device.
        /// Ignores if dwFlags is WAVE_FORMAT_QUERY</param>
        /// <param name="uDeviceID">Device identifier or descriptor of opened device.
        /// Can be used just WAVE_MAPPER to get first format appropriate device.</param>
        /// <param name="lpFormat">Address of WaveFormatEx structure.</param>
        /// <param name="dwCallback">Address of callback function, event descriptor,
        /// window descriptor or thread identifier. Any of these objects could handle recording process info.</param>
        /// <param name="dwInstance">Parameter of callback function dwCallback.</param>
        /// <param name="dwFlags">Mix of flags:
        /// CALLBACK_EVENT - event descriptor contained in dwCallback.
        /// CALLBACK_FUNCTION - callback function address contained in dwCallback.
        /// CALLBACK_NULL - callback not used. Default.
        /// CALLBACK_THREAD - thread identifier contained in dwCallback.
        /// CALLBACK_WINDOW - window descriptor contained in dwCallback.
        /// WAVE_FORMAT_DIRECT - Audio data covertion is not allowed.
        /// WAVE_FORMAT_QUERY - check if device could be opened.
        /// </param>
        /// <returns>Error in case if device is opened.
        /// MMSYSER: NOERROR, ALLOCATED, BADDEVICEID, NODRIVER, NOMEM
        /// WAVERR: BADFORMAT</returns>
        [DllImport("winmm.dll")]
        private static extern MMSYSERR waveInOpen(out IntPtr phwi, int uDeviceID, ref WaveFormatEx lpFormat, WaveDelegate dwCallback, int dwInstance, int dwFlags);

        /// <summary>
        /// Initiates the beginning of recording process
        /// if any data block is in memory (after waveInAddBuffer).
        /// </summary>
        /// <param name="hwi">Handler of opened audio in device, which you got after WaveInOpen.</param>
        /// <returns>MMSYSER: NOERROR, INVALIDHANDLE, NODRIVER, NOMEM</returns>
        [DllImport("winmm.dll")]
        private static extern MMSYSERR waveInStart(IntPtr hwi);

        /// <summary>
        /// Stops recording process.
        /// </summary>
        /// <param name="hwi">Handler of opened audio in device, which you got after WaveInOpen.</param>
        /// <returns>MMSYSER: NOERROR, INVALIDHANDLE, NODRIVER, NOMEM</returns>
        [DllImport("winmm.dll")]
        private static extern MMSYSERR waveInStop(IntPtr hwi);

        /// <summary>
        /// Finishes recording process. Returns all data blocks to the application and set
        /// current position to zero. If nothing to do - do nothing.
        /// </summary>
        /// <param name="hwi">Handler of opened audio in device, which you got after WaveInOpen.</param>
        /// <returns>MMSYSER: NOERROR, INVALIDHANDLE, NODRIVER, NOMEM</returns>
        [DllImport("winmm.dll")]
        private static extern MMSYSERR waveInReset(IntPtr hwi);

        /// <summary>
        /// Closes audio input device. Returns error if some data blocks are still
        /// in recording queue (that is mean you should call WaveInReset before).
        /// Call WaveInReset before this method always if you are not sure for the queue...
        /// </summary>
        /// <param name="hwi">Handler of opened audio in device, which you got after WaveInOpen.</param>
        /// <returns>MMSYSER: NOERROR, INVALIDHANDLE, NODRIVER, NOMEM
        ///          WAVERR: STILLPLAYING</returns>
        [DllImport("winmm.dll")]
        private static extern MMSYSERR waveInClose(IntPtr hwi);

        #endregion

        #region ThreadProc

        /// <summary>
        /// Main recorder cycle.
        /// </summary>
        private void _ThreadProc()
        {
            while (!_isFinished)
            {
                _Advance();
                if (_eDoneProc != null && !_isFinished)
                    _eDoneProc(_currentBuffer.Data, _currentBuffer.Size);
                _currentBuffer.Record();
            }
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Callback function for handling recording process information.
        /// </summary>
        /// <param name="hdrvr">Audio input device handler.</param>
        /// <param name="uMsg">WIM_CLOSE, WIM_DONE, WIM_OPEN</param>
        /// <param name="dwUser">Additional data for clients use.</param>
        /// <param name="wavhdr">Wave Header.</param>
        /// <param name="dwParam2">Additional parameter.</param>
        public delegate void WaveDelegate(IntPtr hdrvr, int uMsg, int dwUser, ref WaveHDR wavhdr, int dwParam2);

        #endregion

        #region Public Methods

        /// <summary>
        /// Begin recording.
        /// </summary>
        /// <returns>MMSYSERR.</returns>
        public MMSYSERR Record()
        {
            MMSYSERR mmr = waveInOpen(out _pWaveIn, _device, ref _waveFormat,
                _cBufferProc, 0, CALLBACK_FUNCTION_FLAG);  // system function

            if (mmr == MMSYSERR.NOERROR)
            {
                _AllocateBuffers(BUFFER_SIZE, BUFFERS_COUNT);

                for (uint i = 0; i < BUFFERS_COUNT; i++)
                {
                    _SelectNextBuffer();
                    _currentBuffer.Record();
                }

                waveInStart(_pWaveIn); // system function

                _thread = new Thread(new ThreadStart(_ThreadProc));
                _thread.Start();
            }

            return mmr;
        }

        /// <summary>Stop recording and reset.</summary>
        /// <returns>MMSYSERR.</returns>
        public MMSYSERR Stop()
        {
            MMSYSERR mmr = MMSYSERR.ERROR;
            try
            {
                _isFinished = true;

                if (_pWaveIn != IntPtr.Zero)
                    waveInReset(_pWaveIn); // system function

                if (_thread != null)
                    _thread.Abort();

                _eDoneProc = null;
                _FreeBuffers();

                if (_pWaveIn != IntPtr.Zero)
                    mmr = waveInClose(_pWaveIn); // system function
                else
                    mmr = MMSYSERR.NOERROR;
            }
            finally
            {
                _thread = null;
                _pWaveIn = IntPtr.Zero;
            }
            return mmr;
        }

        /// <summary>Pause recording.</summary>
        /// <returns>MMSYSERR.</returns>
        public MMSYSERR Pause()
        {
            _thread.Suspend();
            return waveInStop(_pWaveIn); // system function
        }

        /// <summary>Resujme recording.</summary>
        /// <returns>MMSYSERR.</returns>
        public MMSYSERR Resume()
        {
            _thread.Resume();
            return waveInStart(_pWaveIn); // system function
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Allocate internal buffers.
        /// </summary>
        /// <param name="bufferSize">Size of buffer.</param>
        /// <param name="bufferCount">Number of buffers to allocate.</param>
        private void _AllocateBuffers(uint bufferSize, uint bufferCount)
        {
            _FreeBuffers();
            if (bufferCount > 0)
            {
                _buffers = new WaveInBuffer(_pWaveIn, bufferSize);
                WaveInBuffer Prev = _buffers;
                try
                {
                    for (int i = 1; i < bufferCount; i++)
                    {
                        WaveInBuffer Buf = new WaveInBuffer(_pWaveIn, bufferSize);
                        Prev.NextBuffer = Buf;
                        Prev = Buf;
                    }
                }
                finally
                {
                    Prev.NextBuffer = _buffers;
                }
            }
        }

        /// <summary>
        /// Free the internal buffers.
        /// </summary>
        private void _FreeBuffers()
        {
            _currentBuffer = null;
            if (_buffers != null)
            {
                WaveInBuffer first = _buffers;
                _buffers = null;

                WaveInBuffer current = first;
                do
                {
                    WaveInBuffer Next = current.NextBuffer;
                    current.Dispose();
                    current = Next;
                } while (current != first);
            }
        }

        /// <summary>
        /// Advance to the next buffer.
        /// </summary>
        private void _Advance()
        {
            _SelectNextBuffer();

            _currentBuffer.WaitFor();
        }

        /// <summary>
        /// Select next internal buffer.
        /// </summary>
        private void _SelectNextBuffer()
        {
            _currentBuffer = _currentBuffer == null ? _buffers : _currentBuffer.NextBuffer;
        }

        #endregion

        #region Constants

        /// <summary>
        /// Callback function.
        /// </summary>
        private const int CALLBACK_FUNCTION_FLAG = 0x00030000;

        /// <summary>
        /// Buffer size.
        /// </summary>
        private const int BUFFER_SIZE = 4096;

        /// <summary>
        /// Count of buffers.
        /// </summary>
        private const int BUFFERS_COUNT = 4;

        #endregion

        #region Fields

        /// <summary>
        /// Is current recoring finished.
        /// </summary>
        private bool _isFinished;

        /// <summary>
        /// Current audio input device.
        /// </summary>
        private IntPtr _pWaveIn;

        /// <summary>
        /// Wave format.
        /// </summary>
        private WaveFormatEx _waveFormat;

        /// <summary>
        /// Buffers.
        /// </summary>
        private WaveInBuffer _buffers;

        /// <summary>
        /// Current buffer.
        /// </summary>
        private WaveInBuffer _currentBuffer;

        /// <summary>
        /// Current thread.
        /// </summary>
        private Thread _thread;

        /// <summary>
        /// Buffer is done event handler.
        /// </summary>
        private BufferDoneEventHandler _eDoneProc;

        /// <summary>
        /// Wave delegate to process system messages.
        /// </summary>
        private WaveDelegate _cBufferProc = new WaveDelegate(WaveInBuffer.WaveInProc);

        /// <summary>
        /// Recording device id.
        /// </summary>
        private int _device;

        #endregion
    }
}
