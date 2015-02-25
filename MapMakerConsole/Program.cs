using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helpers;
using Microsoft.Win32.SafeHandles;

namespace MapMakerConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var Map = new MapBuilder(4, 79, 30);
            Map.PrintToConsole();
            File.WriteAllText(FileHelpers.IterFileName("map_{0}.txt"), Map.RenderAsString());
            Console.ReadLine();
        }
    }
}
