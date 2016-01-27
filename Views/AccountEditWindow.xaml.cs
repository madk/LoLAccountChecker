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
using System.Linq;
using System.Windows;
using LoLAccountChecker.Classes;
using PVPNetConnect;
using System.Collections.Generic;

#endregion

namespace LoLAccountChecker.Views
{
    public partial class AccountEdit
    {
        private List<Account> _accounts;

        public AccountEdit(List<Account> accounts = null)
        {
            InitializeComponent();

            RegionBox.ItemsSource = Enum.GetValues(typeof(Region)).Cast<Region>();
            RegionBox.SelectedItem = Settings.Config.SelectedRegion;

            this._accounts = accounts;

            if (Settings.Config.ShowPasswords)
            {
                PasswordBoxText.Text = PasswordBox.Password;
                PasswordBoxText.Visibility = Visibility.Visible;
                PasswordBox.Visibility = Visibility.Hidden;
            }
            else
            {
                PasswordBox.Password = PasswordBoxText.Text;
                PasswordBoxText.Visibility = Visibility.Hidden;
                PasswordBox.Visibility = Visibility.Visible;
            }

            if (this._accounts != null)
            {
                if (this._accounts.Count == 1)
                {
                    UsernameBox.Text = _accounts[0].Username;
                    PasswordBox.Password = _accounts[0].Password;
                    PasswordBoxText.Text = _accounts[0].Password;
                    RegionBox.SelectedItem = _accounts[0].Region;
                }
                else
                {
                    UsernameLabel.Visibility = Visibility.Collapsed;
                    UsernameBox.Visibility = Visibility.Collapsed;

                    PasswordLabel.Visibility = Visibility.Collapsed;
                    PasswordBox.Visibility = Visibility.Collapsed;
                    PasswordBoxText.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void BtnSaveClick(object sender, RoutedEventArgs e)
        {
            if (Checker.IsChecking)
            {
                ResultLabel.Content = "Stop the checker before saving.";
                return;
            }

            if ((this._accounts != null) && (this._accounts.Count > 1))
            {
                foreach (Account acc in _accounts)
                {
                    Account account = Checker.Accounts.FirstOrDefault(a => a == acc);
                    Region newRegion = (Region)RegionBox.SelectedItem;
                    if ((account != null) && (account.Region != newRegion))
                    {
                        account.Region = newRegion;
                    }
                }

                this.RefreshControls();
                this.Close();
            }

            string password = Settings.Config.ShowPasswords ? PasswordBoxText.Text : PasswordBox.Password;

            if (string.IsNullOrEmpty(UsernameBox.Text) || string.IsNullOrWhiteSpace(password))
            {
                ResultLabel.Content = "Insert a username and password!";
                return;
            }

            if (this._accounts == null)
            {
                if (Checker.Accounts.Any(a => a.Username.ToLower() == UsernameBox.Text.ToLower()))
                {
                    ResultLabel.Content = "Username already exists!";
                    return;
                }

                Account newAccount = new Account
                {
                    Username = UsernameBox.Text,
                    Password = password,
                    Region = (Region)RegionBox.SelectedItem
                };

                Checker.Accounts.Add(newAccount);

                ResultLabel.Content = "Account successfuly added!";

                UsernameBox.Text = string.Empty;
                PasswordBoxText.Text = string.Empty;
                PasswordBox.Password = string.Empty;
            }
            else
            {
                if (_accounts[0].Username != UsernameBox.Text &&
                    Checker.Accounts.Any(a => a.Username.ToLower() == UsernameBox.Text.ToLower()))
                {
                    ResultLabel.Content = "Username already exists!";
                    return;
                }

                Account account = Checker.Accounts.FirstOrDefault(a => a == _accounts[0]);

                if (account != null)
                {
                    account.Username = UsernameBox.Text;
                    account.Password = password;
                    account.Region = (Region)RegionBox.SelectedItem;
                    _accounts[0] = account;
                    ResultLabel.Content = "Account successfuly edited!";
                }
            }

            this.RefreshControls();
        }

        private void RefreshControls()
        {
            if (AccountsWindow.Instance != null)
            {
                AccountsWindow.Instance.UpdateControls();
            }

            MainWindow.Instance.UpdateControls();

            Settings.Config.SelectedRegion = (Region)RegionBox.SelectedItem;
        }
    }
}