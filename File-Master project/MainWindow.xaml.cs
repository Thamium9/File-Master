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

namespace File_Master_project
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Classes
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

        class Backupsettings_Global
        {
            public bool IsTempfolderEnabled;
            public DirectoryInfo TempFolder;
        }

        class Backupsettings_Local
        {
            public bool IsSingleCopy;
            public int NumberOfCopies; //automatically 1 if 'IsSingleCopy' is true
            private Interval Save_interval;
            public string Save_interval_Code; // Save_interval serialized version
            public bool AbsoluteCopy;
            public bool ManualDetermination = false; //automatically false when 'AbsoluteCopy' is true or 'IsSingleCopy' is false
            public bool StoreDeletedInRBin = false; //automatically false when 'AbsoluteCopy' is true
            public bool PopupWhenRBinIsFull = false; //automatically false when 'StoreDeletedInRBin' is false
            public bool SmartSave;
            public bool UseMaxStorageData;
            public int MaxStorageData; //no value if 'UseMaxStorageData' is false
            private Interval RetryWaitTime;
            public string RetryWaitTime_Code; // RetryWaitTime serialized version
            public int MaxNumberOfRetries;
            public bool PopupOnFail;
            public bool FileCompression;

            public Backupsettings_Local(bool isSingleCopy, int numberOfCopies, Interval save_interval, bool absoluteCopy, bool manualDetermination, bool storeDeletedInRBin, bool popupWhenRBinIsFull, bool smartSave, bool useMaxStorageData, int maxStorageData, Interval retryWaitTime, int maxNumberOfRetries, bool popupOnFail, bool fileCompression)
            {
                IsSingleCopy = isSingleCopy;
                if (IsSingleCopy) NumberOfCopies = 1;
                else NumberOfCopies = numberOfCopies;
                Save_interval = save_interval;
                AbsoluteCopy = absoluteCopy;
                if (!AbsoluteCopy)
                {
                    if (IsSingleCopy) ManualDetermination = manualDetermination;
                    StoreDeletedInRBin = storeDeletedInRBin;
                    if (StoreDeletedInRBin) PopupWhenRBinIsFull = popupWhenRBinIsFull;
                }
                SmartSave = smartSave;
                UseMaxStorageData = useMaxStorageData;
                if (UseMaxStorageData) MaxStorageData = maxStorageData;
                RetryWaitTime = retryWaitTime;
                MaxNumberOfRetries = maxNumberOfRetries;
                PopupOnFail = popupOnFail;
                FileCompression = fileCompression;
            }

            public Interval GetSave_interval()
            {
                return Save_interval;
            }

            public Interval GetRetryWaitTime()
            {
                return RetryWaitTime;
            }

            #region Serialization
            public void Serialize()
            {
                Save_interval_Code = JsonConvert.SerializeObject(Save_interval);
                RetryWaitTime_Code = JsonConvert.SerializeObject(RetryWaitTime);
            }

            public void Deserialize()
            {
                Save_interval = JsonConvert.DeserializeObject<Interval>(Save_interval_Code);
                RetryWaitTime = JsonConvert.DeserializeObject<Interval>(RetryWaitTime_Code);
            }
            #endregion
        }

        class Backupitem
        {
            public int ID;
            [JsonIgnore] public FileSystemInfo Source;
            [JsonProperty] private string SourcePath;
            [JsonIgnore] public FileSystemInfo Destination;
            [JsonProperty] private string DestinationPath;
            public DateTime LastSaved;
            public bool Isenabled;
            [JsonIgnore] public bool CanBeEnabled = true;
            private Backupsettings_Local Configuration;
            public string Configuration_Code; // Configuration serialized version

            public Backupitem()
            {

            }

            public Backupitem(int id, FileSystemInfo source, FileSystemInfo destination, DateTime lastSaved, bool isenabled, Backupsettings_Local settings)
            {
                ID = id;
                Source = source;
                Destination = destination;
                LastSaved = lastSaved;
                Isenabled = isenabled;
                Configuration = settings;
            }

            public Backupsettings_Local GetBackupsettings()
            {
                return Configuration;
            }

            #region Serialization
            public void Serialize()
            {
                Configuration.Serialize();
                Configuration_Code = JsonConvert.SerializeObject(Configuration);
                SourcePath = Source.FullName;
                DestinationPath = Destination.FullName;
            }

            public void Deserialize()
            {
                Configuration = JsonConvert.DeserializeObject<Backupsettings_Local>(Configuration_Code);
                Configuration.Deserialize();
                Source = GetPathInfo(SourcePath);
                Destination = GetPathInfo(DestinationPath);
            }
            #endregion
        }

        class Backupdrive
        {
            private List<Backupitem> Itemlist = new List<Backupitem>();
            public string Itemlist_Code; // Itemlist serialized version
            [JsonIgnore] public DriveInfo DriveInformation;
            [JsonIgnore] public bool IsAvailable = true;
            public string DriveID;

            public void ValidityCheck(Dictionary<string, DriveInfo> AllDriveInfo)
            {
                foreach (var thisDriveInfo in AllDriveInfo)
                {
                    if (thisDriveInfo.Key == DriveID) DriveInformation = thisDriveInfo.Value;
                }
                if (DriveInformation == null) IsAvailable = false;
            }

            public void AddBackupitem(Backupitem Item)
            {
                Itemlist.Add(Item);
            }

            public Backupitem GetBackupitem(int index)
            {
                return Itemlist[index];
            }

            public Backupitem GetBackupitemFromID(int ID)
            {
                Backupitem Item;
                Item = null;
                foreach (var item in Itemlist)
                {
                    if (item.ID == ID)
                    {
                        Item = item;
                    }
                }
                return Item;
            }

            public void RemoveBackupitem(int ID)
            {
                foreach (var item in Itemlist)
                {
                    if (item.ID == ID)
                    {
                        Itemlist.Remove(item);
                        break;
                    }
                }
            }

            public void SetBackupitemState(int ID, bool State)
            {
                foreach (var item in Itemlist)
                {
                    if (item.ID == ID)
                    {
                        item.Isenabled = State;
                        break;
                    }
                }
            }

            public int CountItems()
            {
                return Itemlist.Count();
            }

            public List<int> GetIDs()
            {
                List<int> temp = new List<int>();
                foreach (var BackupItem in Itemlist)
                {
                    temp.Add(BackupItem.ID);
                }
                return temp;
            }

            #region Serialization
            public void Serialize()
            {
                foreach (var item in Itemlist)
                {
                    item.Serialize();
                }
                Itemlist_Code = JsonConvert.SerializeObject(Itemlist);
            }

            public void Deserialize()
            {
                Itemlist = JsonConvert.DeserializeObject<List<Backupitem>>(Itemlist_Code);
                foreach (var item in Itemlist)
                {
                    item.Deserialize();
                }
            }
            #endregion
        }

        class BackupProcess
        {
            public List<Backupdrive> Backupdrives;
            public Backupsettings_Global Settings;
            private Dictionary<string, DriveInfo> AllDriveInfo=new Dictionary<string, DriveInfo>();

            public BackupProcess(List<Backupdrive> backupdrives, Backupsettings_Global settings)
            {
                GetAllDriveInfo();
                Backupdrives = backupdrives;
                Settings = settings;
                foreach (var Drive in Backupdrives)
                {
                    Drive.ValidityCheck(AllDriveInfo);
                }
            }

            private void GetAllDriveInfo()
            {
                DriveInfo[] AllDrives = DriveInfo.GetDrives();
                foreach (var Drive in AllDrives)
                {
                    if(Drive.IsReady)
                    {
                        string Serial = GetHardDiskDSerialNumber($"{Drive.Name[0]}");
                        AllDriveInfo.Add(Serial, Drive);
                    }
                }
            }

            public string GetHardDiskDSerialNumber(string drive)//not my code : https://ukacademe.com/TutorialExamples/CSharp/Get_Serial_Number_of_Hard_Drive
            {
                //Check to see if the user provided a drive letter
                //If not default it to "C"
                if (string.IsNullOrEmpty(drive) || drive == null)
                {
                    drive = "C";
                }
                //Create our ManagementObject, passing it the drive letter to the
                //DevideID using WQL
                ManagementObject disk = new ManagementObject($"Win32_LogicalDisk.DeviceID=\"{drive}:\"");
                //bind our management object
                disk.Get();
                //Return the serial number
                return disk["VolumeSerialNumber"].ToString();
            }           
        }

        class Settings
        {
            //default settings
            public bool Shortsource = true;
            public bool Minimize_as_TaskbarIcon = true;
            public bool Start_with_minimized = false;

            public int Savefilesize_Limit = 1000000;//in bytes
            public int Savefoldersize_Limit = 1000000;//in bytes

            private string[] Temp;
            private string Path = $"{Directory.GetCurrentDirectory()}\\config\\settings.txt";

            public Settings()
            {
                if (!File.Exists(Path)) File.Create(Path);
            }

            public void Load_Settings()
            {
                Temp = File.ReadAllLines(Path);
                foreach (var item in Temp)
                {
                    if (item.Split('=')[0] == "Shortsource") Shortsource = Get_Bool(item.Split('=')[1]);
                    if (item.Split('=')[0] == "Minimize_as_TaskbarIcon") Minimize_as_TaskbarIcon = Get_Bool(item.Split('=')[1]);
                    if (item.Split('=')[0] == "Start_with_minimized") Start_with_minimized = Get_Bool(item.Split('=')[1]);

                    if (item.Split('=')[0] == "Savefilesize_Limit") Savefilesize_Limit = int.Parse(item.Split('=')[1]);
                    if (item.Split('=')[0] == "Savefoldersize_Limit") Savefoldersize_Limit = int.Parse(item.Split('=')[1]);
                }
            }

            private bool Get_Bool(string value)
            {
                if (value == "true") return true;
                else return false;
            }
        }

        #endregion

        #region Options (User)
        private bool shortsource = true;
        private int Savefilesize_Limit = 1000000;//in bytes
        private int Savefoldersize_Limit = 1000000;//in bytes
        private bool Minimize_as_TaskbarIcon = true;
        private bool Start_with_minimized = false;
        #endregion

        #region Options (System)
        private bool InDevelopment = true;
        private bool Hasprivileges = false;
        private bool Hasadminrights = false;
        private string Menu;
        private bool Emptyconfig = false;
        #endregion

        private BackupProcess Backup;
        private DateTime CurrentTime = DateTime.Now;
        private List<string> Backupinfo_List = new List<string>(); //structure : {int index}*{char type}src<{string source_path}|dst<{string destination_path}|{interval}|{*if empty it is saved, othervise a save is required to apply changes}
        private string CurrentDir = Directory.GetCurrentDirectory();

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                #region Window visibility on startup
                if (!Minimize_as_TaskbarIcon || !Start_with_minimized) NotifyIcon_Taskbar.Visibility = Visibility.Collapsed; //hide notifyicon when not needed
                if (Start_with_minimized) Program_Minimize(Minimize_as_TaskbarIcon);
                else Main_window.WindowState = WindowState.Normal;
                #endregion

                Settings Usersettings = new Settings();
                Usersettings.Shortsource = false;

                if (InDevelopment)
                {
                    Unstable_Warning();
                }
                HideAllMenu();
                Backup_grid.Visibility = Visibility.Visible;
                Main_window.Activate();
                Startup();
                Menu = "Backup";

                #region debug
                /*
                List<Backupdrive> DataBackupdrives = new List<Backupdrive>();
                Backupdrive Drive = new Backupdrive();
                Interval SI = new Interval("60 min");
                Interval RWT = new Interval("10 min");

                //Backupsettings_Local config = new Backupsettings_Local(true, 1, SI, true, false, false, false, true, false, 0, RWT, 3, true, false);
                //Backupitem Item = new Backupitem(0,"source path", "destination path", CurrentTime, true, config);
                Drive.DriveID = "02466E75";
                //Drive.AddBackupitem(Item);
                DataBackupdrives.Add(Drive);
                Upload_Backupinfo(DataBackupdrives);*/
                #endregion

                Backup = LoadBackupProcess();
                Display_Backupitems();
            }
            #region Runtime Error
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Runtime error!", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            #endregion
        }

        #region Startup

        private void Startup()
        {
            Directory.CreateDirectory(CurrentDir + "\\Logs");
            Directory.CreateDirectory(CurrentDir + "\\config");
            if (!(File.Exists(CurrentDir + "\\config\\backup.txt")))
            {
                File.Create(CurrentDir + "\\config\\backup.txt");
                Emptyconfig = true;
            }
        }
        #endregion

        #region Backup feature

        #region Backup Data-IN
        private string[] Load_backupinfo()
        {
            string[] Backupinfo;
            Backupinfo = File.ReadAllLines(Directory.GetCurrentDirectory() + "\\config\\backup.json");
            #region UI-changes
            Warning2_label.Visibility = Visibility.Hidden;
            #endregion
            return Backupinfo;
        }

        private BackupProcess LoadBackupProcess()
        {
            #region Backupdrives
            string[] Backupinfo = Load_backupinfo();
            List<Backupdrive> Backupdrives = new List<Backupdrive>();
            foreach (var item in Backupinfo)
            {
                Backupdrive Drive = JsonConvert.DeserializeObject<Backupdrive>(item);
                Drive.Deserialize();
                Backupdrives.Add(Drive);
            }
            #endregion
            #region BackupSettings_Global
            Backupsettings_Global Settings = new Backupsettings_Global();
            #endregion
            return new BackupProcess(Backupdrives, Settings);
        }
        #endregion

        #region Backup Data-Use
        private void Display_Backupitems()
        {
            Backuptask_listbox.Items.Clear();
            Warning2_label.Visibility = Visibility.Hidden;
            Warning3_label.Visibility = Visibility.Hidden;
            Warning4_label.Visibility = Visibility.Hidden;
            ListBoxItem ListItem;
            foreach (var Drive in Backup.Backupdrives)
            {
                #region Add backupdrive to list
                ListItem = new ListBoxItem();
                string part1 = "Backup drive: ";
                part1 += $"{Drive.DriveInformation.VolumeLabel} ({Drive.DriveInformation.Name[0]}:)";
                string part3 = "(5,9GB / 60GB)";
                string part2 = "";
                for (int i = 0; i < 80 - (part1.Length + part3.Length); i++)
                {
                    part2 += "-";
                }
                ListItem.Content = part1 + part2 + part3;
                ListItem.Tag = Drive.DriveID;
                CheckDriveStatus(Drive, ref ListItem);
                Backuptask_listbox.Items.Add(ListItem);
                #endregion
                #region Add backupitems of backupdrive
                for (int i = 0; i < Drive.CountItems(); i++)
                {
                    ListItem = new ListBoxItem();
                    string part4 = $"-> {GetBackupType(Drive.GetBackupitem(i))}: {Drive.GetBackupitem(i).Source}{Drive.GetBackupitem(i).Source.Name} - (5,6GB)";
                    ListItem.Content = part4;
                    ListItem.Tag = $"{Drive.GetBackupitem(i).ID}";
                    CheckStatus(Drive.GetBackupitem(i), ref ListItem);
                    Backuptask_listbox.Items.Add(ListItem);
                }
                #endregion
            }
            #region Add 'Add new task' button
            ListItem = new ListBoxItem();
            ListItem.Content = "➕ Add new task";
            ListItem.FontWeight = FontWeights.Normal;
            ListItem.HorizontalAlignment = HorizontalAlignment.Left;
            ListItem.Tag = "add";
            ListItem.MouseLeftButtonUp += new MouseButtonEventHandler(Additem_button_Click);
            Backuptask_listbox.Items.Add(ListItem);
            #endregion
        }

        private void Display_Backupitem(string Tag)
        {
            Backupitem Item = GetBackupitemFromTag(Tag);

            #region Loads interval
            Item.GetBackupsettings().GetSave_interval().Humanize();
            Interval_label.Content = Item.GetBackupsettings().GetSave_interval().GetTime();
            #endregion

            #region Loads destination
            Destination_textbox.Text = Item.Destination.FullName;
            #endregion

            #region Loads Smart save
            if (Item.GetBackupsettings().SmartSave) Smartsave_label.Content = "Smart save: ON";
            else Smartsave_label.Content = "Smart save: OFF";
            #endregion

            #region Loads Last saved
            if(Item.LastSaved==DateTime.MinValue) Lastsaved_label.Content = $"Last saved: Never";
            else Lastsaved_label.Content = $"Last saved: {Item.LastSaved}";
            #endregion

            #region Get status
            bool CanRunBackupProcess;
            bool MissingSource;
            CheckStatus(Item, out CanRunBackupProcess, out MissingSource);
            #endregion

            EnableActionButtons(Item, CanRunBackupProcess, MissingSource);
        }

        private void EnableActionButtons(Backupitem Item, bool CanRunBackupProcess, bool MissingSource)
        {
            #region Remove item
            Removeitem_button.IsEnabled = true;
            Removeitem_button.Opacity = 1;
            #endregion
            #region Enable/Disable backup
            if (Item.Isenabled)
            {
                Enablebackup_button.Visibility = Visibility.Hidden;
                Disablebackup_button.Visibility = Visibility.Visible;
            }
            else
            {
                Disablebackup_button.Visibility = Visibility.Hidden;
                if (CanRunBackupProcess)
                {
                    Enablebackup_button.IsEnabled = true;
                    Enablebackup_button.Visibility = Visibility.Visible;
                    Enablebackup_button.Opacity = 1;
                }
                else
                {
                    Enablebackup_button.IsEnabled = false;
                    Enablebackup_button.Visibility = Visibility.Visible;
                    Enablebackup_button.Opacity = 0.5;
                }
            }
            #endregion
            #region Configuration/Repair/Restore
            if (MissingSource)
            {
                Repair_button.Visibility = Visibility.Visible;
                Configuration_button.Visibility = Visibility.Hidden;
                Restorefiles_button.Opacity = 1;
                Restorefiles_button.IsEnabled = true;
            }
            else
            {
                Configuration_button.Opacity = 1;
                Configuration_button.Visibility = Visibility.Visible;
                Configuration_button.IsEnabled = true;
                Repair_button.Visibility = Visibility.Hidden;
                Restorefiles_button.Opacity = 0.5;
                Restorefiles_button.IsEnabled = false;
            }
            #endregion
            #region Manual save
            if (CanRunBackupProcess)
            {
                Manualsave_button.IsEnabled = true;
                Manualsave_button.Opacity = 1;
            }
            else
            {
                Manualsave_button.IsEnabled = false;
                Manualsave_button.Opacity = 0.5;
            }
            #endregion
        }

        private void CheckDriveStatus(Backupdrive Drive, ref ListBoxItem ListItem)
        {
            ListItem.Foreground = new SolidColorBrush(Color.FromRgb(230, 0, 0));
        }

        private void CheckStatus(Backupitem Item, ref ListBoxItem ListItem)//Sets warning labels, and item color
        {
            #region Default
            ListItem.Foreground = new SolidColorBrush(Color.FromRgb(0, 230, 120));
            #endregion
            if (!Item.Isenabled)
            {
                Warning2_label.Visibility = Visibility.Visible;
                ListItem.Foreground = new SolidColorBrush(Color.FromRgb(240, 70, 0));
            }
            if (GetBackupType(Item) == "Unknown")
            {
                ListItem.Foreground = new SolidColorBrush(Color.FromRgb(200, 0, 180));
                Warning3_label.Visibility = Visibility.Visible;
            }
            if (false)//unknown issue
            {
                ListItem.Foreground = new SolidColorBrush(Color.FromRgb(230, 0, 0));
                Warning4_label.Visibility = Visibility.Visible;
            }
            if (!Item.CanBeEnabled)
            {
                if (false)//Can save to temp-drive temp
                {
                    ListItem.Foreground = new SolidColorBrush(Color.FromRgb(225, 225, 0));
                }
                else
                {
                    ListItem.Foreground = new SolidColorBrush(Color.FromRgb(230, 0, 0));
                    Warning4_label.Visibility = Visibility.Visible;
                }
            }
            if (false)//Item.CanBeEnabled == false
            {
                ListItem.Foreground = new SolidColorBrush(Color.FromRgb(230, 0, 0));
                Warning4_label.Visibility = Visibility.Visible;
            }
        }

        private void CheckStatus(Backupitem Item, out bool CanRunBackupProcess, out bool MissingSource)//Sets Status label, and returns if it can be enabled
        {
            #region Default status
            CanRunBackupProcess = true;
            MissingSource = false;
            Destination_textbox.Foreground = new SolidColorBrush(Color.FromRgb(0, 230, 120));
            Status_label.Foreground = new SolidColorBrush(Color.FromRgb(0, 230, 120));
            Status_label.Content = "Status: OK!";
            #endregion
            if (!Item.Isenabled)
            {
                Status_label.Foreground = new SolidColorBrush(Color.FromRgb(240, 70, 0));
                Status_label.Content = "Status: The backup item is disabled!";
            }
            if (GetBackupType(Item) == "Unknown")
            {
                Status_label.Foreground = new SolidColorBrush(Color.FromRgb(200, 0, 180));
                Status_label.Content = "Status: The source is missing, cannot continue the backup process!";
                CanRunBackupProcess = false;
                MissingSource = true;
            }
            if (!Item.CanBeEnabled)
            {
                if (false)//Can save to temp-drive temp
                {
                    Destination_textbox.Foreground = new SolidColorBrush(Color.FromRgb(225, 225, 0));
                    Status_label.Foreground = new SolidColorBrush(Color.FromRgb(225, 225, 0));
                    Status_label.Content = "Status: OK (alternative destination)!";
                    CanRunBackupProcess = false;
                }
                else
                {
                    Destination_textbox.Foreground = new SolidColorBrush(Color.FromRgb(230, 0, 0));
                    Status_label.Foreground = new SolidColorBrush(Color.FromRgb(230, 0, 0));
                    Status_label.Content = "Status: The destination is unreachable, cannot continue the backup process!";
                    CanRunBackupProcess = false;
                }          
            }
            else if(false)//unknown issue
            {
                Status_label.Foreground = new SolidColorBrush(Color.FromRgb(230, 0, 0));
                Status_label.Content = "Status: Cannot continue the backup process due to unknown circumstances!";
                CanRunBackupProcess = false;
            }
        }
        #endregion

        #region Backup Data-OUT
        private void Upload_Backupinfo(List<Backupdrive> Data)
        {
            File.WriteAllText(CurrentDir + "\\config\\backup.json", "");
            foreach (var item in Data)
            {
                Backupdrive Drive = item;
                Drive.Serialize();
                string Code = JsonConvert.SerializeObject(Drive);
                File.AppendAllText(CurrentDir + "\\config\\backup.json", Code);
            }        
            
            Emptyconfig = false;

            #region UI-changes
            Warning2_label.Visibility = Visibility.Hidden;
            #endregion
        }
        #endregion

        #region Backup Data-Extraction
        private Backupitem GetBackupitemFromTag(string Tag)
        {
            Backupitem Item = new Backupitem();
            #region GetBackupItem from Tag
            int ID = int.Parse(Tag);
            foreach (var Drive in Backup.Backupdrives)
            {
                Item = Drive.GetBackupitemFromID(ID);
                if (Item != null) break;
            }
            #endregion
            return Item;
        }

        private string GetBackupType(Backupitem Item)
        {           
            if (Directory.Exists(Item.Source.FullName)) return "Folder";
            else if (File.Exists(Item.Source.FullName)) return "File";
            else return "Unknown";
        }

        static private FileSystemInfo GetPathInfo(string Path)
        {
            if (Directory.Exists(Path)) return new DirectoryInfo(Path);
            else return new FileInfo(Path);
        }

        #region old
        private string GetSource(int index)
        {
            string result;
            result = Backupinfo_List[index].Split('|')[0].Split('<')[1];
            return result;
        }

        private string GetDestination(int index)
        {
            string result;
            result = Backupinfo_List[index].Split('|')[1].Split('<')[1];
            return result;
        }

        private char GetType(int index)
        {
            char result;
            result = Backupinfo_List[index].Split('|')[0].Split('*')[1][0];
            return result;
        }

        private Interval GetInterval(int index)
        {
            Interval result = new Interval(Backupinfo_List[index].Split('|')[2]);
            return result;
        }
        #endregion

        #endregion

        #region Backup process (SAVEING)
        private void Save(string sourcepath, string destinationpath, bool isFile)
        {
            bool success = false;
            try
            {
                if (isFile)
                {
                    #region Filesave
                    FileInfo Size = new FileInfo(sourcepath);
                    if (Size.Length < Savefilesize_Limit)//if the file is smaller than 1Mb
                    {
                        if (File.Exists($"{destinationpath}\\{System.IO.Path.GetFileName(sourcepath)}"))//if it has to overwrite
                        {
                            MessageBox.Show("I cannot overwrite files yet!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            File.Copy(sourcepath, $"{destinationpath}\\{System.IO.Path.GetFileName(sourcepath)}", false);
                            success = true;
                        }
                    }
                    else
                    {
                        MessageBox.Show("I cannot save files bigger than 1MB!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    #endregion
                }
                else
                {
                    MessageBox.Show("I cannot save folders yet!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("The operation was unsuccessful!", "Unknown Error!", MessageBoxButton.OK, MessageBoxImage.Error);
            }         
            if(success) MessageBox.Show("The save was successful!", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Save(Backupitem Item)
        {
            bool success = false;
            try
            {
                if (File.Exists(Item.Source.FullName))
                {
                    
                }
                else
                {
                    
                }
            }
            catch (Exception)
            {
                MessageBox.Show("The operation was unsuccessful due to an error!", "Manual Save", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            if (success) MessageBox.Show("The operation was successful!", "Manual Save", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private void Manualsave_button_Click(object sender, RoutedEventArgs e)
        {
            if(Warning_Save())
            {
                int i = Backuptask_listbox.SelectedIndex;
                if (GetType(i) == 'F')
                {
                    Save(GetSource(i), GetDestination(i), true);
                }
                else
                {
                    Save(GetSource(i), GetDestination(i), false);
                }
            }
        }
        #endregion

        #endregion

        #region UI

        #region Warnings
        private bool Unstable_Warning()
        {
            return MessageBox.Show("This is an unstable version of the program! \nUse it at your own risk!", "Warning!", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.ServiceNotification).Equals(MessageBoxResult.OK);
            //return true;
        }

        private bool Warning_Save()
        {
            return MessageBox.Show("This is an unstable version of the program! \nIt might cause some issues! \nDo you want to save anyway?", "Warning!", MessageBoxButton.YesNo, MessageBoxImage.Exclamation).Equals(MessageBoxResult.Yes);
            //return true; 
        }
        #endregion

        #region Backup menu

        #region Select backuptask
        private void Backuptask_listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)//changes when you select an item from the source list
        {
            if (Backuptask_listbox.SelectedIndex != -1)//if the index is -1 there is no item selected
            {
                ListBoxItem Item = (ListBoxItem)Backuptask_listbox.SelectedItem;
                string Tag = Item.Tag.ToString();
                if (int.TryParse(Tag, out int temp))
                {
                    Display_Backupitem(Tag);
                    #region UI-changes
                    Warning1_label.Visibility = Visibility.Hidden;
                    #endregion
                }
                else
                {
                    Reset_Backupmenu();
                }
            }
            else if(Backuptask_listbox.Items.Count!=0)
            {
                Reset_Backupmenu();
            }
        }
        #endregion

        #region Modify backuptasks (submenu)

        #region Add item (submenu)
        private void Additem_button_Click(object sender, RoutedEventArgs e)
        {
            HideAllMenu();
            Backupsubmenu1_grid.Visibility = Visibility.Visible;
            Menu = "Backup.sub1";
        }
        #endregion

        #region Remove item
        private void Removeitem_button_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete this item? \nIt will be deleted permanently!", "Delete", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel).Equals(MessageBoxResult.Yes))
            {
                ListBoxItem Item = (ListBoxItem)Backuptask_listbox.SelectedItem;
                int ID = int.Parse(Item.Tag.ToString());
                foreach (var Drive in Backup.Backupdrives)
                {
                    Drive.RemoveBackupitem(ID);
                }

                Reset_Backupmenu();
            }
        }
        #endregion

        #region Restore files !
        private void Relocate_button_Click(object sender, RoutedEventArgs e)
        {
            HideAllMenu();
            Backupsubmenu1_grid.Visibility = Visibility.Visible;
            Menu = "Backup.sub1";
            Destinationinput_textbox.IsEnabled = false;
            Intervalselection_combobox.Visibility = Visibility.Hidden;
            Interval2_label.Visibility = Visibility.Visible;
            Newitemapply_button.Visibility = Visibility.Hidden;
            Replaceitemapply_button.Visibility = Visibility.Visible;

            #region Data load
            int index = Backuptask_listbox.SelectedIndex;
            #region Loads interval
            if (GetInterval(index).Convert_to_min() < 60)
            {
                Interval2_label.Content = $"{GetInterval(index).Convert_to_min()} min";
            }
            else if (GetInterval(index).Convert_to_hour() < 24)
            {
                Interval2_label.Content = $"{GetInterval(index).Convert_to_hour()} hour";
            }
            else
            {
                Interval2_label.Content = $"{GetInterval(index).Convert_to_day()} day";
            }
            #endregion
            Destinationinput_textbox.Text = GetDestination(index);
            if (GetType(index) == 'D')
            {
                //Optionfolder_radiobutton.IsChecked = true;
            }
            else
            {
               //Optionfile_radiobutton.IsChecked = true;
            }
            #endregion
        }

        private void Replaceitemapply_button_Click(object sender, RoutedEventArgs e)
        {
           /* if (Sourceinput_textbox.Text == "")
            {
                MessageBox.Show("You have to provide more information!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (MessageBox.Show("Are you sure you want to apply these changes?", "Apply", MessageBoxButton.YesNo, MessageBoxImage.None).Equals(MessageBoxResult.Yes))
            {
                HideAllMenu();
                Backup_grid.Visibility = Visibility.Visible;
                Menu = "Backup";

                #region Get Type
                char type;
                if (Optionfolder_radiobutton.IsChecked == true)
                {
                    type = 'D';
                }
                else type = 'F';
                #endregion

                #region Get interval
                string interval = Interval2_label.Content.ToString();
                #endregion
                int index = Backuptask_listbox.SelectedIndex;
                Backupinfo_List[index] = $"{GetIndex(Backuptask_listbox.SelectedIndex)}*{type}src<{Sourceinput_textbox.Text}|dst<{Destinationinput_textbox.Text}|{interval}|<!change!>";
                //Load_Backupitems(Backupinfo_List);
                Backuptask_listbox.SelectedIndex = (Backuptask_listbox.Items.Count - 1);//selects the relocated item automatically

                #region Submenu reset
                Sourceinput_textbox.Text = "";
                Destinationinput_textbox.Text = "";
                Intervalselection_combobox.SelectedIndex = -1;
                Optionfolder_radiobutton.IsChecked = true;

                Destinationinput_textbox.IsEnabled = true;
                Intervalselection_combobox.Visibility = Visibility.Visible;
                Interval2_label.Visibility = Visibility.Hidden;
                Interval2_label.Content = "";
                Newitemapply_button.Visibility = Visibility.Visible;
                Replaceitemapply_button.Visibility = Visibility.Hidden;
                #endregion
            }*/
        }
        #endregion

        #region Configuration (submenu)
        private void Configuration_button_Click(object sender, RoutedEventArgs e)
        {
            HideAllMenu();
            Backupsubmenu1_grid.Visibility = Visibility.Visible;
            Menu = "Backup.sub1";
            Destinationinput_textbox.IsEnabled = false;
            Sourceinput_textbox.IsEnabled = false;
            Intervalselection_combobox.Visibility = Visibility.Hidden;
            Interval2_label.Visibility = Visibility.Visible;
            Newitemapply_button.Visibility = Visibility.Hidden;
            //Optionfile_radiobutton.IsEnabled = false;
            //Optionfolder_radiobutton.IsEnabled = false;

            #region Data load
            int index = Backuptask_listbox.SelectedIndex;
            #region Loads interval
            if (GetInterval(index).Convert_to_min() < 60)
            {
                Interval2_label.Content = $"{GetInterval(index).Convert_to_min()} min";
            }
            else if (GetInterval(index).Convert_to_hour() < 24)
            {
                Interval2_label.Content = $"{GetInterval(index).Convert_to_hour()} hour";
            }
            else
            {
                Interval2_label.Content = $"{GetInterval(index).Convert_to_day()} day";
            }
            #endregion
            Destinationinput_textbox.Text = GetDestination(index);
            Sourceinput_textbox.Text = GetSource(index);
            if (GetType(index) == 'D')
            {
                //Optionfolder_radiobutton.IsChecked = true;
            }
            else
            {
                //Optionfile_radiobutton.IsChecked = true;
            }
            #endregion
        }
        #endregion

        #region Enable/Disable backup
        private void Enablebackup_button_Click(object sender, RoutedEventArgs e)
        {
            ListBoxItem Item = (ListBoxItem)Backuptask_listbox.SelectedItem;
            string Tag = Item.Tag.ToString();
            int ID = int.Parse(Tag);
            foreach (var Drive in Backup.Backupdrives)
            {
                Drive.SetBackupitemState(ID, true);
            }
            Refresh_Backupmenu();
        }

        private void Disablebackup_button_Click(object sender, RoutedEventArgs e)
        {
            ListBoxItem Item = (ListBoxItem)Backuptask_listbox.SelectedItem;
            string Tag = Item.Tag.ToString();
            int ID = int.Parse(Tag);
            foreach (var Drive in Backup.Backupdrives)
            {
                Drive.SetBackupitemState(ID, false);
            }
            Refresh_Backupmenu();
        }
        #endregion

        #endregion

        #region Submenu1

        #region Backupdrivelist combobox
        private void Backupdriveselect_combobox_DropDownOpened(object sender, EventArgs e)
        {
            Backupdriveselect_combobox.Items.Clear();
            ComboBoxItem CI = new ComboBoxItem();
            CI.Content = "Select a backup drive!";
            CI.Tag = "none";
            Backupdriveselect_combobox.Items.Add(CI);
            Backupdriveselect_combobox.SelectedIndex = 0;
            foreach (var Drive in Backup.Backupdrives)
            {
                CI = new ComboBoxItem();
                CI.Content = $"{Drive.DriveInformation.VolumeLabel} ({Drive.DriveInformation.Name[0]}:)";
                CI.Tag = Drive.DriveID;
                Backupdriveselect_combobox.Items.Add(CI);
            }
        }
        #endregion

        #region Apply buttons
        private void Newitemapply_button_Click(object sender, RoutedEventArgs e)//adds the new item to the system
        {
            if(CheckInfo())
            {
                if (MessageBox.Show("Are you sure you want to add this item to the list?", "Apply", MessageBoxButton.YesNo, MessageBoxImage.None).Equals(MessageBoxResult.Yes))
                {
                    HideAllMenu();
                    Backup_grid.Visibility = Visibility.Visible;
                    Menu = "Backup";
                    Backupsettings_Local Settings = CreateBackupsettings_Local();
                    ComboBoxItem CI = (ComboBoxItem)Backupdriveselect_combobox.SelectedItem;
                    foreach (var Drive in Backup.Backupdrives)
                    {
                        if (Drive.DriveID==CI.Tag.ToString())
                        {
                            Drive.AddBackupitem(CreateBackupitem(Settings));
                            Upload_Backupinfo(Backup.Backupdrives);
                            break;
                        }
                    }               
                    Reset_Backupmenu();
                    Reset_BackupSubmenu1();
                }
            }
        }

        private bool CheckInfo()
        {
            if (Sourceinput_textbox.Text == "" || Destinationinput_textbox.Text == "" || Intervalselection_combobox.SelectedIndex == -1 || Backupdriveselect_combobox.SelectedIndex == 0)
            {
                MessageBox.Show("You have to provide more information!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (!(File.Exists(Sourceinput_textbox.Text) || Directory.Exists(Sourceinput_textbox.Text)))
            {
                MessageBox.Show("The source doesn't exists!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (!Directory.Exists(Destinationinput_textbox.Text))
            {
                MessageBox.Show("The destination doesn't exists!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                return true;
            }
            return false;
        }

        private Backupitem CreateBackupitem(Backupsettings_Local Settings)
        {
            #region Get newID
            List<int> IDs = new List<int>();
            foreach (var Drive in Backup.Backupdrives)
            {
                foreach (var ID in Drive.GetIDs())
                {
                    IDs.Add(ID);
                }
            }
            int newID = 0;
            foreach (var ID in IDs)
            {
                if (newID == ID)
                {
                    newID++;
                }
                else
                {
                    break;
                }
            }
            #endregion          
            FileSystemInfo Source = GetPathInfo(Sourceinput_textbox.Text);
            FileSystemInfo Destination = GetPathInfo(Destinationinput_textbox.Text);
            Backupitem Item=new Backupitem(newID, Source, Destination, DateTime.MinValue,false, Settings);
            return Item;
        }

        private Backupsettings_Local CreateBackupsettings_Local()
        {
            bool IsSingleCopy = Singlecopy_radiobutton.IsChecked.Value;
            int NumberOfCopies = 1;
            ComboBoxItem  CI = (ComboBoxItem)Intervalselection_combobox.SelectedItem;
            Interval Save_interval = new Interval(CI.Tag.ToString());
            bool AbsoluteCopy = true;
            bool ManualDetermination = false; 
            bool StoreDeletedInRBin = false; //automatically false when 'AbsoluteCopy' is true
            bool PopupWhenRBinIsFull = false; //automatically false when 'StoreDeletedInRBin' is false
            bool SmartSave = Smartsave_checkbox.IsChecked.Value;
            bool UseMaxStorageData = false;
            int MaxStorageData=0; //no value if 'UseMaxStorageData' is false
            Interval RetryWaitTime = new Interval("5 min");
            int MaxNumberOfRetries = 3;
            bool PopupOnFail=false;
            bool FileCompression = Compress_checkbox.IsChecked.Value;
            Backupsettings_Local Settings = new Backupsettings_Local(IsSingleCopy,NumberOfCopies,Save_interval,AbsoluteCopy,ManualDetermination,StoreDeletedInRBin,PopupWhenRBinIsFull,SmartSave,UseMaxStorageData,MaxStorageData,RetryWaitTime,MaxNumberOfRetries,PopupOnFail,FileCompression);
            return Settings;
        }

        #endregion

        #region Cancel button
        private void Backupsubmenu1cancel_button_Click(object sender, RoutedEventArgs e)
        {
            HideAllMenu();
            Backup_grid.Visibility = Visibility.Visible;
            Menu = "Backup";
            Reset_Backupmenu();
            Reset_BackupSubmenu1();
        }
        #endregion

        #region Settings menu
        #region Toggle menu
        private void MSettings_radiobutton_Checked(object sender, RoutedEventArgs e)
        {
            Backupsubmenu1_settings2_grid.Visibility = Visibility.Hidden;
            Backupsubmenu1_settings1_grid.Visibility = Visibility.Visible;
        }

        private void ASettings_radiobutton_Checked(object sender, RoutedEventArgs e)
        {
            Backupsubmenu1_settings2_grid.Visibility = Visibility.Visible;
            Backupsubmenu1_settings1_grid.Visibility = Visibility.Hidden;
        }
        #endregion
        #region Reset menu
        private void Reset_BackupSubmenu1Settings()
        {
            Singlecopy_radiobutton.IsChecked = true;
        }
        #endregion
        #endregion

        #region Menu actions
        private void Reset_BackupSubmenu1()
        {
            Sourceinput_textbox.Text = "";
            Destinationinput_textbox.Text = "";
            Intervalselection_combobox.SelectedIndex = -1;
            MSettings_radiobutton.IsChecked = true;

            Destinationinput_textbox.IsEnabled = true;
            Sourceinput_textbox.IsEnabled = true;
            Intervalselection_combobox.Visibility = Visibility.Visible;
            Interval2_label.Visibility = Visibility.Hidden;
            Interval2_label.Content = "";
            Newitemapply_button.Visibility = Visibility.Visible;
            Replaceitemapply_button.Visibility = Visibility.Hidden;
        }
        #endregion

        #endregion

        #region Menu actions
        private void Reset_Backupmenu()
        {
            Backuptask_listbox.SelectedIndex = -1;
            Warning1_label.Visibility = Visibility.Visible;
            Destination_textbox.Foreground = new SolidColorBrush(Color.FromRgb(226, 154, 6));
            Destination_textbox.Text = "Select a source folder!";
            Interval_label.Content = "";
            Status_label.Content = "Status: No items are selected!";
            Status_label.Foreground = new SolidColorBrush(Color.FromRgb(226, 154, 6));
            Smartsave_label.Content = "Smart save:";
            Lastsaved_label.Content = "Last saved:";
            Removeitem_button.IsEnabled = false;
            Removeitem_button.Opacity = 0.5;
            Restorefiles_button.IsEnabled = false;
            Restorefiles_button.Opacity = 0.5;
            Configuration_button.IsEnabled = false;
            Configuration_button.Opacity = 0.5;
            Configuration_button.Visibility = Visibility.Visible;
            Repair_button.Visibility = Visibility.Hidden;
            Manualsave_button.IsEnabled = false;
            Manualsave_button.Opacity = 0.5;
            Save_image.Opacity = 0.5;
            Disablebackup_button.Visibility = Visibility.Hidden;
            Enablebackup_button.IsEnabled = false;
            Enablebackup_button.Visibility = Visibility.Visible;
            Enablebackup_button.Opacity = 0.5;
            Display_Backupitems();
        }
        private void Refresh_Backupmenu()
        {
            if(Backuptask_listbox.SelectedIndex!=-1)
            {
                ListBoxItem Item = (ListBoxItem)Backuptask_listbox.SelectedItem;
                string Tag = Item.Tag.ToString();
                Display_Backupitems();
                for (int i = 0; i < Backuptask_listbox.Items.Count; i++)
                {
                    ListBoxItem temp = (ListBoxItem)Backuptask_listbox.Items[i];
                    string tag = temp.Tag.ToString();
                    if (tag == Tag) Backuptask_listbox.SelectedIndex = i;
                }
                Display_Backupitem(Tag);
            }
            Upload_Backupinfo(Backup.Backupdrives);
        }

        #endregion

        #endregion











        #region Categorization menu

        #endregion

        #region Menu Grids
        private void HideAllMenu()
        {
            Backup_grid.Visibility = Visibility.Hidden;
            Backupsubmenu1_grid.Visibility = Visibility.Hidden;
            Debug_grid.Visibility = Visibility.Hidden;           
        }
        #endregion

        #region Side panel

        #region Backup menu

        private void Bc_label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsSubmenu())
            {
                FileCleanup_menu.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
                Backup_menu.Background = new SolidColorBrush(Color.FromRgb(26, 26, 26));
                Categorization_menu.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
                Settings_menu.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));

                HideAllMenu();
                Backup_grid.Visibility = Visibility.Visible;
                Menu = "Backup";
            }
            else
            {
                SystemSounds.Exclamation.Play();
            }
        }

        private void Bc_label_MouseEnter(object sender, MouseEventArgs e)
        {
            Backup_menu.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        }

        private void Bc_label_MouseLeave(object sender, MouseEventArgs e)
        {
            if (Menu.Split('.')[0] == "Backup") Backup_menu.Background = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            else Backup_menu.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
        }

        #endregion

        #region File-Cleanup menu
        private void Fc_label_MouseEnter(object sender, MouseEventArgs e)
        {
            FileCleanup_menu.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        }

        private void Fc_label_MouseLeave(object sender, MouseEventArgs e)
        {
            if (Menu.Split('.')[0] == "File-Cleanup") FileCleanup_menu.Background = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            else FileCleanup_menu.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
        }

        private void Fc_label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FileCleanup_menu.Background = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            Backup_menu.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
            Categorization_menu.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
            Settings_menu.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));

            HideAllMenu();
            Menu = "File-Cleanup";
        }



        #endregion

        #region Categorization menu
        private void Ctg_label_MouseEnter(object sender, MouseEventArgs e)
        {
            Categorization_menu.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        }

        private void Ctg_label_MouseLeave(object sender, MouseEventArgs e)
        {
            if(Menu.Split('.')[0] == "Categorization") Categorization_menu.Background = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            else Categorization_menu.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
        }

        private void Ctg_label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FileCleanup_menu.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
            Backup_menu.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
            Categorization_menu.Background = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            Menu = "Categorization";
            Settings_menu.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));

            HideAllMenu();
        }


        #endregion

        #region Settings menu

        #endregion

        private bool IsSubmenu()
        {
            return Menu.Split('.').Length > 1;
        }

        #endregion

        #region Menu bar

        #region Top right menu buttons

        #region Mouseover effect
        private void Close_image_MouseEnter(object sender, MouseEventArgs e)
        {
            Closebackground_rectangle.Visibility = Visibility.Visible;
        }

        private void Close_image_MouseLeave(object sender, MouseEventArgs e)
        {
            Closebackground_rectangle.Visibility = Visibility.Hidden;
        }

        private void Minimize_image_MouseEnter(object sender, MouseEventArgs e)
        {
            Minimizebackground_rectangle.Visibility = Visibility.Visible;
        }

        private void Minimize_image_MouseLeave(object sender, MouseEventArgs e)
        {
            Minimizebackground_rectangle.Visibility = Visibility.Hidden;
        }
        #endregion

        #region Close program
        private void Close_image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
             Close();
        }

        private void Main_window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!MessageBox.Show("Are you sure you want to close the program? \nIf you close it, all unsaved changes will be lost!", "Close", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.Yes).Equals(MessageBoxResult.Yes))
            {
                e.Cancel = true;
            }
        }
        #endregion

        #region Minimize program
        private void Minimize_image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Program_Minimize(Minimize_as_TaskbarIcon);
        }

        private void Program_Minimize(bool Minimize_as_TaskbarIcon)
        {
            if (Minimize_as_TaskbarIcon)
            {
                Main_window.WindowState = WindowState.Minimized;
                NotifyIcon_Taskbar.Visibility = Visibility.Visible;
                Main_window.ShowInTaskbar = false;
            }
            else
            {
                Main_window.WindowState = WindowState.Minimized;
            }
        }

        #region NotifyIcon
        private void NotifyIcon_Taskbar_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            Main_window.WindowState = WindowState.Normal;
            Main_window.Activate();
            NotifyIcon_Taskbar.Visibility = Visibility.Collapsed;
            Main_window.ShowInTaskbar = true;
        }
        #endregion

        #endregion

        #endregion

        #region Move window
        private void Main_window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.LeftButton.Equals(MouseButtonState.Pressed))
                {
                    this.DragMove();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Unexpected error with dragging the window!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #endregion

        #endregion

        #region debug
        private void Temp()
        {
            string path = Directory.GetCurrentDirectory() + "\\encrypt.txt";
            File.WriteAllText(path, "Ezt a szöveget ne tudd elolvasni!!!");
            File.Encrypt(path);
            Debugwindow_textbox.Text += Directory.GetCurrentDirectory();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //Temp();
            /*if (!Submenu)
            {
                Backup_grid.Visibility = Visibility.Hidden;
                Debug_grid.Visibility = Visibility.Visible;
            }*/
            SystemSounds.Asterisk.Play();
            #region folder selection
            /*Winform.FolderBrowserDialog folderDlg = new Winform.FolderBrowserDialog();
            folderDlg.ShowNewFolderButton = true;
            folderDlg.RootFolder = Environment.SpecialFolder.MyComputer;
            while (true)
            {
                Winform.DialogResult result = folderDlg.ShowDialog();
                string temp3 = folderDlg.SelectedPath.ToString().Split('\\')[0];
                if (temp3 != "G:")
                {
                    MessageBox.Show(temp3, "err");
                }
                else
                {
                    break;
                }
            }*/
            #endregion
            #region file selection
            /*Winform.OpenFileDialog openFileDialog1 = new Winform.OpenFileDialog();
            openFileDialog1.InitialDirectory = @"G:";
            //openFileDialog1.RestoreDirectory = true;
            openFileDialog1.ValidateNames = false;
            openFileDialog1.CheckFileExists = false;
            openFileDialog1.CheckPathExists = true;
            openFileDialog1.FileName = "Folder selection";

            while (true)
            {
                openFileDialog1.ShowDialog();
                string temp3 = openFileDialog1.FileName.ToString().Split('\\')[0];
                if (temp3 != "G:")
                {
                    MessageBox.Show(temp3, "err");
                }
                else
                {
                    break;
                }
            }*/
            #endregion

            //Process.Start("G:");
            //Warning_Save();
            //Autoconfig_upload();
        }
        private void Main_window_MouseMove(object sender, MouseEventArgs e)
        {
            debug_label.Content = Menu;
        }
        #endregion
    }
}