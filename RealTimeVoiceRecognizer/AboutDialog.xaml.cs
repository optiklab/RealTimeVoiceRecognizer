/*****************************************************
************* Copyright OptikLab 2011 ****************
************* http://www.ayarkov.com  ****************
******************************************************/
using System.Text;
using System.Windows;

namespace RealTimeVoiceRecognizer
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class AboutDialog : Window
    {
        public AboutDialog()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine(@"Application created by Anton Yarkov as an independent software");
            text.AppendLine(@"developer. Application uses Google Voice service for recognizing");
            text.AppendLine(@"your speech while you are telling something in microphone.");
            text.AppendLine(@"");
            text.AppendLine(@"Please read full description and stay tuned:");
            Description.Text = text.ToString();

            DescriptionContinue.Text = @"If you have any questions:";

            TechSupportMail.Text = @" anton.yarkov@gmail.com";
        }

        private void CloseWindow_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
