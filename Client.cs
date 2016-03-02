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
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LoLAccountChecker.Classes;
using PVPNetConnect;

#endregion

namespace LoLAccountChecker
{
    public class Client
    {
        private PVPNetConnection pvpnet;
        public Account Data;

        public TaskCompletionSource<bool> IsCompleted;

        public Client(Region region, string username, string password)
        {
            Data = new Account
            {
                Username = username,
                Password = password,
                Region = region,
                Refunds = 0
            };

            IsCompleted = new TaskCompletionSource<bool>();

            pvpnet = new PVPNetConnection();
            pvpnet.OnLogin += OnLogin;
            pvpnet.OnError += OnError;

            pvpnet.Connect(username, password, region, Settings.Config.ClientVersion);
        }

        public void Disconnect()
        {
            if (!pvpnet.IsConnected())
            {
                return;
            }
            pvpnet.Disconnect();
        }

        private void OnLogin(object sender, string username, string ipAddress)
        {
            GetData();
        }

        private void OnError(object sender, Error error)
        {
            Data.ErrorMessage = error.Message;
            Data.State = Account.Result.Error;
            IsCompleted.TrySetResult(true);
        }

        public async void GetData()
        {
            try
            {
                var loginPacket = await pvpnet.GetLoginDataPacketForUser();

                if (loginPacket.AllSummonerData == null)
                {
                    Data.ErrorMessage = "Summoner not created.";
                    Data.State = Account.Result.Error;

                    IsCompleted.TrySetResult(true);
                    return;
                }

                await GetChampions();
                await GetStoreData();
                await GetRunes(loginPacket.AllSummonerData.Summoner.SumId);

                Data.Summoner = loginPacket.AllSummonerData.Summoner.Name;
                Data.SummonerId = (int)loginPacket.AllSummonerData.Summoner.SumId;
                Data.Level = (int) loginPacket.AllSummonerData.SummonerLevel.Level;
                Data.RpBalance = (int) loginPacket.RpBalance;
                Data.IpBalance = (int) loginPacket.IpBalance;
                Data.RunePages = loginPacket.AllSummonerData.SpellBook.BookPages.Count;

                if (loginPacket.EmailStatus != null)
                {
                    var emailStatus = loginPacket.EmailStatus.Replace('_', ' ');
                    Data.EmailStatus = char.ToUpper(emailStatus[0]) + emailStatus.Substring(1);
                }
                else
                {
                    Data.EmailStatus = "Unknown";
                }

                if (Data.Level == 30)
                {
                    var myLeagues = await pvpnet.GetMyLeaguePositions();
                    var soloqLeague = myLeagues.SummonerLeagues.FirstOrDefault(l => l.QueueType == "RANKED_SOLO_5x5");
                    Data.SoloQRank = soloqLeague != null
                        ? string.Format(
                            "{0}{1} {2}", char.ToUpper(soloqLeague.Tier[0]), soloqLeague.Tier.Substring(1).ToLower(),
                            soloqLeague.Rank)
                        : "Unranked";
                }
                else
                {
                    Data.SoloQRank = "Unranked";
                }

                Data.LastPlay = loginPacket.AllSummonerData.Summoner.LastGameDate;

                //var recentGames = await pvpnet.GetRecentGames(loginPacket.AllSummonerData.Summoner.AcctId);
                //var lastGame = recentGames.GameStatistics.FirstOrDefault();

                //if (lastGame != null)
                //{
                //Data.LastPlay = lastGame.CreateDate;
                //}

                Data.CheckedTime = DateTime.Now;
                Data.State = Account.Result.Success;
            }
            catch (Exception e)
            {
                Utils.ExportException(e);
                Data.ErrorMessage = string.Format("Exception found: {0}", e.Message);
                Data.State = Account.Result.Error;
            }

            IsCompleted.TrySetResult(true);
        }

        private async Task GetStoreData()
        {
            Data.Transfers = new List<TransferData>();

            Regex regexTransfers = new Regex("\\\'account_transfer(.*)\\\'\\)", RegexOptions.Multiline);
            Regex regexTransferData = new Regex("rp_cost\\\":(.*?),(?:.*)name\\\":\\\"(.*?)\\\"");
            Regex regexRefunds = new Regex("credit_counter\\\">(\\d[1-3]?)<");
            Regex regexRegion = new Regex("\\.(.*?)\\.");

            var storeUrl = await pvpnet.GetStoreUrl();

            var region = regexRegion.Match(storeUrl).Groups[1];

            string storeUrlMisc = string.Format("https://store.{0}.lol.riotgames.com/store/tabs/view/misc", region);
            string storeUrlHist = string.Format("https://store.{0}.lol.riotgames.com/store/accounts/rental_history", region);

            CookieContainer cookies = new CookieContainer();

            await Utils.GetHtmlResponse(storeUrl, cookies);

            string miscHtml = await Utils.GetHtmlResponse(storeUrlMisc, cookies);
            string histHtml = await Utils.GetHtmlResponse(storeUrlHist, cookies);

            foreach (Match match in regexTransfers.Matches(miscHtml))
            {
                var data = regexTransferData.Matches(match.Value);

                var transfer = new TransferData
                {
                    Price = int.Parse(data[0].Groups[1].Value.Replace("\"", "")),
                    Name = data[0].Groups[2].Value
                };

                Data.Transfers.Add(transfer);
            }

            if (regexRefunds.IsMatch(histHtml))
            {
                Data.Refunds = int.Parse(regexRefunds.Match(histHtml).Groups[1].Value);
            }
        }

        private async Task GetChampions()
        {
            var champions = await pvpnet.GetAvailableChampions();

            Data.ChampionList = new List<ChampionData>();
            Data.SkinList = new List<SkinData>();

            foreach (var champion in champions)
            {
                Champion championData = LeagueData.Champions.FirstOrDefault(c => c.Id == champion.ChampionId);

                if (championData == null)
                {
                    continue;
                }

                if (champion.Owned)
                {
                    Data.ChampionList.Add(new ChampionData
                    {
                        Id = championData.Id,
                        Name = championData.Name,
                        PurchaseDate = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(champion.PurchaseDate / 1000d)),
                    });
                }

                foreach (var skin in champion.ChampionSkins.Where(skin => skin.Owned))
                {
                    Skin skinData = championData.Skins.FirstOrDefault(s => s.Id == skin.SkinId);

                    if (skinData == null)
                    {
                        continue;
                    }

                    Data.SkinList.Add(new SkinData
                    {
                        Id = skinData.Id,
                        Name = skinData.Name,
                        StillObtainable = skin.StillObtainable,
                        ChampionId = championData.Id
                    });
                }

                foreach (ChampionData champ in Data.ChampionList)
                {
                    SkinData skin = Data.SkinList.FirstOrDefault(c => c.ChampionId == champ.Id);

                    champ.HasSkin = (skin != null) ? true : false;
                }
            }
        }

        private async Task GetRunes(double summmonerId)
        {
            Data.Runes = new List<RuneData>();

            var runes = await pvpnet.GetSummonerRuneInventory(summmonerId);

            if(runes == null)
            {
                return;
            }

            foreach (var rune in runes.SummonerRunes)
            {
                var runeData = LeagueData.Runes.FirstOrDefault(r => r.Id == rune.RuneId);

                if (runeData == null)
                {
                    continue;
                }

                Data.Runes.Add(new RuneData
                {
                    Name = runeData.Name,
                    Description = runeData.Description,
                    Quantity = rune.Quantity,
                    Tier = runeData.Tier
                });
            }
        }
    }
}