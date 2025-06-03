using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yafes.GameData
{
    public class GamesDatabase
    {
        public List<Yafes.Models.GameData> Games { get; set; } = new List<Yafes.Models.GameData>();  // ❗ EXPLICIT
        public string Version { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }
}