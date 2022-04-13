using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace File_Master_project
{
    public class DiskSpace
    {
        [JsonProperty] public long Bytes { get; set; }
        [JsonIgnore] public double Kilobytes { get { return (double)Bytes / Math.Pow(1024, 1); } set { Bytes = (long)(value * Math.Pow(1024, 1)); } }
        [JsonIgnore] public double Megabytes { get { return (double)Bytes / Math.Pow(1024, 2); } set { Bytes = (long)(value * Math.Pow(1024, 2)); } }
        [JsonIgnore] public double Gigabytes { get { return (double)Bytes / Math.Pow(1024, 3); } set { Bytes = (long)(value * Math.Pow(1024, 3)); } }
        [JsonIgnore] public double Terrabytes { get { return (double)Bytes / Math.Pow(1024, 4); } set { Bytes = (long)(value * Math.Pow(1024, 4)); } }

        public DiskSpace(long bytes)
        {
            Bytes = bytes;
        }

        public string Humanize()
        {
            if (Megabytes < 1) return $"{Math.Round(Kilobytes, 2)} KB";
            else if (Gigabytes < 1) return $"{Math.Round(Megabytes, 2)} MB";
            else if (Terrabytes < 1) return $"{Math.Round(Gigabytes, 2)} GB";
            else return $"{Math.Round(Terrabytes, 2)} TB";
        }
    }
}
