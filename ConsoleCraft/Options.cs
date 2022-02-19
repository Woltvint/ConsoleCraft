using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleCraft
{
    class Options
    {
        public int pigs { get; set; } = 10;
        public int trees { get; set; } = 4;
        public int renderDistance { get; set; } = 20;
        public int mapSize { get; set; } = 50;
        public int screenWidth { get; set; } = 50;
        public int screenHeight { get; set; } = 50;
        public bool windowAutoSize { get; set; } = true;
    }
}
