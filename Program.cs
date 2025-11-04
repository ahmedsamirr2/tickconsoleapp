using System;
using System.IO;
using System.Threading.Tasks;
using TickConsoleApp.Services;
using TickConsoleApp.Models;

namespace TickConsoleApp
{
    class Program
    {
        static async Task Main()
        {
            string folder = Path.Combine(Environment.CurrentDirectory, "Data");
            Directory.CreateDirectory(Path.Combine(folder, "EURUSD"));


            var start = new DateTime(2024, 1, 1);
            
            var end = new DateTime(2025, 1, 31);

            var generator = new ReplayTickGenerator(start, end, folder, "EURUSD")
            {
                Speed = 1.0 
            };

            generator.Tick += (s, e) =>
            {
                Console.WriteLine($"{e.Tick.Timestamp:yyyy-MM-dd HH:mm:ss.fff 'UTC'}  |  {e.Tick.Value:F5}");
            };

            await generator.Initialize();

            Console.WriteLine("Press 'S' to start, 'X' to stop, 'Q' to quit.\n");

            bool exit = false;
            while (!exit)
            {
                var key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.S:
                        generator.Start();
                        break;
                    case ConsoleKey.X:
                        generator.Stop();
                        break;
                    case ConsoleKey.Q:
                        generator.Stop();
                        exit = true;
                        break;
                }
            }
        }
    }
}
