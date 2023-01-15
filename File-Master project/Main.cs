using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Media;
using System.Diagnostics;
using Winform = System.Windows.Forms;
using Newtonsoft.Json;
using System.Xml;
using System.Xml.Serialization;
using System.Management;
using System.Timers;

namespace File_Master_project
{
    static public class Main
    {
        #region Bakcup -> Manage backup drives
        static public Dictionary<string, TextBox> BackupDriveSizeLimits { get; set; } = new Dictionary<string, TextBox>();
        static public Dictionary<string, Button> BackupDriveUpdateButtons { get; set; } = new Dictionary<string, Button>();
        #endregion

        static public string GetBackupType(BackupTask Item)
        {
            if (Directory.Exists(Item.Source.FullName)) return "Folder";
            else if (File.Exists(Item.Source.FullName)) return "File";
            else return "Unknown";
        }
        
        static public FileSystemInfo GetPathInfo(string Path)
        {
            if (Directory.Exists(Path)) return new DirectoryInfo(Path);
            else return new FileInfo(Path);
        }
    }
}
