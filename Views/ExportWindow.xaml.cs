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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using LoLAccountChecker.Classes;
using Microsoft.Win32;

#endregion

namespace LoLAccountChecker.Views
{
    public partial class ExportWindow
    {
        public static ExportWindow Instance;

        private readonly IEnumerable<Account> _accounts;

        public ExportWindow(IEnumerable<Account> accounts)
        {
            InitializeComponent();

            Instance = this;

            _accounts = accounts;

            if (File.Exists("ExportFormat.txt"))
            {
                using (var sr = new StreamReader("ExportFormat.txt"))
                {
                    FormatBox.Text = sr.ReadToEnd();
                }
            }
        }

        private void BtnSaveClick(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button))
                return;
            Button s = (Button)sender;
            var sfd = new SaveFileDialog();
            if (s.Name == "save")
            {

                if (sfd.ShowDialog() != true)
                {
                    return;
                }
            }

            using (var sw = new StreamWriter("ExportFormat.txt"))
            {
                sw.Write(FormatBox.Text);
            }

            var sb = new StringBuilder();

            foreach (var account in _accounts)
            {
                var format = FormatBox.Text;

                // Champion List
                var champListRegex = new Regex("\\[%CHAMPIONLIST%](.*?)\\[\\/%CHAMPIONLIST%]", RegexOptions.Singleline);

                if (champListRegex.IsMatch(format))
                {
                    var clSb = new StringBuilder();

                    var clFormat = champListRegex.Matches(format)[0];

                    foreach (var champion in account.ChampionList)
                    {
                        var cFormat = clFormat.Groups[1].Value;

                        if (cFormat.StartsWith("\n"))
                        {
                            cFormat = cFormat.Substring(1);
                        }

                        cFormat = cFormat.Replace("%ID%", champion.Id.ToString());
                        cFormat = cFormat.Replace("%NAME%", champion.Name);
                        cFormat = cFormat.Replace("%PUCHASEDATE%", champion.PurchaseDate.ToString());

                        clSb.Append(cFormat);
                    }

                    format = format.Replace(clFormat.Value, clSb.ToString());
                }

                // Skin List
                var skinListRegex = new Regex("\\[%SKINLIST%](.*?)\\[\\/%SKINLIST%]", RegexOptions.Singleline);

                if (skinListRegex.IsMatch(format))
                {
                    var slSb = new StringBuilder();

                    var slFormat = skinListRegex.Matches(format)[0];

                    foreach (var skin in account.SkinList)
                    {
                        var sFormat = slFormat.Groups[1].Value;

                        if (sFormat.StartsWith("\n"))
                        {
                            sFormat = sFormat.Substring(1);
                        }

                        sFormat = sFormat.Replace("%ID%", skin.Id.ToString());
                        sFormat = sFormat.Replace("%CHAMPION%", LeagueData.GetChampion(skin.ChampionId).Name);
                        sFormat = sFormat.Replace("%NAME%", skin.Name);

                        slSb.Append(sFormat);
                    }

                    format = format.Replace(slFormat.Value, slSb.ToString());
                }

                // Rune List
                var runeListRegex = new Regex("\\[%RUNELIST%](.*?)\\[\\/%RUNELIST%]", RegexOptions.Singleline);

                if (runeListRegex.IsMatch(format))
                {
                    var rlSb = new StringBuilder();

                    var rlFormat = skinListRegex.Matches(format)[0];

                    foreach (var rune in account.Runes)
                    {
                        var rFormat = rlFormat.Groups[1].Value;

                        if (rFormat.StartsWith("\n"))
                        {
                            rFormat = rFormat.Substring(1);
                        }

                        rFormat = rFormat.Replace("%NAME%", rune.Name);
                        rFormat = rFormat.Replace("%DESCRIPTION%", rune.Description);
                        rFormat = rFormat.Replace("%TIER%", rune.Tier.ToString());
                        rFormat = rFormat.Replace("%QUANTITY%", rune.Quantity.ToString());

                        rlSb.Append(rFormat);
                    }

                    format = format.Replace(rlFormat.Value, rlSb.ToString());
                }

                // Replace
                format = format.Replace("%USERNAME%", account.Username);
                format = format.Replace("%PASSWORD%", account.Password);
                format = format.Replace("%SUMMONERNAME%", account.Summoner);
                format = format.Replace("%LEVEL%", account.Level.ToString());
                format = format.Replace("%EMAILSTATUS%", account.EmailStatus);
                format = format.Replace("%RP%", account.RpBalance.ToString());
                format = format.Replace("%IP%", account.IpBalance.ToString());
                format = format.Replace("%CHAMPIONS%", account.Champions.ToString());
                format = format.Replace("%SKINS%", account.Skins.ToString());
                format = format.Replace("%RUNEPAGES%", account.RunePages.ToString());
                format = format.Replace("%REFUNDS%", account.Refunds.ToString());
                format = format.Replace("%REGION%", account.Region.ToString());
                format = format.Replace("%LASTPLAY%", account.LastPlay.ToString());
                format = format.Replace("%RANK%", account.SoloQRank);
                format = format.Replace("%CHECKTIME%", account.CheckedTime.ToString());


                sb.Append(format + Environment.NewLine);
            }

            if (s.Name == "save")
            {
                using (var sw = new StreamWriter(sfd.FileName))
                {
                    sw.Write(sb.ToString());
                }
            }
            else if (s.Name == "copy")
            {
                Clipboard.SetText(sb.ToString());
            }

            Close();
        }
    }
}