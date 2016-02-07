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
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Data;

#endregion

namespace LoLAccountChecker.Views
{
    public partial class MainWindow
    {
        public static MainWindow Instance;
        public bool IsSearchActive;

        public MainWindow()
        {
            InitializeComponent();

            Instance = this;

            IsSearchActive = false;

            AccountsDataGrid.PreviewKeyDown += Common.AccountsDataGrid_SearchByLetterKey;

            Loaded += WindowLoaded;
            Closed += WindowClosed;
        }

        private async void WindowLoaded(object sender, RoutedEventArgs e)
        {
            await LeagueData.Load();
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

            ClearSearch();

            var numCheckedAcccounts = Checker.Accounts.Count(a => a.State != Account.Result.Unchecked);

            ProgressBar.Value = Checker.Accounts.Any() ? ((numCheckedAcccounts * 100f) / Checker.Accounts.Count()) : 0;

            ExportButton.IsEnabled = numCheckedAcccounts > 0;
            StartButton.IsEnabled = numCheckedAcccounts < Checker.Accounts.Count;
            StartButton.Content = Checker.IsChecking ? "Stop" : "Start";
            SearchButton.IsEnabled = numCheckedAcccounts > 0;

            if (Checker.IsChecking)
            {
                StatusLabel.Content = "Status: Checking...";
            }
            else if (numCheckedAcccounts > 0 && Checker.Accounts.All(a => a.State != Account.Result.Unchecked))
            {
                StatusLabel.Content = "Status: Finished!";
            }

            CheckedLabel.Content = string.Format("Checked: {0}/{1}", numCheckedAcccounts, Checker.Accounts.Count);

            AccountsDataGrid.ItemsSource = Checker.Accounts.Where(a => a.State == Account.Result.Success);

            if (AccountsWindow.Instance != null)
            {
                AccountsWindow.Instance.UpdateControls();
            }
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

        private void BtnUpdateClick(object sender, RoutedEventArgs e)
        {

        }

        private void BtnDonateClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=X9559SH2MKQ7S");
        }

        private void BtnGithubClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/yokrysty/LoLAccountChecker");
        }

        #endregion

        #region Buttons

        private async void BtnImportClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "JavaScript Object Notation (*.json)|*.json";

            if (ofd.ShowDialog() != true)
            {
                return;
            }

            string file = ofd.FileName;
            if (!File.Exists(file))
            {
                return;
            }

            var jsonformat = JsonFormat.Import(file);

            if (jsonformat == null)
            {
                await this.ShowMessageAsync("Error", "Unable to load JSON file, it might be corrupted or from an old version.");
                return;
            }

            var importedVer = Version.Parse(jsonformat.Version);
            var currentVer = Assembly.GetExecutingAssembly().GetName().Version;

            if (importedVer != currentVer)
            {
                var msg = await this.ShowMessageAsync(
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

            int count = 0;
            foreach (Account account in jsonformat.Accounts)
            {
                if (!Checker.Accounts.Exists(a => a.Username.ToLower() == account.Username.ToLower()))
                {
                    foreach (ChampionData champion in account.ChampionList)
                    {
                        SkinData skin = account.SkinList.FirstOrDefault(c => c.ChampionId == champion.Id);

                        champion.HasSkin = (skin != null) ? true : false;
                    }

                    Checker.Accounts.Add(account);
                    count++;
                }
            }

            if (count > 0)
            {
                UpdateControls();
                AccountsDataGrid.Focus();
                return;
            }

            await this.ShowMessageAsync("Import", "No new accounts found.");
        }

        private void BtnExportToFileClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "output";
            sfd.Filter = "JavaScript Object Notation (*.json)|*.json";

            if (sfd.ShowDialog() == true)
            {
                JsonFormat.Export(sfd.FileName, Checker.Accounts.Where(a => a.State == Account.Result.Success).ToList());
                this.ShowMessageAsync("Export", string.Format("Exported {0} accounts.", Checker.Accounts.Count));
            }
        }

        private void BtnAccountsClick(object sender, RoutedEventArgs e)
        {
            if (AccountsWindow.Instance == null)
            {
                AccountsWindow.Instance = new AccountsWindow();
                AccountsWindow.Instance.Show();
                return;
            }
            
            if (AccountsWindow.Instance.WindowState == WindowState.Minimized)
            {
                AccountsWindow.Instance.WindowState = WindowState.Normal;
                return;
            }
            
            AccountsWindow.Instance.Activate();
        }

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

        private void BtnSearchClick(object sender, RoutedEventArgs e)
        {
            if (IsSearchActive)
            {
                ClearSearch();
                return;
            }

            SearchWindow sw = new SearchWindow();
            sw.ShowDialog();
        }

        #endregion

        #region Context Menu

        private void CmCopyUsername(object sender, RoutedEventArgs routedEventArgs)
        {
            if (AccountsDataGrid.SelectedItem == null)
            {
                return;
            }

            Clipboard.SetDataObject(((Account)AccountsDataGrid.SelectedItem).Username);
        }

        private void CmCopyPassword(object sender, RoutedEventArgs routedEventArgs)
        {
            if (AccountsDataGrid.SelectedItem == null)
            {
                return;
            }

            Clipboard.SetDataObject(((Account)AccountsDataGrid.SelectedItem).Password);
        }

        private void CmCopyCombo(object sender, RoutedEventArgs e)
        {
            Common.CopyCombo(AccountsDataGrid);
        }

        private void CmCopySummoner(object sender, RoutedEventArgs e)
        {
            if (AccountsDataGrid.SelectedItem == null)
            {
                return;
            }

            Clipboard.SetDataObject(((Account)AccountsDataGrid.SelectedItem).Summoner);
        }

        private void ViewAccount(object selectedItem)
        {
            if (AccountsDataGrid.SelectedItem == null)
            {
                return;
            }

            AccountWindow window = new AccountWindow((Account)AccountsDataGrid.SelectedItem);
            window.Show();
        }

        private void CmViewAccount(object sender, RoutedEventArgs e)
        {
            if (AccountsDataGrid.SelectedItem == null)
            {
                return;
            }

            ViewAccount(AccountsDataGrid.SelectedItem);
        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGridRow row = ItemsControl.ContainerFromElement((DataGrid)sender, e.OriginalSource as DependencyObject) as DataGridRow;

            if (row == null)
            {
                return;
            }

            ViewAccount(row.Item);
        }

        private void CmExportJson(object sender, RoutedEventArgs e)
        {
            if (AccountsDataGrid.SelectedItems.Count == 0)
            {
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
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
            if (AccountsDataGrid.SelectedItems.Count == 0)
            {
                return;
            }

            var accounts = AccountsDataGrid.SelectedItems.Cast<Account>();
            ExportWindow window = new ExportWindow(accounts);
            window.ShowDialog();
        }

        #endregion

        public void ClearSearch()
        {
            if (!IsSearchActive)
            {
                return;
            }

            CollectionView cv = (CollectionView)CollectionViewSource.GetDefaultView(AccountsDataGrid.ItemsSource);
            if (cv != null)
            {
                cv.Filter = null;
            }
            SearchButton.Content = "Search";
            IsSearchActive = false;
        }
    }
}