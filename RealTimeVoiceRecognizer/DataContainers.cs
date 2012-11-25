/*****************************************************
************* Copyright OptikLab 2011 ****************
************* http://www.ayarkov.com  ****************
******************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace RealTimeVoiceRecognizer
{
    /// <summary>
    /// Biquad filter data class.
    /// </summary>
    internal struct BiquadCoefficients
    {
        /// <summary>a0.</summary>
        public double a0;
        /// <summary>a1.</summary>
        public double a1;
        /// <summary>a2.</summary>
        public double a2;
        /// <summary>a3.</summary>
        public double a3;
        /// <summary>a4.</summary>
        public double a4;
        /// <summary>x1.</summary>
        public double x1;
        /// <summary>x2.</summary>
        public double x2;
        /// <summary>y1.</summary>
        public double y1;
        /// <summary>y2.</summary>
        public double y2;
    }

    /// <summary>
    /// WaveFormatEx structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
    internal struct WaveFormatEx
    {
        /// <summary>
        /// Audio type (see mmreg.h)
        /// </summary>
        public ushort wFormatTag;
        /// <summary>
        /// Number of channels (1 mono, 2 stereo).
        /// </summary>
        public ushort nChannels;
        /// <summary>
        ///Discrete frequency (for WAVE_FORMAT_PCM is 8000Hz, 11025Hz, 22050Hz, 44100Hz).
        /// </summary>
        public uint nSamplesPerSec;
        /// <summary>
        /// Recording speed ( = nSamplesPerSec * nBloackAlign).
        /// </summary>
        public uint nAvgBytesPerSec;
        /// <summary>
        /// Size of minimum unit of measurement (for WAVE_FORMAT_PCM is wBitsPerSample / 8 * nChannels).
        /// </summary>
        public ushort nBlockAlign;
        /// <summary>
        /// Number of bits in one signal block ((for WAVE_FORMAT_PCM is 8 or 16).
        /// </summary>
        public ushort wBitsPerSample;
        /// <summary>
        /// Size of additional information, which lies right after WaveFormatEx.
        /// If not needed, set to NULL.
        /// </summary>
        public ushort cbSize;
    }

    /// <summary>
    /// Identifies header of recording data block.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct WaveHDR
    {
        /// <summary> LPSTR.
        /// Pointer to locked data buffer.
        /// </summary>
        public IntPtr lpData;
        /// <summary>DWORD.
        /// Length of data buffer.
        /// </summary>
        public uint dwBufferLength;
        /// <summary>DWORD.
        /// Count of recorded data. Used for input only.
        /// Always less or equal to dwBufferLength.
        /// </summary>
        public uint dwBytesRecorded;
        /// <summary>DWORD.
        /// Additional data for client's use.
        /// </summary>
        public IntPtr dwUser;
        /// <summary>DWORD.
        /// Mixes of flags below means the state of data block:
        /// WHDR_DONE - driver informs about finishing recording process.
        /// WHDR_PREPARED - system informs that buffer prepared with waveInPrepareHeader.
        /// </summary>
        public uint dwFlags;
        /// <summary>DWORD.
        /// Loop control counter. Not used for input.
        /// </summary>
        public uint dwLoops;
        /// <summary>wavehdr_tag
        /// Reserved for driver.
        /// </summary>
        public IntPtr lpNext;
        /// <summary>DWORD.
        /// Reserved for driver.
        /// </summary>
        public uint reserved;
    }

    /// <summary>
    /// Structure is keeping info about characteristics of audio input device.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct WaveInCaps
    {
        /// <summary>WORD.
        /// Unique identifier of device manufacturer.
        /// </summary>
        public short wMid;
        /// <summary>WORD.
        /// Unique identifier of device.
        /// </summary>
        public short wPid;
        /// <summary>MMVERSION.
        /// Version of device driver.
        /// </summary>
        public int vDriverVersion;

        /// <summary>DWORD.
        /// List of common supported sound formats. Could be mixed from flags below:
        /// WAVE_FORMAT_1M08 // 11025Hz, mono, 8 bit
        /// WAVE_FORMAT_1M16 // 11025Hz, mono, 16 bit
        /// WAVE_FORMAT_1S08 // 11025Hz, stereo, 8 bit
        /// WAVE_FORMAT_1S16 // 11025Hz, stereo, 16 bit
        /// WAVE_FORMAT_2M08 // 22050Hz, mono, 8 bit
        /// WAVE_FORMAT_2M16 // 22050Hz, mono, 16 bit
        /// WAVE_FORMAT_2S08 // 22050Hz, stereo, 8 bit
        /// WAVE_FORMAT_2S16 // 22050Hz, stereo, 16 bit
        /// WAVE_FORMAT_4M08 // 44100Hz, mono, 8 bit
        /// WAVE_FORMAT_4M16 // 44100Hz, mono, 16 bit
        /// WAVE_FORMAT_4S08 // 44100Hz, stereo, 8 bit
        /// WAVE_FORMAT_4S16 // 44100Hz, stereo, 16 bit
        /// </summary>
        public uint dwFormats;

        /// <summary>WORD.
        /// Number of supported channels: 1 (mono), 2 (stereo), etc.
        /// </summary>
        public short wChannels;

        /// <summary>WORD.
        /// Not used. Equal to 0.
        /// </summary>
        public short wReserved;

        /// <summary>CHAR.
        /// Name of device driver as string with zero-end.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public char[] szPname;
    }

    /// <summary>
    /// Audio settings.
    /// </summary>
    public sealed class AudioSettings
    {
        #region Public properties

        /// <summary>
        /// Number of channels.
        /// </summary>
        public ushort Channels { get; set; }

        /// <summary>
        /// Is 8bit (if true) or 16bit (if false).
        /// </summary>
        public bool isEightBitSample { get; set; }

        /// <summary>
        /// Samples per second rate.
        /// </summary>
        public int SamplesPerSecond { get; set; }

        /// <summary>
        /// Bit rate.
        /// </summary>
        public int BitRate
        {
            get
            {
                if (isEightBitSample)
                    return SAMPLE_RATE_8BIT;
                else
                    return SAMPLE_RATE_16BIT;
            }
        }

        ///// <summary>
        ///// 
        ///// </summary>
        //public uint AvgBytesPerSecond { get; set; }

        /// <summary>
        /// Audio input device index.
        /// </summary>
        public int AudioInputDevice { get; set; }

        #endregion

        #region Private constants

        /// <summary>
        /// 8 bit rate.
        /// </summary>
        private const int SAMPLE_RATE_8BIT = 256;

        /// <summary>
        /// 16 bit rate.
        /// </summary>
        private const int SAMPLE_RATE_16BIT = 8192;

        #endregion
    }

    /// <summary>
    /// Sample consists of a real and an imaginary value in gaussian complex plane.
    /// </summary>
    internal struct Sample
    {
        /// <summary>
        /// Real.
        /// </summary>
        public double Real;

        /// <summary>
        /// Image.
        /// </summary>
        public double Imag;
    }
}
