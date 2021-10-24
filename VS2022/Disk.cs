using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ResourceMonitor
{
    class Disk
    {
        private static DirectoryInfo directoryInfo;

        public Disk(string path)
        {
            directoryInfo = new DirectoryInfo(path);
        }

        private static float ToKB(long value) => value / 1024.0f;
        private static float ToMB(long value) => value / 1024.0f / 1024.0f;
        private static float ToGB(long value) => value / 1024.0f / 1024.0f / 1024.0f;

        private static float ConvertUnit(long value, SizeUnit unit)
        {
            switch (unit)
            {
                case SizeUnit.KB:
                    return ToKB(value);
                case SizeUnit.MB:
                    return ToMB(value);
                case SizeUnit.GB:
                    return ToGB(value);
            }
            return 0.0f;
        }

        public float SolutionSize(SizeUnit unit)
        {
            long size = 0;
            foreach (var fileInfo in directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                Console.WriteLine($"{fileInfo.FullName} -> {fileInfo.Length}");
                size += fileInfo.Length;
            }
            return ConvertUnit(size, unit);
        }
    }
}
