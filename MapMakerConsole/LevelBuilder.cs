using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;

namespace MapMakerConsole
{
    class LevelBuilder
    {
        private readonly Random rand = new Random();
        private List<Func<bool[][], bool[][]>> mapEditors = new List<Func<bool[][], bool[][]>>();
        private bool[][] map; // true = wall, false = floor
        public readonly int Width, Height;
        private const double INIT_COVERAGE = 0.45;
        private const int SMOOTH_WINDOW_SIZE = 3;
        private const int FILL_WINDOW_SIZE = 5;
        private const int WALL_POSITIVE_THRESH = 5;
        private const int FILL_NEGATIVE_THRESH = 1;
        private const int MIN_ENT_EXIT_WIDTH = 3;
        private const double ENT_EXIT_WIDTH_PERCENTAGE = 0.05;

        public LevelBuilder(int Width, int Height)
        {
            this.Width = Width;
            this.Height = Height;

            Initialize();
            GenerateMap();
        }

        private void Initialize()
        {
            map = new bool[Height][];
            map =
                map.Select((bs, row) => new bool[Width].Select((bs2, col) => rand.NextDouble() < INIT_COVERAGE).ToArray())
                    .ToArray();
        }

        public bool this[int row, int col]
        {
            get { return map.BoundedGet(row).BoundedGet(col); }
            set { map[row][col] = value; }
        }

        private bool Kernel1(bool[][] workingMap, int row, int col)
        {
            return CountWallsInWindow(workingMap, row, col, SMOOTH_WINDOW_SIZE) >= WALL_POSITIVE_THRESH
                   || CountWallsInWindow(workingMap, row, col, FILL_WINDOW_SIZE) <= FILL_NEGATIVE_THRESH;
        }

        private bool Kernel2(bool[][] workingMap, int row, int col)
        {
            return CountWallsInWindow(workingMap, row, col, SMOOTH_WINDOW_SIZE) >= WALL_POSITIVE_THRESH;
        }

        private int CountWallsInWindow(bool[][] workingMap, int row, int col, int size)
        {
            int shift = (size - 1)/2;
            return Enumerable.Range(col - shift, size)
                .SelectMany(c => Enumerable.Range(row - shift, size)
                    .Select(r => workingMap.BoundedGet(r).BoundedGet(c))).Count(b => b);
        }

        /// <summary>
        /// Applies Kernel 1 to the given map
        /// </summary>
        /// <param name="startingMap"></param>
        /// <returns></returns>
        private bool[][] _ApplyKernel1(bool[][] startingMap)
        {
            return startingMap.Select((b_row, row) => b_row.Select((val, col) => Kernel1(startingMap, row, col)).ToArray()).ToArray();
        }

        /// <summary>
        /// Applies Kernel 2 to the given map
        /// </summary>
        /// <param name="startingMap"></param>
        /// <returns></returns>
        private bool[][] _ApplyKernel2(bool[][] startingMap)
        {
            return startingMap.Select((b_row, row) => b_row.Select((val, col) => Kernel2(startingMap, row, col)).ToArray()).ToArray();
        }

        /// <summary>
        /// Makes sure that the whole map has a border
        /// </summary>
        /// <param name="startingMap"></param>
        /// <returns></returns>
        private bool[][] _BoundMap(bool[][] startingMap)
        {
            return startingMap.Select((b_row, row) => b_row.Select((val, col) => row == 0 || col == 0 || row == Height - 1 || col == Width - 1 || val).ToArray()).ToArray();
        }

        /// <summary>
        /// Deletes rooms that aren't attached to the main room
        /// </summary>
        /// <param name="staringMap"></param>
        /// <returns></returns>
        private bool[][] _KillOrphans(bool[][] startingMap)
        {
            // Start with an all-false map of the same shape as the starting map
            bool[][] Filled = startingMap.Select(bs => bs.Select(val => true).ToArray()).ToArray();

            // Pick a random point on the map that's open
            int row, col, iters = 0, maxiters = 10000;
            do
            {
                row = rand.Next(Height);
                col = rand.Next(Width);
                iters++;
            } while (startingMap[row][col] && iters < maxiters);
            if(iters == maxiters)
                throw new Exception("Couldn't find a valid starting point to fill the main room from");

            // Expand from there
            var Visited = new HashSet<Tuple<int, int>>();
            var ToVisit = new HashSet<Tuple<int, int>>();
            ToVisit.Add(new Tuple<int, int>(row, col));
            while (ToVisit.Any())
            {
                var NextToVisit = new HashSet<Tuple<int, int>>();
                foreach (var Current in ToVisit)
                    Visited.Add(Current);
                //ToVisit.AsParallel().ForAll(Current =>
                foreach(var Current in ToVisit)
                {
                    if (!startingMap[Current.Item1][Current.Item2])
                    {
                        Filled[Current.Item1][Current.Item2] = false;
                        NextToVisit.AddIfNotIn(new Tuple<int, int>(Current.Item1 + 1, Current.Item2), Visited);
                        NextToVisit.AddIfNotIn(new Tuple<int, int>(Current.Item1 - 1, Current.Item2), Visited);
                        NextToVisit.AddIfNotIn(new Tuple<int, int>(Current.Item1, Current.Item2 + 1), Visited);
                        NextToVisit.AddIfNotIn(new Tuple<int, int>(Current.Item1, Current.Item2 - 1), Visited);
                    }
                }//);
                ToVisit = NextToVisit;
            }

            return Filled;
        }

