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
        public PVPNetConnection Connection;
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

            Connection = new PVPNetConnection();
            Connection.OnLogin += OnLogin;
            Connection.OnError += OnError;

            Connection.Connect(username, password, region, Settings.Config.ClientVersion);
        }

        public void Disconnect()
        {
            if (!Connection.IsConnected())
            {
                return;
            }

            Connection.Disconnect();
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
                var loginPacket = await Connection.GetLoginDataPacketForUser();

                if (loginPacket.AllSummonerData == null)
                {
                    Data.ErrorMessage = "Summoner not created.";
                    Data.State = Account.Result.Error;

                    IsCompleted.TrySetResult(true);
                    return;
                }

                await GetChampions();
                await GetStoreData();

                GetRunes(loginPacket.AllSummonerData.Summoner.SumId);

                Data.Summoner = loginPacket.AllSummonerData.Summoner.Name;
                Data.Level = (int) loginPacket.AllSummonerData.SummonerLevel.Level;
                Data.RpBalance = (int) loginPacket.RpBalance;
                Data.IpBalance = (int) loginPacket.IpBalance;
                Data.RunePages = loginPacket.AllSummonerData.SpellBook.BookPages.Count;

                if (loginPacket.EmailStatus != null)
                {
                    var emailStatus = loginPacket.EmailStatus.Replace('_', ' ');
                    Data.EmailStatus = Char.ToUpper(emailStatus[0]) + emailStatus.Substring(1);
                }
                else
                {
                    Data.EmailStatus = "Unknown";
                }

                // Leagues
                if (Data.Level == 30)
                {
                    var myLeagues = await Connection.GetMyLeaguePositions();
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

                // Last Play
                var recentGames = await Connection.GetRecentGames(loginPacket.AllSummonerData.Summoner.AcctId);
                var lastGame = recentGames.GameStatistics.FirstOrDefault();

                if (lastGame != null)
                {
                    Data.LastPlay = lastGame.CreateDate;
                }

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

            // Regex
            var regexTransfers = new Regex("\\\'account_transfer(.*)\\\'\\)", RegexOptions.Multiline);
            var regexTransferData = new Regex("rp_cost\\\":(.*?),(?:.*)name\\\":\\\"(.*?)\\\"");
            var regexRefunds = new Regex("credit_counter\\\">(\\d[1-3]?)<");
            var regexRegion = new Regex("\\.(.*?)\\.");

            var storeUrl = await Connection.GetStoreUrl();

            var region = regexRegion.Match(storeUrl).Groups[1];

            var storeUrlMisc = string.Format("https://store.{0}.lol.riotgames.com/store/tabs/view/misc", region);
            var storeUrlHist = string.Format(
                "https://store.{0}.lol.riotgames.com/store/accounts/rental_history", region);

            var cookies = new CookieContainer();

            Utils.GetHtmlResponse(storeUrl, cookies);

            var miscHtml = Utils.GetHtmlResponse(storeUrlMisc, cookies);
            var histHtml = Utils.GetHtmlResponse(storeUrlHist, cookies);

            // Transfers
            foreach (Match match in regexTransfers.Matches(miscHtml))
            {
                var data = regexTransferData.Matches(match.Value);

                var transfer = new TransferData
                {
                    Price = Int32.Parse(data[0].Groups[1].Value.Replace("\"", "")),
                    Name = data[0].Groups[2].Value
                };

                Data.Transfers.Add(transfer);
            }

            // Refunds credits
            if (regexRefunds.IsMatch(histHtml))
            {
                Data.Refunds = Int32.Parse(regexRefunds.Match(histHtml).Groups[1].Value);
            }
        }

        private async Task GetChampions()
        {
            var champions = await Connection.GetAvailableChampions();

            Data.ChampionList = new List<ChampionData>();
            Data.SkinList = new List<SkinData>();

            foreach (var champion in champions)
            {
                var championData = LeagueData.Champions.FirstOrDefault(c => c.Id == champion.ChampionId);

                if (championData == null)
                {
                    continue;
                }

                if (champion.Owned)
                {
                    Data.ChampionList.Add(
                        new ChampionData
                        {
                            Id = championData.Id,
                            Name = championData.Name,
                            PurchaseDate =
                                new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(
                                    Math.Round(champion.PurchaseDate / 1000d))
                        });
                }

                foreach (var skin in champion.ChampionSkins.Where(skin => skin.Owned))
                {
                    var skinData = championData.Skins.FirstOrDefault(s => s.Id == skin.SkinId);

                    if (skinData == null)
                    {
                        continue;
                    }

                    Data.SkinList.Add(
                        new SkinData
                        {
                            Id = skinData.Id,
                            Name = skinData.Name,
                            StillObtainable = skin.StillObtainable,
                            ChampionId = championData.Id
                        });
                }
            }
        }

        private async void GetRunes(double summmonerId)
        {
            Data.Runes = new List<RuneData>();

            var runes = await Connection.GetSummonerRuneInventory(summmonerId);
            if (runes != null)
            {
                foreach (var rune in runes.SummonerRunes)
                {
                    var runeData = LeagueData.Runes.FirstOrDefault(r => r.Id == rune.RuneId);

                    if (runeData == null)
                    {
                        continue;
                    }

                    var rn = new RuneData
                    {
                        Name = runeData.Name,
                        Description = runeData.Description,
                        Quantity = rune.Quantity,
                        Tier = runeData.Tier
                    };

                    Data.Runes.Add(rn);
                }
            }
        }
    }
}