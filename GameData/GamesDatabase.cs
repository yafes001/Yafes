using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yafes.GameData
{
    internal class GamesDatabase
    {
        public List<GameData> Games { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Version { get; set; }
    }
}
