using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helpers;

namespace MapMakerConsole
{
    public class MapBuilder
    {
        private LevelBuilder[] Levels;

        public MapBuilder(int NumLevels, int LevelWidth, int LevelHeight)
        {
            Levels = new LevelBuilder[NumLevels];
            for(int i = 0; i < NumLevels; i++)
                Levels[i] = new LevelBuilder(LevelWidth, LevelHeight);
        }

        public string RenderAsString()
        {
            return "\r\n".Combine(Levels.Reverse().Select(lev => lev.RenderAsString()).ToArray());
        }

        public void PrintToConsole()
        {
            //Console.SetCursorPosition(0, 0);
            Console.WriteLine(RenderAsString());
        }
    }
}
