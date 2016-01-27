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

using Newtonsoft.Json;
using System.Windows.Media.Imaging;

#endregion

namespace LoLAccountChecker.Classes
{
    public class SkinData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ChampionId { get; set; }
        public bool StillObtainable { get; set; }

        [JsonIgnore]
        public string Obtainable
        { 
            get { return this.StillObtainable == true ? "Yes" : "No"; }
        }

        [JsonIgnore]
        public Champion Champion
        {
            get { return LeagueData.GetChampion(this.ChampionId);}
        }

        [JsonIgnore]
        public Skin Skin
        {
            get { return LeagueData.GetSkin(this.Champion, this.Id); }
        }

        [JsonIgnore]
        public string ImageUrl
        {
            get
            {
                try
                {
                    string champId = this.Champion.StrId;
                    string skinId = this.Skin.Num.ToString();

                    string url = string.Format("http://ddragon.leagueoflegends.com/cdn/img/champion/loading/{0}_{1}.jpg", champId, skinId);

                    return url;
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}