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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LoLAccountChecker.Views;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;

#endregion

namespace LoLAccountChecker.Classes
{
    internal static class LeagueData
    {
        private static List<string> _files;

        static LeagueData()
        {
            _files = new List<string>
            {
                "Champions.json",
                "Runes.json"
            };

            for (int i = 0; i <= _files.Count - 1; i++)
            {
                _files[i] = Path.Combine(Directory.GetCurrentDirectory(), "League", _files[i]);
            }
        }

        public static List<Champion> Champions { get; private set; }
        public static List<Rune> Runes { get; private set; }

        public static async Task Load()
        {
            var b = _files.Where(f => File.Exists(f));

            if (!b.Any())
            {
                MessageDialogResult dialog = await MainWindow.Instance.ShowMessageAsync("Error", "Missing data files.");
                MainWindow.Instance.Close();
                return;
            }

            using (StreamReader sr = new StreamReader(_files[0]))
            {
                Champions = JsonConvert.DeserializeObject<List<Champion>>(sr.ReadToEnd());
            }

            using (StreamReader sr = new StreamReader(_files[1]))
            {
                Runes = JsonConvert.DeserializeObject<List<Rune>>(sr.ReadToEnd());
            }
        }

        public static Champion GetChampion(int championId)
        {
            return Champions.FirstOrDefault(c => c.Id == championId);
        }

        public static Skin GetSkin(Champion champion, int id)
        {
            return champion.Skins.FirstOrDefault(s => s.Id == id);
        }

        public static string GetChampionImagePath(int championId)
        {
            string champStrID = Champions.FirstOrDefault(c => c.Id == championId).StrId;

            if (champStrID == null)
            {
                return null;
            }
            
            string imagesDir = Path.Combine(Directory.GetCurrentDirectory(), "League", "images");
            
            string imagePath = Path.Combine(imagesDir, string.Format("{0}.png", champStrID));

            if (!File.Exists(imagePath))
            {
                return null;
            }

            return imagePath;
        }
    }
}