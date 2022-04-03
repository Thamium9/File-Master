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
        [JsonProperty] public int ID { get; set; }
        [JsonIgnore] public FileSystemInfo Source { get; set; }
        [JsonProperty] private string SourcePath;
        [JsonIgnore] public FileSystemInfo Destination;
        [JsonProperty] private string DestinationPath;
        [JsonProperty] public DateTime LastSaved;
        [JsonProperty] public bool IsEnabled { get; set; }
        [JsonIgnore] public bool CanBeEnabled { get; set; } = true;
        [JsonIgnore] private Backupsettings_Local Configuration;
        [JsonProperty] public string Configuration_Code; // Configuration serialized version
        [JsonIgnore] private Timer Backuptimer = new Timer(); //Timer for the next backup task call


        public Backupitem(int id, FileSystemInfo source, FileSystemInfo destination, DateTime lastSaved, bool isenabled, Backupsettings_Local settings)
        {
            ID = id;
            Source = source;
            Destination = destination;
            LastSaved = lastSaved;
            IsEnabled = isenabled;
            Configuration = settings;
        }
        public Backupsettings_Local GetBackupsettings()
        {
            return Configuration;
        }

        private DateTime GetNextCallTime()
        {
            return LastSaved.AddTicks(Configuration.GetSave_interval().Convert_to_ticks());
        }

        #region Backup process (SAVEING)
        public void Backup(bool isManual)
        {
            bool success = false;
            if (CheckPermission())
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
            File.Copy(ThisSource.FullName, $"{Destination.FullName}{AdditionalPath}\\{ThisSource.Name}", false);
        }

        private void SaveDirectory(FileSystemInfo ThisSource)
        {
            foreach (var item in Directory.GetFiles(ThisSource.FullName))
            {
                SaveFile(Main.GetPathInfo(item), ThisSource.FullName.Replace(Source.FullName, $"\\{Source.Name}"));
            }
        }

        private bool CheckPermission()
        {
            return IsEnabled;
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
            DateTime NextCall = LastSaved.AddTicks(Configuration.GetSave_interval().Convert_to_ticks()); //this is the date when the next backup will happen
            TimeSpan diff = (NextCall - DateTime.Now);
            Backuptimer.Interval = Math.Min(Math.Max(60000, diff.Ticks / 10000), 2147483647); //the interval cannot be less than a second (or in this case 10000000 ticks or 1000 miliseconds)  AND  the interval cannot be more than 2147483647 miliseconds
            Backuptimer.Elapsed += Backuptimer_Elapsed;
            Backuptimer.AutoReset = false;
            Backuptimer.Start();
        }
        #endregion

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
            Source = Main.GetPathInfo(SourcePath);
            Destination = Main.GetPathInfo(DestinationPath);

            StartTimer();
        }
        #endregion
    }

    class Backupdrive
    {
        [JsonProperty] public string DriveID;
        [JsonProperty] private string DefaultVolumeLabel;
        [JsonIgnore] public DriveInfo DriveInformation;
        [JsonIgnore] public bool IsAvailable = true;
        [JsonProperty] public DiskSpace SizeLimit { get; set; } //bytes
        [JsonProperty] public string Backups_Code; // Backups serialized version
        [JsonIgnore] private List<Backupitem> Backups = new List<Backupitem>();

        public Backupdrive()
        {

        }

        public Backupdrive(string driveID, string defaultVolumeLabel, DiskSpace sizeLimit)
        {
            DriveID = driveID;
            DefaultVolumeLabel = defaultVolumeLabel;
            SizeLimit = sizeLimit;
            ValidityCheck();
        }

        public void ValidityCheck()
        {
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
            BackupProcess.Upload_Backupinfo();
        }

        public void AddBackupitem(Backupitem Item)
        {
            Backups.Add(Item);
        }

        public Backupitem GetBackupitem(int index)
        {
            return Backups[index];
        }

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

        public void Backup()
        {
            foreach (var item in Backups)
            {
                item.Backup(true);
            }
        }

        #region Serialization
        public void Serialize()
        {
            foreach (var item in Backups)
            {
                item.Serialize();
            }
            Backups_Code = JsonConvert.SerializeObject(Backups);
        }

        public void Deserialize()
        {
            Backups = JsonConvert.DeserializeObject<List<Backupitem>>(Backups_Code);
            foreach (var item in Backups)
            {
                item.Deserialize();
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
            foreach (var Drive in Backupdrives)
            {
                Drive.ValidityCheck();
            }
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
                foreach (var Drive in Backupdrives)
                {
                    Drive.Deserialize();
                }
            }
            catch (Exception)
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
            foreach (var Drive in Backupdrives)
            {
                Drive.Serialize();
            }
            string Code = JsonConvert.SerializeObject(Backupdrives);
            File.WriteAllText(CurrentDir + "\\config\\backup.json", Code);

            #region UI-changes
            //Warning2_label.Visibility = Visibility.Hidden;

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
