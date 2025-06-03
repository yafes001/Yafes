using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yafes.Data;  // ❗ Eklendi

namespace Yafes.GameData
{
    public class GamesDatabase
    {
        public List<GameData> Games { get; set; } = new List<GameData>();  // ❗ Düzeltildi
        public string Version { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }
}