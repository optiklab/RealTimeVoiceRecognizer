/*****************************************************
************* Copyright OptikLab 2011 ****************
************* http://www.ayarkov.com  ****************
******************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace RealTimeVoiceRecognizer
{
    /// <summary>
    /// Buffer.
    /// </summary>
    internal class WaveInBuffer : IDisposable
    {
        #region Constructor

        /// <summary>Constructor.</summary>
        /// <param name="waveInHandle">Wave In Handle.</param>
        /// <param name="size">Size of buffer.</param>
        public WaveInBuffer(IntPtr waveInHandle, uint size)
        {
            _pWaveIn = waveInHandle;
            _headerHandle = GCHandle.Alloc(_waveHDR, GCHandleType.Pinned);
            _waveHDR.dwUser = (IntPtr)GCHandle.Alloc(this);
            _headerData = new byte[size];
            _headerDataHandle = GCHandle.Alloc(_headerData, GCHandleType.Pinned);
            _waveHDR.lpData = _headerDataHandle.AddrOfPinnedObject();
            _waveHDR.dwBufferLength = size;

            waveInPrepareHeader(_pWaveIn, ref _waveHDR, Marshal.SizeOf(_waveHDR));
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~WaveInBuffer()
        {
            Dispose();
        }

        /// <summary>
        /// Dispose current buffer.
        /// </summary>
        public void Dispose()
        {
            // If wave header was intialized, process disposing algorithm.
            if (_waveHDR.lpData != IntPtr.Zero)
            {
                waveInUnprepareHeader(_pWaveIn, ref _waveHDR, Marshal.SizeOf(_waveHDR));
                _headerHandle.Free();
                _waveHDR.lpData = IntPtr.Zero;
            }

            _recordEvent.Close();

            if (_headerDataHandle.IsAllocated)
                _headerDataHandle.Free();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Imported functions

        /// <summary>
        /// Prepares data block for sending to audio input device.
        /// Before calling this method WaveHDR block should be initializes by zero values
        /// and size and adress of block.
        /// </summary>
        /// <param name="hWaveIn">Handler of opened audio in device, which you got after WaveInOpen.</param>
        /// <param name="lpWaveInHdr">Address of filled block WaveHDR</param>
        /// <param name="uSize">Size of WaveHDR.</param>
        /// <returns>MMSYSER: NOERROR, INVALIDHANDLE, NODRIVER, NOMEM</returns>
        [DllImport("winmm.dll")]
        private static extern MMSYSERR waveInPrepareHeader(IntPtr hWaveIn, ref WaveHDR lpWaveInHdr, int uSize);

        /// <summary>Releases header of the playback block.</summary>
        /// <param name="hWaveIn">Handler of audio input device.</param>
        /// <param name="lpWaveInHdr">Address of filled WaveHDR.</param>
        /// <param name="uSize">Size of WaveHDR.</param>
        /// <returns>MMSYSER: NOERROR, INVALIDHANDLE, NODRIVER, NOMEM
        ///          WAVERR: STILLPLAYING</returns>
        [DllImport("winmm.dll")]
        private static extern MMSYSERR waveInUnprepareHeader(IntPtr hWaveIn, ref WaveHDR lpWaveInHdr, int uSize);

        /// <summary>Sends prepared (see waveInPrepareHeader) data block to recording queue.</summary>
        /// <param name="hwi">Handler of opened audio in device, which you got after WaveInOpen.</param>
        /// <param name="pwh">Address of filled WaveHDR.</param>
        /// <param name="cbwh">Size of WaveHDR.</param>
        /// <returns>MMSYSER: NOERROR, INVALIDHANDLE, NODRIVER, NOMEM, UNPREPARED</returns>
        [DllImport("winmm.dll")]
        private static extern MMSYSERR waveInAddBuffer(IntPtr hwi, ref WaveHDR pwh, int cbwh);

        #endregion

        #region Public properties

        /// <summary>
        /// Next wave in buffer.
        /// </summary>
        public WaveInBuffer NextBuffer
        {
            get;
            set;
        }

        /// <summary>
        /// Size of buffer.
        /// </summary>
        public uint Size
        {
            get { return _waveHDR.dwBufferLength; }
        }

        /// <summary>
        /// Buffer data.
        /// </summary>
        public IntPtr Data
        {
            get { return _waveHDR.lpData; }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Start recroding into buffer.
        /// </summary>
        public void Record()
        {
            lock (this)
            {
                // Reset event before recording to block other threads.
                _recordEvent.Reset();

                if (waveInAddBuffer(_pWaveIn, ref _waveHDR, Marshal.SizeOf(_waveHDR)) == MMSYSERR.NOERROR)
                {
                    _isRecording = true;
                }
                else
                {
                    _isRecording = false;

                    throw new Exception("Can't start recording.");
                }
            }
        }

        /// <summary>
        /// Wait for another thread.
        /// </summary>
        public void WaitFor()
        {
            if (_isRecording)
                _isRecording = _recordEvent.WaitOne();
            else
                Thread.Sleep(0);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Proceed other threads when recording is completed.
        /// </summary>
        private void _OnCompleted()
        {
            _recordEvent.Set();

            _isRecording = false;
        }

        #endregion

        #region Callback function

        /// <summary>Callback function for handling recording process information.</summary>
        /// <param name="hdrvr">Audio input device handler.</param>
        /// <param name="uMsg">WIM_CLOSE, WIM_DONE, WIM_OPEN</param>
        /// <param name="dwUser">Additional data for clients use.</param>
        /// <param name="wavhdr">Wave Header.</param>
        /// <param name="dwParam2">Additional parameter.</param>
        internal static void WaveInProc(IntPtr hdrvr, int uMsg, int dwUser, ref WaveHDR wavhdr, int dwParam2)
        {
            if (uMsg == MM_WIM_DATA)
            {
                try
                {
                    GCHandle h = (GCHandle)wavhdr.dwUser;
                    WaveInBuffer buf = (WaveInBuffer)h.Target;
                    buf._OnCompleted();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        /// <summary>Recording of data block finished.</summary>
        private const int MM_WIM_DATA = 0x3C0;

        #endregion

        #region Fields

        /// <summary>
        /// Is recording in progress.
        /// </summary>
        private bool _isRecording;

        /// <summary>
        /// Handler of opened audio in device, which you got after WaveInOpen.
        /// </summary>
        private IntPtr _pWaveIn;

        /// <summary
        /// >Header data and handle for it (allows to allocate and free header data).
        /// </summary>
        private byte[] _headerData;
        private GCHandle _headerDataHandle;

        /// <summary>
        /// Event for synchronising between recording threads.
        /// </summary>
        private AutoResetEvent _recordEvent = new AutoResetEvent(false);

        /// <summary>
        /// Wave header and handle for it (allows to allocate and free wave header).
        /// </summary>
        private WaveHDR _waveHDR;
        private GCHandle _headerHandle;

        #endregion
    }
}
