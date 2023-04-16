using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace File_Master_project
{
    public class Interval
    {
        [JsonProperty] public double Time { get; private set; }
        [JsonProperty] public string Unit { get; private set; }

        [JsonConstructor] public Interval(double time, string unit)
        {
            Time = time;
            Unit = unit;
            Simplify();
        }

        public Interval(string time)
        {
            string[] temp = time.Split(' ');
            Time = double.Parse(temp[0]);
            Unit = temp[1];
            Simplify();
        }

        public Interval(TimeSpan time)
        {
            Time = Math.Max((double)time.TotalMinutes, 1);
            Unit = "minute";
            Simplify();
        }

        private double Convert_to_min()
        {
            if (Unit == "minute")
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

        private double Convert_to_hour()
        {
            if (Unit == "minute")
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

        private double Convert_to_day()
        {
            if (Unit == "minute")
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

        private void Simplify()
        {
            if (Convert_to_day() >= 1 && Convert_to_day() % 1 == 0)
            {
                Time = Math.Floor(Convert_to_day());
                Unit = "day";
            }
            else if (Convert_to_hour() >= 1 && Convert_to_hour() % 1 == 0)
            {
                Time = Math.Floor(Convert_to_hour());
                Unit = "hour";
            }
            else
            {
                Time = Math.Floor(Convert_to_min());
                Unit = "minute";
            }
        }

        public string GetRounded()
        {
            if (Convert_to_min() < 60)
            {
                return $"{Math.Floor(Convert_to_min())} minute";
            }
            else if (Convert_to_hour() < 24)
            {
                return $"{Math.Floor(Convert_to_hour())} hour";
            }
            else
            {
                return $"{Math.Floor(Convert_to_day())} day";
            }
        }

        public string GetTime()
        {
            return $"{Time} {Unit}";
        }

        public bool IsPlural()
        {
            if (Time == 1) return false;
            else return true;
        }
    }
}
