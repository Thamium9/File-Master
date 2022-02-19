using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace File_Master_project
{
    class Interval
    {
        [JsonProperty] private double Time;
        [JsonProperty] private string Unit;

        public Interval()
        {
        }

        public Interval(double time, string unit)
        {
            Time = time;
            Unit = unit;
        }

        public Interval(string time)
        {
            string[] temp = time.Split(' ');
            Time = double.Parse(temp[0]);
            Unit = temp[1];
        }

        public double Convert_to_min()
        {
            if (Unit == "min")
            {
                return Time;
            }
            else if (Unit == "hour")
            {
                return (Time * 60);
            }
            else
            {
                return (Time * 60 * 24);
            }
        }

        public double Convert_to_hour()
        {
            if (Unit == "min")
            {
                return (Time / 60);
            }
            else if (Unit == "hour")
            {
                return (Time);
            }
            else
            {
                return (Time * 24);
            }
        }

        public double Convert_to_day()
        {
            if (Unit == "min")
            {
                return (Time / 60 / 24);
            }
            else if (Unit == "hour")
            {
                return (Time / 24);
            }
            else
            {
                return (Time);
            }
        }

        public long Convert_to_ticks()
        {
            return (long)Convert_to_min() * 60 * 10000000;
        }

        public void Humanize()
        {
            if (Convert_to_min() < 60)
            {
                Time = Convert_to_min();
                Unit = "min";
            }
            else if (Convert_to_hour() < 24)
            {
                Time = Convert_to_hour();
                Unit = "hour";
            }
            else
            {
                Time = Convert_to_day();
                Unit = "day";
            }
        }

        public string GetTime()
        {
            return $"{Time} {Unit}";
        }
    }
}
