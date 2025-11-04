using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TickConsoleApp.Models;

namespace TickConsoleApp.Services
{
    public interface ITickGenerator
    {
        event EventHandler<TickEventArgs>? Tick;
        Task Initialize();
        void Start();
        void Stop();
        
    }
}
