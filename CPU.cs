using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourceMonitor
{
    static class CPU
    {
        private static readonly PerformanceCounter counter = new PerformanceCounter("Processor", "% Processor Time", "_Total", Environment.MachineName);

        public static int Usage => (int)counter.NextValue();
    }
}
