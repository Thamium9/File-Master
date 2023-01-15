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
using System.ComponentModel;

namespace File_Master_project
{
    public class Backup_Settings
    {
        public bool IsTempfolderEnabled = false;
        public DirectoryInfo TempFolder;
    }

    public class BackupTask_Settings
    {
        [JsonProperty] public char Method { get; } // F -> Full , I -> Incremental, D -> Differential
        [JsonProperty] public int NumberOfCycles { get; }
        [JsonProperty] public Interval CycleInterval  { get; }
        [JsonProperty] public bool OnlySaveOnChange  { get; }
        [JsonProperty] public int MaxStorageSpace { get; }
        [JsonProperty] public Interval RetryWaitTime  { get; }
        [JsonProperty] public int MaxNumberOfRetries  { get; }
        [JsonProperty] public bool PopupOnFail  { get; }
        [JsonProperty] public bool FileCompression  { get; }

        public BackupTask_Settings(char method, int numberOfCycles, Interval cycleInterval, bool onlySaveOnChange, int maxStorageSpace, Interval retryWaitTime, int maxNumberOfRetries, bool popupOnFail, bool fileCompression)
        {
            Method = method;
            NumberOfCycles = numberOfCycles;
            CycleInterval = cycleInterval;
            OnlySaveOnChange = onlySaveOnChange;
            MaxStorageSpace = maxStorageSpace;
            RetryWaitTime = retryWaitTime;
            MaxNumberOfRetries = maxNumberOfRetries;
            PopupOnFail = popupOnFail;
            FileCompression = fileCompression;
        }
    }

    public class Backup
    {
        //public int ID { get; }
        //public bool Partial { get; } //partial if it is not a full backup (differential / incremental)
        //public DiskSpace Size { get; }
        //public DateTime Creation { get; }
        public BackupProgressReportModel Progress { get; set; }
        public string Root { get; }
        public List<string> Files { get; }
        public List<string> Folders { get; }
        /*public bool IsEmpty { 
            get { 
                if (Files.Count == 0) return true; 
                else return false;
            } 
        }*/

        public Backup(List<string> files, List<string> folders)
        {
            Files = files;
            Folders = folders;
        }

        public Backup()
        {

        }

        public bool CheckIntegrity()
        {
            bool result = true;
            foreach (var file in Files)
            {
                if(!File.Exists(file)) result = false;
            }
            foreach (var folder in Folders)
            {
                if (!Directory.Exists(folder)) result = false;
            }
            return result;
        }

        public void DeleteBackup()
        {
            Directory.Delete(Root, true);
        }
    }

    public struct BackupProgressReportModel
    {
        public double Percentage { get { return Math.Round((double)((FinishedData.Bytes * 100) / AllData.Bytes), 1); } }
        public BackupTask Item;
        public int AllFiles;
        public int FinisedFiles;
        public DiskSpace AllData;
        public DiskSpace FinishedData;
        public string NextItem;

        public BackupProgressReportModel(BackupTask item, int allFiles, int finisedFiles, DiskSpace allData, DiskSpace finishedData, string nextItem)
        {
            Item = item;
            AllFiles = allFiles;
            FinisedFiles = finisedFiles;
            AllData = allData;
            FinishedData = finishedData;
            NextItem = nextItem;
        }
    }

