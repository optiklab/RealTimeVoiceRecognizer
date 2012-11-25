/*****************************************************
************* Copyright OptikLab 2011 ****************
************* http://www.ayarkov.com  ****************
******************************************************/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace RealTimeVoiceRecognizer
{
    /// <summary>
    /// Interaction logic for Main Window
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Public properties

        //public ObservableCollection<Command> CommandsCollection
        //{
        //    get
        //    {
        //        return _commands;
        //    }
        //}

        #endregion

        #region Public methods

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            this.Closing += MainWindow_Closing;

            _recognizer = new VoiceRecognizer();
            _recognizer.VoiceRecognized += Recognizer_VoiceRecognized;

            _InitSettings();

            _IsEnvironmentGood();
        }

        #endregion

        #region Events handlers

        /// <summary>
        /// Stops all running processes.
        /// </summary>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_recognizer != null && _recognizer.State == ApplicationState.Recording)
            {
                _StopTimeCounters();
                _recognizer.Stop();
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void CancelIt_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeIt_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            AboutDialog dlg = new AboutDialog();
            dlg.ShowDialog();
        }

        private void AddCommand_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(CommandText.Text) && !string.IsNullOrEmpty(ConsoleText.Text))
            {
                // To fix possible Keys conflicts add spaces to after command text (it doesn't affect the speech recognition).
                while (Properties.Settings.Default.Context.ContainsKey(COMMAND_TEXT + CommandText.Text))
                {
                    CommandText.Text += " ";
                }

                Properties.Settings.Default.Context.Add(COMMAND_TEXT + CommandText.Text, ConsoleText.Text);
                Properties.Settings.Default.Save();
                var newCommand = new Command(CommandText.Text, ConsoleText.Text);
                //_commands.Add(newCommand);
                CommandsListView.Items.Add(newCommand);
            }
        }

        private void RemoveCommand_Click(object sender, RoutedEventArgs e)
        {
            if (CommandsListView.SelectedIndex >= 0)
            {
                Command command = (Command)CommandsListView.Items[CommandsListView.SelectedIndex];

                Properties.Settings.Default.Context.Remove(command.CommandText);
                Properties.Settings.Default.Save();

                CommandsListView.Items.RemoveAt(CommandsListView.SelectedIndex);
            }
        }

        /// <summary>
        /// Shows recognized text to UI.
        /// </summary>
        private void Recognizer_VoiceRecognized(object sender, VoiceRecognizedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(delegate()
            {
                var result = new ResultsData(DateTime.Now.ToLongTimeString(), e.RecognizedText);

                Properties.Settings.Default.Reload();

                // Save result to log file.
                if (Properties.Settings.Default.AutomaticallySaveText)
                {
                    var sw = new StreamWriter(Properties.Settings.Default.DefaultLogFile, true);
                    sw.WriteLine(e.RecognizedText);
                    sw.Close();
                }

                // Show result
                recordedText.Items.Add(result);
            }));
        }

        /// <summary>
        /// Starts or stops recognizing process.
        /// </summary>
        private void StopStartSwitch_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = StopStartSwitch.IsChecked.GetValueOrDefault();

            if (isChecked)
            {
                if (!_IsEnvironmentGood())
                    return;

                if (_recognizer != null && _recognizer.State == ApplicationState.Recording)
                    return;

                // Create appropriate settings.
                AudioSettings audioSettings = new AudioSettings();


                audioSettings.AudioInputDevice = _selectedDevice; // -1 default
                audioSettings.isEightBitSample = false; // 8 or 16 bits
                audioSettings.Channels = 1; // mono
                audioSettings.SamplesPerSecond = 16000;// 48000;

                // Load equalizer.
                GraphicEqualizer.UpdateEqualizer(audioSettings);

                // Count timing
                _StartTimeCounters();

                // Start recognizing.
                _recognizer.Start(audioSettings, GraphicEqualizer.ProcessSound,
                    _selectedShortCultureName, TemporaryFolderPath.Text);

                // Disable some elements.
                SettingsTab.IsEnabled = false;
            }
            else
            {
                if (_recognizer == null || _recognizer.State == ApplicationState.Idle)
                    return;

                _StopTimeCounters();

                _recognizer.Stop();

                // Enable disabled elements.
                SettingsTab.IsEnabled = true;
            }
        }

        /// <summary>
        /// Update elapsed time in UI.
        /// </summary>
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(
                new Action(
                    delegate()
                    {
                        var elapsed = _stopTimer.Elapsed;
                        TimerLabel.Content = (object)(elapsed.Hours + ":" +
                            elapsed.Minutes + ":" +
                            elapsed.Seconds + ":" + 
                            elapsed.Milliseconds);
                    }));
        }

        /// <summary>
        /// 
        /// </summary>
        private void LanguagesSelectControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                var selected = e.AddedItems[0].ToString();

                string selectedCulture = string.Empty;
                if (_cultures.TryGetValue(selected, out selectedCulture))
                {
                    _selectedShortCultureName = selectedCulture;
                    Properties.Settings.Default.SelectedCulture = selected;
                    Properties.Settings.Default.Save();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeviceSelectControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                var selected = e.AddedItems[0].ToString();
                _selectedDevice = _availableDevices.IndexOf(selected);

                if (_selectedDevice < 0)
                {
                    _selectedDevice = -1;
                }
            }
            else
            {
                _selectedDevice = -1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void TemporaryFolderPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.TempFolder = TemporaryFolderPath.Text;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// 
        /// </summary>
        private void AutomaticallySaveTextCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutomaticallySaveText = true;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// 
        /// </summary>
        private void AutomaticallySaveTextCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutomaticallySaveText = false;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// 
        /// </summary>
        private void OpenTempFolder_Click(object sender, RoutedEventArgs e)
        {
            string res = _OpenFolder();

            if (!string.IsNullOrEmpty(res))
            {
                TemporaryFolderPath.Text = res;
                Properties.Settings.Default.TempFolder = res;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void OpenLogFile_Click(object sender, RoutedEventArgs e)
        {
            string res = _OpenFile();

            if (!string.IsNullOrEmpty(res))
            {
                LogFilePath.Text = res;
                Properties.Settings.Default.DefaultLogFile = res;
                Properties.Settings.Default.Save();
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// 
        /// </summary>
        private void _InitSettings()
        {
            // Fill cultures.
            foreach (var cultureName in Properties.Settings.Default.SupportedCultures)
            {
                int separatorIndex = cultureName.IndexOf(COMMA);
                string value = cultureName.Substring(0, separatorIndex);
                string key = cultureName.Substring(separatorIndex + 1,
                    cultureName.Length - separatorIndex - 1);

                _cultures.Add(key, value);
            }

            // Add cultures to select control.
            foreach (var key in _cultures.Keys)
            {
                LanguagesSelectControl.Items.Add(key);
            }

            // Add commands.
            foreach (var keyItem in Properties.Settings.Default.Context.Keys)
            {
                string key = keyItem.ToString();
                if (key != null && key.StartsWith(COMMAND_TEXT))
                {
                    string commandValue = key.Remove(0, COMMAND_TEXT.Length);

                    var newCommand = new Command(commandValue, Properties.Settings.Default.Context[keyItem].ToString());
                    //_commands.Add(newCommand);
                    CommandsListView.Items.Add(newCommand);
                }
            }
            // Set selection to controls.
            TemporaryFolderPath.Text = Properties.Settings.Default.TempFolder;
            LogFilePath.Text = Properties.Settings.Default.DefaultLogFile;
            LanguagesSelectControl.SelectedValue = Properties.Settings.Default.SelectedCulture;

            AutomaticallySaveTextCheckBox.IsChecked = Properties.Settings.Default.AutomaticallySaveText;
            AutomaticallySaveTextCheckBox.Checked += AutomaticallySaveTextCheckBox_Checked;
            AutomaticallySaveTextCheckBox.Unchecked += AutomaticallySaveTextCheckBox_Unchecked;

            _availableDevices = DeviceValidator.GetDevices();

            // Fill combo box with devices names
            if (_availableDevices.Count > 0)
            {
                foreach (var deviceName in _availableDevices)
                {
                    // If device name is empty, this is common case of DEFAULT device. Set appropriate name.
                    if (deviceName == SPACE && !DeviceSelectControl.Items.Contains(DEFAULT_DEVICE_NAME))
                    {
                        DeviceSelectControl.Items.Add(DEFAULT_DEVICE_NAME);
                    }
                    else // otherwise set name as is.
                    {
                        DeviceSelectControl.Items.Add(deviceName);
                    }
                }
            }
        }

        /// <summary>
        /// Method shows to user Open Folder dialog and returns path to the folder.
        /// </summary>
        /// <returns>Selected path in the Open Folder dialog</returns>
        private string _OpenFolder()
        {
            String textboxString = String.Empty;
            try
            {
                var fbd = new System.Windows.Forms.FolderBrowserDialog();
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    textboxString = fbd.SelectedPath;
                }
            }
            catch (Exception)
            {
                string errorLogFilePath = (string)App.Current.FindResource("ErrorLogFilePath");
                string text = string.Format((string)App.Current.FindResource("UnexpectedError"),
                    errorLogFilePath);
                System.Windows.MessageBox.Show(text, "Error!",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return textboxString;
        }

        /// <summary>
        /// Method shows to user OpenFileDialog and returns path to the file.
        /// </summary>
        /// <returns>Selected path in the Open File dialog</returns>
        private string _OpenFile()
        {
            String textboxString = String.Empty;
            try
            {
                var ofd = new System.Windows.Forms.OpenFileDialog();
                ofd.Filter = (string)App.Current.FindResource("FileDialogSettings");

                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (ofd.CheckFileExists == true)
                    {
                        textboxString = ofd.FileName;
                    }
                }
            }
            catch (Exception)
            {
                string errorLogFilePath = (string)App.Current.FindResource("ErrorLogFilePath");
                string text = string.Format((string)App.Current.FindResource("UnexpectedError"),
                    errorLogFilePath);
                System.Windows.MessageBox.Show(text, "Error!",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return textboxString;
        }

        /// <summary>
        /// Checks is current environment, like Internet or Recording devices, are good to work.
        /// If not: state labels become highlighted by colors.
        /// </summary>
        private bool _IsEnvironmentGood()
        {
            bool result = true;

            if (_IsMicrophoneOn())
            {
                MicrophoneStatusBox.IsChecked = true;
            }
            else
            {
                MicrophoneStatusBox.IsChecked = false;
                MessageBox.Show("Не найдено ни одно звукозаписывающее устройство!");

                result = false;
            }

            if (VoiceWebService.IsConnectionOn())
            {
                InternetStatusBox.IsChecked = true;
            }
            else
            {
                InternetStatusBox.IsChecked = false;
                MessageBox.Show("Не удалось установить соединение с сервисом распознавания голоса!");

                result = false;
            }

            return result;
        }

        /// <summary>
        /// Check if any recording device is on.
        /// </summary>
        private bool _IsMicrophoneOn()
        {
            return (DeviceValidator.GetDevicesCount() > 0);
        }

        /// <summary>
        /// Starts time counters.
        /// </summary>
        private void _StartTimeCounters()
        {
            // Count timing
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
            _stopTimer.Start();
        }

        /// <summary>
        /// Stops time counters.
        /// </summary>
        private void _StopTimeCounters()
        {
            _timer.Stop();
            _timer.Elapsed -= Timer_Elapsed;
            _stopTimer.Stop();
            _stopTimer.Reset();
        }

        #endregion

        #region Private const

        /// <summary>
        /// Default sample length in seconds.
        /// </summary>
        private const int DEFAULT_SAMPLE_LENGTH = 5; // seconds

        /// <summary>
        /// 
        /// </summary>
        private const string DEFAULT_DEVICE_NAME = "(default)";

        private const string COMMAND_TEXT = "CommandText ";

        /// <summary>
        /// 
        /// </summary>
        private const string SPACE = " ";

        /// <summary>
        /// 
        /// </summary>
        private const string COMMA = ",";

        #endregion

        #region Private fields

        /// <summary>
        /// Voice recognizer.
        /// </summary>
        private VoiceRecognizer _recognizer;

        /// <summary>
        /// Timers.
        /// </summary>
        private Timer _timer = new Timer(1);
        private Stopwatch _stopTimer = new Stopwatch();

        /// <summary>
        /// Supported cultures.
        /// </summary>
        private IDictionary<string, string> _cultures = new Dictionary<string, string>();

        /// <summary>
        /// Supported commands.
        /// </summary>
        //ObservableCollection<Command> _commands = new ObservableCollection<Command>();
        //List<Command> _commands = new List<Command>();


        /// <summary>
        /// 
        /// </summary>
        private string _selectedShortCultureName = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        private List<string> _availableDevices = new List<string>();

        /// <summary>
        /// 
        /// </summary>
        private int _selectedDevice = -1;

        #endregion
    }
}
