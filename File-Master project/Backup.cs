using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using Timer = System.Timers.Timer;

namespace File_Master_project
{
    public struct BackupSettings
    {
        public bool IsTempFolderEnabled {get;set;}
        public DirectoryInfo TempFolder { get;set;} 

        public BackupSettings(bool isTempFolterEnabled)
        {
            IsTempFolderEnabled = isTempFolterEnabled;
            TempFolder = null;
        }
    }

    public struct BackupTaskConfiguration
    {
        [JsonProperty] public char Method { get; } // F -> Full , I -> Incremental, D -> Differential
        [JsonProperty] public int CycleLength { get; }
        [JsonProperty] public int NumberOfCycles { get; }
        [JsonProperty] public Interval CycleInterval  { get; }
        [JsonProperty] public bool OnlySaveOnChange  { get; }
        [JsonProperty] public DiskSpace MaxStorageSpace { get; }
        [JsonProperty] public Interval RetryWaitTime  { get; }
        [JsonProperty] public int MaxNumberOfRetries  { get; }
        [JsonProperty] public bool PopupOnFail  { get; }
        [JsonProperty] public bool FileCompression  { get; }

        [JsonConstructor] public BackupTaskConfiguration(char method, int cycleLength, int numberOfCycles, Interval cycleInterval, bool onlySaveOnChange, DiskSpace maxStorageSpace, Interval retryWaitTime, int maxNumberOfRetries, bool popupOnFail, bool fileCompression)
        {
            Method = method;
            CycleLength = cycleLength;
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
        [JsonProperty] public string Root { get; }
        [JsonProperty] public List<string> Files { get; }
        [JsonProperty] public List<string> Folders { get; }
        [JsonProperty] public DiskSpace Size { get; }
        [JsonProperty] public DateTime Creation { get; }
        [JsonProperty] public Backup Reference { get; }
        [JsonIgnore] public bool Partial { get { if (Reference != null) return true; else return false; } } //partial if it is not a full backup (differential / incremental)
        [JsonIgnore] public int NumberID
        {
            get
            {
                string SID = new DirectoryInfo(Root).Name;
                int ID;
                SID = SID.Trim();
                if (int.TryParse(SID, out ID))
                {
                    return ID;
                }
                else return -1;
            }
        }

        // constructor for storing a folder
        public Backup(string root, List<string> files, List<string> folders, Backup reference = null)
        {
            Files = files;
            Folders = folders;
            Creation = DateTime.Now;
            Size = GetSize();
            Reference = reference;
            Root = root;
        }

        // constructor for storing one file
        public Backup(string root, string file, Backup reference = null)
        {
            Files = new List<string> { file };
            Folders = new List<string>();
            Creation = DateTime.Now;
            Size = GetSize();
            Reference = reference;
            Root = root;
        }

        private DiskSpace GetSize()
        {
            DiskSpace BackupSize = new DiskSpace(0);
            foreach (var item in Files)
            {
                FileInfo file = new FileInfo(item);
                BackupSize.Bytes += file.Length;
            }
            return BackupSize;
        }

        public bool CheckIntegrity()
        {
            bool result = true;
            foreach (var file in Files)
            {
                if (!File.Exists(file)) result = false;
            }
            foreach (var folder in Folders)
            {
                if (!Directory.Exists(folder)) result = false;
            }
            if (Size.Bytes != GetSize().Bytes) result = false;
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
        public bool FoldersCreated;
        public int AllFiles;
        public int FinisedFiles;
        public DiskSpace AllData;
        public DiskSpace FinishedData;
        public string NextItem;

        public BackupProgressReportModel(BackupTask item, int allFiles, int finisedFiles, DiskSpace allData, DiskSpace finishedData, string nextItem)
        {
            Item = item;
            FoldersCreated = true;
            AllFiles = allFiles;
            FinisedFiles = finisedFiles;
            AllData = allData;
            FinishedData = finishedData;
            NextItem = nextItem;
        }
        public BackupProgressReportModel(BackupTask item)
        {
            Item = item;
            FoldersCreated = false;
            AllFiles = 0;
            FinisedFiles = 0;
            AllData = null;
            FinishedData = null;
            NextItem = null;
        }
    }

    public class BackupTask
    {
        [JsonProperty] public int ID { get; private set; }
        [JsonProperty] public string Label { get; private set; }
        [JsonIgnore] public FileSystemInfo Source { get; private set; }
        [JsonProperty] private string SourcePath;
        [JsonIgnore] public DirectoryInfo Destination { get; set; }
        [JsonProperty] private string DestinationPath;
        [JsonIgnore] public string RootDirectoty
        {
            get
            {
                if (Label != null && Label != "")
                {
                    return $@"{Destination.FullName}\{Label}";
                }
                else
                {
                    return $@"{Destination.FullName}\{Source.Name}";
                }
            }
        }
        [JsonIgnore] public BackupTaskConfiguration Configuration { get; private set; }
        [JsonIgnore] public List<Backup> Backups { get; private set; }
        [JsonIgnore] public DateTime LastSaved { 
            get 
            {
                return DateTime.Today;
            } 
        }
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
        [JsonIgnore] public BackupDrive BackupDriveOfItem { get { return BackupProcess.GetBackupDriveFromBackupTask(this); } }

        [JsonIgnore] private Timer Backuptimer = new Timer(); //Timer for the next backup task call
        [JsonIgnore] private Task<Backup> CurrentTask;
        [JsonIgnore] public bool ActiveTask { get { return CurrentTask != null && CurrentTask.Status == TaskStatus.Running; } }
        [JsonIgnore] public CancellationTokenSource CancelBackup { get; private set; }

        [JsonConstructor] private BackupTask(int iD, string sourcePath, string destinationPath, bool isEnabled)
        {
            ID = iD;
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
            CancelBackup = new CancellationTokenSource();
            Destination = new DirectoryInfo(DestinationPath);
            Source = GetPathInfo(SourcePath);
            IsEnabled = isEnabled;
            LoadBackupConfig();
            LoadBackupInfo();
            // start timer
        }

        public BackupTask(int iD, string sourcePath, string destinationPath, BackupTaskConfiguration configuration)
        {
            ID = iD;
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
            Configuration = configuration;
            CancelBackup = new CancellationTokenSource();
            Destination = new DirectoryInfo(DestinationPath);
            Source = GetPathInfo(SourcePath);
            Backups = new List<Backup>();
            IsEnabled = false;
            StoreBackupConfig();
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
            Backup Target = null;           
            if (Backups.Count < (Configuration.NumberOfCycles * Configuration.CycleLength)) return Target;
            foreach (var item in Backups)
            {
                if (Target == null || item.Creation.Ticks < Target.Creation.Ticks)
                {
                    Target = item;
                }
            }
            /*if (Target == null || Backups.Count < this.Configuration.NumberOfCycles)
            {
                Backups.Add(Target);
            }*/
            return Target;
        }

        #endregion

        #region Backup management
        public async Task BackupRequest_Async(bool isManual, Backup OutdatedBackup)
        {
            if (CheckPermission(isManual))
            {
                bool done = false;
                try
                {
                    Progress<BackupProgressReportModel> progress = new Progress<BackupProgressReportModel>();
                    progress.ProgressChanged += BackupProcess.DisplayBackupProgress;

                    CurrentTask = Task.Run(() => CreateBackup(progress, Source));
                    BackupProcess.BackupTasks.Add(this, CurrentTask);
                    Backup NewBackup = await CurrentTask;
                    if(OutdatedBackup != null) Backups.Remove(OutdatedBackup);
                    Backups.Add(NewBackup);
                    done = true;
                }
                catch (Exception error)
                {
                    if (isManual) MessageBox.Show(error.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    //else
                    //LOG
                }

                if (isManual)
                {
                    if (done) MessageBox.Show("The operation was successful!", "Manual save report", MessageBoxButton.OK, MessageBoxImage.Information);
                    else if(isManual) MessageBox.Show("The operation was unsuccessful!", "Manual save report", MessageBoxButton.OK, MessageBoxImage.Error);
                    //else
                    //LOG
                }
                BackupProcess.BackupTasks.Remove(this);
                CurrentTask = null;
            }
            StartTimer();
            CancelBackup = new CancellationTokenSource();
        }

        private Backup CreateBackup(IProgress<BackupProgressReportModel> ProgressReport, FileSystemInfo Source, string Destination)
        {
            Backup Result;
            BackupProgressReportModel Progress;
            if (((Source.Attributes & FileAttributes.System) == FileAttributes.System))
            {
                throw new Exception("System files are not allowed to be accessed!");
            }
            try
            {
                if ((Source.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    List<string> SourceFiles, BackupFiles = new List<string>();
                    List<string> SourceFolders, BackupFolders = new List<string>();
                    GetDirectoryContent((DirectoryInfo)Source, out SourceFiles, out SourceFolders);
                    foreach (var SourceItem in SourceFolders)
                    {
                        string Target = ConvertPath_Destination(SourceItem);
                        BackupFolders.Add(Directory.CreateDirectory(Target).FullName);

                        Progress = new BackupProgressReportModel(this);
                        ProgressReport.Report(Progress);
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
                        Progress = new BackupProgressReportModel(this, SourceFiles.Count, BackupFiles.Count, All, Finished, new FileInfo(SourceItem).Name);
                        ProgressReport.Report(Progress);

                        string Target = ConvertPath_Destination(SourceItem);
                        BackupFiles.Add(Target);
                        CopyFile(SourceItem, Target, ProgressReport, Progress, CancelBackup.Token);
                    }
                    Progress = new BackupProgressReportModel(this, SourceFiles.Count, BackupFiles.Count, All, Finished, "none");
                    ProgressReport.Report(Progress);
                    Result = new Backup(Destination, BackupFiles, BackupFolders);
                }
                else
                {
                    FileInfo SourceFile = new FileInfo(Source.FullName);
                    string BackupFile = ConvertPath_Destination(SourceFile.FullName);
                    #region DiskSpaces
                    DiskSpace All = new DiskSpace(SourceFile.Length);
                    DiskSpace Finished = new DiskSpace(0);
                    #endregion
                    Progress = new BackupProgressReportModel(this, 1, 0, All, Finished, SourceFile.Name);
                    ProgressReport.Report(Progress);

                    Directory.CreateDirectory(new FileInfo(BackupFile).Directory.FullName);
                    CopyFile(SourceFile.FullName, BackupFile, ProgressReport, Progress, CancelBackup.Token);

                    Progress = new BackupProgressReportModel(this, 1, 1, All, Finished, "none");
                    ProgressReport.Report(Progress);
                    Result = new Backup(Destination, BackupFile);
                }

                BackupProcess.Upload_BackupInfo();
                return Result;
            }
            catch (Exception error)
            {
                throw new Exception($"{error.Message}");
            }
        }

        private void CopyFile(string source, string destination, IProgress<BackupProgressReportModel> progressreport, BackupProgressReportModel progress, CancellationToken CancelOperation)
        {
            FileStream Input = new FileStream(source, FileMode.Open, FileAccess.Read);
            FileStream Output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write);

            while (Input.Position != Input.Length)
            {
                int Kb = 1024;
                byte[] Buffer = new byte[64 * Kb];
                int count = Input.Read(Buffer, 0, Buffer.Length);
                Output.Write(Buffer, 0, count);
                progress.FinishedData.Bytes += count;
                progressreport.Report(progress);
                if(CancelOperation.IsCancellationRequested)
                {
                    Input.Close();
                    Output.Close();
                    CancelOperation.ThrowIfCancellationRequested();
                }
            }
            Input.Close();
            Output.Close();
        }

        private void GetDirectoryContent(DirectoryInfo MainDir, out List<string> Files, out List<string> Folders)
        {
            Files = new List<string>();
            Folders = new List<string>();
            Folders.Add(MainDir.FullName);
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

        public void DeleteBackup(Backup Item)
        {

        }

        public void RecoverBackup(Backup Item)
        {

        }

        private void StoreBackupConfig()
        {
            string data = JsonConvert.SerializeObject(Configuration, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText($@"{RootDirectoty}\configuration.json", data);
        }

        private void LoadBackupConfig()
        {
            string path = $@"{RootDirectoty}\configuration.json";
            try
            {
                string data = File.ReadAllText(path);
                Configuration = JsonConvert.DeserializeObject<BackupTaskConfiguration>(data);
            }
            catch (Exception)
            {
                //LOG
                throw;
            }
        }

        private void StoreBackupInfo()
        {
            string data = JsonConvert.SerializeObject(Backups, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText($@"{RootDirectoty}\backups.json", data);
        }

        private void LoadBackupInfo()
        {
            string path = $@"{RootDirectoty}\backups.json";
            if (File.Exists(path))
            {
                try
                {
                    string data = File.ReadAllText(path);
                    Backups = JsonConvert.DeserializeObject<List<Backup>>(data);
                }
                catch (Exception)
                {
                    //LOG
                    throw;
                }
            }
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
        [JsonProperty] public List<BackupTask> BackupTasks { get; private set; } = new List<BackupTask>();

        [JsonConstructor] public BackupDrive(string driveID, string defaultVolumeLabel, DiskSpace sizeLimit, List<BackupTask> backupTasks)
        {
            DriveID = driveID;
            DefaultVolumeLabel = defaultVolumeLabel;
            BackupTasks = backupTasks;
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
            BackupProcess.Upload_BackupInfo();
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
                    UpdateBackupTaskDestination();
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
        public void AddBackupTask(BackupTask Item)
        {
            BackupTasks.Add(Item);
        }

        public void RemoveBackupTask(BackupTask Item)
        {
            Item.DeleteBackups();
            BackupTasks.Remove(Item);
        }

        public void SetBackupTaskState(bool State, BackupTask Item)
        {
            Item.IsEnabled = State;
        }

        private void UpdateBackupTaskDestination()
        {
            foreach (var item in BackupTasks)
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
            foreach (var BackupItem in BackupTasks)
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
            foreach (var item in BackupTasks)
            {
                space.Bytes += item.GetBackupsSize().Bytes;
            }
            return space;
        }
        #endregion

        #region Backup
        public async void Backup_Async()
        {
            foreach (var item in BackupTasks)
            {
                await item.BackupRequest_Async(true, item.SelectNextBackup());
            }
        }
        #endregion
    }

    static public class BackupProcess 
    {
        static public List<BackupDrive> BackupDrives { get; private set; }
        static public BackupSettings Settings { get; private set; }
        static public Dictionary<string, AdvancedDriveInfo> AllDriveInfo { get; } //key: serial number , value: DriveInfo
        static public Dictionary<BackupTask, Task<Backup>> BackupTasks;
        public delegate void UIChanges();

        static BackupProcess()
        {
            BackupDrives = new List<BackupDrive>();
            AllDriveInfo = new Dictionary<string, AdvancedDriveInfo>();
            BackupTasks = new Dictionary<BackupTask, Task<Backup>>();
            LoadAllDriveInfo();
            LoadBackupProcess();
            Upload_BackupInfo();
        }

        #region Actions
        static public void ActivateBackupDrive(string Serial, DiskSpace SizeLimit)
        {
            AllDriveInfo.TryGetValue(Serial, out AdvancedDriveInfo Value);
            BackupDrives.Add(new BackupDrive(Serial, Value.DriveInformation.VolumeLabel, SizeLimit));
            Upload_BackupInfo();
        }

        static public void DeactivateBackupDrive(string Serial)
        {
            if (MessageBox.Show("Are you sure you want to remove this Backupdrive?\nAll of its backup tasks will be deleted, but not the bakcup files!", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning).Equals(MessageBoxResult.Yes))
            {
                BackupDrives.Remove(GetBackupDriveFromSerial(Serial));
                Upload_BackupInfo();
            }
        }
        #endregion

        #region Get Data
        static public string GetHardDiskSerialNumber(string drive)//not my code : https://ukacademe.com/TutorialExamples/CSharp/Get_Serial_Number_of_Hard_Drive
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

        static public BackupDrive GetBackupDriveFromSerial(string Serial)
        {
            BackupDrive result = null;
            foreach (var Drive in BackupDrives)
            {
                if (Drive.DriveID == Serial) result = Drive;
            }
            return result;
        }

        static public BackupDrive GetBackupDriveFromBackupTask(BackupTask Task)
        {
            foreach (var Drive in BackupDrives)
            {
                foreach (var Item in Drive.BackupTasks)
                {
                    if (Item == Task)
                    {
                        return Drive;
                    }
                }
            }
            return null;
        }

        static public bool IsBackupdrive(string serial)
        {
            foreach (var Drive in BackupDrives)
            {
                if (Drive.DriveID == serial) return true;
            }
            return false;
        }

        static public int GetNewBackupID()
        {
            List<int> IDs = new List<int>();
            foreach (var Drive in BackupDrives)
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
            Load_BackupInfo(out string Backupinfo);
            try
            {
                BackupDrives = JsonConvert.DeserializeObject<List<BackupDrive>>(Backupinfo);
                if (BackupDrives == null || !IntegrityCheck())
                {
                    BackupDrives = new List<BackupDrive>();
                    throw null;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to load in the user data due to data corruption!\nAll backup configurations are cleared!", "Data corruption!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                Directory.CreateDirectory(@".\config\corrupted");
                string dest = $@".\config\corrupted\{DateTime.Now.ToString("yyyyMMddHHmm")}_backup.json";
                if (File.Exists(dest)) File.Delete(dest);
                File.Move(@".\config\backup.json", dest);
                Upload_BackupInfo();
            }
            #endregion

            #region BackupSettings_Global
            Settings = new BackupSettings(false);
            #endregion
        }

        static private void Load_BackupInfo(out string BackupInfo)
        {
            BackupInfo = "[]";
            string filepath = @".\config\backup.json";
            if (File.Exists(filepath))
            {
                BackupInfo = File.ReadAllText(filepath);
            }
            else
            {
                File.WriteAllText(filepath, BackupInfo);
            }
        }

        static private void LoadAllDriveInfo()
        {
            DriveInfo[] AllDrives = DriveInfo.GetDrives();
            foreach (var Drive in AllDrives)
            {
                if (Drive.IsReady)
                {
                    string Serial = GetHardDiskSerialNumber($"{Drive.Name[0]}");
                    if (AllDriveInfo.ContainsKey(Serial)) // delete drives with conflicting serials
                    {
                        AllDriveInfo.Remove(Serial);
                        //LOG
                    }
                    else
                    {
                        AllDriveInfo.Add(Serial, new AdvancedDriveInfo(Drive, Serial));
                    }
                }
            }
        }

        static private bool IntegrityCheck()
        {
            bool Intact = true;
            foreach (var Drive in BackupDrives)
            {
                if(Drive.DriveID == null) Intact = false;
                else if(Drive.SizeLimit == null) Intact = false;
                else
                {
                    foreach (var Item in Drive.BackupTasks)
                    {
                        if (Item.Source == null) Intact = false;
                        else if (Item.Destination == null) Intact = false;                  
                        else if (Item.LastSaved == null) Intact = false;
                        //else if (Item.Configuration == null) Intact = false;
                        else if (Item.Configuration.CycleInterval == null) Intact = false;
                        else if (Item.Configuration.RetryWaitTime == null) Intact = false;
                    }
                }
            }
            return Intact;
        }
        #endregion

        #region Upload Data
        static public void Upload_BackupInfo()
        {
            string Code = JsonConvert.SerializeObject(BackupDrives, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText( @".\config\backup.json", Code);
            #region UI-changes
            
            #endregion
        }
        #endregion

        #region Backup
        static public async Task Manualsave_Async(BackupTask Item)
        {
            await Item.BackupRequest_Async(true, Item.SelectNextBackup());
        }

        static private void ManualsaveALL()
        {
            foreach (var Drive in BackupDrives)
            {
                Drive.Backup_Async();
            }
        }

        static public void DisplayBackupProgress(object sender, BackupProgressReportModel report)
        {
            Application.Current.Dispatcher.Invoke(() => {
                MainWindow MW = Application.Current.Windows[0] as MainWindow;
                if (MW.GetSelectedBackupTask() == report.Item)
                {
                    if(report.FoldersCreated)
                    {
                        MW.BackupProgress_progressbar.Value = report.Percentage;
                        MW.BackupProgressPercentage_label.Content = $"{report.Percentage} % complete";
                        string CurrentItem = $"Current item: {report.NextItem}";
                        string RemainingItems = $"Items remaining: {report.AllFiles - report.FinisedFiles}";
                        DiskSpace RemainingData = new DiskSpace(report.AllData.Bytes - report.FinishedData.Bytes);
                        MW.BackupProgressData_label.Content = $"{CurrentItem}\n{RemainingItems} ({RemainingData.Humanize()})";
                        if (report.Percentage < 100) { MW.CancelBackupOperation_button.IsEnabled = true; MW.CancelBackupOperation_button.Opacity = 1; }
                        else { MW.CancelBackupOperation_button.IsEnabled = false; MW.CancelBackupOperation_button.Opacity = 0.5; }
                        }
                    else
                    {
                        MW.BackupProgress_progressbar.Value = 0;
                        MW.BackupProgressPercentage_label.Content = $"0% complete";
                        MW.BackupProgressData_label.Content = $"Creating folders...";
                        MW.CancelBackupOperation_button.IsEnabled = true;
                        MW.CancelBackupOperation_button.Opacity = 1;
                    }
                }
            });
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
