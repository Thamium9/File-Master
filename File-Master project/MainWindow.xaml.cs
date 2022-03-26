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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Classes

        class FileExplorerItem
        {
            private DirectoryInfo Current;
            private FileSystemInfo Item;
            private long Size_value=-1;
            
            public FileExplorerItem(FileInfo file)
            {
                Item = file;
                Size_value = file.Length;
            }

            public FileExplorerItem(DirectoryInfo folder)
            {
                Item = folder;
            }

            public string Name { get { return Item.Name; } }
            public string Size { get { if (Size_value!=-1) return $"{Size_value} bytes"; else return "-"; } }
            public string Type { get { return Item.Extension; } }
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
        static private bool Emptyconfig = false;
        #endregion

        static public string CurrentDir = Directory.GetCurrentDirectory();

        public MainWindow()
        {
            bool debug = true;
            if (debug)
            {
                InitializeComponent();

                #region Window visibility on startup
                if (!Minimize_as_TaskbarIcon || !Start_with_minimized) NotifyIcon_Taskbar.Visibility = Visibility.Collapsed; //hide notifyicon when not needed
                if (Start_with_minimized) Program_Minimize(Minimize_as_TaskbarIcon);
                else Main_window.WindowState = WindowState.Normal;
                #endregion

                Settings Usersettings = new Settings();
                Usersettings.Shortsource = false;

                HideAllMenu();
                Backup_grid.Visibility = Visibility.Visible;
                Main_window.Activate();
                //Startup();
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

                Display_Backupitems();
            }
            else
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
                    //Startup();
                    Menu = "Backup";

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
        }

        #region Backup

        #region Backup Data-Display
        private void Display_Backupitems()
        {
            Backuptask_listbox.Items.Clear();
            Warning2_label.Visibility = Visibility.Hidden;
            Warning3_label.Visibility = Visibility.Hidden;
            Warning4_label.Visibility = Visibility.Hidden;
            ListBoxItem ListItem;
            foreach (var Drive in BackupProcess.Backupdrives)
            {
                #region Add backupdrive to list
                ListItem = new ListBoxItem();
                string part1 = "Backup drive: ";
                part1 += $"{Drive.GetVolumeLabel()} ({Drive.GetDriveLetter()}:)";
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
                    string part4 = $"-> {GetBackupType(Drive.GetBackupitem(i))}: {Drive.GetBackupitem(i).Source.FullName} - (5,6GB)";
                    ListItem.Content = part4;
                    ListItem.Tag = $"{Drive.GetBackupitem(i).ID}";
                    CheckBackupitemStatus(Drive.GetBackupitem(i), ref ListItem);
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
            Backupitem Item = BackupProcess.GetBackupitemFromTag(Tag);

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
            LoadBackupitemStatus(Item, out CanRunBackupProcess, out MissingSource);
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
            if (Item.IsEnabled)
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
                Modification_button.Visibility = Visibility.Hidden;
                Restorefiles_button.Opacity = 1;
                Restorefiles_button.IsEnabled = true;
            }
            else
            {
                Modification_button.Opacity = 1;
                Modification_button.Visibility = Visibility.Visible;
                Modification_button.IsEnabled = true;
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
            if (Drive.IsAvailable == false)
            {
                ListItem.Foreground = new SolidColorBrush(Color.FromRgb(230, 0, 0));
            }
            else
            {
                ListItem.Foreground = new SolidColorBrush(Color.FromRgb(0, 230, 120));
            }
        }

        private void CheckBackupitemStatus(Backupitem Item, ref ListBoxItem ListItem)//Sets warning labels, and item color
        {
            #region Default
            ListItem.Foreground = new SolidColorBrush(Color.FromRgb(0, 230, 120));
            #endregion
            if (!Item.IsEnabled)
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

        private void LoadBackupitemStatus(Backupitem Item, out bool CanRunBackupProcess, out bool MissingSource)//Sets Status label, and returns if it can be enabled
        {
            #region Default status
            CanRunBackupProcess = true;
            MissingSource = false;
            Destination_textbox.Foreground = new SolidColorBrush(Color.FromRgb(0, 230, 120));
            Status_label.Foreground = new SolidColorBrush(Color.FromRgb(0, 230, 120));
            Status_label.Content = "Status: OK!";
            #endregion
            if (!Item.IsEnabled)
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

        #region Backup Data-Extraction
        public string GetSelectedBackupitemTag()
        {
            ListBoxItem Item = (ListBoxItem)Backuptask_listbox.SelectedItem;
            string Tag = Item.Tag.ToString();
            return Tag;
        }

        private string GetBackupType(Backupitem Item)
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

        #endregion

        #region Manual save
        private void Manualsave_button_Click(object sender, RoutedEventArgs e)
        {
            if (Warning_Save())
            {
                string Tag = GetSelectedBackupitemTag();
                BackupProcess.Manualsave(Tag);
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
                string Tag = GetSelectedBackupitemTag();
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

        #region Buttons

        private void Additem_button_Click(object sender, RoutedEventArgs e)
        {
            HideAllMenu();
            Backupsubmenu1_grid.Visibility = Visibility.Visible;
            Menu = "Backup.sub1";
        }

        private void Removeitem_button_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete this item? \nIt will be deleted permanently!", "Delete", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel).Equals(MessageBoxResult.Yes))
            {
                string Tag = GetSelectedBackupitemTag();
                int ID = int.Parse(Tag.ToString());
                foreach (var Drive in BackupProcess.Backupdrives)
                {
                    Drive.RemoveBackupitem(ID);
                }

                Reset_Backupmenu();
            }
        }

        #region Restore files !
        private void Restorefiles_button_Click(object sender, RoutedEventArgs e)
        {/*
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
            #endregion*/
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

        private void Modification_button_Click(object sender, RoutedEventArgs e)
        {
            HideAllMenu();
            Backupsubmenu1_grid.Visibility = Visibility.Visible;
            Menu = "Backup.sub1";
            Destinationinput_textbox.IsEnabled = false;
            Sourceinput_textbox.IsEnabled = false;
            Intervalselection_combobox.Visibility = Visibility.Hidden;
            Interval2_label.Visibility = Visibility.Visible;
            Newitemapply_button.Visibility = Visibility.Hidden;

            #region Data load

            #endregion
        }

        private void Enablebackup_button_Click(object sender, RoutedEventArgs e)
        {
            int ID = int.Parse(GetSelectedBackupitemTag());
            foreach (var Drive in BackupProcess.Backupdrives)
            {
                Drive.SetBackupitemState(true, ID);
            }
            Refresh_Backupmenu();
        }

        private void Disablebackup_button_Click(object sender, RoutedEventArgs e)
        {
            int ID = int.Parse(GetSelectedBackupitemTag());
            foreach (var Drive in BackupProcess.Backupdrives)
            {
                Drive.SetBackupitemState(false, ID);
            }
            Refresh_Backupmenu();
        }

        private void ManageBackupDrives_button_Click(object sender, RoutedEventArgs e)
        {
            HideAllMenu();
            Backupsubmenu2_grid.Visibility = Visibility.Visible;
            Menu = "Backup.sub2";
            UpdateSubmenu2();
        }

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
            foreach (var Drive in BackupProcess.Backupdrives)
            {
                CI = new ComboBoxItem();
                CI.Content = $"{Drive.GetVolumeLabel()} ({Drive.GetDriveLetter()}:)";
                CI.Tag = Drive.DriveID;
                Backupdriveselect_combobox.Items.Add(CI);
            }
        }
        #endregion

        #region Buttons

        #region Apply button
        private void Newitemapply_button_Click(object sender, RoutedEventArgs e)//adds the new item to the system
        {
            if (CheckInfo())
            {
                if (MessageBox.Show("Are you sure you want to add this item to the list?", "Apply", MessageBoxButton.YesNo, MessageBoxImage.None).Equals(MessageBoxResult.Yes))
                {
                    HideAllMenu();
                    Backup_grid.Visibility = Visibility.Visible;
                    Menu = "Backup";
                    Backupsettings_Local Settings = CreateBackupsettings_Local();
                    ComboBoxItem CI = (ComboBoxItem)Backupdriveselect_combobox.SelectedItem;
                    foreach (var Drive in BackupProcess.Backupdrives)
                    {
                        if (Drive.DriveID == CI.Tag.ToString())
                        {
                            Drive.AddBackupitem(CreateBackupitem(Settings));
                            BackupProcess.Upload_Backupinfo();
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
            foreach (var Drive in BackupProcess.Backupdrives)
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
            Backupitem Item = new Backupitem(newID, Source, Destination, DateTime.MinValue, false, Settings);
            return Item;
        }

        private Backupsettings_Local CreateBackupsettings_Local()
        {
            bool IsSingleCopy = Singlecopy_radiobutton.IsChecked.Value;
            int NumberOfCopies = 1;
            ComboBoxItem CI = (ComboBoxItem)Intervalselection_combobox.SelectedItem;
            Interval Save_interval = new Interval(CI.Tag.ToString());
            bool AbsoluteCopy = true;
            bool ManualDetermination = false;
            bool StoreDeletedInRBin = false; //automatically false when 'AbsoluteCopy' is true
            bool PopupWhenRBinIsFull = false; //automatically false when 'StoreDeletedInRBin' is false
            bool SmartSave = Smartsave_checkbox.IsChecked.Value;
            bool UseMaxStorageData = false;
            int MaxStorageData = 0; //no value if 'UseMaxStorageData' is false
            Interval RetryWaitTime = new Interval("5 min");
            int MaxNumberOfRetries = 3;
            bool PopupOnFail = false;
            bool FileCompression = Compress_checkbox.IsChecked.Value;
            Backupsettings_Local Settings = new Backupsettings_Local(IsSingleCopy, NumberOfCopies, Save_interval, AbsoluteCopy, ManualDetermination, StoreDeletedInRBin, PopupWhenRBinIsFull, SmartSave, UseMaxStorageData, MaxStorageData, RetryWaitTime, MaxNumberOfRetries, PopupOnFail, FileCompression);
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
            Reset_BackupSubmenu1Settings();
        }
        #endregion

        #endregion

        #region Submenu2
        private void Showunavailable_checkbox_Click(object sender, RoutedEventArgs e)
        {
            UpdateSubmenu2();
        }

        #region Buttons
        private void ActivateDrive_Click(object sender, RoutedEventArgs e)
        {
            BackupProcess.ActivateBackupdrive(((Button)sender).Tag.ToString(), 1024 * 1024);
            UpdateSubmenu2();
        }

        private void DeactivateDrive_Click(object sender, RoutedEventArgs e)
        {
            BackupProcess.DeactivateBackupdrive(((Button)sender).Tag.ToString());
            UpdateSubmenu2();
        }
        private void Backupsubmenu2cancel_button_Click(object sender, RoutedEventArgs e)
        {
            HideAllMenu();
            Backup_grid.Visibility = Visibility.Visible;
            Menu = "Backup";
            Reset_Backupmenu();
            ResetBackupSubmenu2();
        }

        #endregion

        #region Menu functions
        private StackPanel CreateAvailableDrivesSP()
        {
            StackPanel Drives = new StackPanel();
            Drives.HorizontalAlignment = HorizontalAlignment.Stretch;
            #region Available label
            Label Unavailable = new Label();
            Unavailable.Content = "Available drives: ";
            Unavailable.Foreground = Brushes.Gray;
            Unavailable.FontSize = 15;
            Unavailable.FontWeight = FontWeights.Bold;
            Unavailable.VerticalAlignment = VerticalAlignment.Center;
            Unavailable.HorizontalAlignment = HorizontalAlignment.Left;
            Unavailable.Margin = new Thickness(5, 20, 0, 5);
            Drives.Children.Add(Unavailable);
            #endregion
            foreach (var ThisDrive in BackupProcess.AllDriveInfo)
            {
                var ThisDriveSerial = ThisDrive.Key;
                var ThisDriveInfo = ThisDrive.Value;
                double AvailableSpaceRatio = (double)ThisDriveInfo.AvailableFreeSpace / (double)ThisDriveInfo.TotalSize;
                #region Stackpanel
                StackPanel Drive = new StackPanel();
                Drive.Orientation = Orientation.Horizontal;
                if (BackupProcess.IsBackupdrive(ThisDriveSerial)) Drive.Background = new SolidColorBrush(Color.FromRgb(25, 50, 30));
                else Drive.Background = new SolidColorBrush(Color.FromRgb(25, 25, 25));
                Drive.Margin = new Thickness(0, 5, 0, 5);
                Drive.Height = 130;
                Drive.HorizontalAlignment = HorizontalAlignment.Stretch;
                Drive.Width = Drives.Width;
                #endregion
                #region Icon
                Image Icon = new Image();
                if (ThisDriveInfo.DriveType == DriveType.Removable) Icon.Source = new BitmapImage(new Uri(@"/Icons/usb_drive.png", UriKind.Relative));
                else if (ThisDriveInfo.DriveType == DriveType.Fixed) Icon.Source = new BitmapImage(new Uri(@"/Icons/hard_drive.png", UriKind.Relative));
                Icon.Width = 100;
                Icon.Height = 100;
                Icon.Margin = new Thickness(15, 0, 15, 0);
                Icon.VerticalAlignment = VerticalAlignment.Center;
                Drive.Children.Add(Icon);
                #endregion
                #region Info
                StackPanel Information = new StackPanel();
                Information.VerticalAlignment = VerticalAlignment.Center;
                Information.Width = 250;
                #region Drivename
                Label Info = new Label();
                Info.Content = $"{ThisDriveInfo.VolumeLabel} ({ThisDriveInfo.Name})";
                Info.Foreground = Brushes.LightGray;
                Info.FontSize = 14;
                Info.FontWeight = FontWeights.Bold;
                Info.VerticalAlignment = VerticalAlignment.Center;
                Info.HorizontalAlignment = HorizontalAlignment.Left;
                #endregion
                #region FreeSpace
                ProgressBar Space = new ProgressBar();
                double value = 100 - (AvailableSpaceRatio * 100);
                Space.Value = value;
                Space.Width = 200;
                Space.Height = 25;
                Space.HorizontalAlignment = HorizontalAlignment.Left;
                Space.Margin = new Thickness(5, 0, 0, 0);
                if (AvailableSpaceRatio < 0.1) Space.Foreground = Brushes.Red;
                else if (AvailableSpaceRatio < 0.2) Space.Foreground = Brushes.Orange;                
                #endregion
                #region FreeSpaceInfo
                Label SpaceInfo = new Label();
                string freespace;
                if ((double)ThisDriveInfo.AvailableFreeSpace > Math.Pow(1024, 4)) freespace = $"{Math.Round((double)ThisDriveInfo.AvailableFreeSpace / Math.Pow(1024, 4), 2)} TB";
                else if ((double)ThisDriveInfo.AvailableFreeSpace > Math.Pow(1024, 3)) freespace = $"{Math.Round((double)ThisDriveInfo.AvailableFreeSpace / Math.Pow(1024, 3), 2)} GB";
                else freespace = $"{Math.Round((double)ThisDriveInfo.AvailableFreeSpace / Math.Pow(1024, 2), 2)} MB";
                string totalspace;
                if ((double)ThisDriveInfo.TotalSize > Math.Pow(1024, 4)) totalspace = $"{Math.Round((double)ThisDriveInfo.TotalSize / Math.Pow(1024, 4), 2)} TB";
                else if ((double)ThisDriveInfo.TotalSize > Math.Pow(1024, 3)) totalspace = $"{Math.Round((double)ThisDriveInfo.TotalSize / Math.Pow(1024, 3), 2)} GB";
                else totalspace = $"{Math.Round((double)ThisDriveInfo.TotalSize / Math.Pow(1024, 2), 2)} MB";
                SpaceInfo.Content = $"{freespace} free of {totalspace}";
                SpaceInfo.Foreground = Brushes.Gray;
                SpaceInfo.FontSize = 14;
                SpaceInfo.FontWeight = FontWeights.Bold;
                SpaceInfo.VerticalAlignment = VerticalAlignment.Center;
                SpaceInfo.HorizontalAlignment = HorizontalAlignment.Left;
                #endregion
                Information.Children.Add(Info);
                Information.Children.Add(Space);
                Information.Children.Add(SpaceInfo);
                Drive.Children.Add(Information);
                #endregion
                #region Set size
                StackPanel Size = new StackPanel();
                Size.Width = 150;
                Drive.Children.Add(Size);
                #endregion
                #region Button

                StackPanel Button = new StackPanel();
                Button.Margin = new Thickness(80, 0, 0, 0);
                Button SetDrive = new Button();
                SetDrive.VerticalAlignment = VerticalAlignment.Center;
                SetDrive.HorizontalAlignment = HorizontalAlignment.Right;
                SetDrive.FontWeight = FontWeights.Bold;
                SetDrive.Height = 34;
                SetDrive.Width = 100;
                SetDrive.Margin = new Thickness(0, 0, 25, 0);
                SetDrive.FontSize = 14;

                if (!BackupProcess.IsBackupdrive(ThisDriveSerial))
                {
                    SetDrive.Content = "Activate";
                    if (AvailableSpaceRatio < 0.1 || ThisDriveInfo.AvailableFreeSpace < 4000000000) { SetDrive.IsEnabled = false; SetDrive.Opacity = 0.5; }
                    SetDrive.Tag = ThisDriveSerial;
                    SetDrive.Click += ActivateDrive_Click;
                }

                else
                {
                    SetDrive.Content = "Deactivate";
                    SetDrive.Tag = ThisDriveSerial;
                    SetDrive.Click += DeactivateDrive_Click;
                }

                Button.Children.Add(SetDrive);
                Button.VerticalAlignment = VerticalAlignment.Center;
                Drive.Children.Add(Button);
                #endregion
                Drives.Children.Add(Drive);
            }

            return Drives;
        }
        private StackPanel CreateUnavailableDrivesSP()
        {
            StackPanel Drives = new StackPanel();
            Drives.HorizontalAlignment = HorizontalAlignment.Stretch;
            #region Unavailable label
            Label Unavailable = new Label();
            Unavailable.Content = "Unavailable backup drives: ";
            Unavailable.Foreground = Brushes.Gray;
            Unavailable.FontSize = 15;
            Unavailable.FontWeight = FontWeights.Bold;
            Unavailable.VerticalAlignment = VerticalAlignment.Center;
            Unavailable.HorizontalAlignment = HorizontalAlignment.Left;
            Unavailable.Margin = new Thickness(5, 20, 0, 5);
            Drives.Children.Add(Unavailable);
            #endregion
            foreach (var ThisDrive in BackupProcess.Backupdrives)
            {
                if (!ThisDrive.IsAvailable)
                {
                    #region Stackpanel
                    StackPanel Drive = new StackPanel();
                    Drive.Orientation = Orientation.Horizontal;
                    Drive.Background = new SolidColorBrush(Color.FromRgb(25, 25, 25));
                    Drive.Margin = new Thickness(0, 5, 0, 5);
                    Drive.Height = 130;
                    Drive.HorizontalAlignment = HorizontalAlignment.Stretch;
                    Drive.Width = Drives.Width;
                    #endregion
                    #region Icon
                    Image Icon = new Image();
                    //Icon.Source = new BitmapImage(new Uri(@"/Icons/unknown_drive.png", UriKind.Relative));
                    Icon.Width = 100;
                    Icon.Height = 100;
                    Icon.Margin = new Thickness(15, 0, 15, 0);
                    Icon.VerticalAlignment = VerticalAlignment.Center;
                    Drive.Children.Add(Icon);
                    #endregion
                    #region Info
                    StackPanel Information = new StackPanel();
                    Information.VerticalAlignment = VerticalAlignment.Center;
                    #region Drivename
                    Label Info = new Label();
                    Info.Content = $"{ThisDrive.GetVolumeLabel()} ({ThisDrive.GetDriveLetter()})";
                    Info.Foreground = Brushes.DarkRed;
                    Info.FontSize = 14;
                    Info.FontWeight = FontWeights.Bold;
                    Info.VerticalAlignment = VerticalAlignment.Center;
                    Info.HorizontalAlignment = HorizontalAlignment.Left;
                    #endregion
                    Information.Children.Add(Info);
                    Drive.Children.Add(Information);
                    #endregion
                    Drives.Children.Add(Drive);
                }
            }

            return Drives;
        }

        #endregion

        #region Menu actions
        private void UpdateSubmenu2()
        {
            StackPanel Drives = new StackPanel();
            Drives.Children.Add(CreateAvailableDrivesSP());
            if (Showunavailable_checkbox.IsChecked.Value) Drives.Children.Add(CreateUnavailableDrivesSP());

            Alldrives_scrollviewer.Content = Drives;
        }
        private void ResetBackupSubmenu2()
        {
            Alldrives_scrollviewer.Content = null;
        }
        #endregion

        #endregion

        #region Menu actions
        public void Reset_Backupmenu()
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
            Modification_button.IsEnabled = false;
            Modification_button.Opacity = 0.5;
            Modification_button.Visibility = Visibility.Visible;
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

        public void Refresh_Backupmenu()
        {
            if(Backuptask_listbox.SelectedIndex!=-1)
            {
                string Tag = GetSelectedBackupitemTag();
                Display_Backupitems();
                for (int i = 0; i < Backuptask_listbox.Items.Count; i++)
                {
                    ListBoxItem temp = (ListBoxItem)Backuptask_listbox.Items[i];
                    string tag = temp.Tag.ToString();
                    if (tag == Tag) Backuptask_listbox.SelectedIndex = i;
                }
                Display_Backupitem(Tag);
            }
            BackupProcess.Upload_Backupinfo();
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
            Backupsubmenu2_grid.Visibility = Visibility.Hidden;
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
        //*/
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
            Winform.FolderBrowserDialog folderDlg = new Winform.FolderBrowserDialog();
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
            }
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