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

#endregion

namespace LoLAccountChecker.Views
{
    public partial class AccountEdit
    {
        public static AccountEdit Instance;

        private Account _account;

        public AccountEdit(Account account = null)
        {
            InitializeComponent();

            RegionBox.ItemsSource = Enum.GetValues(typeof(Region)).Cast<Region>();
            RegionBox.SelectedItem = Settings.Config.SelectedRegion;

            Instance = this;

            Loaded += (o, a) => UpdateWindow();
            Closed += (o, a) => Instance = null;

            if (account == null)
            {
                return;
            }

            _account = account;

            UsernameBox.Text = _account.Username;
            PasswordBox.Password = _account.Password;
            PasswordBoxText.Text = _account.Password;
            RegionBox.SelectedItem = _account.Region;
        }

        public void UpdateWindow()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(UpdateWindow);
                return;
            }

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
        }

        private void BtnSaveClick(object sender, RoutedEventArgs e)
        {
            var password = Settings.Config.ShowPasswords ? PasswordBoxText.Text : PasswordBox.Password;

            if (string.IsNullOrEmpty(UsernameBox.Text) || string.IsNullOrWhiteSpace(password))
            {
                ResultLabel.Content = "Insert a username and password!";
                return;
            }

            if (_account != null)
            {
                if (Checker.IsChecking)
                {
                    ResultLabel.Content = "Stop the checker before saving.";
                    return;
                }

                if (_account.Username != UsernameBox.Text &&
                    Checker.Accounts.Any(a => a.Username.ToLower() == UsernameBox.Text.ToLower()))
                {
                    ResultLabel.Content = "Username already exists!";
                    return;
                }

                var account = Checker.Accounts.FirstOrDefault(a => a == _account);

                if (account != null)
                {
                    account.Username = UsernameBox.Text;
                    account.Password = password;
                    account.Region = (Region) RegionBox.SelectedItem;
                    _account = account;
                    ResultLabel.Content = "Account successfuly edited!";
                }
            }
            else
            {
                if (Checker.Accounts.Any(a => a.Username.ToLower() == UsernameBox.Text.ToLower()))
                {
                    ResultLabel.Content = "Username already exists!";
                    return;
                }

                var newAccount = new Account
                {
                    Username = UsernameBox.Text,
                    Password = password,
                    Region = (Region) RegionBox.SelectedItem
                };

                _account = newAccount;
                Checker.Accounts.Add(_account);

                ResultLabel.Content = "Account successfuly added!";
            }

            if (AccountsWindow.Instance != null)
            {
                AccountsWindow.Instance.RefreshAccounts();
            }

            MainWindow.Instance.UpdateControls();

            Settings.Config.SelectedRegion = (Region) RegionBox.SelectedItem;
        }
    }
}