    public class BackupTask
    {
        [JsonProperty] public int ID { get; private set; }
        [JsonProperty] public string Label { get; set; }
        [JsonIgnore] public FileSystemInfo Source { get; private set; }
        [JsonProperty] private string SourcePath;
        [JsonIgnore] public DirectoryInfo Destination { get; set; }
        [JsonProperty] private string DestinationPath;
        [JsonIgnore] public string RootDirectoty {
            get { 
                if(Label != null && Label != "")
                {
                    return $@"{Destination.FullName}\{Label}";
                }
                else
                {
                    return $@"{Destination.FullName}\{Source.Name}";
                }
            } 
        }
        [JsonProperty] public DateTime LastSaved { get; private set; }
        [JsonProperty] public Backup Backups { get; private set; }
        [JsonProperty] public bool IsEnabled { get; set; }
        [JsonIgnore] public bool IsAvailable { 
            get 
            {
                if(BackupDriveOfItem == null) return false;
                else return BackupDriveOfItem.IsAvailable;
            }
        }
        [JsonIgnore] public bool IsOutOfSpace { 
            get 
            {
                if (BackupDriveOfItem == null) return true;
                return BackupDriveOfItem.IsOutOfSpace; 
            } 
        }
        [JsonIgnore] public BackupDrive BackupDriveOfItem { get { return BackupProcess.GetBackupdriveFromBackupitem(this); } }
        [JsonProperty] public BackupTask_Settings Configuration { get; set; }
        [JsonIgnore] private Timer Backuptimer = new Timer(); //Timer for the next backup task call
        [JsonIgnore] private Task<Backup> CurrentTask;
        [JsonIgnore] public bool ActiveTask { get { return CurrentTask != null && CurrentTask.Status == TaskStatus.Running; } }

        [JsonConstructor] public BackupTask(int iD, string sourcePath, string destinationPath, DateTime lastSaved, bool isEnabled, BackupTask_Settings configuration)
        {
            ID = iD;
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
            LastSaved = lastSaved;
            IsEnabled = isEnabled;
            Configuration = configuration;
            Destination = new DirectoryInfo(DestinationPath);
            Source = GetPathInfo(SourcePath);
            // start timer
        }

        #region Get data
        private DateTime GetNextCallTime()
        {
            return LastSaved.AddTicks(Configuration.CycleInterval.Convert_to_ticks());
        }

