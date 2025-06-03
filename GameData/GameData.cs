using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yafes.GameData
{
    internal class GameData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ImageName { get; set; }
        public string SetupPath { get; set; }
        public string Category { get; set; }
        public string Size { get; set; }
        public bool IsInstalled { get; set; }
        public DateTime LastPlayed { get; set; }
        public string Description { get; set; }
    }
}
