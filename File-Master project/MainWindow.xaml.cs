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
            [JsonProperty]private double Time;
            [JsonProperty]private string Unit;

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
                if (Unit=="min")
                {
                    return Time;
                }
                else if (Unit == "hour")
                {
                    return (Time*60);
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
            public List<string> BackupdriveList = new List<string>();
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
                if(!AbsoluteCopy)
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
            public string SourcePath;
            public string DestinationPath;
            public DateTime LastSaved; 
            public bool Isenabled;
            private Backupsettings_Local Configuration;
            public string Configuration_Code; // Configuration serialized version

            public Backupitem()
            {

            }

            public Backupitem(int id, string source_path, string destination_path, DateTime lastSaved, bool isenabled, Backupsettings_Local settings)
            {
                ID = id;
                SourcePath = source_path;
                DestinationPath = destination_path;
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
            }

            public void Deserialize()
            {
                Configuration = JsonConvert.DeserializeObject<Backupsettings_Local>(Configuration_Code);
                Configuration.Deserialize();
            }
            #endregion
        }

        class Backupdrive
        {
            private List<Backupitem> Itemlist = new List<Backupitem>();
            public string Itemlist_Code; // Itemlist serialized version
            public string Drivename;
            public char Driveletter;
            public string DriveID;

            public void AddBackupitem(Backupitem item)
            {
                Itemlist.Add(item);
            }

            public Backupitem GetBackupitem(int index)
            {
                return Itemlist[index];
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

        private List<Backupdrive> Backupdrives;
        private DateTime CurrentTime = DateTime.Now;
        private List<string> Backupinfo_List = new List<string>(); //structure : {int index}*{char type}src<{string source_path}|dst<{string destination_path}|{interval}|{*if empty it is saved, othervise a save is required to apply changes}
        private string CurrentDir = Directory.GetCurrentDirectory();

        public MainWindow()
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
            
            /*
            List<Backupdrive> DataBackupdrives = new List<Backupdrive>();
            Backupdrive Drive = new Backupdrive();
            Interval SI = new Interval("60 min");
            Interval RWT = new Interval("10 min");

            Backupsettings_Local config = new Backupsettings_Local(true, 1, SI, true, false, false, false, true, false, 0, RWT, 3, true, false);
            Backupitem Item = new Backupitem(0,"source path", "destination path", CurrentTime, true, config);
            Drive.DriveID = "911";
            Drive.Driveletter = 'H';
            Drive.Drivename = "HDD-K01";
            Drive.AddBackupitem(Item);
            DataBackupdrives.Add(Drive);
            Upload_Backupinfo(DataBackupdrives);*/
          
            Backupdrives = LoadBackupElements();
            Display_Backupitems();
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

        #region Backupinfo list recount ?
        private void Autorecount()
        {
            for (int i = 0; i < Backupinfo_List.Count(); i++)
            {
                string[] temp = Backupinfo_List[i].Split('*');//separates the indexnumber from the rest of the code
                Backupinfo_List[i] = $"{i+1}*{temp[1]}";
            }
        }
        #endregion

        #region Data-IN
        private string[] Load_backupinfo()
        {
            string[] Backupinfo;
            Backupinfo = File.ReadAllLines(Directory.GetCurrentDirectory() + "\\config\\backup.json");
            #region UI-changes
            Apply_label.IsEnabled = false;
            Apply_label.Opacity = 0.5;
            Dismiss_label.IsEnabled = false;
            Dismiss_label.Opacity = 0.5;
            Warning2_label.Visibility = Visibility.Hidden;
            #endregion
            return Backupinfo;
        }

        private List<Backupdrive> LoadBackupElements()
        {
            string[] Backupinfo = Load_backupinfo();
            List<Backupdrive> Backupdrives = new List<Backupdrive>();         
            foreach (var item in Backupinfo)
            {
                Backupdrive Drive = JsonConvert.DeserializeObject<Backupdrive>(item);
                Drive.Deserialize();
                Backupdrives.Add(Drive);
            }
            return Backupdrives;
        }

        private void Display_Backupitems()
        {
            Backuptask_listbox.Items.Clear();
            Warning2_label.Visibility = Visibility.Hidden;
            Warning3_label.Visibility = Visibility.Hidden;
            Warning4_label.Visibility = Visibility.Hidden;
            ListBoxItem ListItem;
            foreach (var Drive in Backupdrives)
            {
                #region Add backupdrive to list
                ListItem = new ListBoxItem();
                string part1 = "Backup drive: ";
                part1 += $"{Drive.Drivename} ({Drive.Driveletter}:) ";
                string part3 = "(5,9GB / 60GB)";
                string part2 = "";
                for (int i = 0; i < 80 - (part1.Length + part3.Length); i++)
                {
                    part2 += "-";
                }
                ListItem.Content = part1 + part2 + part3;
                ListItem.Tag = Drive.DriveID;
                Backuptask_listbox.Items.Add(ListItem);
                CheckStatus(Drive, ref ListItem);
                #endregion
                #region Add backupitems of backupdrive
                for (int i = 0; i < Drive.CountItems(); i++)
                {
                    ListItem = new ListBoxItem();
                    string part4 = $"-> {GetBackupType(Drive.GetBackupitem(i))}: {Drive.GetBackupitem(i).SourcePath} - (5,6GB)";
                    ListItem.Content = part4;
                    CheckStatus(Drive.GetBackupitem(i), ref ListItem);
                    ListItem.Tag = $"{Drive.DriveID}\\{Drive.GetBackupitem(i).ID}";
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
        
        private void Display_Backupitem(string Tag)//loads destination + interval + status
        {
            Backupitem Item = GetBackupitemFromTag(Tag);

            #region Loads interval
            Item.GetBackupsettings().GetSave_interval().Humanize();
            Interval_label.Content = Item.GetBackupsettings().GetSave_interval().GetTime();
            #endregion

            #region Loads destination
            Destination_textbox.Text = Item.DestinationPath;
            #endregion

            #region Loads Smart save
            if(Item.GetBackupsettings().SmartSave) Smartsave_label.Content = "Smart save: ON";
            else Smartsave_label.Content = "Smart save: OFF";
            #endregion

            #region Loads Last saved
            Lastsaved_label.Content = $"Last saved: {Item.LastSaved.ToString()}";
            #endregion

            #region Loads status

            #endregion
        }

        private void CheckStatus(Backupdrive Drive, ref ListBoxItem ListItem)
        {
            ListItem.Foreground = new SolidColorBrush(Color.FromRgb(222, 0, 0));
        }

        private void CheckStatus(Backupitem Item, ref ListBoxItem ListItem)
        {
            if (GetBackupType(Item) == "Item")
            {
                ListItem.Foreground = new SolidColorBrush(Color.FromRgb(200, 0, 180));
                Warning3_label.Visibility = Visibility.Visible;
            }
        }
        #endregion

        #region Data_extraction
        private Backupitem GetBackupitemFromTag(string Tag)
        {
            Backupitem Item = new Backupitem();
            #region GetBackupItem from Tag
            int ID = int.Parse(Tag.Split('\\')[1]);
            string DriveID = Tag.Split('\\')[0];
            foreach (var Drive in Backupdrives)
            {
                if (Drive.DriveID == DriveID)
                {
                    for (int i = 0; i < Drive.CountItems(); i++)
                    {
                        if (Drive.GetBackupitem(i).ID == ID)
                        {
                            Item = Drive.GetBackupitem(i);
                        }
                    }
                }
            }
            #endregion
            return Item;
        }


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

        private string GetBackupType(Backupitem Item)
        {
            if (Directory.Exists(Item.SourcePath)) return "Folder";
            else if (File.Exists(Item.SourcePath)) return "File";
            else return "Item";
        }

        private int GetIndex(int index)
        {
            int result;
            result = int.Parse(Backupinfo_List[index].Split('|')[0].Split('*')[0]);
            return result;
        }

        private Interval GetInterval(int index)
        {
            Interval result = new Interval(Backupinfo_List[index].Split('|')[2]);
            return result;
        }

        private bool GetSavestatus(int index)
        {
            if (Backupinfo_List[index].Split('|')[3] == "") return true;
            else return false;
        }

        #endregion

        #region Data-OUT
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
            Apply_label.IsEnabled = false;
            Apply_label.Opacity = 0.5;
            Dismiss_label.IsEnabled = false;
            Dismiss_label.Opacity = 0.5;
            Warning2_label.Visibility = Visibility.Hidden;
            #endregion
        }
        #endregion

        #region Saving
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
                if (Tag != "add")
                {
                    Display_Backupitem(Tag);
                    #region UI-changes
                    Warning1_label.Visibility = Visibility.Hidden;
                    Destination_textbox.Foreground = new SolidColorBrush(Color.FromRgb(0, 230, 120));
                    Interval_label.Foreground = new SolidColorBrush(Color.FromRgb(0, 230, 120));
                    #endregion
                }
            }
        }
        #endregion

        #region ?
        private void Apply_label_MouseEnter(object sender, MouseEventArgs e)
        {
            Apply_label.Foreground = new SolidColorBrush(Color.FromRgb(0, 200, 0));
        }

        private void Apply_label_MouseLeave(object sender, MouseEventArgs e)
        {
            Apply_label.Foreground = new SolidColorBrush(Color.FromRgb(172, 172, 172));
        }

        private void Dismiss_label_MouseEnter(object sender, MouseEventArgs e)
        {
            Dismiss_label.Foreground = new SolidColorBrush(Color.FromRgb(200, 0, 0));
        }

        private void Dismiss_label_MouseLeave(object sender, MouseEventArgs e)
        {
            Dismiss_label.Foreground = new SolidColorBrush(Color.FromRgb(172, 172, 172));
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
            if (Backuptask_listbox.SelectedIndex == -1)
            {
                MessageBox.Show("You have to select an item in order to delete it!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                if (MessageBox.Show("Are you sure you want to delete this item? \nIt will be deleted permanently!", "Delete", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel).Equals(MessageBoxResult.Yes))
                {
                    Backupinfo_List.RemoveAt(Backuptask_listbox.SelectedIndex);
                    Backuptask_listbox.SelectedIndex = -1;

                    #region Activate apply/dismiss options
                    Apply_label.IsEnabled = true;
                    Apply_label.Opacity = 1;
                    Dismiss_label.IsEnabled = true;
                    Dismiss_label.Opacity = 1;
                    Warning2_label.Visibility = Visibility.Visible;
                    #endregion

                    Autorecount();
                    //Load_Backupitems(Backupinfo_List);
                    Reset_Backupmenu();
                }
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

        #region Enable/Disable backup !
        private void Apply_label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to apply the changes?", "Apply changes", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes).Equals(MessageBoxResult.Yes)) ;
            {
                //Upload_Backupinfo();
                //Load_Backupitems(Backupinfo_List);
                Reset_Backupmenu();
            }
        }

        private void Dismiss_label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (MessageBox.Show("Do you want to cancel the changes?", "Dismiss changes", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No).Equals(MessageBoxResult.Yes)) ;
            {
                Load_backupinfo();
                //Load_Backupitems(Backupinfo_List);
                Reset_Backupmenu();
            }
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
            foreach (var Drive in Backupdrives)
            {
                CI = new ComboBoxItem();
                CI.Content = $"{Drive.Drivename} ({Drive.Driveletter}:)";
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
                    foreach (var Drive in Backupdrives)
                    {
                        if (Drive.DriveID==CI.Tag.ToString())
                        {
                            Drive.AddBackupitem(CreateBackupitem(Settings));
                            Upload_Backupinfo(Backupdrives);
                            break;
                        }
                    }               
                    Reset_Backupmenu();
                    Backuptask_listbox.SelectedIndex = (Backuptask_listbox.Items.Count - 1);//selects the new item automatically
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
            List<int> IDs=new List<int>();
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
                    break;
                }
            }
            Backupitem Item=new Backupitem(newID, Sourceinput_textbox.Text, Destinationinput_textbox.Text,DateTime.MinValue,false, Settings);
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

            #region Submenu reset
            Sourceinput_textbox.Text = "";
            Destinationinput_textbox.Text = "";
            Intervalselection_combobox.SelectedIndex = -1;
            //Optionfolder_radiobutton.IsChecked = true;

            Destinationinput_textbox.IsEnabled = true;
            Sourceinput_textbox.IsEnabled = true;
            Intervalselection_combobox.Visibility = Visibility.Visible;
            Interval2_label.Visibility = Visibility.Hidden;
            Interval2_label.Content = "";
            Newitemapply_button.Visibility = Visibility.Visible;
            Replaceitemapply_button.Visibility = Visibility.Hidden;

            //Optionfile_radiobutton.IsEnabled = true;
            //Optionfolder_radiobutton.IsEnabled = true;
            #endregion
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

        #region Menu reset
        private void Reset_BackupSubmenu1()
        {
            Sourceinput_textbox.Text = "";
            Destinationinput_textbox.Text = "";
        }
        #endregion

        #endregion

        #region Menu reset
        private void Reset_Backupmenu()
        {
            Backuptask_listbox.SelectedItem = -1;
            Warning1_label.Visibility = Visibility.Visible;
            Destination_textbox.Foreground = new SolidColorBrush(Color.FromRgb(226, 154, 6));
            Destination_textbox.Text = "Select a source folder!";
            Interval_label.Foreground = new SolidColorBrush(Color.FromRgb(226, 154, 6));
            Interval_label.Content = "N.A.";
            Status_label.Content = "Status: N.A";
            Status_label.Foreground = new SolidColorBrush(Color.FromRgb(226, 154, 6));
            Fixissue_button.IsEnabled = false;
            Fixissue_button.Opacity = 0.5;
            Configuration_button.IsEnabled = false;
            Configuration_button.Opacity = 0.5;
            Manualsave_button.IsEnabled = false;
            Manualsave_button.Opacity = 0.5;
            Save_image.Opacity = 0.5;
            Display_Backupitems();
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