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
        private int LevelWidth, LevelHeight;

        public MapBuilder(int NumLevels, int LevelWidth, int LevelHeight)
        {
            Levels = new LevelBuilder[NumLevels];
            for(int i = 0; i < NumLevels; i++)
                Levels[i] = new LevelBuilder(LevelWidth, LevelHeight);

            this.LevelWidth = LevelWidth;
            this.LevelHeight = LevelHeight;
        }

        public string RenderAsString()
        {
            string[][] LevelStrs =
            Levels.Select(lvl => lvl.RenderAsStrings()).ToArray();
            return
                "\r\n".Combine(
                    Enumerable.Range(0, LevelHeight)
                        .Select(i => "".Combine(LevelStrs.Select(lvl_str => lvl_str[i]).ToArray()))
                        .ToArray());
        }

        public bool this[int x, int y]
        {
            get
            {
                int lvl = x/LevelWidth;
                x = x%LevelWidth;
                return Levels[lvl][y, x];
            }
            set
            {
                int lvl = x / LevelWidth;
                x = x % LevelWidth;
                Levels[lvl][y, x] = value;
            }
        }

        public void PrintToConsole()
        {
            //Console.SetCursorPosition(0, 0);
            Console.WriteLine(RenderAsString());
        }
    }
}
