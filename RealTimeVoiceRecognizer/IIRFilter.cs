/*****************************************************
************* Copyright OptikLab 2011 ****************
************* http://www.ayarkov.com  ****************
******************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace RealTimeVoiceRecognizer
{
    /// <summary>
    /// 
    /// </summary>
    internal unsafe class IIRFilter
    {
        #region Imported functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hHeap"></param>
        /// <param name="dwFlags"></param>
        /// <param name="dwBytes"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern BiquadCoefficients* HeapAlloc(IntPtr hHeap, uint dwFlags, uint dwBytes);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hHeap"></param>
        /// <param name="dwFlags"></param>
        /// <param name="lpMem"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool HeapFree(IntPtr hHeap, uint dwFlags, BiquadCoefficients* lpMem);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcessHeap();

        #endregion

        #region Memory

        /// <summary>Allocate heap memory</summary>
        /// <param name="size">size desired</param>
        /// <returns>memory address</returns>
        private BiquadCoefficients* Alloc(int size)
        {
            return HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, (uint)size);
        }

        /// <summary>Release heap memory</summary>
        /// <param name="pmem">memory address</param>
        private void Free(BiquadCoefficients* b)
        {
            HeapFree(GetProcessHeap(), 0, b);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="b"></param>
        public void FreeMem(BiquadCoefficients* b)
        {
            Free(b);
        }

        #endregion

        #region Biquad

        /// <summary>
        /// Calculates IIR.
        /// </summary>
        /// <param name="sample">sample rate</param>
        /// <param name="pBiquad">BiquadCoefficients pointer</param>
        public void BiQuad(ref Int16 sample, BiquadCoefficients* pBiquad)
        {
            // compute result
            double result = pBiquad->a0 * sample + pBiquad->a1 * pBiquad->x1 +
                pBiquad->a2 * pBiquad->x2 - pBiquad->a3 * pBiquad->y1 - pBiquad->a4 * pBiquad->y2;
            // shift x1 to x2, sample to x1
            pBiquad->x2 = pBiquad->x1;
            pBiquad->x1 = sample;
            // shift y1 to y2, result to y1
            pBiquad->y2 = pBiquad->y1;
            pBiquad->y1 = result;

            sample = (Int16)result;
        }

        /// <summary>
        /// Calculates IIR.
        /// </summary>
        /// <param name="sample">sample rate</param>
        /// <param name="pBiquad">BiquadCoefficients pointer</param>
        public void BiQuad(ref byte sample, BiquadCoefficients* pBiquad)
        {
            // compute result
            double result = pBiquad->a0 * sample + pBiquad->a1 * pBiquad->x1 +
                pBiquad->a2 * pBiquad->x2 - pBiquad->a3 * pBiquad->y1 - pBiquad->a4 * pBiquad->y2;
            // shift x1 to x2, sample to x1
            pBiquad->x2 = pBiquad->x1;
            pBiquad->x1 = sample;
            // shift y1 to y2, result to y1
            pBiquad->y2 = pBiquad->y1;
            pBiquad->y1 = result;

            sample = (byte)result;
        }

        /// <summary>Set up a BiQuad Filter</summary>
        /// <param name="type">filter type</param>
        /// <param name="dbGain">gain of filter</param>
        /// <param name="freq">center frequency</param>
        /// <param name="srate">sampling rate</param>
        /// <param name="bandwidth">bandwidth in octaves</param>
        /// <returns>BiquadCoefficients pointer</returns>
        public BiquadCoefficients* BiQuadFilter(FiltersType type, double dbGain, double freq,
            double srate, double bandwidth)
        {
            BiquadCoefficients* pBiquad;
            double a0, a1, a2, b0, b1, b2;

            pBiquad = Alloc(sizeof(BiquadCoefficients));
            if (pBiquad == null)
                return null;

            // setup variables
            double A = Math.Pow(10, dbGain / 40);
            double omega = 2 * Math.PI * freq / srate;
            double sinW0 = Math.Sin(omega);
            double cosW0 = Math.Cos(omega);
            double alpha = sinW0 * Math.Sinh(LN2 / 2 * bandwidth * omega / sinW0);
            double beta = Math.Sqrt(A + A);

            switch (type)
            {
                case FiltersType.LPF:
                    b0 = (1 - cosW0) / 2;
                    b1 = 1 - cosW0;
                    b2 = (1 - cosW0) / 2;
                    a0 = 1 + alpha;
                    a1 = -2 * cosW0;
                    a2 = 1 - alpha;
                    break;
                case FiltersType.PEQ:
                    b0 = 1 + (alpha * A);
                    b1 = -2 * cosW0;
                    b2 = 1 - (alpha * A);
                    a0 = 1 + (alpha / A);
                    a1 = -2 * cosW0;
                    a2 = 1 - (alpha / A);
                    break;
                case FiltersType.HSH:
                    b0 = A * ((A + 1) + (A - 1) * cosW0 + beta * sinW0);
                    b1 = -2 * A * ((A - 1) + (A + 1) * cosW0);
                    b2 = A * ((A + 1) + (A - 1) * cosW0 - beta * sinW0);
                    a0 = (A + 1) - (A - 1) * cosW0 + beta * sinW0;
                    a1 = 2 * ((A - 1) - (A + 1) * cosW0);
                    a2 = (A + 1) - (A - 1) * cosW0 - beta * sinW0;
                    break;
                default:
                    Free(pBiquad);
                    return null;
            }

            // precompute the coefficients
            pBiquad->a0 = b0 / a0;
            pBiquad->a1 = b1 / a0;
            pBiquad->a2 = b2 / a0;
            pBiquad->a3 = a1 / a0;
            pBiquad->a4 = a2 / a0;
            // zero initial samples
            pBiquad->x1 = pBiquad->x2 = 0;
            pBiquad->y1 = pBiquad->y2 = 0;

            return pBiquad;
        }

        #endregion

        #region Private fields

        /// <summary>
        /// Math.Log(2,2)
        /// </summary>
        private const double LN2 = 0.69314718055994530942;

        /// <summary>
        /// Heap zero memory.
        /// </summary>
        private const int HEAP_ZERO_MEMORY = 0x00000008;

        #endregion
    }
}
