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
using Newtonsoft.Json;
using PVPNetConnect;

#endregion

namespace LoLAccountChecker.Classes
{
    public class Account
    {
        public enum Result
        {
            Unchecked,
            Success,
            Error
        }

        public string Username { get; set; }
        public string Password { get; set; }
        public string Summoner { get; set; }
        public int Level { get; set; }
        public string EmailStatus { get; set; }
        public int RpBalance { get; set; }
        public int IpBalance { get; set; }
        public int RunePages { get; set; }
        public int Refunds { get; set; }
        public string SoloQRank { get; set; }
        public DateTime LastPlay { get; set; }
        public DateTime CheckedTime { get; set; }
        public List<ChampionData> ChampionList { get; set; }
        public List<SkinData> SkinList { get; set; }
        public List<RuneData> Runes { get; set; }
        public List<TransferData> Transfers { get; set; }
        public string ErrorMessage { get; set; }
        public Result State { get; set; }
        public Region Region { get; set; }

        [JsonIgnore]
        public int Champions
        {
            get { return ChampionList != null ? ChampionList.Count : 0; }
        }

        [JsonIgnore]
        public int Skins
        {
            get { return SkinList != null ? SkinList.Count : 0; }
        }

        [JsonIgnore]
        public string PasswordDisplay
        {
            get
            {
                if (Settings.Config.ShowPasswords)
                {
                    return Password;
                }

                return "••••••••";
            }
        }

        [JsonIgnore]
        public string StateDisplay
        {
            get
            {
                switch (State)
                {
                    case Result.Success:
                        return "Successfully Checked";

                    case Result.Unchecked:
                        return "Unchecked";

                    case Result.Error:
                        return ErrorMessage;

                    default:
                        return string.Empty;
                }
            }
        }
    }
}