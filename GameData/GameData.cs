using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yafes.Data  // GameData yerine Data kullanın
{
    public class GameData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ImageName { get; set; } = string.Empty;
        public string SetupPath { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public bool IsInstalled { get; set; }
        public DateTime LastPlayed { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