        public DiskSpace GetBackupsSize()
        {
            DiskSpace space = new DiskSpace(0);
            if ((Source.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                DirectoryInfo backup = new DirectoryInfo($@"{Destination.FullName}\{Source.Name}");
                if (backup.Exists)
                {
                    foreach (var item in Directory.GetFiles(backup.FullName, "*", SearchOption.AllDirectories))
                    {
                        space.Bytes += new FileInfo(item).Length;
                    }
                }
            }
            else
            {
                FileInfo backup = new FileInfo($@"{Destination.FullName}\{Source.Name}");
                if (backup.Exists)
                {
                    space.Bytes += backup.Length;
                }
            }
            return space;
        }

        private FileSystemInfo GetPathInfo(string Path)
        {
            if (Directory.Exists(Path)) return new DirectoryInfo(Path);
            else return new FileInfo(Path);
        }

        public string GetBackupType()
        {
            if (Source.GetType() == typeof(DirectoryInfo)) return "Folder";
            else if (Source.GetType() == typeof(FileInfo)) return "File";
            else return "Unknown";
        }

        public Backup SelectNextBackup()
        {
            return null;
        }

        #endregion

        #region Backup process (SAVEING)
        public async Task Backup_Async(bool isManual, Backup Target)
        {
            if (CheckPermission(isManual))
            {
                try
                {
                    BackgroundWorker bg = new BackgroundWorker();
                    bg.WorkerReportsProgress = true;
                    Progress<BackupProgressReportModel> progress = new Progress<BackupProgressReportModel>();
                    progress.ProgressChanged += BackupProcess.DisplayBackupProgress;

                    CurrentTask = Task.Run(() => CreateBackup(progress, Source));
                    BackupProcess.BackupTasks.Add(this, CurrentTask);
                    Target = await CurrentTask;
                }
                catch (Exception error)
                {
                    if (isManual) MessageBox.Show(error.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    //else
                    //LOG
                }

                if (isManual)
                {
                    if (Target != null) MessageBox.Show("The operation was successful!", "Manual save report", MessageBoxButton.OK, MessageBoxImage.Information);
                    else if(isManual) MessageBox.Show("The operation was unsuccessful!", "Manual save report", MessageBoxButton.OK, MessageBoxImage.Error);
                    //else
                    //LOG
                }
                BackupProcess.BackupTasks.Remove(this);
                CurrentTask = null;
            }
            StartTimer();
        }

        private Backup CreateBackup(IProgress<BackupProgressReportModel> ProgressReport, FileSystemInfo Source, List<Backup> Previous = null)
        {
            Backup Result;
            if (((Source.Attributes & FileAttributes.System) == FileAttributes.System))
            {
                throw new Exception("System files are not allowed to be accessed!");
            }
            try
            {
                if ((Source.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    Result = new Backup();
                    List<string> SourceFiles, BackupFiles = new List<string>();
                    List<string> SourceFolders, BackupFolders = new List<string>();
                    GetDirectoryContent((DirectoryInfo)Source, out SourceFiles, out SourceFolders);
                    foreach (var SourceItem in SourceFolders)
                    {
                        string Target = ConvertPath_Destination(SourceItem);
                        BackupFolders.Add(Directory.CreateDirectory(Target).FullName);
                    }
                    #region DiskSpaces
                    DiskSpace All = new DiskSpace(0);
                    foreach (var item in SourceFiles)
                    {
                        FileInfo file = new FileInfo(item);
                        All.Bytes += file.Length;
                    }
                    DiskSpace Finished = new DiskSpace(0);
                    #endregion
                    foreach (var SourceItem in SourceFiles)
                    {
                        Result.Progress = new BackupProgressReportModel(this, SourceFiles.Count, BackupFiles.Count, All, Finished, SourceItem);
                        ProgressReport.Report(Result.Progress);

                        string Target = ConvertPath_Destination(SourceItem);
                        var Error = CopyFile(SourceItem, Target, ProgressReport, Result.Progress).Exception;
                        if(Error != null) throw new Exception($"Error during the file operaiton: {Error}");
                        BackupFiles.Add(Target);
                    }
                    Result = new Backup(BackupFiles, BackupFolders);
                }
                else
                {
                    Result = new Backup();
                    string SourceFile = Source.FullName;
                    string BackupFile = ConvertPath_Destination(SourceFile);
                    List<string> BackupFiles = new List<string>();                          
                    var Error = CopyFile(SourceFile, BackupFile, ProgressReport, Result.Progress).Exception;
                    if (Error != null) throw new Exception($"Error during the file operaiton: {Error}");
                    BackupFiles.Add(BackupFile);
                    Result = new Backup(BackupFiles, null);
                }

                LastSaved = DateTime.Now;
                BackupProcess.Upload_Backupinfo();
                return Result;
            }
            catch (Exception error)
            {
                throw error;
            }
        }

        private async Task CopyFile(string source, string destination, IProgress<BackupProgressReportModel> progressreport, BackupProgressReportModel progress)
        {
            //Directory.CreateDirectory(destination.Directory.FullName);
            FileStream Input = new FileStream(source, FileMode.Open, FileAccess.Read);
            FileStream Output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write);

            while (Input.Position != Input.Length)
            {
                byte[] Buffer = new byte[1024];
                int count = Input.Read(Buffer, 0, Buffer.Length);
                Output.Write(Buffer, 0, count);
                progress.FinishedData.Bytes += count;
                progressreport.Report(progress);
            }
            Input.Close();
            Output.Close();
        }

        private void GetDirectoryContent(DirectoryInfo MainDir, out List<string> Files, out List<string> Folders)
        {
            Files = new List<string>();
            Folders = new List<string>();
            foreach (var directory in MainDir.GetDirectories("*", SearchOption.AllDirectories))
            {
                Folders.Add(directory.FullName);
            }
            foreach (var file in MainDir.GetFiles("*", SearchOption.AllDirectories))
            {
                Files.Add(file.FullName);
            }
        }

        private string ConvertPath_Destination(string Item)
        {
            string Result = Item;
            string Parent = Directory.GetParent(Source.FullName).FullName;
            Result = Result.Replace(Parent, $@"{RootDirectoty}");
            return Result;
        }

        private bool CheckPermission(bool isManual)
        {
            bool result = true;
            if (!isManual)
            {
                if (!IsEnabled) result = false;
            }
            if (!IsAvailable) result = false;
            return result;
        }

        private void Backuptimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now >= GetNextCallTime())
            {
                //Backup_Async(false);
            }
            else
            {
                StartTimer();
            }
        }

