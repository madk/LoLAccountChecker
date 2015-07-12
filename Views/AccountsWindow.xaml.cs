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
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using LoLAccountChecker.Classes;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;

#endregion

namespace LoLAccountChecker.Views
{
    public partial class AccountsWindow
    {
        public static AccountsWindow Instance;

        public AccountsWindow()
        {
            InitializeComponent();
            Instance = this;

            _accountsGrid.ItemsSource = Checker.Accounts;
            _showPasswords.IsChecked = Settings.Config.ShowPasswords;
        }

        private void BtnAddAccountClick(object sender, RoutedEventArgs e)
        {
            if (AccountEdit.Instance != null)
            {
                return;
            }

            var window = new AccountEdit();
            window.ShowDialog();
        }

        private void BtnImportClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt"
            };

            var result = ofd.ShowDialog();

            if (result == true)
            {
                if (!File.Exists(ofd.FileName))
                {
                    return;
                }

                var accounts = Utils.GetLogins(ofd.FileName);

                if (!accounts.Any())
                {
                    return;
                }

                var window = new ImportWindow(accounts);
                window.ShowDialog();
            }
        }

        private void ShowPasswordsClick(object sender, RoutedEventArgs e)
        {
            Settings.Config.ShowPasswords = _showPasswords.IsChecked == true;
            RefreshAccounts();

            if (AccountEdit.Instance != null)
            {
                AccountEdit.Instance.UpdateWindow();
            }
        }

        public void RefreshAccounts()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(RefreshAccounts);
                return;
            }

            _accountsGrid.Items.Refresh();
        }

        private async void BtnExportClick(object sender, RoutedEventArgs e)
        {
            var accounts = Checker.Accounts.Where(a => a.State != Account.Result.Unchecked).ToList();

            if (!accounts.Any())
            {
                return;
            }

            var sfd = new SaveFileDialog
            {
                FileName = "output",
                Filter = "Text file (*.txt)|*.txt"
            };

            if (sfd.ShowDialog() == false)
            {
                return;
            }

            var settings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "No",
                FirstAuxiliaryButtonText = "Cancel"
            };

            var exportErrors = false;

            if (Checker.Accounts.Any(a => a.State == Account.Result.Error))
            {
                var dialog =
                    await
                        this.ShowMessageAsync(
                            "Export", "Export errors?", MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary,
                            settings);

                if (dialog == MessageDialogResult.FirstAuxiliary)
                {
                    return;
                }

                exportErrors = dialog == MessageDialogResult.Affirmative;
            }


            Utils.ExportLogins(sfd.FileName, accounts, exportErrors);

            await this.ShowMessageAsync("Export", "All the accounts have been exported!");
        }

        private void CmEditClick(object sender, RoutedEventArgs e)
        {
            if (Checker.IsChecking)
            {
                this.ShowMessageAsync("Error", "You can't edit accounts while the checker is working.");
                return;
            }

            var account = _accountsGrid.SelectedItem as Account;

            if (AccountEdit.Instance != null || account == null)
            {
                return;
            }

            var w = new AccountEdit(account);
            w.ShowDialog();
        }

        private void CmCopyComboClick(object sender, RoutedEventArgs e)
        {
            if (_accountsGrid.SelectedItems.Count > 1)
            {
                var sb = new StringBuilder();
                foreach (Account a in _accountsGrid.SelectedItems)
                {
                    sb.Append(string.Format("{0}:{1}{2}", a.Username, a.Password, Environment.NewLine));
                }
                Clipboard.SetText(sb.ToString());

                this.ShowMessageAsync(
                    "Copy combo",
                    string.Format("Copied {0} combos to your clipboard.", _accountsGrid.SelectedItems.Count));

                return;
            }

            var account = _accountsGrid.SelectedItem as Account;

            if (account == null)
            {
                return;
            }

            Clipboard.SetText(string.Format("{0}:{1}", account.Username, account.Password));

            this.ShowMessageAsync("Copy combo", "Combo copied to clipboard!");
        }

        private async void CmMakeUncheckedClick(object sender, RoutedEventArgs e)
        {
            if (_accountsGrid.SelectedItems.Count > 1)
            {
                var c = 0;
                var uncheckSuccess = false;
                var toAll = false;
                var settings = new MetroDialogSettings
                {
                    AffirmativeButtonText = "No",
                    NegativeButtonText = "Yes",
                    FirstAuxiliaryButtonText = "No to All",
                    SecondAuxiliaryButtonText = "Yes to All"
                };

                foreach (Account acc in _accountsGrid.SelectedItems)
                {
                    if (acc.State == Account.Result.Success)
                    {
                        if (!toAll)
                        {
                            var confirm =
                                await
                                    this.ShowMessageAsync(
                                        "Make Unchecked",
                                        string.Format(
                                            "This account ({0}) was successfully checked, are you sure that you wanna make it unchecked?",
                                            acc.Username), MessageDialogStyle.AffirmativeAndNegativeAndDoubleAuxiliary,
                                        settings);

                            switch (confirm)
                            {
                                case MessageDialogResult.Affirmative:
                                    continue;
                                case MessageDialogResult.FirstAuxiliary:
                                    toAll = true;
                                    continue;
                                case MessageDialogResult.SecondAuxiliary:
                                    toAll = true;
                                    uncheckSuccess = true;
                                    break;
                            }
                        }
                        else
                        {
                            if (!uncheckSuccess)
                            {
                                continue;
                            }
                        }
                    }
                    c++;
                    acc.State = Account.Result.Unchecked;
                }

                RefreshAccounts();
                MainWindow.Instance.UpdateControls();

                await
                    this.ShowMessageAsync(
                        "Make Unchecked",
                        string.Format("Account state has been changed to Unchecked on {0} accounts", c));

                return;
            }

            var account = _accountsGrid.SelectedItem as Account;

            if (account == null)
            {
                return;
            }

            if (account.State == Account.Result.Unchecked)
            {
                await this.ShowMessageAsync("Make Unchecked", "This account has not been checked yet.");
                return;
            }

            if (account.State == Account.Result.Success)
            {
                var confirm =
                    await
                        this.ShowMessageAsync(
                            "Make Unchecked",
                            "This account was successfully checked, are you sure that you wanna make it unchecked?",
                            MessageDialogStyle.AffirmativeAndNegative);

                if (confirm == MessageDialogResult.Negative)
                {
                    return;
                }
            }

            account.State = Account.Result.Unchecked;
            RefreshAccounts();
            MainWindow.Instance.UpdateControls();
            await this.ShowMessageAsync("Make Unchecked", "Account state has been changed to Unchecked.");
        }

        private async void CmRemoveClick(object sender, RoutedEventArgs e)
        {
            MessageDialogResult confirm;
            if (_accountsGrid.SelectedItems.Count > 1)
            {
                confirm =
                    await
                        this.ShowMessageAsync(
                            "Remove",
                            string.Format(
                                "Are you sure that you wanna remove {0} accounts?", _accountsGrid.SelectedItems.Count),
                            MessageDialogStyle.AffirmativeAndNegative);
            }
            else
            {
                confirm =
                    await this.ShowMessageAsync("Remove", "Are you sure?", MessageDialogStyle.AffirmativeAndNegative);
            }

            if (confirm == MessageDialogResult.Negative)
            {
                return;
            }

            if (_accountsGrid.SelectedItems.Count > 1)
            {
                var count = 0;
                foreach (Account a in _accountsGrid.SelectedItems)
                {
                    if (Checker.Accounts.Any(acc => acc.Username == a.Username))
                    {
                        Checker.Accounts.RemoveAll(acc => acc.Username == a.Username);
                        count++;
                    }
                }

                RefreshAccounts();
                MainWindow.Instance.UpdateControls();
                await this.ShowMessageAsync("Remove", string.Format("Removed {0} accounts!", count));
                return;
            }

            var account = _accountsGrid.SelectedItem as Account;

            if (account == null)
            {
                return;
            }

            if (Checker.Accounts.Contains(account))
            {
                Checker.Accounts.Remove(account);
            }

            RefreshAccounts();
            MainWindow.Instance.UpdateControls();
            await this.ShowMessageAsync("Remove", "Account removed!");
        }
    }
}