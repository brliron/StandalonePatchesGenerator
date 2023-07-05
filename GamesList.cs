using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace StandaloneGeneratorV3
{
    class Game
    {
        public string Id { get; set; }
        public string Name { get; set; }
        [JsonIgnore]
        public string ImagePath { get => AppContext.BaseDirectory + @".\res\icon\" + Id + ".png"; }
        private BitmapImage _image;
        [JsonIgnore]
        public BitmapImage Image { get {
                if (_image == null)
                {
                    _image = new BitmapImage();
                    _image.BeginInit();
                    _image.UriSource = new Uri(ImagePath);
                    _image.CacheOption = BitmapCacheOption.OnLoad;
                    _image.EndInit();
                }
                return _image;
            } }
        [JsonIgnore]
        public bool IsSelected { get; set; }
        public Game(string id, string name)
        {
            Id = id;
            Name = name;
            IsSelected = false;
        }
    }
    class GamesList
    {
        public static readonly string BaseURL = "https://www.thpatch.net/";
        public static List<Game> Load()
        {
            try
            {
                return JsonSerializer.Deserialize<List<Game>>(File.ReadAllText("res\\games_list.js"));
            }
            catch
            {
                return new List<Game>();
            }
        }

        private static async Task<Game> GameDomToObject(WebClient webClient, HtmlNode gameDom)
        {
            string id;
            string name;
            string iconUrl;

            HtmlNode titleNode = gameDom.SelectSingleNode("td[1]/a[2]");
            id = titleNode.Attributes["title"].Value.ToLower();
            name = titleNode.InnerText;
            var game = new Game(id, name);

            iconUrl = GamesList.BaseURL + gameDom.SelectSingleNode(".//a[@class='image']/img").Attributes["src"].Value;
            if (!Directory.Exists("res\\icon"))
                Directory.CreateDirectory("res\\icon");
            await webClient.DownloadFileTaskAsync(iconUrl, game.ImagePath);

            return game;
        }

        public static async Task<List<Game>> Reload()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            WebClient webClient = new WebClient();
            string mainPage;
            try
            {
                mainPage = await webClient.DownloadStringTaskAsync(GamesList.BaseURL);
            }
            catch (Exception e)
            {
                ThcrapDll.log_print("Could not download games list: " + e.Message + "\n");
                return null;
            }
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(mainPage);

            var domGamesTable = doc.DocumentNode.SelectSingleNode("//table[@class='progtable']");
            var domGamesList = domGamesTable.SelectNodes("tbody/tr").Skip(1);

            var gamesList = new List<Game>();
            foreach (var domGame in domGamesList)
                gamesList.Add(await GameDomToObject(webClient, domGame));

            string gamesListJs = JsonSerializer.Serialize(gamesList, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("res\\games_list.js", gamesListJs);

            return gamesList;
        }
    }
}