        private void StartTimer()
        {
            DateTime NextCall = LastSaved.AddTicks(Configuration.CycleInterval.Convert_to_ticks()); //this is the date when the next backup will happen
            TimeSpan diff = (NextCall - DateTime.Now);
            Backuptimer.Interval = Math.Min(Math.Max(60000, diff.Ticks / 10000), 2147483647); //the interval cannot be less than a second (or in this case 10000000 ticks or 1000 miliseconds)  AND  the interval cannot be more than 2147483647 miliseconds
            Backuptimer.Elapsed += Backuptimer_Elapsed;
            Backuptimer.AutoReset = false;
            Backuptimer.Start();
        }

        public void DeleteBackups()
        {
            //code
        }

        public void RecoverBackups()
        {

        }
        #endregion
    }

    public class BackupDrive
    {
        [JsonProperty] public string DriveID { get; private set; }
        [JsonProperty] private string DefaultVolumeLabel;
        [JsonIgnore] public DriveInfo DriveInformation { get; private set; }
        [JsonIgnore] public bool IsAvailable { get; private set; }
        [JsonIgnore] public bool IsOutOfSpace { get; private set; }
        [JsonProperty] public DiskSpace SizeLimit { get; set; }
        [JsonProperty] public List<BackupTask> Backups { get; private set; } = new List<BackupTask>();

        [JsonConstructor] public BackupDrive(string driveID, string defaultVolumeLabel, DiskSpace sizeLimit, List<BackupTask> backups)
        {
            DriveID = driveID;
            DefaultVolumeLabel = defaultVolumeLabel;
            Backups = backups;
            SizeLimit = sizeLimit;
            Update();
        }

        public BackupDrive(string driveID, string defaultVolumeLabel, DiskSpace sizeLimit)
        {
            DriveID = driveID;
            DefaultVolumeLabel = defaultVolumeLabel;
            SizeLimit = sizeLimit;
            ValidityCheck();           
            if(IsAvailable) LimitCheck();
            BackupProcess.Upload_Backupinfo();
        }

        public void Update()
        {
            ValidityCheck();
            if (IsAvailable)
            {
                if (SizeLimitCheck(out double limit))
                {
                    SizeLimit.Gigabytes = limit;
                }
                LimitCheck();
            }
        }

        #region Checks
        private void ValidityCheck() // sets the IsAvailable value
        {
            IsAvailable = true;
            foreach (var thisDriveInfo in BackupProcess.AllDriveInfo)
            {
                if (thisDriveInfo.Key == DriveID)
                {
                    DriveInformation = thisDriveInfo.Value.DriveInformation;
                    DefaultVolumeLabel = DriveInformation.VolumeLabel;
                    UpdateBackupitemDestination();
                }
            }
            if (DriveInformation == null) IsAvailable = false;
        }

        private void LimitCheck()// sets the isOutOfSpace value
        {
            if ((SizeLimit.Bytes > 0 && GetBackupSize().Bytes > SizeLimit.Bytes) || ((double)DriveInformation.AvailableFreeSpace < (double)DriveInformation.TotalSize * 0.1)) IsOutOfSpace = true;
            else IsOutOfSpace = false;
        }

