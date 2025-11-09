using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TickConsoleApp.Models
{
    public readonly struct Tick   
    {
        

        public double Value { get; init; }
        public DateTime Timestamp { get; init; }

    }

}
