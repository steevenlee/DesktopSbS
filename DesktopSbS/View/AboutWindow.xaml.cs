﻿using DesktopSbS.Interop;
using DesktopSbS.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace DesktopSbS.View
{

    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window, INotifyPropertyChanged
    {

        private static AboutWindow instance = null;

        public static AboutWindow Instance
        {
            get
            {
                if (instance == null) instance = new AboutWindow();
                return instance;
            }
        }

        public List<Tuple<ModifierKeys, Key, ShortcutCommands>> KeyboardShortcuts
        {
            get { return Options.KeyboardShortcuts; }
        }

        private Thread threadNeal;
        public AboutWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            this.hideNextTime.IsChecked = Options.HideAboutOnStartup;

            this.threadNeal = new Thread(asyncReadNreal);
            this.threadNeal.IsBackground = true;
            this.threadNeal.Start();
        }

        private void CloseCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            this.threadNeal.Abort();
            this.Close();
            NrealAir.Reset();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Options.HideAboutOnStartup = this.hideNextTime.IsChecked == true;
            instance = null;
        }

        private void PropertiesCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            new SettingsWindow().ShowDialog();
            RaisePropertyChanged(nameof(KeyboardShortcuts));
        }


        #region CheckUpdate

        private RelayCommand cmdCheckUpdate;
        public ICommand CmdCheckUpdate
        {
            get
            {
                if (cmdCheckUpdate == null) { cmdCheckUpdate = new RelayCommand(fctCheckUpdate, canCheckUpdate, true); }
                return cmdCheckUpdate;
            }
        }

        private bool canCheckUpdate(object y)
        {
            return true;
        }

        private void fctCheckUpdate(object x)
        {
            if (CmdCheckUpdate.CanExecute(x))
            {
                AppUpdater.CheckForUpdates(false);
            }

        }

        #endregion


        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public static bool Continue { get; private set; }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow.Continue = true;
            this.threadNeal.Abort();
            this.Close();
        }

        private Euler euler;

        private void asyncReadNreal()
        {
            if (!NrealAir.Connected)
                return;
            while (true)
            {
                this.euler = NrealAir.Euler;
                this.Dispatcher.Invoke(updateEuler);
                Thread.Sleep(100);
            }
        }

        private void updateEuler()
        {
            this.btnEuler.Content = "Nreal euler: " + euler;

        }

        private void Button_Click_Nreal(object sender, RoutedEventArgs e)
        {
            NrealAir.Reset();
        }
    }
}
