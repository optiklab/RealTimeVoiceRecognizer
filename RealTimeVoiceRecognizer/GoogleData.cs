using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace RealTimeVoiceRecognizer
{
    [DataContract]
    internal class RecognizedData
    {
        [DataMember(Name = "status")]
        public int Status
        {
            get;
            set;
        }

        [DataMember(Name = "id")]
        public string Id
        {
            get;
            set;
        }

        [DataMember(Name = "hypotheses")]
        public RecognitionStat[] Hypotheses
        {
            get;
            set;
        }
    }

    [DataContract]
    internal class RecognitionStat
    {
        [DataMember(Name = "utterance")]
        public string Utterance
        {
            get;
            set;
        }

        [DataMember(Name="confidence")]
        public double Confidence
        {
            get;
            set;
        }
    }
}
