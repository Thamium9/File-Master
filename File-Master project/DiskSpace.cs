using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace File_Master_project
{
    class DiskSpace
    {
        public long Bytes { get; set; }
        public double Kilobytes { get { return (double)Bytes / Math.Pow(1024, 1); } set { Bytes = (long)(value * Math.Pow(1024, 1)); } }
        public double Megabytes { get { return (double)Bytes / Math.Pow(1024, 2); } set { Bytes = (long)(value * Math.Pow(1024, 2)); } }
        public double Gigabytes { get { return (double)Bytes / Math.Pow(1024, 3); } set { Bytes = (long)(value * Math.Pow(1024, 3)); } }
        public double Terrabytes { get { return (double)Bytes / Math.Pow(1024, 4); } set { Bytes = (long)(value * Math.Pow(1024, 4)); } }

        public DiskSpace(long bytes)
        {
            Bytes = bytes;
        }

        public string Humanize()
        {
            if (Kilobytes < 1) return $"{Bytes} Byte";
            else if (Megabytes < 1) return $"{Math.Round(Kilobytes, 2)} KB";
            else if (Gigabytes < 1) return $"{Math.Round(Megabytes, 2)} MB";
            else if (Terrabytes < 1) return $"{Math.Round(Gigabytes, 2)} GB";
            else return $"{Math.Round(Terrabytes, 2)} TB";
        }
    }
}
