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
    internal sealed class ResultsData
    {
        public ResultsData(string time, string data)
        {
            Time = time;
            Data = data;
        }

        public string Time
        {
            get;
            private set;
        }

        public string Data
        {
            get;
            private set;
        }
    }
}
