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
            public int Time;
            public string Unit;

            public Interval(string auto)
            {
                string[] temp = auto.Split(' ');
                Time = int.Parse(temp[0]);
                Unit = temp[1];
            }

            public int Convert_to_min()
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

            public int Convert_to_hour()
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

            public int Convert_to_day()
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
        }

        class Backupsettings_Global
        {
            public bool IsTempfolderEnabled;
            public List<string> BackupdriveList = new List<string>();
        }

        class Backupsettings_Local
        {
            private bool IsSingleCopy;
            private int NumberOfCopies; //automatically 1 if 'IsSingleCopy' is true
            private Interval Save_interval;
            private bool AbsoluteCopy;
            private bool ManualDetermination = false; //automatically false when 'AbsoluteCopy' is true or 'IsSingleCopy' is false
            private bool StoreDeletedInRBin = false; //automatically false when 'AbsoluteCopy' is true
            private bool PopupWhenRBinIsFull = false; //automatically false when 'StoreDeletedInRBin' is false
            private bool SmartSave;
            private bool UseMaxStorageData;
            private int MaxStorageData; //no value if 'UseMaxStorageData' is false
            private Interval RetryWaitTime;
            private int MaxNumberOfRetries;
            private bool PopupOnFail;
            private bool FileCompression;

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
        }

        class Backupitem
        {
            private int Index;
            private char Type;
            private string Source_path;
            private string Destination_path;
            private string State;
            private Backupsettings_Local Configuration;

            public Backupitem(int index, char type, string source_path, string destination_path, string state, Backupsettings_Local settings)
            {
                Index = index;
                Type = type;
                Source_path = source_path;
                Destination_path = destination_path;
                State = state;
                Configuration = settings;
            }
        }

        class Backuplist
        {
            private List<Backupitem> Itemlist = new List<Backupitem>();
            private string Drivename;
            private string Driveletter;
            private string DriveID;

            public void AddBackupitem(Backupitem item)
            {
                Itemlist.Add(item);
            }

            public Backupitem GetBackupitem(int index)
            {
                return Itemlist[index];
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

        private List<string> Backupinfo_List = new List<string>(); //structure : {int index}*{char type}src<{string source_path}|dst<{string destination_path}|{interval}|{*if empty it is saved, othervise a save is required to apply changes}
        private string Currentdir = Directory.GetCurrentDirectory();

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
            Main_window.Activate();
            Startup();
            Menu = "Backup";
            Load_backupinfo();
            Load_Backupitems(Backupinfo_List);
        }

        #region Startup

        private void Startup()
        {          
            Directory.CreateDirectory(Currentdir + "\\Logs");
            Directory.CreateDirectory(Currentdir + "\\config");
            if (!(File.Exists(Currentdir + "\\config\\backup.txt")))
            {
                File.Create(Currentdir + "\\config\\backup.txt");
                Emptyconfig = true;
            }
        }
        #endregion

        #region Backup feature

        #region Backupinfo list recount
        private void Autorecount()
        {
            for (int i = 0; i < Backupinfo_List.Count(); i++)
            {
                string[] temp = Backupinfo_List[i].Split('*');//separates the indexnumber from the rest of the code
                Backupinfo_List[i] = $"{i+1}*{temp[1]}";
            }
        }
        #endregion

        #region Data_import/Load
        private void Load_backupinfo()
        {
            if (!Emptyconfig)//if the config file is empty, it won't try to load it
            {
                Backupinfo_List = File.ReadAllLines(Directory.GetCurrentDirectory() + "\\config\\backup.txt").ToList();
                #region Check for empty rows to avoid crash
                for (int i = 0; i < Backupinfo_List.Count(); i++)
                {
                    if (Backupinfo_List[i] == "")
                    {
                        Backupinfo_List.RemoveAt(i);
                    }
                }
                #endregion
            }
            #region UI-changes
            Apply_label.IsEnabled = false;
            Apply_label.Opacity = 0.5;
            Dismiss_label.IsEnabled = false;
            Dismiss_label.Opacity = 0.5;
            Warning2_label.Visibility = Visibility.Hidden;
            #endregion
        }

        private void Load_Backupitems(List<string> Backupinfo_List)//only loads source / checks for missing-unsaved sources 
        {
            Warning3_label.Visibility = Visibility.Hidden;
            //Source_listbox.Items.Clear();
            int index = 0;
            foreach (var item in Backupinfo_List)
            {
                string status = "";
                if (GetType(index)=='D')//D->Directory
                {
                    int i = GetIndex(index);
                    #region Status checks
                    if (!Directory.Exists(GetSource(index)))//check for missing directory
                    {
                        status += " MISSING";
                        Warning3_label.Visibility = Visibility.Visible;
                    }
                    if (!GetSavestatus(index))
                    {
                        #region UI-changes
                        Apply_label.IsEnabled = true;
                        Apply_label.Opacity = 1;
                        Dismiss_label.IsEnabled = true;
                        Dismiss_label.Opacity = 1;
                        Warning2_label.Visibility = Visibility.Visible;
                        #endregion
                        if (status!="")
                        {
                            status += "+UNSAVED";
                        }
                        else
                        {
                            status += " UNSAVED";
                        }
                    }
                    #endregion

                    #region Add folder with statusinfo to list
                    if (shortsource)
                    {
                        string[] temp = GetSource(index).Split('\\');
                        Source_listbox.Items.Add($"{i}.{status} Folder : {temp[temp.Length - 1]}");
                    }
                    else
                    {
                        Source_listbox.Items.Add($"{i}.{status} Folder : {GetSource(index)}");
                    }
                    #endregion
                }
                else
                {
                    int i = GetIndex(index);
                    #region Status checks
                    if (!File.Exists(GetSource(index)))//check for missing file
                    {
                        status += " MISSING";
                        Warning3_label.Visibility = Visibility.Visible;
                    }
                    if (!GetSavestatus(index))
                    {
                        #region UI-changes
                        Apply_label.IsEnabled = true;
                        Apply_label.Opacity = 1;
                        Dismiss_label.IsEnabled = true;
                        Dismiss_label.Opacity = 1;
                        Warning2_label.Visibility = Visibility.Visible;
                        #endregion
                        if (status != "")
                        {
                            status += "+UNSAVED";
                        }
                        else
                        {
                            status += " UNSAVED";
                        }
                    }
                    #endregion

                    #region Add file with statusinfo to list
                    if (shortsource)
                    {
                        string[] temp = GetSource(index).Split('\\');
                        Source_listbox.Items.Add($"{i}.{status} File : {temp[temp.Length - 1]}");
                    }
                    else
                    {
                        Source_listbox.Items.Add($"{i}.{status} File : {GetSource(index)}");
                    }
                    #endregion
                }
                index++;
            }          
        }

        private void Load_Backupitem(List<string> Backupinfo_List, int index)//loads destination + interval + status
        {
            Configuration_button.IsEnabled = true;
            Configuration_button.Opacity = 1;

            #region Loads interval
            if (GetInterval(index).Convert_to_min() < 60)
            {
                Interval_label.Content = $"{GetInterval(index).Convert_to_min()} min";
            }
            else if (GetInterval(index).Convert_to_hour() < 24)
            {
                Interval_label.Content = $"{GetInterval(index).Convert_to_hour()} hour";
            }
            else
            {
                Interval_label.Content = $"{GetInterval(index).Convert_to_day()} day";
            }
            #endregion

            #region Loads destination
            Destination_textbox.Text = GetDestination(index);
            #endregion

            #region Loads status
            string[] temp = Source_listbox.Items[index].ToString().Split(' ')[1].Split('+');
            if (temp.Length>1)// if there is more than one issue
            {
                if (temp.Length == 2)// if there is two issue
                {
                    if (temp[0][0] == 'M'&& temp[1][0] == 'U')
                    {
                        Status_label.Content = "Status: Missing source and\nneeds to be applied!";
                        Status_label.Foreground = new SolidColorBrush(Color.FromRgb(220, 0, 0));

                        Fixissue_button.Opacity = 1;
                        Fixissue_button.IsEnabled = true;

                        Manualsave_button.IsEnabled = false;
                        Manualsave_button.Opacity = 0.5;
                        Save_image.Opacity = 0.5;
                    }
                }
            }
            else//if there is one issue or none
            {
                if (temp[0][0] == 'M')//if the source is missing
                {
                    Status_label.Content = "Status: Missing source!";
                    Status_label.Foreground = new SolidColorBrush(Color.FromRgb(200, 0, 180));

                    Fixissue_button.Opacity = 1;
                    Fixissue_button.IsEnabled = true;

                    Manualsave_button.IsEnabled = false;
                    Manualsave_button.Opacity = 0.5;
                    Save_image.Opacity = 0.5;
                }
                else if (temp[0][0] == 'U')//if the source is unsaved
                {
                    Status_label.Content = "Status: Needs to be applied!";
                    Status_label.Foreground = new SolidColorBrush(Color.FromRgb(220, 90, 36));

                    Fixissue_button.Opacity = 0.5;
                    Fixissue_button.IsEnabled = false;

                    Manualsave_button.IsEnabled = false;
                    Manualsave_button.Opacity = 0.5;
                    Save_image.Opacity = 0.5;
                }
                else//no issue
                {
                    Status_label.Content = "Status: OK!";
                    Status_label.Foreground = new SolidColorBrush(Color.FromRgb(0, 230, 120));

                    Fixissue_button.Opacity = 0.5;
                    Fixissue_button.IsEnabled = false;

                    Manualsave_button.IsEnabled = true;
                    Manualsave_button.Opacity = 1;
                    Save_image.Opacity = 1;
                }
            }
            #endregion
        }
        #endregion

        #region Data_extraction

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

        #region Data_export
        private void Upload_Backupinfo()
        {
            #region Status to saved
            for (int i = 0; i < Backupinfo_List.Count; i++)
            {
                Backupinfo_List[i] = Backupinfo_List[i].Replace("<!change!>", "");
            }
            #endregion

            File.WriteAllLines(Currentdir + "\\config\\backup.txt", Backupinfo_List.ToArray());
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
                int i = Source_listbox.SelectedIndex;
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

        #region UI-changes/program running

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

        #region Source item selection
        private void Source_listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)//changes when you select an item from the source list
        {
            int i = Source_listbox.SelectedIndex;
            if (i != -1)//if the index is -1 there is no item selected
            {
                Load_Backupitem(Backupinfo_List, i);

                #region UI-changes
                Warning1_label.Visibility = Visibility.Hidden;
                Destination_textbox.Foreground = new SolidColorBrush(Color.FromRgb(0, 230, 120));
                Interval_label.Foreground = new SolidColorBrush(Color.FromRgb(0, 230, 120));
                #endregion
            }
        }
        #endregion

        #region Apply/Dismiss_buttons
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

        #region Itemlist edit (submenu)

        #region Add item (submenu)
        private void Additem_button_Click(object sender, RoutedEventArgs e)
        {
            HideAllMenu();
            Backupsubmenu1_grid.Visibility = Visibility.Visible;
            Menu = "Backup.sub1";
        }

        private void Newitemapply_button_Click(object sender, RoutedEventArgs e)//adds the new item to the system
        {
            if (Sourceinput_textbox.Text == "" || Destinationinput_textbox.Text == "" || Intervalselection_combobox.SelectedIndex == -1)
            {
                MessageBox.Show("You have to provide more information!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if ((Optionfile_radiobutton.IsChecked == true && !File.Exists(Sourceinput_textbox.Text)) || (Optionfolder_radiobutton.IsChecked == true && !Directory.Exists(Sourceinput_textbox.Text)))
            {
                MessageBox.Show("The source doesn't exists! \nMake sure you have selected the right source type!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (!Directory.Exists(Destinationinput_textbox.Text))
            {
                MessageBox.Show("The destination doesn't exists!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (MessageBox.Show("Are you sure you want to add this item to the list?", "Apply", MessageBoxButton.YesNo, MessageBoxImage.None).Equals(MessageBoxResult.Yes))
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
                Interval intv = new Interval(Intervalselection_combobox.SelectedItem.ToString().Remove(0, 38));//the first 38 element is system data
                #endregion

                string temp = $"{Backupinfo_List.Count() + 1}*{type}src<{Sourceinput_textbox.Text}|dst<{Destinationinput_textbox.Text}|{intv.Time} {intv.Unit}|<!change!>";
                Backupinfo_List.Add(temp);
                Load_Backupitems(Backupinfo_List);
                Source_listbox.SelectedIndex = (Source_listbox.Items.Count - 1);//selects the new item automatically

                #region Submenu reset
                Sourceinput_textbox.Text = "";
                Destinationinput_textbox.Text = "";
                Intervalselection_combobox.SelectedIndex = -1;
                Optionfolder_radiobutton.IsChecked = true;
                #endregion
            }
        }
        #endregion

        #region Remove item
        private void Removeitem_button_Click(object sender, RoutedEventArgs e)
        {
            if (Source_listbox.SelectedIndex == -1)
            {
                MessageBox.Show("You have to select an item in order to delete it!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                if (MessageBox.Show("Are you sure you want to delete this item? \nIt will be deleted permanently!", "Delete", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel).Equals(MessageBoxResult.Yes))
                {
                    Backupinfo_List.RemoveAt(Source_listbox.SelectedIndex);
                    Source_listbox.SelectedIndex = -1;

                    #region Activate apply/dismiss options
                    Apply_label.IsEnabled = true;
                    Apply_label.Opacity = 1;
                    Dismiss_label.IsEnabled = true;
                    Dismiss_label.Opacity = 1;
                    Warning2_label.Visibility = Visibility.Visible;
                    #endregion

                    Autorecount();
                    Load_Backupitems(Backupinfo_List);
                    Reset_Backupmenu();
                }
            }
        }
        #endregion

        #region Relocate item (submenu)
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
            int index = Source_listbox.SelectedIndex;
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
                Optionfolder_radiobutton.IsChecked = true;
            }
            else
            {
                Optionfile_radiobutton.IsChecked = true;
            }
            #endregion
        }

        private void Replaceitemapply_button_Click(object sender, RoutedEventArgs e)
        {
            if (Sourceinput_textbox.Text == "")
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
                int index = Source_listbox.SelectedIndex;
                Backupinfo_List[index] = $"{GetIndex(Source_listbox.SelectedIndex)}*{type}src<{Sourceinput_textbox.Text}|dst<{Destinationinput_textbox.Text}|{interval}|<!change!>";
                Load_Backupitems(Backupinfo_List);
                Source_listbox.SelectedIndex = (Source_listbox.Items.Count - 1);//selects the relocated item automatically

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
            }
        }
        #endregion

        #region View all settings (submenu)
        private void Viewallsettings_button_Click(object sender, RoutedEventArgs e)
        {
            HideAllMenu();
            Backupsubmenu1_grid.Visibility = Visibility.Visible;
            Menu = "Backup.sub1";
            Destinationinput_textbox.IsEnabled = false;
            Sourceinput_textbox.IsEnabled = false;
            Intervalselection_combobox.Visibility = Visibility.Hidden;
            Interval2_label.Visibility = Visibility.Visible;
            Newitemapply_button.Visibility = Visibility.Hidden;
            Optionfile_radiobutton.IsEnabled = false;
            Optionfolder_radiobutton.IsEnabled = false;

            #region Data load
            int index = Source_listbox.SelectedIndex;
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
                Optionfolder_radiobutton.IsChecked = true;
            }
            else
            {
                Optionfile_radiobutton.IsChecked = true;
            }
            #endregion
        }
        #endregion

        #region Cancel button  (universal submenu)
        private void Backupsubmenu1cancel_button_Click(object sender, RoutedEventArgs e)
        {
            HideAllMenu();
            Backup_grid.Visibility = Visibility.Visible;
            Menu = "Backup";

            #region Submenu reset
            Sourceinput_textbox.Text = "";
            Destinationinput_textbox.Text = "";
            Intervalselection_combobox.SelectedIndex = -1;
            Optionfolder_radiobutton.IsChecked = true;

            Destinationinput_textbox.IsEnabled = true;
            Sourceinput_textbox.IsEnabled = true;
            Intervalselection_combobox.Visibility = Visibility.Visible;
            Interval2_label.Visibility = Visibility.Hidden;
            Interval2_label.Content = "";
            Newitemapply_button.Visibility = Visibility.Visible;
            Replaceitemapply_button.Visibility = Visibility.Hidden;

            Optionfile_radiobutton.IsEnabled = true;
            Optionfolder_radiobutton.IsEnabled = true;
            #endregion
        }
        #endregion

        #region Apply/Dismiss buttons
        private void Apply_label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to apply the changes?", "Apply changes", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes).Equals(MessageBoxResult.Yes)) ;
            {
                Upload_Backupinfo();
                Load_Backupitems(Backupinfo_List);
                Reset_Backupmenu();
            }
        }

        private void Dismiss_label_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (MessageBox.Show("Do you want to cancel the changes?", "Dismiss changes", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No).Equals(MessageBoxResult.Yes)) ;
            {
                Load_backupinfo();
                Load_Backupitems(Backupinfo_List);
                Reset_Backupmenu();
            }
        }
        #endregion

        #endregion

        #region Menu reset
        private void Reset_Backupmenu()
        {
            Source_listbox.SelectedItem = -1;
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
        }


        #endregion

        #endregion

        #region Categorization menu

        #endregion

        #region Menu control (Grids)
        private void HideAllMenu()
        {
            Backup_grid.Visibility = Visibility.Hidden;
            Backupsubmenu1_grid.Visibility = Visibility.Hidden;
            Debug_grid.Visibility = Visibility.Hidden;           
        }
        #endregion

        #region Side panel transitions

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

        #region Close/Minimize buttons
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

        #region Top right menu button actions

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