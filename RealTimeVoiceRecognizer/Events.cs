/*****************************************************
************* Copyright OptikLab 2011 ****************
************* http://www.ayarkov.com  ****************
******************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Shapes;

namespace RealTimeVoiceRecognizer
{
    public class SoundBandsDisappearEventArgs : EventArgs
    {
    }

    public class VoiceRecognizedEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="recognizedText"></param>
        public VoiceRecognizedEventArgs(string recognizedText)
        {
            RecognizedText = recognizedText;
        }

        /// <summary>
        /// 
        /// </summary>
        public string RecognizedText
        {
            get;
            private set;
        }
    }

    public delegate void SoundBandsDisappearEventHandler(Object sender,
        SoundBandsDisappearEventArgs e);

    public delegate void VoiceRecognizedEventHandler(Object sender, VoiceRecognizedEventArgs e);
}
