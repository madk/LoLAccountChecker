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

using System.Diagnostics;
using System.Windows;
using LoLAccountChecker.Classes;

#endregion

namespace LoLAccountChecker.Views
{
    public partial class AccountWindow
    {
        public AccountWindow(Account account)
        {
            InitializeComponent();

            Title = string.Format("{0} - View account", account.Username);

            if (account.ChampionList != null)
            {
                ChampionsGrid.ItemsSource = account.ChampionList;
            }

            if (account.SkinList != null)
            {
                SkinsGrid.ItemsSource = account.SkinList;
            }

            if (account.Runes != null)
            {
                RunesGrid.ItemsSource = account.Runes;
            }

            if (account.Transfers != null)
            {
                TransfersGrid.ItemsSource = account.Transfers;
            }
        }

        private void CmViewModel(object sender, RoutedEventArgs e)
        {
            var selectedSkin = SkinsGrid.SelectedItem as SkinData;

            if (selectedSkin == null)
            {
                return;
            }

            Process.Start(
                string.Format(
                    "http://www.lolking.net/models/?champion={0}&skin={1}", selectedSkin.ChampionId,
                    selectedSkin.Skin.Number - 1));
        }
    }
}