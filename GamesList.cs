using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StandaloneGeneratorV3
{
    class Game
    {
        public string Id { get; set; }
        public string Name { get; set; }
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
        public static List<Game> Load()
        {
            return new List<Game>()
            {
                new Game("th06", "Embodiment of Scarlet Devil"),
                new Game("th18", "Unconnected Marketeers")
            };
        }
    }
}
