using ApplicationSettingsNS;
using ARCL;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MobileConfiguration
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ARCLConnection Connection = new ARCLConnection();
        public ConfigManager ConfigManager = new ConfigManager();

        private bool IsLoading = true;
        public MainWindow()
        {
            InitializeComponent();

            BtnConnect.Background = Brushes.Red;
            BtnSend.IsEnabled = false;
            BtnReadSectionValues.IsEnabled = false;
            BtnReadAllSectionValues.IsEnabled = false;
            BtnReloadSections.IsEnabled = false;

            BtnSaveSection.IsEnabled = false;
            BtnSaveAllSections.IsEnabled = false;
            BtnWriteSection.IsEnabled = false;


            //Load the last Connection string the user entered.
            TxtConnectionString.Text = App.Settings.ConenctionString;

            //Load the last Window position or if the Left Shift Key is held down, reset.
            if (Keyboard.IsKeyDown(Key.LeftShift))
            {
                App.Settings.MainWindow = new ApplicationSettings_Serializer.ApplicationSettings.WindowSettings();
            }
            this.Left = App.Settings.MainWindow.Left;
            this.Top = App.Settings.MainWindow.Top;

            IsLoading = false;
        }

        /// <summary>
        /// Read a configration Sections values.
        /// Stored in the ConfigManager.Sections dictionary.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnReadSectionValues_Click(object sender, RoutedEventArgs e)
        {
            BtnReadSectionValues.Background = Brushes.Yellow;

            ConfigManager.Start(Connection);
            ConfigManager.ReadSectionValues($"{CmbAvailableSections.SelectedItem}");

            Stopwatch sw = new Stopwatch();
            sw.Restart();

            while (!ConfigManager.IsSynced & sw.ElapsedMilliseconds < 60000) { Thread.Sleep(1); }

            if (ConfigManager.IsSynced)
                BtnReadSectionValues.Background = Brushes.Green;
            else
                BtnReadSectionValues.Background = Brushes.Red;

            ConfigManager.Stop();

            UpdateCmbLoadedSections();
        }
        private void BtnReadAllSectionValues_Click(object sender, RoutedEventArgs e)
        {
            BtnReadAllSectionValues.Background = Brushes.Yellow;

            ConfigManager.Start(Connection);

            foreach (string item in CmbAvailableSections.Items)
            {
                ConfigManager.ReadSectionValues($"{item}");

                Stopwatch sw = new Stopwatch();
                sw.Restart();

                while (!ConfigManager.IsSynced && sw.ElapsedMilliseconds < 60000) {  }

                if (!ConfigManager.IsSynced)
                    break;
            }

            if (ConfigManager.IsSynced)
                BtnReadAllSectionValues.Background = Brushes.Green;
            else
                BtnReadAllSectionValues.Background = Brushes.Red;

            ConfigManager.Stop();

            UpdateCmbLoadedSections();
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            Connection.ConnectionString = TxtConnectionString.Text;

            if (!Connection.IsConnected)
            {
                if (Connection.Connect())
                {
                    BtnConnect.Background = Brushes.Green;
                    BtnSend.IsEnabled = true;
                    BtnReloadSections.IsEnabled = true;

                    if (CmbLoadedSections.SelectedIndex > -1)
                        BtnWriteSection.IsEnabled = true;
                    else
                        BtnWriteSection.IsEnabled = false;
                    return;
                }
            }
            else
                Connection.Close();

            BtnConnect.Background = Brushes.Red;
            BtnSend.IsEnabled = false;
            BtnReloadSections.IsEnabled = false;
            BtnWriteSection.IsEnabled = false;

            CmbAvailableSections.Items.Clear();
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e) => Connection?.Write(TxtSendMessage.Text);

        private void BtnLoadSection_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog of = new OpenFileDialog
            {
                CheckFileExists = true,
                DefaultExt = "txt",
                Filter = "Text file (*.txt)|*.txt"
            };

            if ((bool)of.ShowDialog())
            {
                if(ConfigManager.TextToSectionValues(File.ReadAllText(of.FileName)))
                    UpdateCmbLoadedSections();
            }
        }

        private void BtnSaveSection_Click(object sender, RoutedEventArgs e)
        {
            if (CmbLoadedSections.SelectedIndex == -1) return;

            if (ConfigManager.Sections.ContainsKey(CmbLoadedSections.SelectedItem.ToString()))
            {
                SaveFileDialog sf = new SaveFileDialog
                {
                    DefaultExt = "txt",
                    Filter = "Text file (*.txt)|*.txt"
                };

                if ((bool)sf.ShowDialog())
                    File.WriteAllText(sf.FileName, ConfigManager.SectionValuesToText(CmbLoadedSections.SelectedItem.ToString()));
            }

        }

        private void BtnSaveAllSections_Click(object sender, RoutedEventArgs e)
        {
            if (CmbLoadedSections.Items.Count == 0) return;

            SaveFileDialog sf = new SaveFileDialog
                {
                    DefaultExt = "txt",
                    Filter = "Text file (*.txt)|*.txt"
                };

                if ((bool)sf.ShowDialog())
                {
                    using (StreamWriter sw = File.CreateText(sf.FileName))
                        foreach (var cs in ConfigManager.Sections)
                            sw.WriteLine(ConfigManager.SectionValuesToText(cs.Key));
                }

        }

        private void BtnWriteSection_Click(object sender, RoutedEventArgs e)
        {
            if (CmbLoadedSections.SelectedIndex == -1) return;

            ConfigManager.Start(Connection);
            ConfigManager.WriteConfigSection(CmbLoadedSections.SelectedItem.ToString());
            ConfigManager.Stop();
        }

        private void UpdateCmbLoadedSections()
        {
            foreach (var key in ConfigManager.Sections)
                if (key.Value.Count > 0)
                {
                    if (!CmbLoadedSections.Items.Contains(key.Key))
                        CmbLoadedSections.Items.Add(key.Key);
                }

            if (CmbLoadedSections.Items.Count > 0)
                BtnSaveAllSections.IsEnabled = true;
            else
                BtnSaveAllSections.IsEnabled = false;
        }
        private void UpdateCmbAvailableSections()
        {
            CmbAvailableSections.Items.Clear();
            foreach (var key in ConfigManager.Sections)
                if (!CmbAvailableSections.Items.Contains(key.Key))
                    CmbAvailableSections.Items.Add(key.Key);

            if (CmbAvailableSections.Items.Count > 0)
                BtnReadAllSectionValues.IsEnabled = true;
            else
                BtnReadAllSectionValues.IsEnabled = false;
        }
        private void CmbLoadedSections_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbLoadedSections.SelectedIndex > -1)
            {
                DgvSectionValues.ItemsSource = ConfigManager.Sections[CmbLoadedSections.SelectedValue.ToString()];

                BtnSaveSection.IsEnabled = true;
                if (Connection.IsConnected)
                    BtnWriteSection.IsEnabled = true;
                else
                    BtnWriteSection.IsEnabled = false;
            }
            else
            {
                BtnSaveSection.IsEnabled = false;
                BtnWriteSection.IsEnabled = false;
            }
        }
        private void CmbAvailableSections_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbAvailableSections.SelectedIndex > -1)
                BtnReadSectionValues.IsEnabled = true;
            else
                BtnReadSectionValues.IsEnabled = false;
        }

        private void BtnReloadSections_Click(object sender, RoutedEventArgs e)
        {
            BtnReloadSections.Background = Brushes.Yellow;

            ConfigManager.Start(Connection);
            ConfigManager.ReadSectionsList();

            Stopwatch sw = new Stopwatch();
            sw.Restart();

            while (!ConfigManager.IsSynced & sw.ElapsedMilliseconds < 60000) { Thread.Sleep(1); }

            if (ConfigManager.IsSynced)
            {
                BtnReloadSections.Background = Brushes.Green;
            }
            else BtnReloadSections.Background = Brushes.Red;

            ConfigManager.Stop();

            UpdateCmbAvailableSections();
        }

        /// <summary>
        /// If the Window moves store the location.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if (IsLoading) return;

            App.Settings.MainWindow.Top = Top;
            App.Settings.MainWindow.Left = Left;
        }
        /// <summary>
        /// If the Window closes store the AppSettings to a file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ApplicationSettings_Serializer.Save("appsettings.xml", App.Settings);
        }


    }
}
