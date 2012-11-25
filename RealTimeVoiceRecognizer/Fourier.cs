/*****************************************************
************* Copyright OptikLab 2011 ****************
************* http://www.ayarkov.com  ****************
******************************************************/
using System;
using System.Runtime.InteropServices;

namespace RealTimeVoiceRecognizer
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed unsafe class Fourier : IDisposable
    {
        #region Constructor

        /// <summary>
        /// </summary>
        public Fourier()
        {
            _smpS = Alloc(sizeof(Sample));
            _smpT = Alloc(sizeof(Sample));
            _smpU = Alloc(sizeof(Sample));
            // set defaults
            _bUnknownSize = true;
            _smpValues = new Sample[0];
            _dPi = 4 * Math.Atan(1);
            _dTwoPi = _dPi + _dPi;
            _bProcess = true;
        }

        /// <summary></summary>
        ~Fourier()
        {
            Dispose();
        }

        /// <summary></summary>
        public void Dispose()
        {
            try
            {
                if (_smpS != null)
                    Free(_smpS);
                if (_smpT != null)
                    Free(_smpT);
                if (_smpU != null)
                    Free(_smpU);
            }
            catch { }
        }
        #endregion

        #region API

        /// <summary></summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern Sample* HeapAlloc(IntPtr hHeap, uint dwFlags, uint dwBytes);

        /// <summary></summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool HeapFree(IntPtr hHeap, uint dwFlags, Sample* lpMem);

        /// <summary></summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcessHeap();

        /// <summary></summary>
        private Sample* Alloc(int size)
        {
            return HeapAlloc(GetProcessHeap(), HEAP_ZERO_MEMORY, (uint)size);
        }

        /// <summary></summary>
        private void Free(Sample* pmem)
        {
            HeapFree(GetProcessHeap(), 0, pmem);
        }

        #endregion

        #region Public Methods

        /// <summary></summary>
        public double ComplexOut(int index)
        {
            Sample rt = GetIt(index);
            return Math.Sqrt(rt.Real * rt.Real + rt.Imag * rt.Imag);
        }

        /// <summary></summary>
        public void ImagIn(int index, double value)
        {
            if (!(_bUnknownSize || index > _smpValues.Length))
            {
                _smpValues[Mirror(index, _iCount)].Imag = value;
                _bProcess = true;
            }
        }

        /// <summary></summary>
        public double ImagOut(int index)
        {
            return GetIt(index).Imag;
        }

        /// <summary></summary>
        public void NumberOfSamples(int count)
        {
            if (count > 0 && (count - 1 & count) == 0)
            {
                _smpValues = new Sample[count];
                _iUB = count - 1;
                // the number of bits needed to express UBSamples
                _iCount = (int)(Math.Log(count) / Math.Log(2));
                _bUnknownSize = false;
                _bProcess = true;
            }
        }

        /// <summary></summary>
        public void RealIn(int index, double value)
        {
            if (!(_bUnknownSize || index > _smpValues.Length))
            {
                if (_iTimeWindow > 0)
                {
                    _dTemp = _dTwoPi * index / _iUB;
                    // three term blackman time window function
                    _smpValues[Mirror(index, _iCount)].Real = value * (0.42 - 0.5 * Math.Cos(_dTemp) + 0.08 * Math.Cos(2 * _dTemp));
                }
                else
                {
                    _smpValues[Mirror(index, _iCount)].Real = value / 2;
                }
                _bProcess = true;
            }
        }

        /// <summary></summary>
        public double RealOut(int index)
        {
            return GetIt(index).Real;
        }

        /// <summary></summary>
        public void WithTimeWindow(int size)
        {
            _iTimeWindow = size;
        }

        #endregion

        #region Private Methods

        /// <summary></summary>
        private void Butterfly(Sample* ps, Sample* pu, Sample* oj, Sample* ok)
        {
            _smpT->Real = pu->Real * ok->Real - pu->Imag * ok->Imag;
            _smpT->Imag = pu->Imag * ok->Real + pu->Real * ok->Imag;
            ok->Real = oj->Real - _smpT->Real;
            oj->Real += _smpT->Real;
            ok->Imag = oj->Imag - _smpT->Imag;
            oj->Imag += _smpT->Imag;
            _dTemp = ps->Real * pu->Real + ps->Imag * pu->Imag;
            pu->Imag += ps->Imag * pu->Real - ps->Real * pu->Imag;
            pu->Real -= _dTemp;
        }

        /// <summary></summary>
        private int Mirror(int index, int count)
        {
            int mr = 0;
            for (int j = 0; j < count; j++)
            {
                mr = mr * 2 | (index & 1);
                index = index / 2;
            }
            return mr;
        }

        /// <summary></summary>
        private Sample GetIt(int index)
        {
            if (!(_bUnknownSize || index > _iUB))
            {
                if (_bProcess)
                {
                    _bProcess = false;
                    _iStageSz = 1;
                    int i = 0, j = 0;
                    do
                    {
                        //divide and conquer
                        _iNumBf = _iStageSz;
                        _iStageSz = _iNumBf * 2;
                        _dTemp = _dPi / _iStageSz;
                        _smpS->Real = Math.Sin(_dTemp);
                        _smpS->Real = 2 * _smpS->Real * _smpS->Real;
                        _smpS->Imag = Math.Sin((_dTemp * 2));

                        for (i = 0; i < _iUB + 1; i += _iStageSz)
                        {
                            _smpU->Real = 1;
                            _smpU->Imag = 0;
                            for (j = i; j < (i + _iNumBf); j++)
                            {
                                fixed (Sample* pV1 = &_smpValues[j], pV2 = &_smpValues[j + _iNumBf])
                                { Butterfly(_smpS, _smpU, pV1, pV2); }
                            }
                        }
                    } while (!(_iStageSz > _iUB));
                }
            }
            return _smpValues[index];
        }

        #endregion

        #region Constants

        /// <summary></summary>
        private const int HEAP_ZERO_MEMORY = 0x00000008;

        #endregion

        #region Fields

        /// <summary></summary>
        private bool _bUnknownSize;
        private bool _bProcess;
        private int _iUB;       //upper bound of samples
        private int _iCount;    //number of bits needed to express above
        private int _iStageSz;  //the number of samples in current computation stage
        private int _iNumBf;    //the number of butterflies in current stage
        private int _iTimeWindow;
        private double _dPi;
        private double _dTwoPi;
        private double _dTemp;
        private Sample* _smpS;
        private Sample* _smpT;
        private Sample* _smpU;
        private Sample[] _smpValues;

        #endregion
    }
}