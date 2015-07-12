#region License

// Copyright 2015 LoLAccountChecker
// 
// This file is part of LoLAccountChecker.
// 
// LoLAccountChecker is free software: you can redistribute it and/or modify 
// it under the terms of the GNU General Public License as published 
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// LoLAccountChecker is distributed in the hope that it will be useful, but 
// WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License 
// along with LoLAccountChecker. If not, see http://www.gnu.org/licenses/.

#endregion

#region

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using LoLAccountChecker.Classes;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;

#endregion

namespace LoLAccountChecker.Views
{
    public partial class MainWindow
    {
        public static MainWindow Instance;

        public MainWindow()
        {
            InitializeComponent();

            Instance = this;

            AccountsDataGrid.ItemsSource = Checker.Accounts.Where(a => a.State == Account.Result.Success);

            Loaded += WindowLoaded;
            Closed += WindowClosed;
        }

        private async void WindowLoaded(object sender, RoutedEventArgs e)
        {
            await LeagueData.Load();
            await Utils.UpdateClientVersion();
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            Settings.Save();
            Application.Current.Shutdown();
        }

        public void UpdateControls()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(UpdateControls);
                return;
            }

            var numCheckedAcccounts = Checker.Accounts.Count(a => a.State != Account.Result.Unchecked);

            // Progress Bar
            ProgressBar.Value = Checker.Accounts.Any() ? ((numCheckedAcccounts * 100f) / Checker.Accounts.Count()) : 0;

            // Export Button
            ExportButton.IsEnabled = numCheckedAcccounts > 0;

            // Start Button
            StartButton.IsEnabled = numCheckedAcccounts < Checker.Accounts.Count;
            StartButton.Content = Checker.IsChecking ? "Stop" : "Start";

            // Status Label
            if (Checker.IsChecking)
            {
                StatusLabel.Content = "Status: Checking...";
            }
            else if (numCheckedAcccounts > 0 && Checker.Accounts.All(a => a.State != Account.Result.Unchecked))
            {
                StatusLabel.Content = "Status: Finished!";
            }

            // Checked Accounts Label
            CheckedLabel.Content = string.Format("Checked: {0}/{1}", numCheckedAcccounts, Checker.Accounts.Count);

