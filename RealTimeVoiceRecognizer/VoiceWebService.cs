using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using System.Runtime.Serialization.Json;

namespace RealTimeVoiceRecognizer
{
    internal class VoiceWebService
    {
        #region Public methods

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool IsConnectionOn()
        {
            WebRequest request = WebRequest.Create(DEFAULT_URL);
            request.Method = "POST";
            request.ContentType = "audio/x-flac; rate=16000";
            request.ContentLength = 1;

            try
            {
                // Get the request stream.
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(new byte[] { 0 }, 0, 1);
                dataStream.Close();

                // Get the response.
                WebResponse response = request.GetResponse();
            }
            catch (WebException)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public RecognizedData RecognizeSoundFile(string path, string shortCultureName)
        {
            string url = string.Format(SERVICE_URL_FORMAT, shortCultureName);
            WebRequest request = WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "audio/x-flac; rate=16000";

            byte[] byteArray = File.ReadAllBytes(path);
            request.ContentLength = byteArray.Length;

            // Get the request stream.
            Stream dataStream = request.GetRequestStream();
            // Write the data to the request stream.
            dataStream.Write(byteArray, 0, byteArray.Length);

            dataStream.Close();

            // Get the response.
            WebResponse response = request.GetResponse();

            var ser = new DataContractJsonSerializer(typeof(RecognizedData));
            RecognizedData data = ser.ReadObject(response.GetResponseStream()) as RecognizedData;

            response.Close();

            return data;
        }

        #endregion

        #region Private constants

        /// <summary>
        /// 
        /// </summary>
        private const string DEFAULT_URL =
            @"https://www.google.com/speech-api/v1/recognize?xjerr=1&client=chromium&lang=ru-RU";

        private const string SERVICE_URL_FORMAT =
            @"https://www.google.com/speech-api/v1/recognize?xjerr=1&client=chromium&lang={0}";

        #endregion
    }
}
