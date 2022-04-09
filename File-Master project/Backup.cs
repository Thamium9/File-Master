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
using WTimer = System.Threading.Timer;

namespace File_Master_project
{
    class Backupsettings_Global
    {
        public bool IsTempfolderEnabled;
        public DirectoryInfo TempFolder;
    }

    class Backupsettings_Local
    {
        [JsonProperty] public bool IsSingleCopy;
        [JsonProperty] public int NumberOfCopies; //automatically 1 if 'IsSingleCopy' is true
        [JsonProperty] public Interval Save_interval;
        [JsonProperty] public bool AbsoluteCopy;
        [JsonProperty] public bool ManualDetermination = false; //automatically false when 'AbsoluteCopy' is true or 'IsSingleCopy' is false
        [JsonProperty] public bool StoreDeletedInRBin = false; //automatically false when 'AbsoluteCopy' is true
        [JsonProperty] public bool PopupWhenRBinIsFull = false; //automatically false when 'StoreDeletedInRBin' is false
        [JsonProperty] public bool SmartSave;
        [JsonProperty] public bool UseMaxStorageData;
        [JsonProperty] public int MaxStorageData; //no value if 'UseMaxStorageData' is false
        [JsonProperty] public Interval RetryWaitTime;
        [JsonProperty] public int MaxNumberOfRetries;
        [JsonProperty] public bool PopupOnFail;
        [JsonProperty] public bool FileCompression;

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
    }

    class Backupitem
    {
        [JsonProperty] public int ID { get; set; }
        [JsonIgnore] public FileSystemInfo Source { get; set; }
        [JsonProperty] private string SourcePath;
        [JsonIgnore] public FileSystemInfo Destination;
        [JsonProperty] private string DestinationPath;
        [JsonProperty] public DateTime LastSaved;
        [JsonProperty] public bool IsEnabled { get; set; }
        [JsonIgnore] public bool CanBeEnabled { get; set; } = true;
        [JsonProperty] public Backupsettings_Local Configuration;
        [JsonIgnore] private Timer Backuptimer = new Timer(); //Timer for the next backup task call

        [JsonConstructor] public Backupitem(int iD, string sourcePath, string destinationPath, DateTime lastSaved, bool isEnabled, Backupsettings_Local configuration)
        {
            ID = iD;
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
            LastSaved = lastSaved;
            IsEnabled = isEnabled;
            Configuration = configuration;
            Destination = GetPathInfo(DestinationPath);
            Source = GetPathInfo(SourcePath);
            // start timer
        }

        #region Get data
        private DateTime GetNextCallTime()
        {
            return LastSaved.AddTicks(Configuration.Save_interval.Convert_to_ticks());
        }

