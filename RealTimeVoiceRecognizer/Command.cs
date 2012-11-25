using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RealTimeVoiceRecognizer
{
    public class Command
    {
        public Command(string command, string console)
        {
            CommandText = command;
            ConsoleText = console;
        }

        public string CommandText { get; set; }

        public string ConsoleText { get; set; }
    }
}
