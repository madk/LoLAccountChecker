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

using System.IO;
using Newtonsoft.Json;
using PVPNetConnect;

#endregion

namespace LoLAccountChecker
{
    internal class Settings
    {
        private static string _file;

        public static Settings Config;

        static Settings()
        {
            _file = "settings.json";

            if (!File.Exists(_file))
            {
                Config = new Settings
                {
                    ShowPasswords = true,
                    SelectedRegion = Region.NA
                };
                Save();
                return;
            }

            Load();
        }

        public bool ShowPasswords { get; set; }
        public Region SelectedRegion { get; set; }
        public string ClientVersion { get; set; }

        public static void Save()
        {
            using (var sw = new StreamWriter(_file))
            {
                sw.Write(JsonConvert.SerializeObject(Config, Formatting.Indented));
            }
        }

        public static void Load()
        {
            using (var sr = new StreamReader(_file))
            {
                Config = JsonConvert.DeserializeObject<Settings>(sr.ReadToEnd());
            }
        }
    }
}