            // Grid
            AccountsDataGrid.ItemsSource = Checker.Accounts.Where(a => a.State == Account.Result.Success);
        }

        public void UpdateProgressBar(double value)
        {
            if (value > 100 || value < 0)
            {
                return;
            }

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => UpdateProgressBar(value));
                return;
            }

            ProgressBar.Value = value;
        }

        #region Right Window Commands

        private void BtnDonateClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=CHEV6LWPMHUMW");
        }

        private void BtnGithubClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/madk/LoLAccountChecker");
        }

        private async void BtnRefreshClick(object sender, RoutedEventArgs e)
        {
            if (Checker.IsChecking)
            {
                await this.ShowMessageAsync("Error", "Please wait for the checking process to be completed.");
                return;
            }

            if (!Checker.Accounts.Any())
            {
                await this.ShowMessageAsync("Error", "You don't have any accounts.");
                return;
            }

            if (Checker.Accounts.Any(a => a.State == Account.Result.Error))
            {
                var dialog =
                    await
                        this.ShowMessageAsync(
                            "Refresh", "Do you want to refresh accounts with Errors?",
                            MessageDialogStyle.AffirmativeAndNegative);

                Checker.Refresh(dialog == MessageDialogResult.Affirmative);
                return;
            }

            Checker.Refresh();
        }

        #endregion

        #region Import Button

        private async void BtnImportClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();

            ofd.Filter = "JavaScript Object Notation (*.json)|*.json";

            var result = ofd.ShowDialog();

            if (result == true)
            {
                var file = ofd.FileName;
                if (!File.Exists(file))
                {
                    return;
                }

                var jsonformat = JsonFormat.Import(file);

                if (jsonformat == null)
                {
                    await
                        this.ShowMessageAsync(
                            "Error", "Unable to load JSON file, it might be corrupted or from an old version.");
                    return;
                }

                var importedVer = Version.Parse(jsonformat.Version);
                var currentVer = Assembly.GetExecutingAssembly().GetName().Version;

                if (importedVer != currentVer)
                {
                    var msg =
                        await
                            this.ShowMessageAsync(
                                "Warning",
                                string.Format(
                                    "The file you are importing is from a {0} version. " +
                                    "Some functions might not might not work, do you still want to load it?",
                                    importedVer < currentVer ? "old" : "new"), MessageDialogStyle.AffirmativeAndNegative);

                    if (msg == MessageDialogResult.Negative)
                    {
                        return;
                    }
                }

                var count = 0;
                foreach (var account in jsonformat.Accounts)
                {
                    if (!Checker.Accounts.Exists(a => a.Username == account.Username))
                    {
                        Checker.Accounts.Add(account);
                        count++;
                    }
                }

                UpdateControls();

                await
                    this.ShowMessageAsync(
                        "Import", count > 0 ? string.Format("Imported {0} accounts.", count) : "No new accounts found.");
            }
        }

        #endregion

        #region Export Button

        private void BtnExportToFileClick(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.FileName = "output";
            sfd.Filter = "JavaScript Object Notation (*.json)|*.json";

            if (sfd.ShowDialog() == true)
            {
                var file = sfd.FileName;

                JsonFormat.Export(file, Checker.Accounts.Where(a => a.State == Account.Result.Success).ToList());
                this.ShowMessageAsync("Export", string.Format("Exported {0} accounts.", Checker.Accounts.Count));
            }
        }

        #endregion

        #region Accounts Button

        private void BtnAccountsClick(object sender, RoutedEventArgs e)
        {
            if (AccountsWindow.Instance == null)
            {
                AccountsWindow.Instance = new AccountsWindow();
                AccountsWindow.Instance.Show();
                AccountsWindow.Instance.Closed += (o, a) => { AccountsWindow.Instance = null; };
            }
            else if (AccountsWindow.Instance != null && !AccountsWindow.Instance.IsActive)
            {
                AccountsWindow.Instance.Activate();
            }
        }

        #endregion

        #region Start Button

        private void BtnStartCheckingClick(object sender, RoutedEventArgs e)
        {
            if (Checker.IsChecking)
            {
                Checker.Stop();
                StartButton.Content = "Start";
                StatusLabel.Content = "Status: Stopped!";
                return;
            }

            if (Checker.Accounts.All(a => a.State != Account.Result.Unchecked))
            {
                this.ShowMessageAsync("Error", "All accounts have already been checked.");
                return;
            }

            StartButton.Content = "Stop";
            StatusLabel.Content = "Status: Checking...";

            Checker.Start();
        }

        #endregion

        #region Context Menu

        private void CmCopyUsername(object sender, RoutedEventArgs routedEventArgs)
        {
            var account = AccountsDataGrid.SelectedItem as Account;

            if (account == null)
            {
                return;
            }

            Clipboard.SetText(account.Username);
            this.ShowMessageAsync("Copy Username", "Username has been copied to your clipboard.");
        }

        private void CmCopyPassword(object sender, RoutedEventArgs routedEventArgs)
        {
            var account = AccountsDataGrid.SelectedItem as Account;

            if (account == null)
            {
                return;
            }

            Clipboard.SetText(account.Password);
            this.ShowMessageAsync("Copy Password", "Password has been copied to your clipboard.");
        }

        private void CmCopyCombo(object sender, RoutedEventArgs e)
        {
            var account = AccountsDataGrid.SelectedItem as Account;

            if (account == null)
            {
                return;
            }

            var combo = string.Format("{0}:{1}", account.Username, account.Password);
            Clipboard.SetText(combo);
        }

        private void CmViewAccount(object sender, RoutedEventArgs e)
        {
            var account = AccountsDataGrid.SelectedItem as Account;

            if (account == null)
            {
                return;
            }

            var window = new AccountWindow(account);
            window.Show();
        }

        private void CmExportJson(object sender, RoutedEventArgs e)
        {
            if (AccountsDataGrid.SelectedItem == null)
            {
                return;
            }

            var sfd = new SaveFileDialog();
            sfd.FileName = "output";
            sfd.Filter = "JavaScript Object Notation (*.json)|*.json";

            var accounts = AccountsDataGrid.SelectedItems.Cast<Account>().ToList();

            if (sfd.ShowDialog() == true)
            {
                var file = sfd.FileName;

                JsonFormat.Export(file, accounts);
                this.ShowMessageAsync("Export", string.Format("Exported {0} accounts.", accounts.Count));
            }
        }

        private void CmExportCustom(object sender, RoutedEventArgs e)
        {
            if (AccountsDataGrid.SelectedItem == null)
            {
                return;
            }

            var accounts = AccountsDataGrid.SelectedItems.Cast<Account>();
            var window = new ExportWindow(accounts);
            window.ShowDialog();
        }

        #endregion
    }
}