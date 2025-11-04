using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using TickConsoleApp.Models;

namespace TickConsoleApp.Services
{
    public class ReplayTickGenerator : ITickGenerator
    {
        public event EventHandler<TickEventArgs>? Tick;
        public double Speed { get; set; } = 1.0;

        private readonly string _rootFolder;
        private readonly string _instrument;
        private DateTime _start;
        private DateTime _end;
        private bool _isRunning;

        private StreamReader? _reader;
        private DateTime? _lastTime;
        private string? _currentFilePath;

        private const string ResumeFile = "resume_state.txt";

        public ReplayTickGenerator(DateTime start, DateTime end, string rootFolder, string instrument)
        {
            _start = start;
            _end = end;
            _rootFolder = rootFolder;
            _instrument = instrument;
        }

        public async Task Initialize()
        {
            await LoadResumeOrFirstFileAsync();
        }

        private async Task LoadResumeOrFirstFileAsync()
        {
            if (File.Exists(ResumeFile))
            {
                string[] parts = File.ReadAllText(ResumeFile).Split(',');
                if (parts.Length == 4)
                {
                    string instrument = parts[0];
                    string yearMonth = parts[1];
                    long position = long.Parse(parts[2]);
                    _lastTime = DateTime.Parse(parts[3], null, DateTimeStyles.RoundtripKind);

                    string filePath = Path.Combine(_rootFolder, instrument, $"{yearMonth}.csv");
                    if (File.Exists(filePath))
                    {
                        Console.WriteLine($"Resuming from {filePath}...");
                        _currentFilePath = filePath;
                        _reader = new StreamReader(filePath);
                        _reader.BaseStream.Seek(position, SeekOrigin.Begin);
                        return;
                    }
                }
            }

            await OpenNextFileAsync(_start);
        }

        private async Task OpenNextFileAsync(DateTime monthDate)
        {
            string filePath = Path.Combine(_rootFolder, _instrument, $"{monthDate:yyyyMM}.csv");
            if (File.Exists(filePath))
            {
                _currentFilePath = filePath;
                _reader = new StreamReader(filePath);
                Console.WriteLine($"Loaded tick file: {filePath}");
            }
            else
            {
                Console.WriteLine($" No tick file found for {monthDate:yyyy-MM}.csv");
                throw new FileNotFoundException($"No file found: {filePath}");
            }

            await Task.CompletedTask;
        }

        public async void Start()
        {
            if (_isRunning)
            {
                Console.WriteLine(" Tick generator already running.");
                return;
            }

            if (_reader == null)
                throw new InvalidOperationException("No file initialized. Run Initialize() first.");

            _isRunning = true;
            Console.WriteLine("Starting tick replay...\n");

            string? line;
            while (_isRunning && (line = await _reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(',');
                if (parts.Length < 2)
                    continue;

                if (!DateTime.TryParseExact(parts[0], "yyyyMMdd HHmmssfff",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime estTime))
                {
                    Console.WriteLine($" Skipped bad line: {line}");
                    continue;
                }

                DateTime utcTime = estTime.AddHours(5);

                if (!double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double bid))
                {
                    Console.WriteLine($"Invalid bid value: {line}");
                    continue;
                }

                var tick = new Tick
                {
                    Value = bid,
                    Timestamp = utcTime
                };


                Tick?.Invoke(this, new TickEventArgs { Tick = tick });


                if (_lastTime.HasValue && Speed > 0)
                {
                    double delayMs = (utcTime - _lastTime.Value).TotalMilliseconds / Speed;
                    if (delayMs > 0)
                        await Task.Delay((int)Math.Min(delayMs, 2000));
                }

                _lastTime = utcTime;
            }

            if (_isRunning)
            {
                await MoveToNextMonthOrStopAsync();
            }
            
        }

        private async Task MoveToNextMonthOrStopAsync()
        {
            DateTime nextMonth = _start.AddMonths(1);

            if (nextMonth <= _end)
            {
                Console.WriteLine("Switching to next month file...");
                await OpenNextFileAsync(nextMonth);
                _start = nextMonth;
                Start();
            }
            else
            {
                Console.WriteLine("No more files available. Stopping.");
                Stop();
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;

            if (_reader != null && _currentFilePath != null)
            {
                string monthName = Path.GetFileNameWithoutExtension(_currentFilePath);
                File.WriteAllText(ResumeFile,
                    $"{_instrument};{monthName};{_reader.BaseStream.Position};{_lastTime:O}");
            }

            Console.WriteLine("Stopped and saved resume state.");
        }
    }
}
