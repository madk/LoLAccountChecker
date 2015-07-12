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
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LoLAccountChecker.Views;
using MahApps.Metro.Controls.Dialogs;
using PVPNetConnect;

#endregion

namespace LoLAccountChecker.Classes
{
    internal class Utils
    {
        public static List<Account> GetLogins(string file)
        {
            var logins = new List<Account>();

            var sr = new StreamReader(file);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                var accountData = line.Split(new[] { ':' });

                if (accountData.Count() < 2)
                {
                    continue;
                }

                Region region;
                if (accountData.Count() < 3 || !Enum.TryParse(accountData[2], true, out region))
                {
                    region = Settings.Config.SelectedRegion;
                }

                var loginData = new Account
                {
                    Username = accountData[0],
                    Password = accountData[1],
                    State = Account.Result.Unchecked,
                    Region = region
                };

                logins.Add(loginData);
            }

            return logins;
        }

        public static void ExportLogins(string file, List<Account> accounts, bool exportErrors)
        {
            using (var sw = new StreamWriter(file))
            {
                if (!exportErrors)
                {
                    accounts = accounts.Where(a => a.State == Account.Result.Success).ToList();
                }

                foreach (var account in accounts)
                {
                    sw.WriteLine("{0}:{1}", account.Username, account.Password);
                }
            }
        }

        public static void ExportException(Exception e)
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs");

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var file = string.Format("crash_{0:dd-MM-yyyy_HH-mm-ss}.txt", DateTime.Now);

            using (var sw = new StreamWriter(Path.Combine(dir, file)))
            {
                sw.WriteLine(e.ToString());
            }
        }

        public static async Task UpdateClientVersion()
        {
            using (var wc = new WebClient())
            {
                try
                {
                    var clientVersion =
                        wc.DownloadString(
                            "https://raw.githubusercontent.com/madk/LoLAccountChecker/master/League/Client.version");

                    if (Settings.Config.ClientVersion == null)
                    {
                        Settings.Config.ClientVersion = clientVersion;
                        return;
                    }

                    if (clientVersion == Settings.Config.ClientVersion)
                    {
                        return;
                    }

                    var result =
                        await
                            MainWindow.Instance.ShowMessageAsync(
                                "Client version outdated",
                                "The client version of League of Legends looks different, do you wanna update it?",
                                MessageDialogStyle.AffirmativeAndNegative);

                    if (result == MessageDialogResult.Affirmative)
                    {
                        Settings.Config.ClientVersion = clientVersion;
                    }
                }
                catch
                {
                    // ignore
                }
            }
        }

        public static string GetHtmlResponse(string url, CookieContainer cookieContainer = null)
        {
            var wr = (HttpWebRequest) WebRequest.Create(url);

            if (cookieContainer != null)
            {
                wr.CookieContainer = cookieContainer;
            }

            try
            {
                string html;

                using (var resp = wr.GetResponse())
                {
                    using (var sr = new StreamReader(resp.GetResponseStream()))
                    {
                        html = sr.ReadToEnd();
                    }
                }

                return html;
            }
            catch (WebException e)
            {
                using (var response = e.Response)
                {
                    var resp = (HttpWebResponse) response;
                }

                return string.Empty;
            }
        }
    }
}