        public DiskSpace GetBackupSize()
        {
            DiskSpace space = new DiskSpace(0);
            if((Source.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                DirectoryInfo backup = new DirectoryInfo($@"{Destination.FullName}\{Source.Name}");
                foreach (var item in Directory.GetFiles(backup.FullName, "*", SearchOption.AllDirectories))
                {
                    space.Bytes += new FileInfo(item).Length;
                }
            }
            else
            {
                FileInfo backup = new FileInfo($@"{Destination.FullName}\{Source.Name}");
                space.Bytes += backup.Length;
            }
            return space;
        }

        private FileSystemInfo GetPathInfo(string Path)
        {
            if (Directory.Exists(Path)) return new DirectoryInfo(Path);
            else return new FileInfo(Path);
        }
        #endregion

        #region Backup process (SAVEING)
        public void Backup(bool isManual)
        {
            bool success = false;
            if (CheckPermission(isManual))
            {
                try
                {
                    if (File.Exists(Source.FullName))
                    {
                        SaveFile(Source);
                    }
                    else
                    {
                        SaveDirectory(Source);
                        foreach (var ThisDirectory in Directory.GetDirectories(Source.FullName, "*", SearchOption.AllDirectories))
                        {
                            SaveDirectory(Main.GetPathInfo(ThisDirectory));
                        }
                    }
                    LastSaved = DateTime.Now;
                    success = true;
                    //Refresh_Backupmenu();
                    BackupProcess.Upload_Backupinfo();
                }
                catch (Exception ex)
                {
                    if (isManual) MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                if (isManual)
                {
                    if (success) MessageBox.Show("The operation was successful!", "Manual Save", MessageBoxButton.OK, MessageBoxImage.Information);
                    else MessageBox.Show("The operation was unsuccessful!", "Manual Save", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            StartTimer();
        }

        private void SaveFile(FileSystemInfo ThisSource, string AdditionalPath = "")
        {
            Directory.CreateDirectory($"{Destination.FullName}{AdditionalPath}");
            File.Copy(ThisSource.FullName, $@"{Destination.FullName}{AdditionalPath}\{ThisSource.Name}", false);
        }

        private void SaveDirectory(FileSystemInfo ThisSource)
        {
            foreach (var item in Directory.GetFiles(ThisSource.FullName))
            {
                SaveFile(Main.GetPathInfo(item), ThisSource.FullName.Replace(Source.FullName, $@"\{Source.Name}"));
            }
        }

        private bool CheckPermission(bool isManual)
        {
            bool result = true;
            if(!isManual)
            {
                if (!IsEnabled) result = false;
            }
            return result;
        }

        private void Backuptimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now >= GetNextCallTime())
            {
                //Backup(false);
            }
            else
            {
                StartTimer();
            }
        }

        private void StartTimer()
        {
            DateTime NextCall = LastSaved.AddTicks(Configuration.Save_interval.Convert_to_ticks()); //this is the date when the next backup will happen
            TimeSpan diff = (NextCall - DateTime.Now);
            Backuptimer.Interval = Math.Min(Math.Max(60000, diff.Ticks / 10000), 2147483647); //the interval cannot be less than a second (or in this case 10000000 ticks or 1000 miliseconds)  AND  the interval cannot be more than 2147483647 miliseconds
            Backuptimer.Elapsed += Backuptimer_Elapsed;
            Backuptimer.AutoReset = false;
            Backuptimer.Start();
        }
        #endregion

        #region UI
        public void SetListItem(ref ListItem item)
        {

        }
        #endregion
    }

    class Backupdrive
    {
        [JsonProperty] public string DriveID;
        [JsonProperty] private string DefaultVolumeLabel;
        [JsonIgnore] public DriveInfo DriveInformation;
        [JsonIgnore] public bool IsAvailable;
        [JsonIgnore] public bool IsOutOfSpace;
        [JsonProperty] public DiskSpace SizeLimit { get; set; }
        [JsonProperty] public List<Backupitem> Backups { get; private set; } = new List<Backupitem>();

        [JsonConstructor] public Backupdrive(string driveID, string defaultVolumeLabel, DiskSpace sizeLimit, List<Backupitem> backups)
        {
            DriveID = driveID;
            DefaultVolumeLabel = defaultVolumeLabel;
            Backups = backups;
            SizeLimit = sizeLimit;
            ValidityCheck();
            LimitCheck();
            SizeLimitCheck();
        }

        public Backupdrive(string driveID, string defaultVolumeLabel, DiskSpace sizeLimit)
        {
            DriveID = driveID;
            DefaultVolumeLabel = defaultVolumeLabel;
            SizeLimit = sizeLimit;
            ValidityCheck();
            LimitCheck();         
            BackupProcess.Upload_Backupinfo();
        }

        #region Checks
        public void ValidityCheck()
        {
            IsAvailable = true;
            Dictionary<string, DriveInfo> AllDriveInfo = BackupProcess.AllDriveInfo;
            foreach (var thisDriveInfo in AllDriveInfo)
            {
                if (thisDriveInfo.Key == DriveID)
                {
                    DriveInformation = thisDriveInfo.Value;
                    DefaultVolumeLabel = DriveInformation.VolumeLabel;
                }
            }
            if (DriveInformation == null) IsAvailable = false;            
        }

        public void LimitCheck()
        {
            if (SizeLimit.Bytes > 0 && GetBackupSize().Bytes > SizeLimit.Bytes) IsOutOfSpace = true;
            else IsOutOfSpace = false;
        }

        public bool SizeLimitCheck(out double result)
        {
            result = 0;
            if (((double)DriveInformation.TotalSize * 0.1) > ((double)DriveInformation.AvailableFreeSpace - SizeLimit.Bytes))
            {
                SizeLimit.Bytes = (long)Math.Max((DriveInformation.AvailableFreeSpace - ((double)DriveInformation.TotalSize * 0.1)), 0);
                SizeLimit.Gigabytes = Math.Floor(SizeLimit.Gigabytes);
                result = SizeLimit.Gigabytes;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SizeLimitCheck()
        {
            if (((double)DriveInformation.TotalSize * 0.1) > ((double)DriveInformation.AvailableFreeSpace - SizeLimit.Bytes))
            {
                SizeLimit.Bytes = (long)Math.Max((DriveInformation.AvailableFreeSpace - ((double)DriveInformation.TotalSize * 0.1)), 0);
                SizeLimit.Gigabytes = Math.Floor(SizeLimit.Gigabytes);
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        #region Modify backupitems
        public void AddBackupitem(Backupitem Item)
        {
            Backups.Add(Item);
        }

        public void RemoveBackupitem(int ID)
        {
            foreach (var item in Backups)
            {
                if (item.ID == ID)
                {
                    Backups.Remove(item);
                    break;
                }
            }
        }

        public void SetBackupitemState(bool State, int ID)
        {
            foreach (var item in Backups)
            {
                if (item.ID == ID)
                {
                    item.IsEnabled = State;
                    break;
                }
            }
        }
        #endregion

        #region Get data
        public Backupitem GetBackupitemFromID(int ID)
        {
            Backupitem Item = null;
            foreach (var item in Backups)
            {
                if (item.ID == ID)
                {
                    Item = item;
                }
            }
            return Item;
        }

        public int CountItems()
        {
            return Backups.Count();
        }

        public List<int> GetIDs()
        {
            List<int> temp = new List<int>();
            foreach (var BackupItem in Backups)
            {
                temp.Add(BackupItem.ID);
            }
            return temp;
        }

        public string GetVolumeLabel()
        {
            if (DriveInformation == null)
            {
                if (DefaultVolumeLabel == null)
                {
                    return "?";
                }
                return DefaultVolumeLabel;
            }

            else return DriveInformation.VolumeLabel;
        }

        public char GetDriveLetter()
        {
            if (DriveInformation == null) return '?';
            else return DriveInformation.Name[0];
        }

        public DiskSpace GetBackupSize()
        {
            DiskSpace space = new DiskSpace(0);
            foreach (var item in Backups)
            {
                space.Bytes += item.GetBackupSize().Bytes;
            }
            return space;
        }
        #endregion

        #region Backup
        public void Backup()
        {
            foreach (var item in Backups)
            {
                item.Backup(true);
            }
        }

        #endregion

        #region UI
        public void SetDriveNameTextbox(ref TextBox Item, ref ListBoxItem ListItem)
        {
            if (IsAvailable == false)
            {
                Item.Foreground = new SolidColorBrush(Color.FromRgb(230, 0, 0));
                ListItem.BorderBrush = new SolidColorBrush(Color.FromRgb(230, 0, 0));
            }
            else
            {
                Item.Foreground = new SolidColorBrush(Color.FromRgb(0, 230, 120));
                ListItem.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 230, 120));
            }
        }

        public void SetDriveSpaceTextbox(ref TextBox Item)
        {
            if (IsOutOfSpace == true)
            {
                Item.Foreground = new SolidColorBrush(Color.FromRgb(230, 0, 0));
            }

            else if (IsAvailable == false)
            {
                Item.Foreground = new SolidColorBrush(Color.FromRgb(230, 0, 0));
            }

            else
            {
                Item.Foreground = new SolidColorBrush(Color.FromRgb(0, 230, 120));
            }
        }
        #endregion
    }

    static class BackupProcess 
    {
        static public List<Backupdrive> Backupdrives { get; private set; }
        static public Backupsettings_Global Settings { get; set; }
        static public Dictionary<string, DriveInfo> AllDriveInfo { get; } = new Dictionary<string, DriveInfo>(); //key: serial number , value: DriveInfo

        static BackupProcess()
        {
            Backupdrives = new List<Backupdrive>();
            LoadAllDriveInfo();
            LoadBackupProcess();
            Upload_Backupinfo();
        }

        #region Actions
        static public void ActivateBackupdrive(string Serial, DiskSpace SizeLimit)
        {
            AllDriveInfo.TryGetValue(Serial, out DriveInfo Drive);
            Backupdrives.Add(new Backupdrive(Serial, Drive.VolumeLabel, SizeLimit));
            Upload_Backupinfo();
        }

        static public void DeactivateBackupdrive(string Serial)
        {
            if (MessageBox.Show("Are you sure you want to remove this Backupdrive?\nAll of its backup tasks will be deleted, but not the bakcup files!", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning).Equals(MessageBoxResult.Yes))
            {
                Backupdrives.Remove(GetBackupdriveFromSerial(Serial));
                Upload_Backupinfo();
            }
        }
        #endregion

        #region Get Data
        static public Backupitem GetBackupitemFromTag(string Tag) //this will reference the original backupitem, so both the copy and the original will be modified on save
        {
            Backupitem Item = null;
            #region GetBackupItem from Tag
            int ID = int.Parse(Tag);
            foreach (var Drive in Backupdrives)
            {
                Item = Drive.GetBackupitemFromID(ID);
                if (Item != null) break;
            }
            #endregion
            return Item;
        }

        static public string GetBackupType(Backupitem Item)
        {
            if (Directory.Exists(Item.Source.FullName)) return "Folder";
            else if (File.Exists(Item.Source.FullName)) return "File";
            else return "Unknown";
        }

        static public string GetHardDiskDSerialNumber(string drive)//not my code : https://ukacademe.com/TutorialExamples/CSharp/Get_Serial_Number_of_Hard_Drive
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

        static public List<DriveInfo> GetAllDriveInfo()
        {
            List<DriveInfo> DriveInfoList = new List<DriveInfo>();
            foreach (var Drive in AllDriveInfo)
            {
                DriveInfoList.Add(Drive.Value);
            }
            return DriveInfoList;
        }

        static public Backupdrive GetBackupdriveFromSerial(string Serial)
        {
            Backupdrive result = null;
            foreach (var Drive in Backupdrives)
            {
                if (Drive.DriveID == Serial) result = Drive;
            }
            return result;
        }

        static public bool IsBackupdrive(string serial)
        {
            foreach (var Drive in Backupdrives)
            {
                if (Drive.DriveID == serial) return true;
            }
            return false;
        }

        #endregion

        #region Load Data
        static private void LoadBackupProcess()
        {
            #region Backupdrives           
            Load_backupinfo(out string Backupinfo);
            try
            {
                Backupdrives = JsonConvert.DeserializeObject<List<Backupdrive>>(Backupinfo);
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to load in the user data due to data corruption!\nAll backup settings are deleted!", "Data corruption!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                Upload_Backupinfo();
            }
            #endregion

            #region BackupSettings_Global
            Settings = new Backupsettings_Global();
            #endregion
        }

        static private void Load_backupinfo(out string Backupinfo)
        {
            Backupinfo = "[]";
            string filepath = $"{Directory.GetCurrentDirectory()}\\config\\backup.json";
            if (File.Exists(filepath))
            {
                Backupinfo = File.ReadAllText(filepath);
            }
            else
            {
                File.WriteAllText(filepath, Backupinfo);
            }
            #region UI-changes
            //((MainWindow)Application.Current.MainWindow).Warning2_label.Visibility = Visibility.Hidden;
            #endregion
        }

        static private void LoadAllDriveInfo()
        {
            DriveInfo[] AllDrives = DriveInfo.GetDrives();
            foreach (var Drive in AllDrives)
            {
                if (Drive.IsReady)
                {
                    string Serial = GetHardDiskDSerialNumber($"{Drive.Name[0]}");
                    AllDriveInfo.Add(Serial, Drive);
                }
            }
        }
        #endregion

        #region Upload Data
        static public void Upload_Backupinfo()
        {
            string Code = JsonConvert.SerializeObject(Backupdrives, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(CurrentDir + "\\config\\backup.json", Code);

            #region UI-changes
            
            #endregion
        }
        #endregion

        #region Backup(Manualsave)
        static public void Manualsave(string Tag)
        {
            Backupitem ManualSave = GetBackupitemFromTag(Tag);
            ManualSave.Backup(true);
        }

        static private void ManualsaveALL()
        {
            foreach (var Drive in Backupdrives)
            {
                Drive.Backup();
            }
        }
        #endregion

        static public string CurrentDir = Directory.GetCurrentDirectory();
    }
}