        public bool SizeLimitCheck(out double result) //returns true if the result is a new limit, and false if no adjustment is needed
        {
            result = 0;
            long SizeLimitReamining = SizeLimit.Bytes - GetBackupSize().Bytes;
            if (((double)DriveInformation.TotalSize * 0.1) > (DriveInformation.AvailableFreeSpace - SizeLimitReamining))
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
        #endregion

        #region Modify backupitems
        public void AddBackupitem(BackupTask Item)
        {
            Backups.Add(Item);
        }

        public void RemoveBackupitem(BackupTask Item)
        {
            Item.DeleteBackups();
            Backups.Remove(Item);
        }

        public void SetBackupitemState(bool State, BackupTask Item)
        {
            Item.IsEnabled = State;
        }

        private void UpdateBackupitemDestination()
        {
            foreach (var item in Backups)
            {
                StringBuilder value = new StringBuilder(item.Destination.FullName);
                value[0] = DriveInformation.Name[0];
                item.Destination = new DirectoryInfo(value.ToString());
            }
        }
        #endregion

        #region Get data
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
                space.Bytes += item.GetBackupsSize().Bytes;
            }
            return space;
        }
        #endregion

        #region Backup
        public async void Backup_Async()
        {
            foreach (var item in Backups)
            {
                await item.Backup_Async(true, item.Backups);
            }
        }
        #endregion
    }

    static public class BackupProcess 
    {
        static public List<BackupDrive> Backupdrives { get; private set; }
        static public Backup_Settings Settings { get; set; }
        static public Dictionary<string, AdvancedDriveInfo> AllDriveInfo { get; } = new Dictionary<string, AdvancedDriveInfo>(); //key: serial number , value: DriveInfo
        static public Dictionary<BackupTask, Task<Backup>> BackupTasks = new Dictionary<BackupTask, Task<Backup>>();
        public delegate void UIChanges();

        static BackupProcess()
        {
            Backupdrives = new List<BackupDrive>();
            LoadAllDriveInfo();
            LoadBackupProcess();
            Upload_Backupinfo();
        }

        #region Actions
        static public void ActivateBackupdrive(string Serial, DiskSpace SizeLimit)
        {
            AllDriveInfo.TryGetValue(Serial, out AdvancedDriveInfo Value);
            Backupdrives.Add(new BackupDrive(Serial, Value.DriveInformation.VolumeLabel, SizeLimit));
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

        static public BackupDrive GetBackupdriveFromSerial(string Serial)
        {
            BackupDrive result = null;
            foreach (var Drive in Backupdrives)
            {
                if (Drive.DriveID == Serial) result = Drive;
            }
            return result;
        }

        static public BackupDrive GetBackupdriveFromBackupitem(BackupTask Target)
        {
            foreach (var Drive in Backupdrives)
            {
                foreach (var Item in Drive.Backups)
                {
                    if (Item == Target)
                    {
                        return Drive;
                    }
                }
            }
            return null;
        }

        static public bool IsBackupdrive(string serial)
        {
            foreach (var Drive in Backupdrives)
            {
                if (Drive.DriveID == serial) return true;
            }
            return false;
        }

        static public int GetNewBackupID()
        {
            List<int> IDs = new List<int>();
            foreach (var Drive in Backupdrives)
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
            return newID;
        }
        #endregion

        #region Load Data
        static private void LoadBackupProcess()
        {
            #region Backupdrives           
            Load_backupinfo(out string Backupinfo);
            try
            {
                Backupdrives = JsonConvert.DeserializeObject<List<BackupDrive>>(Backupinfo);
                if (Backupdrives == null || !IntegrityCheck())
                {
                    Backupdrives = new List<BackupDrive>();
                    throw null;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to load in the user data due to data corruption!\nAll backup configurations are cleared!", "Data corruption!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                Directory.CreateDirectory(@".\config\corrupted");
                File.Move(@".\config\backup.json", @".\config\corrupted\backup.json");
                Upload_Backupinfo();
            }
            #endregion

            #region BackupSettings_Global
            Settings = new Backup_Settings();
            #endregion
        }

        static private void Load_backupinfo(out string Backupinfo)
        {
            Backupinfo = "[]";
            string filepath = @".\config\backup.json";
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
                    if (AllDriveInfo.ContainsKey(Serial))
                    {
                        AllDriveInfo.Remove(Serial);
                        //LOG
                    }
                    AllDriveInfo.Add(Serial, new AdvancedDriveInfo(Drive, Serial));
                }
            }
        }

        static private bool IntegrityCheck()
        {
            bool Intact = true;
            foreach (var Drive in Backupdrives)
            {
                if(Drive.DriveID == null) Intact = false;
                else if(Drive.SizeLimit == null) Intact = false;
                else
                {
                    foreach (var Item in Drive.Backups)
                    {
                        if (Item.Source == null) Intact = false;
                        else if (Item.Destination == null) Intact = false;                  
                        else if (Item.LastSaved == null) Intact = false;
                        else if (Item.Configuration == null) Intact = false;
                        else if (Item.Configuration.CycleInterval == null) Intact = false;
                        else if (Item.Configuration.RetryWaitTime == null) Intact = false;
                    }
                }
            }
            return Intact;
        }
        #endregion

        #region Upload Data
        static public void Upload_Backupinfo()
        {
            string Code = JsonConvert.SerializeObject(Backupdrives, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText( @".\config\backup.json", Code);
            #region UI-changes
            
            #endregion
        }
        #endregion

        #region Backup
        static public async Task Manualsave_Async(BackupTask Item)
        {
            await Item.Backup_Async(true, Item.Backups);
        }

        static private void ManualsaveALL()
        {
            foreach (var Drive in Backupdrives)
            {
                Drive.Backup_Async();
            }
        }

        static public void DisplayBackupProgress(object sender, BackupProgressReportModel report)
        {
            /*BackupProcess.Dispatcher.Invoke(() =>
            {
                MainWindow mainWindow = Application.Current.Windows[0] as MainWindow;
                if (mainWindow.GetSelectedBackupTask() == report.Item)
                {
                    mainWindow.BackupProgress_progressbar.Value = report.Percentage;
                }
            });
            */
        }
        #endregion
    }

    public class AdvancedDriveInfo
    {
        public DriveInfo DriveInformation;
        public string MediaType 
        { 
            get 
            {
                if (_MediaType == null)
                {
                    _MediaType = GetMediaType();
                    return _MediaType;
                }
                else return _MediaType;
            }
        }
        private string _MediaType;
        private string Serial;

        public AdvancedDriveInfo(DriveInfo drive, string serial)
        {
            DriveInformation = drive;
            Serial = serial;
        }

        private string GetMediaType() //code from: https://gist.github.com/MiloszKrajewski/352dc8b8eb132d3a2bc7
        {
            try
            {
                var driveQuery = new ManagementObjectSearcher("select * from Win32_DiskDrive");
                foreach (ManagementObject d in driveQuery.Get())
                {
                    var partitionQueryText = string.Format("associators of {{{0}}} where AssocClass = Win32_DiskDriveToDiskPartition", d.Path.RelativePath);
                    var partitionQuery = new ManagementObjectSearcher(partitionQueryText);
                    foreach (ManagementObject p in partitionQuery.Get())
                    {
                        var logicalDriveQueryText = string.Format("associators of {{{0}}} where AssocClass = Win32_LogicalDiskToPartition", p.Path.RelativePath);
                        var logicalDriveQuery = new ManagementObjectSearcher(logicalDriveQueryText);
                        foreach (ManagementObject ld in logicalDriveQuery.Get())
                        {
                            var volumeSerial = Convert.ToString(ld.Properties["VolumeSerialNumber"].Value); // 12345678
                            var mediaType = Convert.ToString(d.Properties["MediaType"].Value);
                            if (volumeSerial == Serial)
                            {
                                return mediaType;
                            }
                        }
                    }
                }
                return "";
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
