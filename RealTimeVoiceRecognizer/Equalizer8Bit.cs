/*****************************************************
************* Copyright OptikLab 2011 ****************
************* http://www.ayarkov.com  ****************
******************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Shapes;

namespace RealTimeVoiceRecognizer
{
    /// <summary>
    /// 8-bit implementation of equalizer.
    /// </summary>
    internal sealed class Equalizer8Bit : EqualizerBase
    {
        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="settings">Current settings.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <param name="updateGraphicsMethod">Method for updating graphics.</param>
        public Equalizer8Bit(AudioSettings settings, double width,
            double height,
            Action<IList<Rect>, IList<Rect>, IList<Point>, IList<Point>> updateGraphicsMethod)
            : base(settings, width, height, updateGraphicsMethod)
        { }

        #endregion

        #region Public overried methods

        /// <summary>
        /// Process current sound buffer.
        /// </summary>
        /// <param name="buffer">Sound buffer.</param>
        /// <param name="data">Data.</param>
        public override void ProcessSound(List<byte> buffer, IntPtr data)
        {
            Debug.Assert(Settings.isEightBitSample);

            byte[] dataBuffer = buffer.ToArray();
            int len = dataBuffer.Length;

            // eq
            _ProcessEq(ref dataBuffer);
            // pass in
            Marshal.Copy(dataBuffer, 0, data, len);

            // update graphics
            if (Settings.Channels == 1)
            {
                // Process mono time & frequency domains
                RenderTimeDomain(dataBuffer);

                Int16[] left = new Int16[len / 2];
                Buffer.BlockCopy(dataBuffer, 0, left, 0, len);
                DrawFrequencies(left);

                UpdateGraphicsMethod(DrawFrequencies(left), null,
                    RenderTimeDomain(dataBuffer), null);
            }
            else
            {
                // Process stereo time & frequency domains
                byte[] left = new byte[len / 2];
                byte[] right = new byte[len / 2];
                SplitArray(dataBuffer, ref left, ref right);

                Int16[] leftBt = new Int16[left.Length / 2];
                Int16[] rightBt = new Int16[right.Length / 2];

                Buffer.BlockCopy(leftBt, 0, leftBt, 0, left.Length);
                Buffer.BlockCopy(rightBt, 0, rightBt, 0, right.Length);

                UpdateGraphicsMethod(DrawFrequencies(leftBt), DrawFrequencies(rightBt),
                    RenderTimeDomain(left), RenderTimeDomain(right));
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Process dsp chain.
        /// </summary>
        /// <param name="buffer">Samples array.</param>
        private void _ProcessEq(ref byte[] buffer)
        {
            if (Settings.Channels == 1)
                _ProcessDSPMono(ref buffer);
            else
                _ProcessDSPStereo(ref buffer);
        }

        /// <summary>
        /// Process mono dsp chain.
        /// </summary>
        /// <param name="samples">Array of 8bit samples.</param>
        private void _ProcessDSPMono(ref byte[] buffer)
        {
            int len = buffer.Length;
            int i = 0;

            unsafe
            {
                do
                {
                    Bands.BiQuad(ref buffer[i], Biquad100Left);
                    Bands.BiQuad(ref buffer[i], Biquad200Left);
                    Bands.BiQuad(ref buffer[i], Biquad400Left);
                    Bands.BiQuad(ref buffer[i], Biquad800Left);
                    Bands.BiQuad(ref buffer[i], Biquad1600Left);
                    Bands.BiQuad(ref buffer[i], Biquad3200Left);
                    Bands.BiQuad(ref buffer[i], Biquad6400Left);

                    Bands.BiQuad(ref buffer[i], BiquadHPF);

                    Bands.BiQuad(ref buffer[i], BiquadLPF);

                    i++;
                } while (i < len);
            }
        }

        /// <summary>
        /// Process mono dsp chain.
        /// </summary>
        /// <param name="samples">Array of 8bit samples.</param>
        private void _ProcessDSPStereo(ref byte[] buffer)
        {
            int len = buffer.Length;
            int i = 0;

            unsafe
            {
                len -= 1;
                do
                {
                    // left channel
                    Bands.BiQuad(ref buffer[i], Biquad100Left);
                    Bands.BiQuad(ref buffer[i], Biquad200Left);
                    Bands.BiQuad(ref buffer[i], Biquad400Left);
                    Bands.BiQuad(ref buffer[i], Biquad800Left);
                    Bands.BiQuad(ref buffer[i], Biquad1600Left);
                    Bands.BiQuad(ref buffer[i], Biquad3200Left);
                    Bands.BiQuad(ref buffer[i], Biquad6400Left);
                    // right channel
                    Bands.BiQuad(ref buffer[i + 1], Biquad100Right);
                    Bands.BiQuad(ref buffer[i + 1], Biquad200Right);
                    Bands.BiQuad(ref buffer[i + 1], Biquad400Right);
                    Bands.BiQuad(ref buffer[i + 1], Biquad800Right);
                    Bands.BiQuad(ref buffer[i + 1], Biquad1600Right);
                    Bands.BiQuad(ref buffer[i + 1], Biquad3200Right);
                    Bands.BiQuad(ref buffer[i + 1], Biquad6400Right);

                    Bands.BiQuad(ref buffer[i], BiquadHPF);
                    Bands.BiQuad(ref buffer[i + 1], BiquadHPF);

                    Bands.BiQuad(ref buffer[i], BiquadLPF);
                    Bands.BiQuad(ref buffer[i + 1], BiquadLPF);

                    i += 2;
                } while (i < len);
            }
        }

        #endregion
    }
}