        /// <summary>
        /// Digs a whole in the top and bottom until that pathway enters a cavern of equal or greater width
        /// </summary>
        /// <param name="startingMap"></param>
        /// <returns></returns>
        private bool[][] _EntranceExit(bool[][] startingMap)
        {
            // Clone the map
            var retMap = startingMap.Select(bs => bs.Select(val => val).ToArray()).ToArray();

            // Figure out how big to make the hole (5% or the map's width, or 3 cells wide, whichever is bigger
            int OpeningHeight = Math.Max(MIN_ENT_EXIT_WIDTH, (int) (Height*ENT_EXIT_WIDTH_PERCENTAGE));
            int Top = Height/2 - OpeningHeight/2;
            int Bottom = Top + OpeningHeight;

            var Dig = new Action<int, int, int>((lower, upper, dir) =>
            {
                bool NeedToKeepDigging = true;
                // Needs to be set to true if any one of the dug squares started as false (wall)
                for (int col = lower; col*dir < upper*dir && NeedToKeepDigging; col += dir)
                {
                    NeedToKeepDigging = false;
                    for (int row = Top; row < Bottom; row++)
                    {
                        NeedToKeepDigging |= retMap[row][col];
                        retMap[row][col] = false;
                    }
                }
            });

            Dig(0, Width, 1);
            Dig(Width - 1, 0 - 1, -1);

            return retMap;
        }

        private void Update()
        {
            foreach(var editor in mapEditors)
                map = editor(map);
        }

        public string[] RenderAsStrings()
        {
            return map.Select((b_row, row) => "".Combine(b_row.Select(c => c ? "#" : ".").ToArray())).ToArray();
        }

        public void PrintToConsole()
        {
            //Console.SetCursorPosition(0, 0);
            Console.WriteLine("\r\n".Combine(RenderAsStrings()));
        }

        private void GenerateMap()
        {
            do
            {
                try
                {
                    //Console.WriteLine("Generating new map");
                    Initialize();
                    //Console.WriteLine("Applying Kernel 1");
                    // Generate general room
                    ApplyIterations(4, _ApplyKernel1, _BoundMap);
                    //Console.WriteLine("Applying Kernel 2");
                    ApplyIterations(2, _ApplyKernel2, _BoundMap);
                    // Apply finishing touches to make playable
                    //Console.WriteLine("Killing orphans");
                    //Console.WriteLine("Generating entrance and exits");
                    //Console.WriteLine("Refining final map");
                    ApplyIterations(1, _KillOrphans, _EntranceExit, _ApplyKernel2);
                    // Confirm that the room is suitable for use
                }
                catch (Exception ex)
                {
                    PrintToConsole();
                    Console.WriteLine("Error: " + ex.Message);
                    return;
                    //continue;
                }
            } while (!Verify(map));
        }

        private void ApplyIterations(int Iterations, params Func<bool[][], bool[][]>[] Editors)
        {
            mapEditors.AddRange(Editors);
            for (int i = 0; i < Iterations; i++)
            {
                Update();
            }
            mapEditors.Clear();
        }

        /// <summary>
        /// Checks to see if the map seems like a sufficiently interesting map
        /// </summary>
        /// <param name="startingMap"></param>
        /// <returns>True if the map is good, False if the map should be regenerated</returns>
        private bool Verify(bool[][] startingMap)
        {
            return
                // Check that at least half the map is open
                startingMap.SelectMany(bs => bs).Count(val => val) < 0.4*startingMap.Select(bs => bs.Count()).Sum();
        }
    }
}