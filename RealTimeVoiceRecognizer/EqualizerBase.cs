/*****************************************************
************* Copyright OptikLab 2011 ****************
************* http://www.ayarkov.com  ****************
******************************************************/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace RealTimeVoiceRecognizer
{
    /// <summary>
    /// Base class for Equalizers.
    /// </summary>
    internal class EqualizerBase : IDisposable
    {
        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="settings">Audio settings.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <param name="updateGraphicsMethod">Method for updating graphics.</param>
        public EqualizerBase(AudioSettings settings, double width,
            double height, Action<IList<Rect>, IList<Rect>, IList<Point>, IList<Point>> updateGraphicsMethod)
        {
            _dBands = new double[21];
            Bands = new IIRFilter();
            Settings = settings;

            _Load();
            _RecalculateBiquads();

            DrawingHeight = height;
            DrawingWidth = width;
            UpdateGraphicsMethod = updateGraphicsMethod;
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~EqualizerBase()
        {
            Dispose();
        }


        /// <summary>
        /// Dispose and release resources
        /// </summary>
        public void Dispose()
        {
            _Unload();
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Is turned On.
        /// </summary>
        public bool IsTurnedOn { get; set; }

        /// <summary>
        /// Is High Pass Filter On.
        /// </summary>
        public bool IsHighPassOn { get; set; }

        /// <summary>
        /// Is Low Pass Filter On.
        /// </summary>
        public bool IsLowPassOn { get; set; }

        #endregion

        #region Public methods

        /// <summary>
        /// Process current sound buffer.
        /// </summary>
        /// <param name="buffer">Sound buffer.</param>
        /// <param name="data">Data.</param>
        public virtual void ProcessSound(List<byte> buffer, IntPtr data)//(ref byte[] buffer, IntPtr data)
        {
            // Do nothing.
        }

        /// <summary>
        /// Updates drawing size.
        /// </summary>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        public void UpdateDrawingSize(double width, double height)
        {
            DrawingWidth = width;
            DrawingHeight = height;
        }

        #endregion

        #region Protected helpers methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        protected void SplitArray<T>(T[] data, ref T[] left, ref T[] right)
        {
            int len = data.Length - 2;
            int h = 0;
            for (int i = 0; i < len; i += 2)
            {
                left[h] = data[i];
                right[h] = data[i + 2];
                h++;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="intSamples"></param>
        protected List<Rect> DrawFrequencies(Int16[] intSamples)
        {
            _FFT.NumberOfSamples(FFT_SAMPLES);
            _FFT.WithTimeWindow(1);

            double[] real = new double[intSamples.Length];
            double complex = 0;

            // load samples
            for (int i = 0; i < FFT_SAMPLES; i++)
                _FFT.RealIn(i, intSamples[i]);

            // normalize values and cut them at FFT_MAXAMPLITUDE
            for (int i = 0; i < (FFT_SAMPLES / 2) + 1; i++)
            {
                complex = _FFT.ComplexOut(i);
                // normalise
                real[i] = complex / (FFT_SAMPLES / 4) / 32767;

                // cut the output to FFT_MAXAMPLITUDE, so
                // the spectrum doesn't get too small
                if (real[i] > FFT_MAXAMPLITUDE)
                    real[i] = FFT_MAXAMPLITUDE;

                real[i] /= FFT_MAXAMPLITUDE;
            }

            int count = FFT_STARTINDEX;
            double band = 0;
            for (int i = 0; i < FFT_BANDS - 1; i++)
            {
                // average for the current band
                for (int j = count; j < count + FFT_BANDWIDTH + 1; j++)
                    band += real[j];
                // boost frequencies in the middle with a hanning window,
                // because they have less power then the low ones
                band = (band * (_Hanning(i + 3, FFT_BANDS + 3) + 1)) / FFT_BANDWIDTH;

                if (Settings.isEightBitSample)
                    _dBands[i] = band / 8;
                else
                    _dBands[i] = band;

                if (_dBands[i] > 1)
                    _dBands[i] = 1;
                // skip some bands
                count += FFT_BANDSPACE;
            }

            double width = DrawingWidth;
            double height = DrawingHeight;

            double barwidth = width / 21;

            List<Rect> rectangles = new List<Rect>();

            for (int i = 0; i < _dBands.Length; i++)
            {
                double X = (i * barwidth) + (i + 1) * DRW_BARSPACE;
                double Y = height - (height * _dBands[i]);
                double barheight = height - (Y + DRW_BARYOFF);
                if (barheight + Y > height)
                {
                    barheight = height;
                    Y = 1;
                }

                if (barheight < 0)
                    barheight = 0;

                rectangles.Add(new Rect()
                    {
                        X = X,
                        Y = Y,
                        Width = barwidth,
                        Height = barheight
                    });
            }

            return rectangles;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        protected List<Point> RenderTimeDomain<T>(T[] data)
        {
            double width = DrawingWidth;
            double height = DrawingHeight;

            double center = height / 2;

            // Draw channels
            double scale = 0.5 * height / 32000;// Settings.BitRate;

            if (Settings.isEightBitSample)
                scale *= 8;

            int xPrev = 0, yPrev = 0;
            double offset = 0;
            int x = 0, y = 0;
            List<Point> points = new List<Point>();

            for (x = 0; x < width; x++)
            {
                int index = Convert.ToInt32(data.Length / width * x);
                offset = Convert.ToDouble(data[index]);

                if (Settings.isEightBitSample)
                {
                    if (offset == 0)
                        offset = 0;
                    else if (offset > 128)
                        offset -= 128;
                    else if (offset < 128)
                        offset = -(128 - offset);
                    else
                        offset = 0;
                }

                y = (int)(center + (offset * scale));

                // Limit top and bottom borders.
                int h = (int)height;
                if (Math.Abs(y) > h)
                {
                    if (y >= 0)
                        y = h;
                    else
                        y = -h;
                }

                if (Math.Abs(yPrev) > h)
                {
                    if (yPrev >= 0)
                        yPrev = h;
                    else
                        yPrev = -h;
                }

                // Save points.
                if (x == 0)
                {
                    xPrev = 0;
                    yPrev = y;
                }
                else
                {
                    points.Add(new Point(x, y));

                    xPrev = x;
                    yPrev = y;
                }
            }

            return points;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Load equalizer bands.
        /// </summary>
        private unsafe void _Load()
        {
            Biquad100Left = Bands.BiQuadFilter(FiltersType.PEQ, 5, 100, Settings.SamplesPerSecond, 1);
            Biquad200Left = Bands.BiQuadFilter(FiltersType.PEQ, 5, 200, Settings.SamplesPerSecond, 1);
            Biquad400Left = Bands.BiQuadFilter(FiltersType.PEQ, 5, 400, Settings.SamplesPerSecond, 1);
            Biquad800Left = Bands.BiQuadFilter(FiltersType.PEQ, 5, 800, Settings.SamplesPerSecond, 1);
            Biquad1600Left = Bands.BiQuadFilter(FiltersType.PEQ, 5, 1600, Settings.SamplesPerSecond, 1);
            Biquad3200Left = Bands.BiQuadFilter(FiltersType.PEQ, 5, 3200, Settings.SamplesPerSecond, 1);
            Biquad6400Left = Bands.BiQuadFilter(FiltersType.PEQ, 5, 6400, Settings.SamplesPerSecond, 1);
            // right channel
            Biquad100Right = Bands.BiQuadFilter(FiltersType.PEQ, 5, 100, Settings.SamplesPerSecond, 1);
            Biquad200Right = Bands.BiQuadFilter(FiltersType.PEQ, 5, 200, Settings.SamplesPerSecond, 1);
            Biquad400Right = Bands.BiQuadFilter(FiltersType.PEQ, 5, 400, Settings.SamplesPerSecond, 1);
            Biquad800Right = Bands.BiQuadFilter(FiltersType.PEQ, 5, 800, Settings.SamplesPerSecond, 1);
            Biquad1600Right = Bands.BiQuadFilter(FiltersType.PEQ, 5, 1600, Settings.SamplesPerSecond, 1);
            Biquad3200Right = Bands.BiQuadFilter(FiltersType.PEQ, 5, 3200, Settings.SamplesPerSecond, 1);
            Biquad6400Right = Bands.BiQuadFilter(FiltersType.PEQ, 5, 6400, Settings.SamplesPerSecond, 1);
            // filters
            // cut high harmonic
            BiquadLPF = Bands.BiQuadFilter(FiltersType.LPF, 8000, 10000, Settings.SamplesPerSecond, 1);
            // boost mid harmonic
            BiquadHPF = Bands.BiQuadFilter(FiltersType.HSH, 4, 4000, Settings.SamplesPerSecond, 1);
        }

        /// <summary>
        /// Equalizer cleanup.
        /// </summary>
        private unsafe void _Unload()
        {
            // filters
            if (BiquadLPF != null)
                Bands.FreeMem(BiquadLPF);
            if (BiquadHPF != null)
                Bands.FreeMem(BiquadHPF);
            // left
            if (Biquad100Left != null)
                Bands.FreeMem(Biquad100Left);
            if (Biquad200Left != null)
                Bands.FreeMem(Biquad200Left);
            if (Biquad400Left != null)
                Bands.FreeMem(Biquad400Left);
            if (Biquad800Left != null)
                Bands.FreeMem(Biquad800Left);
            if (Biquad1600Left != null)
                Bands.FreeMem(Biquad1600Left);
            if (Biquad3200Left != null)
                Bands.FreeMem(Biquad3200Left);
            if (Biquad6400Left != null)
                Bands.FreeMem(Biquad6400Left);
            // right
            if (Biquad100Right != null)
                Bands.FreeMem(Biquad100Right);
            if (Biquad200Right != null)
                Bands.FreeMem(Biquad200Right);
            if (Biquad400Right != null)
                Bands.FreeMem(Biquad400Right);
            if (Biquad800Right != null)
                Bands.FreeMem(Biquad800Right);
            if (Biquad1600Right != null)
                Bands.FreeMem(Biquad1600Right);
            if (Biquad3200Right != null)
                Bands.FreeMem(Biquad3200Right);
            if (Biquad6400Right != null)
                Bands.FreeMem(Biquad6400Right);

            //_bandsEq.Close();
        }

        /// <summary>
        /// Reload equalizer bands.
        /// </summary>
        private unsafe void _RecalculateBiquads()
        {
            // left channel
            _RecalculateBiquadBand(ref Biquad100Left, 5, 100);
            _RecalculateBiquadBand(ref Biquad200Left, 5, 200);
            _RecalculateBiquadBand(ref Biquad400Left, 5, 400);
            _RecalculateBiquadBand(ref Biquad800Left, 5, 800);
            _RecalculateBiquadBand(ref Biquad1600Left, 5, 1600);
            _RecalculateBiquadBand(ref Biquad3200Left, 5, 3200);
            _RecalculateBiquadBand(ref Biquad6400Left, 5, 6400);
            // right channel
            _RecalculateBiquadBand(ref Biquad100Right, 5, 100);
            _RecalculateBiquadBand(ref Biquad200Right, 5, 200);
            _RecalculateBiquadBand(ref Biquad400Right, 5, 400);
            _RecalculateBiquadBand(ref Biquad800Right, 5, 800);
            _RecalculateBiquadBand(ref Biquad1600Right, 5, 1600);
            _RecalculateBiquadBand(ref Biquad3200Right, 5, 3200);
            _RecalculateBiquadBand(ref Biquad6400Right, 5, 6400);

            _RecalculateFilters();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        private double _Hanning(double x, int len)
        {
            return 0.5 * (1 - Math.Cos((2 * Math.PI * x) / len));
        }

        /// <summary>
        /// Calculate amplitude change in BiquadCoefficients.
        /// </summary>
        private unsafe void _RecalculateBiquadBand(ref BiquadCoefficients* b, double dbGain, double frequency)
        {
            // reduce the gain thresholds on energetic signals to reduce distortion..
            if (!Settings.isEightBitSample)
                dbGain *= .1f;

            BiquadCoefficients* tmp = Bands.BiQuadFilter(FiltersType.PEQ, dbGain, frequency, Settings.SamplesPerSecond, 1);
            b->a0 = tmp->a0;
            b->a1 = tmp->a1;
            b->a2 = tmp->a2;
            b->a3 = tmp->a3;
            b->a4 = tmp->a4;
            Bands.FreeMem(tmp);
            tmp = null;
        }

        /// <summary>
        /// Reload BiquadCoefficients filters.
        /// </summary>
        private unsafe void _RecalculateFilters()
        {
            if (BiquadLPF != null)
            {
                Bands.FreeMem(BiquadLPF);
                BiquadLPF = null;
            }

            if (BiquadHPF != null)
            {
                Bands.FreeMem(BiquadHPF);
                BiquadHPF = null;
            }

            // cut high harmonic
            BiquadLPF = Bands.BiQuadFilter(FiltersType.LPF, 8000, 10000, Settings.SamplesPerSecond, 1);
            // boost mid harmonic

            BiquadHPF = Bands.BiQuadFilter(FiltersType.HSH, 4, 4000, Settings.SamplesPerSecond, 1);
        }

        #endregion

        #region Private constants

        private const double FFT_MAXAMPLITUDE = 0.2;

        private const int FFT_BANDS = 22;
        private const int FFT_BANDSPACE = 1;
        private const int FFT_BANDWIDTH = 3;
        private const int FFT_STARTINDEX = 1;
        private const int FFT_SAMPLES = 512;

        private const int DRW_BARYOFF = 2;
        private const int DRW_BARSPACE = 1;

        #endregion

        #region Protected fields

        /// <summary>Audio settings.</summary>
        protected AudioSettings Settings;

        /// <summary>Bands.</summary>
        protected IIRFilter Bands;

        /// <summary>Left channel frequences filter.</summary>
        protected unsafe BiquadCoefficients* Biquad100Left;
        protected unsafe BiquadCoefficients* Biquad200Left;
        protected unsafe BiquadCoefficients* Biquad400Left;
        protected unsafe BiquadCoefficients* Biquad800Left;
        protected unsafe BiquadCoefficients* Biquad1600Left;
        protected unsafe BiquadCoefficients* Biquad3200Left;
        protected unsafe BiquadCoefficients* Biquad6400Left;

        /// <summary>Right channel frequences filter.</summary>
        protected unsafe BiquadCoefficients* Biquad100Right;
        protected unsafe BiquadCoefficients* Biquad200Right;
        protected unsafe BiquadCoefficients* Biquad400Right;
        protected unsafe BiquadCoefficients* Biquad800Right;
        protected unsafe BiquadCoefficients* Biquad1600Right;
        protected unsafe BiquadCoefficients* Biquad3200Right;
        protected unsafe BiquadCoefficients* Biquad6400Right;

        /// <summary>LPF.</summary>
        protected unsafe BiquadCoefficients* BiquadLPF;

        /// <summary>HPF.</summary>
        protected unsafe BiquadCoefficients* BiquadHPF;

        // Drawing size.
        protected double DrawingWidth;
        protected double DrawingHeight;

        /// <summary>
        /// Method for updating graphics.
        /// </summary>
        protected Action<IList<Rect>, IList<Rect>, IList<Point>, IList<Point>> UpdateGraphicsMethod;

        #endregion

        #region Private fields

        /// <summary>
        /// Fast fourier transformation.
        /// </summary>
        private Fourier _FFT = new Fourier();

        /// <summary>
        /// Bands.
        /// </summary>
        private double[] _dBands;

        #endregion
    }
}
