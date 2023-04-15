using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using Timer = System.Timers.Timer;

namespace File_Master_project
{
    public class BackupSettings
    {
        public bool IsTempFolderEnabled {get;set;}
        public DirectoryInfo TempFolder { get;set;} 

        public BackupSettings(bool isTempFolterEnabled)
        {
            IsTempFolderEnabled = isTempFolterEnabled;
            TempFolder = null;
        }
    }

    public class BackupTaskConfiguration
    {
        [JsonProperty] public string SourcePath { get; }
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

        public BackupTaskConfiguration(string sourcePath, char method, int cycleLength, int numberOfCycles, Interval cycleInterval, bool onlySaveOnChange, DiskSpace maxStorageSpace, Interval retryWaitTime, int maxNumberOfRetries, bool popupOnFail, bool fileCompression)
        {
            SourcePath = sourcePath;
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

    public struct BackupProgressReportModel
    {
        public double Percentage { get { return Math.Round((double)((FinishedData.Bytes * 100) / AllData.Bytes), 1); } }
        public BackupTask Item;
        public bool Recovery;
        public bool Preparation;
        public int AllFiles;
        public int FinisedFiles;
        public DiskSpace AllData;
        public DiskSpace FinishedData;
        public string NextItem;

        public BackupProgressReportModel(BackupTask item, bool recovery, int allFiles, int finisedFiles, DiskSpace allData, DiskSpace finishedData, string nextItem)
        {
            Item = item;
            Recovery = recovery;
            Preparation = false;
            AllFiles = allFiles;
            FinisedFiles = finisedFiles;
            AllData = allData;
            FinishedData = finishedData;
            NextItem = nextItem;
        }
        public BackupProgressReportModel(BackupTask item, bool recovery)
        {
            Item = item;
            Recovery = recovery;
            Preparation = true;
            AllFiles = 0;
            FinisedFiles = 0;
            AllData = null;
            FinishedData = null;
            NextItem = null;
        }
    }

    public class Backup
    {
        [JsonProperty] public string Root { get; private set; }
        [JsonProperty] public List<string> Files { get; private set; }
        [JsonProperty] public List<string> Folders { get; private set; }
        [JsonProperty] public DiskSpace Size { get; }
        [JsonProperty] public DateTime Creation { get; }
        [JsonProperty] public Backup Reference { get; }
        [JsonIgnore] public bool Partial { get { if (Reference != null) return true; else return false; } } //partial if it is not a full backup (differential / incremental)
        [JsonIgnore] public int NumberID
        {
            get
            {
                string SID = new DirectoryInfo(Root).Name.Split('-')[0];                
                int ID;
                SID = SID.Trim();
                if (int.TryParse(SID, out ID))
                {
                    return ID;
                }
                else return -1;
            }
        }

        [JsonConstructor] private Backup(string root, List<string> files, List<string> folders, DiskSpace size, DateTime creation, Backup reference)
        {
            Root = root;
            Files = files;
            Folders = folders;
            Size = size;
            Creation = creation;
            Reference = reference;
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

        public bool UpdateDriveLetter(char DriveLetter) //returns true if the function changed something (if nothing changed it returns false)
        {
            bool changed = false;
            List<string> UpdatedFolders = new List<string>();
            List<string> UpdatedFiles = new List<string>();
            StringBuilder PathBuilder;
            foreach (var folder in Folders)
            {
                PathBuilder = new StringBuilder(folder);
                if (PathBuilder[0] != DriveLetter)
                {
                    changed = true;
                    PathBuilder[0] = DriveLetter;
                }
                UpdatedFolders.Add(PathBuilder.ToString());
            }
            foreach (var file in Files)
            {
                PathBuilder = new StringBuilder(file);
                if (PathBuilder[0] != DriveLetter)
                {
                    changed = true;
                    PathBuilder[0] = DriveLetter;
                }
                UpdatedFiles.Add(PathBuilder.ToString());
            }
            PathBuilder = new StringBuilder(Root);
            if (PathBuilder[0] != DriveLetter)
            {
                changed = true;
                PathBuilder[0] = DriveLetter;
            }
            Root = PathBuilder.ToString();
            Folders = UpdatedFolders;
            Files = UpdatedFiles;
            return changed;
        }

        public void UpdateRoot(string taskroot)
        {
            string replaceable = new DirectoryInfo(Root).Parent.FullName;
            List<string> UpdatedFolders = new List<string>();
            List<string> UpdatedFiles = new List<string>();
            foreach (var folder in Folders)
            {
                UpdatedFolders.Add(folder.Replace(replaceable, taskroot));
            }
            foreach (var file in Files)
            {
                UpdatedFiles.Add(file.Replace(replaceable, taskroot));
            }
            Root = Root.Replace(replaceable, taskroot);
            Folders = UpdatedFolders;
            Files = UpdatedFiles;
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
            if(Directory.Exists(Root)) Directory.Delete(Root, true);              
        }
    }

    public class BackupTask
    {
        [JsonProperty] public string Label { get; private set; }
        [JsonProperty] private string DestinationPath { get; set; }
        [JsonIgnore] public BackupTaskConfiguration Configuration { get; private set; }
        [JsonIgnore] public List<Backup> Backups { get; private set; }
        [JsonIgnore] public string RootDirectoty
        {
            get
            {
                return $@"{DestinationPath}\{Label}";
            }
        }
        [JsonIgnore] public FileSystemInfo Source //only access it if the item is available!
        {
            get
            {
                if (Directory.Exists(Configuration.SourcePath)) return new DirectoryInfo(Configuration.SourcePath);
                else return new FileInfo(Configuration.SourcePath);
            }
        }
        [JsonIgnore] public DirectoryInfo Destination
        {
            get { return new DirectoryInfo(DestinationPath); }
        }
        [JsonIgnore] public DateTime LastSaved { 
            get 
            {
                DateTime Latest = DateTime.MinValue;
                foreach (var Backup in Backups)
                {
                    if (Backup.Creation > Latest) Latest = Backup.Creation;
                }
                return Latest;
            } 
        }
        [JsonProperty] public bool IsEnabled { get; set; }
        [JsonIgnore] public bool IsAvailable { 
            get 
            {
                if(BackupDriveOfItem == null || Configuration == null) return false;
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
        [JsonIgnore] public DiskSpace BackupsSize
        {
            get
            {
                DiskSpace Size = new DiskSpace(0);
                foreach (var backup in Backups)
                {
                    Size.Bytes += backup.Size.Bytes;
                }
                return Size;
            }
        }
        [JsonIgnore] public BackupDrive BackupDriveOfItem { get { return BackupProcess.GetBackupDriveFromBackupTask(this); } }

        [JsonIgnore] private Timer Backuptimer = new Timer(); //Timer for the next backup task call
        [JsonIgnore] public bool ActiveTask { get; private set; }
        [JsonIgnore] public CancellationTokenSource CancelBackup { get; private set; }

        [JsonConstructor] private BackupTask(string destinationPath, string label, bool isEnabled)
        {
            if (label == null || label == "") throw new Exception("The task label cannot be empty value!");
            if (destinationPath == null || destinationPath == "") throw new Exception("The destination cannot be empty value!");
            DestinationPath = destinationPath;
            Label = label;
            CancelBackup = new CancellationTokenSource();
            IsEnabled = isEnabled;
            LoadBackupConfig();
            LoadBackupInfo();
            ActiveTask = false;
            // start timer
        }

        public BackupTask(string destinationPath, string label, BackupTaskConfiguration configuration)
        {
            DestinationPath = destinationPath;
            Label = label;
            Configuration = configuration;
            CancelBackup = new CancellationTokenSource();
            Backups = new List<Backup>();
            IsEnabled = false;
            StoreBackupConfig();
            ActiveTask = false;
            // start timer
        }

        #region Get data
        private DateTime GetNextCallTime()
        {
            return LastSaved.AddTicks(Configuration.CycleInterval.Convert_to_ticks());
        }

        public string GetBackupType()
        {
            if(IsAvailable)
            {
                if (Source.GetType() == typeof(DirectoryInfo)) return "Folder";
                else if (Source.GetType() == typeof(FileInfo)) return "File";
            }
            return "Unknown";
        }

        public Backup SelectNextBackup() //returns null if the next backup is a new one
        {
            Backup Target = null;
            if (Configuration.NumberOfCycles == 0) return Target; // if the number of cycles is zero, it is unlimited
            if (Backups.Count < (Configuration.NumberOfCycles * Configuration.CycleLength)) return Target;
            foreach (var item in Backups)
            {
                if (Target == null || item.Creation.Ticks < Target.Creation.Ticks)
                {
                    Target = item;
                }
            }
            return Target;
        }

        public string GetNextBackupID()
        {
            if(Backups ==  null)
            {
                return "001";
            }
            else
            {
                int ID = 0;
                bool AlreadyExists;
                do
                {
                    ID++;
                    AlreadyExists = false;
                    foreach (var item in Backups)
                    {
                        if (item.NumberID == ID)
                        {
                            AlreadyExists = true;
                            break;
                        }
                    }
                } while (AlreadyExists);
                return ID.ToString("000");
            }
        }

        #endregion

        #region Backup management
        public async Task BackupRequest(bool isManual, Backup OutdatedBackup = null)
        {
            if (CheckPermission(isManual))
            {
                ActiveTask = true;
                bool completed = false;
                try
                {                    
                    Progress<BackupProgressReportModel> progress = new Progress<BackupProgressReportModel>();
                    progress.ProgressChanged += BackupProcess.DisplayBackupProgress;
                    string id;
                    if (OutdatedBackup != null)
                    {
                        id = OutdatedBackup.NumberID.ToString("000");
                        await Task.Run(() => DeleteBackup(OutdatedBackup));
                    }
                    else id = GetNextBackupID();
                    string BackupRoot = $@"{RootDirectoty}\{id} - BACKUP";

                    Backup NewBackup = await Task.Run(() => CreateBackup(progress, Source, BackupRoot));
                    if(OutdatedBackup != null) Backups.Remove(OutdatedBackup);
                    Backups.Add(NewBackup);
                    completed = true;
                    StoreBackupInfo();
                }
                catch (Exception error)
                {
                    if (isManual) MessageBox.Show(error.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    //LOG
                }

                if (isManual)
                {
                    if (completed) MessageBox.Show("The operation was successful!", "Backup report", MessageBoxButton.OK, MessageBoxImage.Information);
                    else MessageBox.Show("The operation was unsuccessful!", "Backup report", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                CancelBackup = new CancellationTokenSource();
                StartTimer();
                ActiveTask = false;
            }
            else
            {
                if(isManual)
                {
                    
                }
                //LOG
            }
        }

        private Backup CreateBackup(IProgress<BackupProgressReportModel> ProgressReport, FileSystemInfo Source, string BackupRoot)
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
                    foreach (var Item in SourceFolders)
                    {
                        string Parent = Directory.GetParent(Source.FullName).FullName;              
                        string Target = Item.Replace(Parent, $@"{BackupRoot}");
                        BackupFolders.Add(Directory.CreateDirectory(Target).FullName);

                        Progress = new BackupProgressReportModel(this, false);
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
                    foreach (var Item in SourceFiles)
                    {
                        Progress = new BackupProgressReportModel(this, false, SourceFiles.Count, BackupFiles.Count, All, Finished, new FileInfo(Item).Name);
                        ProgressReport.Report(Progress);

                        string Parent = Directory.GetParent(Source.FullName).FullName;
                        string Target = Item.Replace(Parent, $@"{BackupRoot}");
                        BackupFiles.Add(Target);
                        CopyFile(Item, Target, ProgressReport, Progress, CancelBackup.Token);
                    }
                    Progress = new BackupProgressReportModel(this, false, SourceFiles.Count, BackupFiles.Count, All, Finished, "none");
                    ProgressReport.Report(Progress);
                    Result = new Backup(BackupRoot, BackupFiles, BackupFolders);
                }
                else
                {
                    FileInfo SourceFile = new FileInfo(Source.FullName);
                    string Parent = Directory.GetParent(Source.FullName).FullName;
                    string BackupFile = SourceFile.FullName.Replace(Parent, $@"{BackupRoot}");
                    #region DiskSpaces
                    DiskSpace All = new DiskSpace(SourceFile.Length);
                    DiskSpace Finished = new DiskSpace(0);
                    #endregion
                    Progress = new BackupProgressReportModel(this, false, 1, 0, All, Finished, SourceFile.Name);
                    ProgressReport.Report(Progress);

                    Directory.CreateDirectory(new FileInfo(BackupFile).Directory.FullName);
                    CopyFile(SourceFile.FullName, BackupFile, ProgressReport, Progress, CancelBackup.Token);

                    Progress = new BackupProgressReportModel(this, false, 1, 1, All, Finished, "none");
                    ProgressReport.Report(Progress);
                    Result = new Backup(BackupRoot, BackupFile);
                }

                BackupProcess.Upload_BackupInfo();
                return Result;
            }
            catch (Exception error)
            {
                throw new Exception($"{error.Message}");
            }
        }

        public async Task RecoveryRequest(Backup Item, string Destination)
        {
            ActiveTask = true;
            bool completed = false;
            try
            {
                Progress<BackupProgressReportModel> progress = new Progress<BackupProgressReportModel>();
                progress.ProgressChanged += BackupProcess.DisplayBackupProgress;

                await Task.Run(() => RecoverBackup(progress, Item, Destination));
                completed = true;
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                //LOG
            }

            if (completed) MessageBox.Show("The operation was successful!", "Recovery report", MessageBoxButton.OK, MessageBoxImage.Information);
            else MessageBox.Show("The operation was unsuccessful!", "Recovery report", MessageBoxButton.OK, MessageBoxImage.Error);
            ActiveTask = false;
        }

        private void RecoverBackup(IProgress<BackupProgressReportModel> ProgressReport, Backup BackupObject, string Destination)
        {
            BackupProgressReportModel Progress;
            try
            {
                foreach (var Item in BackupObject.Folders)
                {
                    string Target = Item.Replace(BackupObject.Root, Destination);
                    Directory.CreateDirectory(Target);
                }
                #region DiskSpaces
                DiskSpace All = new DiskSpace(0);
                foreach (var item in BackupObject.Files)
                {
                    FileInfo file = new FileInfo(item);
                    All.Bytes += file.Length;
                }
                DiskSpace Finished = new DiskSpace(0);
                #endregion
                int completed = 0;
                foreach (var Item in BackupObject.Files)
                {
                    Progress = new BackupProgressReportModel(this, true, BackupObject.Files.Count, completed, All, Finished, new FileInfo(Item).Name);
                    ProgressReport.Report(Progress);

                    string Target = Item.Replace(BackupObject.Root, Destination);
                    CopyFile(Item, Target, ProgressReport, Progress, CancelBackup.Token);
                    completed++;
                }
                Progress = new BackupProgressReportModel(this, true, BackupObject.Files.Count, completed, All, Finished, "none");
                ProgressReport.Report(Progress);
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

        public void RenameBackupLabel(string newName)
        {
            //update every backup path data
        }

        public void DeleteBackups()
        {
            if(Backups != null)
            {
                foreach (var Item in Backups)
                {
                    Item.DeleteBackup();
                }
                Backups.Clear();
                StoreBackupInfo();
            }
        }

        public void DeleteBackup(Backup Item)
        {
            if(Backups.Contains(Item))
            {
                Item.DeleteBackup();
                Backups.Remove(Item);
                StoreBackupInfo();
            }
        }
        #endregion

        #region Data management
        public void UpdateConfiguration(string destination, string label, BackupTaskConfiguration configuration)
        {
            if(Destination.FullName != destination)
            {
                try
                {
                    MoveBackupTask(destination);                   
                }
                catch (Exception)
                {
                    MessageBox.Show("Unable to move the backup to the new locaiton!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            if(Label != label)
            {
                try
                {
                    RenameTaskLabel(label);
                }
                catch (Exception)
                {
                    MessageBox.Show("Unable to change the task label!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
           
            Configuration = configuration;
            StoreBackupConfig();
            BackupProcess.Upload_BackupInfo();
        }

        public void UpdateDriveLetter(char DriveLetter)
        {
            #region UpdateDestination
            StringBuilder PathBuilder = new StringBuilder(DestinationPath);
            if (PathBuilder[0] != DriveLetter)
            {
                PathBuilder[0] = DriveLetter;
                DestinationPath = PathBuilder.ToString();
            }
            #endregion

            #region UpdateBackups
            bool changed = false;
            foreach (var Backup in Backups) { if (Backup.UpdateDriveLetter(DriveLetter)) changed = true; }
            if (changed) StoreBackupInfo();
            #endregion
        }

        private void StoreBackupConfig()
        {
            string data = JsonConvert.SerializeObject(Configuration, Newtonsoft.Json.Formatting.Indented);
            FileInfo target = new FileInfo($@"{RootDirectoty}\configuration.json");
            target.Directory.Create();
            File.WriteAllText(target.FullName, data);
        }

        private void LoadBackupConfig()
        {
            string path = $@"{RootDirectoty}\configuration.json";
            if (File.Exists(path))
            {
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
            else
            {
                //LOG
            }
        }

        private void StoreBackupInfo()
        {
            string data = JsonConvert.SerializeObject(Backups, Newtonsoft.Json.Formatting.Indented);
            FileInfo target = new FileInfo($@"{RootDirectoty}\backups.json");
            target.Directory.Create();
            File.WriteAllText(target.FullName, data);
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
            else
            {
                Backups = new List<Backup>();
            }
        }

        private void MoveBackupTask(string newLocation)
        {
            string Target = $@"{newLocation}\{Label}";
            if (!Directory.Exists(Target))
            {
                Directory.Move(RootDirectoty, Target);
                DestinationPath = newLocation;
                foreach (var item in Backups)
                {
                    item.UpdateRoot(RootDirectoty);
                }
                StoreBackupInfo();
            }
            else throw new Exception("The new location already contains an item with the same name!");
        }

        private void RenameTaskLabel(string label)
        {
            if(!Directory.Exists($@"{DestinationPath}\{label}"))
            {
                string oldRoot = RootDirectoty;
                Label = label;
                Directory.Move(oldRoot, RootDirectoty);
                foreach (var item in Backups)
                {
                    item.UpdateRoot(RootDirectoty);
                }
                StoreBackupInfo();
            }
            else throw new Exception("The parent directory already contains an item with the same name!");
        }
        #endregion

        public void DeleteTask()
        {
            DeleteBackups();
            FileInfo config = new FileInfo($@"{RootDirectoty}\configuration.json");
            if (config.Exists) config.Delete();
            FileInfo backups = new FileInfo($@"{RootDirectoty}\backups.json");
            if (backups.Exists) backups.Delete();
            try
            {
                Directory.Delete(RootDirectoty, false);
            }
            catch (Exception)
            {
                //LOG
            }
        }
    }

    public class BackupDrive
    {
        [JsonProperty] public string DriveID { get; private set; }
        [JsonProperty] private string DefaultVolumeLabel { get; set; }
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
                }
            }
            if (DriveInformation == null) IsAvailable = false;
            else
            {
                foreach (var item in BackupTasks)
                {
                    item.UpdateDriveLetter(GetDriveLetter());
                }
            }
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

        #region Modify backuptasks
        public void AddBackupTask(BackupTask Item)
        {
            BackupTasks.Add(Item);
        }

        public void RemoveBackupTask(BackupTask Item)
        {
            Item.DeleteTask();
            BackupTasks.Remove(Item);
        }

        public void SetBackupTaskState(bool State, BackupTask Item)
        {
            Item.IsEnabled = State;
        }
        #endregion

        #region Get data
        public string GetVolumeLabel()
        {
            if (DriveInformation == null)
            {
                if (DefaultVolumeLabel == null)
                {
                    return "Unknown";
                }
                return DefaultVolumeLabel;
            }
            else if (DriveInformation.VolumeLabel == "")
            {
                    if (DriveInformation.DriveType == DriveType.Fixed) return "Local Disk";
                    else return "Drive";
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
                space.Bytes += item.BackupsSize.Bytes;
            }
            return space;
        }
        #endregion

        #region Backup
        public async void Backup_Async()
        {
            foreach (var item in BackupTasks)
            {
                await item.BackupRequest(true, item.SelectNextBackup());
            }
        }
        #endregion
    }

    static public class BackupProcess 
    {
        static public List<BackupDrive> BackupDrives { get; private set; }
        static public BackupSettings Settings { get; private set; }
        static public Dictionary<string, AdvancedDriveInfo> AllDriveInfo { get; } //key: serial number , value: DriveInfo
        public delegate void UIChanges();

        static BackupProcess()
        {
            BackupDrives = new List<BackupDrive>();
            AllDriveInfo = new Dictionary<string, AdvancedDriveInfo>();
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
        #endregion

        #region Load Data
        static private void LoadBackupProcess()
        {
            #region Backupdrives           
            Load_BackupInfo(out string Backupinfo);
            try
            {
                //https://stackoverflow.com/questions/26107656/ignore-parsing-errors-during-json-net-data-parsing               
                var settings = new JsonSerializerSettings { Error = (se, ev) => 
                { 
                    ev.ErrorContext.Handled = true; 
                    MessageBox.Show("An error was encountered while loading data!\nSome data may have been lost!", "Data corruption!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    CopyConfigToCorruptedFolder();
                } }; 
                BackupDrives = JsonConvert.DeserializeObject<List<BackupDrive>>(Backupinfo, settings);
                if (BackupDrives == null || !IntegrityCheck())
                {
                    BackupDrives = new List<BackupDrive>();
                    throw null;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to load in the user data due to data corruption!\nAll backup configurations are cleared!", "Data corruption!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                CopyConfigToCorruptedFolder();
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
                    /*foreach (var Item in Drive.BackupTasks)
                    {
                        //if (Item.Source == null) Intact = false;
                        if (Item.DestinationPath == null) Intact = false;
                        //else if (Item.LastSaved == null) Intact = false;
                        else if (Item.Configuration == null) Intact = false;
                        else if (Item.Configuration.SourcePath == null) Intact = false;
                        else if (Item.Configuration.CycleInterval == null) Intact = false;
                        else if (Item.Configuration.RetryWaitTime == null) Intact = false;
                    }*/
                }
            }
            return Intact;
        }

        static private void CopyConfigToCorruptedFolder()
        {
            Directory.CreateDirectory(@".\config\corrupted");
            string dest = $@".\config\corrupted\{DateTime.Now.ToString("yyyyMMddHHmm")}_backup.json";
            if (File.Exists(dest)) File.Delete(dest);
            File.Move(@".\config\backup.json", dest);
            //LOG
        }
        #endregion

        #region Upload Data
        static public void Upload_BackupInfo()
        {
            string Code = JsonConvert.SerializeObject(BackupDrives, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText( @".\config\backup.json", Code);
        }
        #endregion

        #region Backup
        static public async Task Manualsave_Async(BackupTask Item)
        {
            await Item.BackupRequest(true, Item.SelectNextBackup());
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
                    if (report.Recovery) MW.BackupOperation_label.Content = "Recovery is in progress...";
                    else MW.BackupOperation_label.Content = "Backup is in progress...";

                    if (!report.Preparation)
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
        static public void DisplayBackupProgress(object sender)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MainWindow MW = Application.Current.Windows[0] as MainWindow;
                if(MW.GetSelectedBackupTask() == sender)
                {
                    MW.BackupOperation_label.Content = "Doing preparations...";
                    MW.BackupProgress_progressbar.Value = 0;
                    MW.BackupProgressPercentage_label.Content = $"Loading...";
                    MW.BackupProgressData_label.Content = $"Preparing for backup operations...";
                    MW.CancelBackupOperation_button.IsEnabled = false;
                    MW.CancelBackupOperation_button.Opacity = 0.5;
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
