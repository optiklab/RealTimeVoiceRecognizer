/*****************************************************
************* Copyright OptikLab 2011 ****************
************* http://www.ayarkov.com  ****************
******************************************************/
/*****************************************************
************* Copyright OptikLab 2011 ****************
************* http://www.ayarkov.com  ****************
******************************************************/
using System;
using System.Windows;
using System.Text;
using System.Windows.Threading;
using System.IO;

namespace RealTimeVoiceRecognizer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            StringBuilder exMessage = new StringBuilder();

            // Add info
            exMessage.Append(DateTime.Now);
            exMessage.Append(System.Environment.NewLine);
            exMessage.Append("Source: \n");
            exMessage.Append(e.Exception.Source);
            exMessage.Append("\nException message: \n");
            exMessage.Append(e.Exception.Message);
            exMessage.Append("\nStack trace: \n");
            exMessage.Append(e.Exception.StackTrace);

            // Log to file
            string errorLogFilePath = (string)App.Current.FindResource("ErrorLogFilePath");
            StreamWriter wr = new StreamWriter(errorLogFilePath, true);
            wr.WriteLine(exMessage.ToString());
            wr.Close();

            string text = string.Format((string)App.Current.FindResource("UnexpectedError"),
                errorLogFilePath);
            MessageBox.Show(text, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
