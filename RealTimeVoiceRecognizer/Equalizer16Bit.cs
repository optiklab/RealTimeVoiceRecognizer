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
    /// 16-bit implementation of equalizer.
    /// </summary>
    internal sealed class Equalizer16Bit : EqualizerBase
    {
        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="settings">Current settings.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <param name="updateGraphicsMethod">Method for updating graphics.</param>
        public Equalizer16Bit(AudioSettings settings,
            double width,
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
            Debug.Assert(!Settings.isEightBitSample);

            byte[] dataBuffer = buffer.ToArray();
            int len = dataBuffer.Length / 2;

            Int16[] samples = new Int16[len];
            Buffer.BlockCopy(dataBuffer, 0, samples, 0, dataBuffer.Length);
            // eq
            _ProcessEq(ref samples);
            // pass in
            Marshal.Copy(samples, 0, data, len);

            // update graphics
            if (Settings.Channels == 1)
            {
                UpdateGraphicsMethod(DrawFrequencies(samples), null,
                    RenderTimeDomain(samples), null);
            }
            else
            {
                Int16[] left = new Int16[len / 2];
                Int16[] right = new Int16[len / 2];
                SplitArray(samples, ref left, ref right);

                UpdateGraphicsMethod(DrawFrequencies(left), DrawFrequencies(right),
                    RenderTimeDomain(left), RenderTimeDomain(right));
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Process dsp chain.
        /// </summary>
        private void _ProcessEq(ref Int16[] samples)
        {
            if (Settings.Channels == 1)
                _ProcessDSPMono(ref samples);
            else
                _ProcessDSPStereo(ref samples);
        }

        /// <summary>
        /// Process mono dsp chain.
        /// </summary>
        /// <param name="samples">Array of 16bit samples.</param>
        private void _ProcessDSPMono(ref Int16[] samples)
        {
            int len = samples.Length;
            int i = 0;

            unsafe
            {
                do
                {
                    Bands.BiQuad(ref samples[i], Biquad100Left);
                    Bands.BiQuad(ref samples[i], Biquad200Left);
                    Bands.BiQuad(ref samples[i], Biquad400Left);
                    Bands.BiQuad(ref samples[i], Biquad800Left);
                    Bands.BiQuad(ref samples[i], Biquad1600Left);
                    Bands.BiQuad(ref samples[i], Biquad3200Left);
                    Bands.BiQuad(ref samples[i], Biquad6400Left);

                    Bands.BiQuad(ref samples[i], BiquadHPF);

                    Bands.BiQuad(ref samples[i], BiquadLPF);

                    i++;
                } while (i < len);
            }
        }

        /// <summary>
        /// Process stereo dsp chain.
        /// </summary>
        /// <param name="samples">Array of 16bit samples.</param>
        private void _ProcessDSPStereo(ref Int16[] samples)
        {
            int len = samples.Length;
            int i = 0;

            unsafe
            {
                len -= 1;
                do
                {
                    // left channel
                    Bands.BiQuad(ref samples[i], Biquad100Left);
                    Bands.BiQuad(ref samples[i], Biquad200Left);
                    Bands.BiQuad(ref samples[i], Biquad400Left);
                    Bands.BiQuad(ref samples[i], Biquad800Left);
                    Bands.BiQuad(ref samples[i], Biquad1600Left);
                    Bands.BiQuad(ref samples[i], Biquad3200Left);
                    Bands.BiQuad(ref samples[i], Biquad6400Left);
                    // right channel
                    Bands.BiQuad(ref samples[i + 1], Biquad100Right);
                    Bands.BiQuad(ref samples[i + 1], Biquad200Right);
                    Bands.BiQuad(ref samples[i + 1], Biquad400Right);
                    Bands.BiQuad(ref samples[i + 1], Biquad800Right);
                    Bands.BiQuad(ref samples[i + 1], Biquad1600Right);
                    Bands.BiQuad(ref samples[i + 1], Biquad3200Right);
                    Bands.BiQuad(ref samples[i + 1], Biquad6400Right);

                    Bands.BiQuad(ref samples[i], BiquadHPF);
                    Bands.BiQuad(ref samples[i + 1], BiquadHPF);

                    Bands.BiQuad(ref samples[i], BiquadLPF);
                    Bands.BiQuad(ref samples[i + 1], BiquadLPF);

                    i += 2;
                } while (i < len);
            }
        }

        #endregion
    }
}
