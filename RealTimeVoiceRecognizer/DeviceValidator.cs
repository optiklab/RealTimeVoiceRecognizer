/*****************************************************
************* Copyright OptikLab 2011 ****************
************* http://www.ayarkov.com  ****************
******************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace RealTimeVoiceRecognizer
{
    internal class DeviceValidator
    {
        #region Imported functions

        /// <summary>
        /// Gets number of audio input devices. Actually this is the biggest boundary value
        /// for the range of allowed devices identifiers: 0...return value.
        /// </summary>
        /// <returns>Number if available audio input devices.</returns>
        [DllImport("winmm.dll")]
        private static extern int waveInGetNumDevs();

        /// <summary>
        /// Function allows to get characteristics of audio input device.
        /// </summary>
        /// <param name="uDeviceID"></param>
        /// <param name="pwic">Object where to save device characteristics.</param>
        /// <param name="cbwic">Size of object for saving.</param>
        /// <returns>MMSYSER: NOERROR, BADDEVICEID, NODRIVER, NOMEM</returns>
        [DllImport("winmm.dll")]
        private static extern MMSYSERR waveInGetDevCaps(uint uDeviceID, out WaveInCaps pwic, uint cbwic);

        #endregion

        #region Public methods

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static List<string> GetDevices()
        {
            List<string> devices = new List<string>();

            // Get devices count.
            int count = waveInGetNumDevs();

            for (int i = 0; i < count; i++)
            {
                string name = string.Empty;
                if (MMSYSERR.NOERROR == _GetInputDeviceName((uint)i, ref name))
                    devices.Add(name);
            }

            return devices;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static int GetDevicesCount()
        {
            return waveInGetNumDevs();
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Get the input device name from device id.
        /// </summary>
        /// <param name="deviceId">Device id.</param>
        /// <param name="prodName">Returns device name.</param>
        /// <returns>MMSYSERR.</returns>
        private static MMSYSERR _GetInputDeviceName(uint deviceId, ref string prodName)
        {
            var caps = new WaveInCaps();
            MMSYSERR result = waveInGetDevCaps(deviceId, out caps, (uint)Marshal.SizeOf(caps)); // system function
            if (result != MMSYSERR.NOERROR)
                return result;
            prodName = new string(caps.szPname);
            if (prodName.Contains("(") && !(prodName.Contains(")")))
            {
                prodName = prodName.Substring(0, prodName.IndexOf("("));
            }
            else if (prodName.Contains(")"))
            {
                if (prodName.IndexOf(")") > 8)
                {
                    prodName = prodName.Substring(0, prodName.IndexOf(")") + 1);
                }
            }
            return MMSYSERR.NOERROR;
        }

        #endregion
    }
}
