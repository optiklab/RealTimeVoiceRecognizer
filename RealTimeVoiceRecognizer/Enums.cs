/*****************************************************
************* Copyright OptikLab 2011 ****************
************* http://www.ayarkov.com  ****************
******************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RealTimeVoiceRecognizer
{
    /// <summary>
    /// Application state.
    /// </summary>
    internal enum ApplicationState
    {
        /// <summary>
        /// Application idle.
        /// </summary>
        Idle,
        /// <summary>
        /// Playing in process.
        /// </summary>
        //Playing,
        /// <summary>
        /// Playing paused.
        /// </summary>
        //PlayPause,
        /// <summary>
        /// Recording in process.
        /// </summary>
        Recording,
        /// <summary>
        /// Recording paused.
        /// </summary>
        //RecordPause,
    }

    /// <summary>
    /// Type of filters.
    /// </summary>
    internal enum FiltersType
    {
        /// <summary>
        /// Low pass filter.
        /// </summary>
        LPF,

        /// <summary>
        /// Peaking band eq filter.
        /// </summary>
        PEQ,

        /// <summary>
        /// high shelf filter
        /// </summary>
        HSH
    }

    /// <summary>
    /// Multimedia system error types.
    /// </summary>
    internal enum MMSYSERR : int
    {
        NOERROR = 0,
        ERROR,
        BADDEVICEID,
        NOTENABLED,
        ALLOCATED,
        INVALHANDLE,
        NODRIVER,
        NOMEM,
        NOTSUPPORTED,
        BADERRNUM,
        INVALFLAG,
        INVALPARAM,
        HANDLEBUSY,
        INVALIDALIAS,
        BADDB,
        KEYNOTFOUND,
        READERROR,
        WRITEERROR,
        DELETEERROR,
        VALNOTFOUND,
        NODRIVERCB,
    }
}
