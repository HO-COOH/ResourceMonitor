using System;
using System.Windows.Forms;
using System.Linq;
using System.Text;


namespace ResourceMonitor
{
    static class Battery
    {
        private static readonly PowerStatus status = SystemInformation.PowerStatus;

        //Returns (hours, minutes, seconds)
        public static (int, int, int) BatteryRemains
        {
            get
            {
                int remainsInSeconds = status.BatteryLifeRemaining;
                int remainsInHour = remainsInSeconds / 60 / 60;
                int remainsMinute = (remainsInSeconds - remainsInHour * 3600) / 60;
                int remainsSecond = remainsInSeconds - remainsInHour * 3600 - remainsMinute * 60;
                return (remainsInHour, remainsMinute, remainsInSeconds);
            }
        }

        public static float BatteryPercent => status.BatteryLifePercent;
    }
}
