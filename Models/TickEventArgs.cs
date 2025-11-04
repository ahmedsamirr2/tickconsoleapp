using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TickConsoleApp.Models
{
    public class TickEventArgs : EventArgs
    {
        public Tick Tick { get; set; } = default! ;

    }
}
