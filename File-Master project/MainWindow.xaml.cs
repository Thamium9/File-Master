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
            InitializeComponent();
        }

        private void Main_window_Loaded(object sender, RoutedEventArgs e)
        {
            bool debug = true;
            try
            {
                #region Window visibility on startup
                if (!Minimize_as_TaskbarIcon || !Start_with_minimized) NotifyIcon_Taskbar.Visibility = Visibility.Collapsed; //hide notifyicon when not needed
                if (Start_with_minimized) Program_Minimize(Minimize_as_TaskbarIcon);
                else Main_window.WindowState = WindowState.Normal;
                #endregion

                Settings Usersettings = new Settings();
                Usersettings.Shortsource = false;

                if (InDevelopment && !debug)
                {
                    Unstable_Warning();
                }
                HideAllMenu();
                Backup_grid.Visibility = Visibility.Visible;
                Main_window.Activate();
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

                Display_Backups();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Runtime error!", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

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

        #region Menu functions
        public Backupitem GetSelectedBackupitem()
        {
            Backupitem Item = null;
            ListBoxItem Selection = (ListBoxItem)Backuptask_listbox.SelectedItem;
            if(Selection.Tag.GetType() == typeof(Backupitem))
            {
                Item = (Backupitem)Selection.Tag;
            }
            return Item;
        }
        #endregion

        #region User Actions
        private void Backuptask_listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)//changes when you select an item from the source list
        {
            if (Backuptask_listbox.SelectedIndex != -1)//if the index is -1 there is no item selected
            {
                Backupitem Item = GetSelectedBackupitem();
                if (Item != null)
                {
                    Display_Backupitem(Item);
                    #region UI-changes
                    Warning1_label.Visibility = Visibility.Hidden;
                    #endregion
                }
                else
                {
                    Reset_Backupmenu();
                }
            }
            else if (Backuptask_listbox.Items.Count != 0)
            {
                Reset_Backupmenu();
            }
        }

        private void ViewPathSelection_Combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(Main_window.IsLoaded)
            {
                Update_Backupmenu();
            }           
        }

        private void Additem_button_Click(object sender, RoutedEventArgs e)
        {
            HideAllMenu();
            Backupsubmenu1_grid.Visibility = Visibility.Visible;
            Menu = "Backup.sub1";
            Newitemapply_button.Visibility = Visibility.Visible;
        }

        private void Removeitem_button_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete this item? \nIt will be deleted permanently!", "Delete", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel).Equals(MessageBoxResult.Yes))
            {
                Backupitem Item = GetSelectedBackupitem();
                foreach (var Drive in BackupProcess.Backupdrives)
                {
                    Drive.RemoveBackupitem(Item);
                }
                Reset_Backupmenu();
                BackupProcess.Upload_Backupinfo();
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
            Modifyitemapply_button.Visibility = Visibility.Visible;

            #region Data load
            Backupitem temp = GetSelectedBackupitem();
            Destinationinput_textbox.Text = temp.Destination.ToString();
            Sourceinput_textbox.Text = temp.Source.ToString();
            foreach (ComboBoxItem item in Intervalselection_combobox.Items)
            {
                if(item.Tag.ToString() == temp.Configuration.CycleInterval.GetTime())
                {
                    Intervalselection_combobox.SelectedItem = item;
                }
            }
            Backupdrive Drive = temp.BackupDriveOfItem;
            ComboBoxItem CI = new ComboBoxItem();
            CI = new ComboBoxItem();
            CI.Content = $"({Drive.GetDriveLetter()}:) {Drive.GetVolumeLabel()}";
            CI.Tag = Drive;
            Backupdriveselect_combobox_Refresh(CI);
            #endregion
        }

        private void Enablebackup_button_Click(object sender, RoutedEventArgs e)
        {
            Backupitem Item = GetSelectedBackupitem();
            foreach (var Drive in BackupProcess.Backupdrives)
            {
                Drive.SetBackupitemState(true, Item);
            }
            Update_Backupmenu();
            BackupProcess.Upload_Backupinfo();
        }

        private void Disablebackup_button_Click(object sender, RoutedEventArgs e)
        {
            Backupitem Item = GetSelectedBackupitem();
            foreach (var Drive in BackupProcess.Backupdrives)
            {
                Drive.SetBackupitemState(false, Item);
            }
            Update_Backupmenu();
            BackupProcess.Upload_Backupinfo();
        }

        private async void Manualsave_button_Click(object sender, RoutedEventArgs e)
        {
            if (Warning_Save())
            {
                Backupitem Item = GetSelectedBackupitem();
                var Backup = Task.Run(() => BackupProcess.Manualsave_Async(Item));
                Update_Backupmenu();
                await Backup;
                Update_Backupmenu();
                BackupProcess.Upload_Backupinfo();
            }
        }

        private async void ManageBackupDrives_button_Click(object sender, RoutedEventArgs e)
        {
            HideAllMenu();
            Backupsubmenu2_grid.Visibility = Visibility.Visible;
            Menu = "Backup.sub2";
            await UpdateSubmenu2_Async();
        }

        private void ViewSelectedPath_button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewPathSelection_Combobox.SelectedIndex == 0)
                {
                    Process.Start(GetSelectedBackupitem().Destination.FullName);
                }
                else
                {
                    FileSystemInfo Source = GetSelectedBackupitem().Source;
                    if ((Source.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        Process.Start(Source.FullName);
                    }
                    else
                    {
                        FileInfo Target = (FileInfo)Source;
                        Process.Start(Target.DirectoryName);
                    }
                }             
            }
            catch (Exception)
            {
                MessageBox.Show("The folder cannot be opened!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }
        #endregion

        #region Submenu1

        #region User Actions

        #region Backupdrivelist combobox
        private void Backupdriveselect_combobox_DropDownOpened(object sender, EventArgs e)
        {
            Backupdriveselect_combobox_Refresh((ComboBoxItem)Backupdriveselect_combobox.SelectedItem);
        }

        private void Backupdriveselect_combobox_Refresh(ComboBoxItem selected)
        {
            Backupdriveselect_combobox.Items.Clear();
            if (selected != null)
            {
                Backupdriveselect_combobox.Items.Add(selected);
                Backupdriveselect_combobox.SelectedItem = selected;
            }
            else
            {
                Backupdriveselect_combobox_Reset();
            }
            foreach (var Drive in BackupProcess.Backupdrives)
            {
                if (Drive != selected.Tag)
                {
                    ComboBoxItem CI = new ComboBoxItem();
                    CI.Content = $"({Drive.GetDriveLetter()}:) {Drive.GetVolumeLabel()}";
                    CI.Tag = Drive;
                    Backupdriveselect_combobox.Items.Add(CI);
                }
            }
        }

        private void Backupdriveselect_combobox_Reset()
        {
            Backupdriveselect_combobox.Items.Clear();
            ComboBoxItem CI = new ComboBoxItem();
            CI.Content = "Select a backup drive!";
            CI.Tag = null;
            Backupdriveselect_combobox.Items.Add(CI);
            Backupdriveselect_combobox.SelectedItem = CI;
        }

        private void Backupdriveselect_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem selected = (ComboBoxItem)Backupdriveselect_combobox.SelectedItem;
            if (selected != null && selected.Tag != null)
            {
                Backupdrive SelectedDrive = (Backupdrive)selected.Tag;
                if (SelectedDrive.DriveInformation != null)
                {
                    AvailableFreeSpaceBD_label.Content = new DiskSpace(SelectedDrive.DriveInformation.AvailableFreeSpace).Humanize();
                    long AvailableAllocated = SelectedDrive.SizeLimit.Bytes - SelectedDrive.GetBackupSize().Bytes;
                    if (SelectedDrive.SizeLimit.Bytes > 0)
                    {
                        if (AvailableAllocated <= 0) AvailableAllocatedSpace_label.Content = "Out of allocated space!";
                        else AvailableAllocatedSpace_label.Content = new DiskSpace(AvailableAllocated).Humanize();
                    }
                    else
                    {
                        AvailableAllocatedSpace_label.Content = "No limit is set!";
                    }
                }
                else
                {
                    AvailableFreeSpaceBD_label.Content = "Not available!";
                    AvailableAllocatedSpace_label.Content = "Not available!";
                }
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
                    Backupitem_Settings Settings = CreateBackupsettings_Local();
                    ComboBoxItem CI = (ComboBoxItem)Backupdriveselect_combobox.SelectedItem;
                    Backupdrive Target = (Backupdrive)CI.Tag;
                    Target.AddBackupitem(CreateBackupitem(Settings));
                    BackupProcess.Upload_Backupinfo();
                    #region UI changes
                    Reset_Backupmenu();
                    Reset_BackupSubmenu1();
                    HideAllMenu();
                    Backup_grid.Visibility = Visibility.Visible;
                    Menu = "Backup";
                    #endregion
                }
            }
        }

        private void Updateitemapply_button_Click(object sender, RoutedEventArgs e)
        {
            if (CheckInfo())
            {
                if (MessageBox.Show("Are you sure you want to modify this item?\nThis action will delete all the backups associated with this item!", "Modify", MessageBoxButton.YesNo, MessageBoxImage.Warning).Equals(MessageBoxResult.Yes))
                {
                    Backupitem SelectedItem = GetSelectedBackupitem();
                    SelectedItem.BackupDriveOfItem.RemoveBackupitem(SelectedItem); //deletes itself

                    Backupitem_Settings Settings = CreateBackupsettings_Local();
                    ComboBoxItem CI = (ComboBoxItem)Backupdriveselect_combobox.SelectedItem;
                    Backupdrive Target = (Backupdrive)CI.Tag;
                    Target.AddBackupitem(CreateBackupitem(Settings));
                    BackupProcess.Upload_Backupinfo();
                    #region UI changes
                    HideAllMenu();
                    Reset_Backupmenu();
                    Reset_BackupSubmenu1();
                    Backup_grid.Visibility = Visibility.Visible;
                    Menu = "Backup";
                    #endregion
                }
            }
        }

        private bool CheckInfo()
        {
            ComboBoxItem Selection = (ComboBoxItem)Backupdriveselect_combobox.SelectedItem;
            Backupdrive Target = null;
            if (Selection.Tag != null && Selection.Tag.GetType() == typeof(Backupdrive))
            {
                Target = (Backupdrive)Selection.Tag;
            }
            if (Sourceinput_textbox.Text == "" || Destinationinput_textbox.Text == "" || Intervalselection_combobox.SelectedIndex == -1 || Target == null)
            {
                MessageBox.Show("You have to provide more information!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (!(File.Exists(Sourceinput_textbox.Text) || Directory.Exists(Sourceinput_textbox.Text)))
            {
                MessageBox.Show("The source doesn't exists!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (new DirectoryInfo(Destinationinput_textbox.Text).Root.FullName != $@"{Target.GetDriveLetter()}:\")
            {
                MessageBox.Show("The destination is not located in the selected backup drive!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                return true;
            }
            return false;
        }

        private Backupitem CreateBackupitem(Backupitem_Settings Settings)
        {
            string Source = Sourceinput_textbox.Text;
            string Destination = Destinationinput_textbox.Text;
            Backupitem Item = new Backupitem(BackupProcess.GetNewBackupID(), Source, Destination, DateTime.MinValue, false, Settings);
            return Item;
        }

        private Backupitem_Settings CreateBackupsettings_Local()
        {
            char Method = 'F';
            int NumberOfCopies = 1;
            ComboBoxItem CI = (ComboBoxItem)Intervalselection_combobox.SelectedItem;
            Interval CycleInterval = new Interval(CI.Tag.ToString());
            int MaxStorageData = 0;
            Interval RetryWaitTime = new Interval("5 minute");
            int MaxNumberOfRetries = 3;
            bool PopupOnFail = false;
            Backupitem_Settings Settings = new Backupitem_Settings(
                Method,
                NumberOfCopies,
                CycleInterval,
                OnlySaveOnChange_checkbox.IsChecked.Value,
                MaxStorageData,
                RetryWaitTime,
                MaxNumberOfRetries,
                PopupOnFail,
                Compress_checkbox.IsChecked.Value
            );
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

        #endregion

        #region Settings menu

        #region Reset menu
        private void Reset_BackupSubmenu1Settings()
        {
            BackupMethodFull_radiobutton.IsChecked = true;
        }
        #endregion

        #endregion

        #region Menu actions
        private void Reset_BackupSubmenu1()
        {
            Sourceinput_textbox.Text = "";
            Destinationinput_textbox.Text = "";
            Intervalselection_combobox.SelectedIndex = -1;
            AvailableFreeSpaceBD_label.Content = "-";
            AvailableAllocatedSpace_label.Content= "-";
            Backupdriveselect_combobox_Reset();

            Destinationinput_textbox.IsEnabled = true;
            Sourceinput_textbox.IsEnabled = true;
            Intervalselection_combobox.Visibility = Visibility.Visible;
            Newitemapply_button.Visibility = Visibility.Hidden;
            Modifyitemapply_button.Visibility = Visibility.Hidden;
            Reset_BackupSubmenu1Settings();
        }
        #endregion

        #endregion

        #region Submenu2

        #region User Actions
        private void Showunavailable_checkbox_click(object sender, RoutedEventArgs e)
        {
            UpdateSubmenu2_Async();
        }

        private void SetLimitChange(object sender, RoutedEventArgs e)
        {
            TextBox Data = (TextBox)sender;
            Main.BackupDriveSizeLimits[Data.Tag.ToString()] = Data;
            Button temp = Main.BackupDriveUpdateButtons[Data.Tag.ToString()];
            if (temp.Content.ToString() == "Update limit")
            {
                temp.IsEnabled = true;
                temp.Opacity = 1;
            }
        }

        private void ActivateDrive_Click(object sender, RoutedEventArgs e)
        {
            string serial = ((Button)sender).Tag.ToString();
            DiskSpace Space = new DiskSpace(0);
            TextBox SizeLimit = Main.BackupDriveSizeLimits[serial];
            if ((double.TryParse(SizeLimit.Text, out double limit) && limit > 0) || SizeLimit.Text == "")
            {
                Space.Gigabytes = limit;
            }
            else
            {
                MessageBox.Show("Invalid limit!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                Main.BackupDriveSizeLimits[serial].Text = "";
            }
            BackupProcess.ActivateBackupdrive(serial, Space);
            if (BackupProcess.GetBackupdriveFromSerial(serial).SizeLimitCheck(out double result))
            {
                MessageBox.Show("The set limit cannot be this big!\nIt will be set to the maximum allowed amount!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                Main.BackupDriveSizeLimits[serial].Text = result.ToString();
            }
            UpdateSubmenu2_Async();
        }

        private void UpdateDrive_Click(object sender, RoutedEventArgs e)
        {
            string serial = ((Button)sender).Tag.ToString();
            TextBox SizeLimit = Main.BackupDriveSizeLimits[serial];
            if (double.TryParse(SizeLimit.Text, out double limit) && limit > 0)
            {
                BackupProcess.GetBackupdriveFromSerial(serial).SizeLimit.Gigabytes = limit;
                if(BackupProcess.GetBackupdriveFromSerial(serial).SizeLimitCheck(out double result))
                {
                    MessageBox.Show("The set limit cannot be this big!\nIt will be set to the maximum allowed amount!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Main.BackupDriveSizeLimits[serial].Text = result.ToString();
                }
            }
            else
            {
                BackupProcess.GetBackupdriveFromSerial(serial).SizeLimit.Gigabytes = 0;
                MessageBox.Show("Invalid limit!\nThe limit has been removed!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                Main.BackupDriveSizeLimits[serial].Text = "";
            }
            BackupProcess.Upload_Backupinfo();
            UpdateSubmenu2_Async();
        }

        private void DeactivateDrive_Click(object sender, RoutedEventArgs e)
        {
            BackupProcess.DeactivateBackupdrive(((Image)sender).Tag.ToString());
            UpdateSubmenu2_Async();
        }

        private void Delete_mouseenter(object sender, RoutedEventArgs e)
        {
            Image item = (Image)sender;
            item.Opacity = 1;
        }

        private void Delete_mouseleave(object sender, RoutedEventArgs e)
        {
            Image item = (Image)sender;
            item.Opacity = 0.3;
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

        #region Menu Functions
        private async Task<StackPanel> CreateAvailableDrivesSP_Async()
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
            Unavailable.Margin = new Thickness(5, 40, 0, 5);
            Drives.Children.Add(Unavailable);
            #endregion
            foreach (var ThisDrive in BackupProcess.AllDriveInfo)
            {
                #region Data gathering
                Func<string> GetMediaType = () => ThisDrive.Value.MediaType;
                string MediaType = await Task.Run(GetMediaType);

                if (MediaType == "") break;
                string ThisDriveSerial = ThisDrive.Key;
                DriveInfo ThisDriveInfo = ThisDrive.Value.DriveInformation;

                bool isBackupEnabled = BackupProcess.IsBackupdrive(ThisDriveSerial);

                double AvailableSpaceRatio = (double)ThisDriveInfo.AvailableFreeSpace / (double)ThisDriveInfo.TotalSize;
                double BackupUsedSpaceRatio = 0;
                double BackupSpaceRatio = 0;
                if (isBackupEnabled)
                {
                    BackupUsedSpaceRatio += (double)BackupProcess.GetBackupdriveFromSerial(ThisDriveSerial).GetBackupSize().Bytes / (double)ThisDriveInfo.TotalSize;
                    BackupSpaceRatio += ((double)BackupProcess.GetBackupdriveFromSerial(ThisDriveSerial).SizeLimit.Bytes / (double)ThisDriveInfo.TotalSize) - BackupUsedSpaceRatio;
                    if (0 > BackupSpaceRatio) BackupSpaceRatio = 0;
                }
                #endregion
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
                if (MediaType == "Fixed hard disk media") Icon.Source = new BitmapImage(new Uri(@"/Icons/hard_drive.png", UriKind.Relative));
                else if (MediaType == "External hard disk media") Icon.Source = new BitmapImage(new Uri(@"/Icons/usb_drive.png", UriKind.Relative));
                else if (MediaType == "Removable Media") Icon.Source = new BitmapImage(new Uri(@"/Icons/pendrive.png", UriKind.Relative));
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
                #region SpaceStackpanel
                StackPanel Space = new StackPanel();
                Space.Width = 200;
                Space.Height = 25;
                Space.Background = Brushes.White;
                Space.Orientation = Orientation.Horizontal;
                Space.HorizontalAlignment = HorizontalAlignment.Left;
                Space.Margin = new Thickness(5, 0, 0, 0);
                #endregion
                #region UsedUpSpace
                Rectangle UsedUpSpace = new Rectangle();
                double value = 200 - ((AvailableSpaceRatio + BackupUsedSpaceRatio)* 200);
                UsedUpSpace.Width = value;
                if (AvailableSpaceRatio < 0.1) UsedUpSpace.Fill = Brushes.Red;
                else if (AvailableSpaceRatio < 0.2) UsedUpSpace.Fill = Brushes.Orange;
                else UsedUpSpace.Fill = Brushes.LimeGreen;
                #endregion
                #region BackupUsedSpace
                Rectangle BackupUsedSpace = new Rectangle();
                double value2 = BackupUsedSpaceRatio * 200;
                BackupUsedSpace.Width = value2;
                BackupUsedSpace.Fill = Brushes.Teal;
                #endregion
                #region BackupSpace
                Rectangle BackupSpace = new Rectangle();
                double value3 = BackupSpaceRatio * 200;
                BackupSpace.Width = value3;
                BackupSpace.Fill = new SolidColorBrush(Color.FromRgb(80, 180, 255));
                #endregion
                Space.Children.Add(UsedUpSpace);
                Space.Children.Add(BackupUsedSpace);
                Space.Children.Add(BackupSpace);
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
                Size.VerticalAlignment = VerticalAlignment.Center;
                Size.Width = 150;
                Size.Orientation = Orientation.Horizontal;
                TextBox DiskSpaceLimit = new TextBox
                {
                    Width = 80,
                    Height = 25,
                    Background = new SolidColorBrush(Color.FromRgb(21, 21, 21)),
                    Foreground = new SolidColorBrush(Color.FromRgb(172, 172, 172)),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(40, 0, 0, 0),
                    Tag = ThisDriveSerial
                };
                if (isBackupEnabled)
                {
                    if (Main.BackupDriveSizeLimits.ContainsKey(ThisDriveSerial))
                    {
                        DiskSpaceLimit.Text = Main.BackupDriveSizeLimits[ThisDriveSerial].Text;
                    }
                    else DiskSpaceLimit.Text = BackupProcess.GetBackupdriveFromSerial(ThisDriveSerial).SizeLimit.Gigabytes.ToString();
                    if (DiskSpaceLimit.Text == "0") DiskSpaceLimit.Text = "";
                }
                DiskSpaceLimit.TextChanged += SetLimitChange;
                if (Main.BackupDriveSizeLimits.ContainsKey(ThisDriveSerial)) Main.BackupDriveSizeLimits[ThisDriveSerial] = DiskSpaceLimit;
                else Main.BackupDriveSizeLimits.Add(ThisDriveSerial, DiskSpaceLimit);
                Label Unit = new Label();
                Unit.Content = "GB";
                Unit.HorizontalAlignment = HorizontalAlignment.Center;
                Unit.Foreground = new SolidColorBrush(Color.FromRgb(172, 172, 172));
                Size.Children.Add(DiskSpaceLimit);
                Size.Children.Add(Unit);
                Drive.Children.Add(Size);
                #endregion
                #region Apply/Modify button
                StackPanel Button = new StackPanel();
                Button.Margin = new Thickness(80, 0, 0, 0);
                Button SetDrive = new Button();
                SetDrive.VerticalAlignment = VerticalAlignment.Center;
                SetDrive.HorizontalAlignment = HorizontalAlignment.Right;
                SetDrive.FontWeight = FontWeights.Bold;
                SetDrive.Height = 34;
                SetDrive.Width = 100;
                SetDrive.Margin = new Thickness(0, 0, 15, 0);
                SetDrive.FontSize = 14;
                SetDrive.Tag = ThisDriveSerial;

                if (!isBackupEnabled)
                {
                    SetDrive.Content = "Activate";
                    if (AvailableSpaceRatio < 0.1 || new DiskSpace(ThisDriveInfo.AvailableFreeSpace).Gigabytes < 5) { SetDrive.IsEnabled = false; SetDrive.Opacity = 0.5; }
                    SetDrive.ToolTip = "use this drive for backups";
                    SetDrive.Click += ActivateDrive_Click;
                }

                else 
                {
                    double.TryParse(DiskSpaceLimit.Text, out double limit);
                    if (BackupProcess.GetBackupdriveFromSerial(ThisDriveSerial).SizeLimit.Gigabytes == limit)
                    {
                        SetDrive.IsEnabled = false;
                        SetDrive.Opacity = 0.5;
                    }
                    SetDrive.Content = "Update limit";
                    SetDrive.Click += UpdateDrive_Click;
                }
                if (!Main.BackupDriveUpdateButtons.ContainsKey(ThisDriveSerial)) Main.BackupDriveUpdateButtons.Add(ThisDriveSerial, SetDrive);
                else Main.BackupDriveUpdateButtons[ThisDriveSerial] = SetDrive;
                Button.Children.Add(SetDrive);
                Button.VerticalAlignment = VerticalAlignment.Center;
                Drive.Children.Add(Button);
                #endregion
                #region Delete button
                if (isBackupEnabled)
                {
                    Image Delete = new Image();
                    Delete.Source = new BitmapImage(new Uri(@"/Icons/Delete icon.png", UriKind.Relative));
                    Delete.Width = 25;
                    Delete.Height = 25;
                    Delete.VerticalAlignment = VerticalAlignment.Center;
                    Delete.Opacity = 0.3;
                    Delete.MouseEnter += Delete_mouseenter;
                    Delete.MouseLeave += Delete_mouseleave;
                    Delete.MouseLeftButtonDown += DeactivateDrive_Click;
                    Delete.Tag = ThisDriveSerial;
                    Delete.ToolTip = "stop using this drive for backups";
                    Drive.Children.Add(Delete);
                }
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

        #region Menu Actions
        private async Task UpdateSubmenu2_Async()// data is for transfering new drive size limit
        {
            StackPanel Drives = new StackPanel();
            #region Loading message
            ListBoxItem Message = new ListBoxItem();
            DockPanel Loading_message = new DockPanel();
            Loading_message.Height = 270;
            Loading_message.Width = 475;
            Loading_message.IsEnabled = false;
            Message.Content = "Loading drives...";
            Message.FontWeight = FontWeights.Normal;
            Message.FontSize = 20;
            Message.Opacity = 0.5;
            Message.VerticalAlignment = VerticalAlignment.Center;
            Message.HorizontalAlignment = HorizontalAlignment.Center;
            Message.VerticalContentAlignment = VerticalAlignment.Center;
            Loading_message.Children.Add(Message);
            Alldrives_scrollviewer.Content = Loading_message;
            #endregion
            StackPanel Available = await CreateAvailableDrivesSP_Async();
            Drives.Children.Add(Available);
            if (Showunavailable_checkbox.IsChecked.Value) Drives.Children.Add(CreateUnavailableDrivesSP());

            Alldrives_scrollviewer.Content = Drives;
        }
        private void ResetBackupSubmenu2()
        {
            Alldrives_scrollviewer.Content = null;
        }
        #endregion

        #endregion

        #region Menu Actions
        public void Reset_Backupmenu()
        {
            Backuptask_listbox.SelectedIndex = -1;
            Warning1_label.Visibility = Visibility.Visible;
            ItemPath_textbox.Foreground = new SolidColorBrush(Color.FromRgb(226, 154, 6));
            ItemPath_textbox.Text = "Select a source folder!";
            Interval_label.Content = "Save interval: ";
            Status_label.Content = "Status info: No items are selected!";
            Status_label.Foreground = new SolidColorBrush(Color.FromRgb(226, 154, 6));
            OnlySaveOnChange_label.Content = "Only save if data is modified: ";
            Lastsaved_label.Content = "Last saved:";
            Backupfilesize_label.Content = "Backup file size: ";
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
            Manualsave_button.Content = "Manual save";
            Save_image.Opacity = 0.5;
            Disablebackup_button.Visibility = Visibility.Hidden;
            Enablebackup_button.IsEnabled = false;
            Enablebackup_button.Visibility = Visibility.Visible;
            Enablebackup_button.Opacity = 0.5;
            ViewSelectedPath_button.IsEnabled = false;
            ViewSelectedPath_button.Opacity = 0.5;
            ViewPathSelection_Combobox.SelectedIndex = 0;
            Display_Backups();
        }

        public void Update_Backupmenu()
        {
            if(Backuptask_listbox.SelectedIndex!=-1)
            {
                Backupitem Item = GetSelectedBackupitem();
                Display_Backups();
                for (int i = 0; i < Backuptask_listbox.Items.Count; i++)
                {
                    ListBoxItem temp = (ListBoxItem)Backuptask_listbox.Items[i];
                    var ThisItem = temp.Tag;
                    if (ThisItem == Item) Backuptask_listbox.SelectedIndex = i;
                }
                Display_Backupitem(Item);
            }
            else
            {
                Display_Backups();
            }
        }

        #region Backup Data-Display
        private void Display_Backups()
        {
            Backuptask_listbox.Items.Clear();
            Backuptask_listbox.IsHitTestVisible = true;
            Warning2_label.Visibility = Visibility.Hidden;
            Warning3_label.Visibility = Visibility.Hidden;
            Warning4_label.Visibility = Visibility.Hidden;
            ListBoxItem ListItem;
            foreach (var Drive in BackupProcess.Backupdrives)
            {
                Drive.Update();
                #region Add backupdrive to list
                ListItem = new ListBoxItem();
                DockPanel Drive_dockpanel = new DockPanel();
                TextBox drivename = new TextBox();
                TextBox drivespace = new TextBox();
                drivename.Text = $"Backup drive: {Drive.GetVolumeLabel()} ({Drive.GetDriveLetter()}:)";
                if (Drive.SizeLimit.Gigabytes == 0) drivespace.Text = $"{Drive.GetBackupSize().Humanize()}";
                else drivespace.Text = $"{Drive.GetBackupSize().Humanize()} / {Drive.SizeLimit.Gigabytes} GB";
                Drive_dockpanel.Children.Add(drivename);
                Drive_dockpanel.Children.Add(drivespace);
                #region Custumization
                DockPanel.SetDock(drivespace, Dock.Right);
                Drive_dockpanel.Width = 475;
                Drive_dockpanel.LastChildFill = false;
                drivename.Background = Brushes.Transparent;
                drivespace.Background = Brushes.Transparent;
                drivename.Foreground = new SolidColorBrush(Color.FromRgb(172, 172, 172));
                drivespace.Foreground = new SolidColorBrush(Color.FromRgb(172, 172, 172));
                drivename.BorderThickness = new Thickness(0);
                drivespace.BorderThickness = new Thickness(0);
                drivename.Focusable = false;
                drivespace.Focusable = false;
                ListItem.BorderThickness = new Thickness(0, 0, 0, 1);
                ListItem.Margin = new Thickness(0, 5, 0, 0);
                #endregion
                Drive.SetDriveNameTextbox(ref drivename, ref ListItem);
                Drive.SetDriveSpaceTextbox(ref drivespace);
                ListItem.Content = Drive_dockpanel;
                ListItem.Tag = Drive.DriveID;
                Backuptask_listbox.Items.Add(ListItem);
                #endregion
                #region Add backupitems of backupdrive
                foreach (var Backupitem in Drive.Backups)
                {
                    ListItem = Backupitem.GetListBoxItem();
                    Backuptask_listbox.Items.Add(ListItem);
                    Backupitem.UpdateWarnings(ref Warning2_label, ref Warning3_label, ref Warning4_label);
                }
                #endregion
            }
            #region Add 'Add new task' button
            if(BackupProcess.Backupdrives.Count()>0)
            {
                ListItem = new ListBoxItem
                {
                    Content = "➕ Add new task",
                    FontWeight = FontWeights.Normal,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Tag = "add"
                };
                ListItem.MouseLeftButtonUp += new MouseButtonEventHandler(Additem_button_Click);
                Backuptask_listbox.Items.Add(ListItem);
            }
            else
            {
                DockPanel container = new DockPanel
                {
                    Height = 270,
                    Width = 475,
                    IsEnabled = false
                };
                ListItem = new ListBoxItem
                {
                    Content = "Go to 'Manage backup drives' to allow\n    backup operations on local drives",
                    FontWeight = FontWeights.Normal,
                    FontSize = 15,
                    Opacity = 0.5,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                container.Children.Add(ListItem);
                Backuptask_listbox.Items.Add(container);
                Backuptask_listbox.IsHitTestVisible = false;
            }
            #endregion
        }

        private void Display_Backupitem(Backupitem Item)
        {
            #region Loads interval
            Item.Configuration.CycleInterval.Humanize();
            Interval_label.Content = $"Save interval: {Item.Configuration.CycleInterval.GetTime()}";
            #endregion

            #region Loads path
            if(ViewPathSelection_Combobox.SelectedIndex == 0)
            {
                Item.SetDestinationTBox(ref ItemPath_textbox);
            }
            else
            {
                Item.SetSourceTBox(ref ItemPath_textbox);
            }
            #endregion

            #region Loads Smart save
            if (Item.Configuration.OnlySaveOnChange) OnlySaveOnChange_label.Content = "Only save if data is modified: ON";
            else OnlySaveOnChange_label.Content = "Only save if data is modified: OFF";
            #endregion

            #region Loads Last saved
            if (Item.LastSaved == DateTime.MinValue) Lastsaved_label.Content = $"Last saved: Never";
            else
            {
                Interval lastsaved = new Interval(DateTime.Now - Item.LastSaved);
                if (lastsaved.IsPlural())
                {
                    Lastsaved_label.Content = $"Last saved: {new Interval(DateTime.Now - Item.LastSaved).GetTime()}s ago";
                }
                else
                {
                    Lastsaved_label.Content = $"Last saved: {new Interval(DateTime.Now - Item.LastSaved).GetTime()} ago";
                }
            }
            #endregion

            #region Get status
            Item.SetStatusInfo(ref Status_label);
            #endregion

            #region Loads backup file size
            Backupfilesize_label.Content = $"Backup file size: {Item.GetBackupSize().Humanize()}";
            #endregion

            #region Buttons

            Item.EnableActionButtons(ref Removeitem_button, ref Enablebackup_button, ref Disablebackup_button, ref Modification_button, ref Repair_button, ref Restorefiles_button, ref Manualsave_button);

            #region View Selected Path
            if (ViewPathSelection_Combobox.SelectedIndex == 0)
            {
                if (Item.IsAvailable && Item.Destination.Exists)
                {
                    ViewSelectedPath_button.IsEnabled = true;
                    ViewSelectedPath_button.Opacity = 1;
                }
                else
                {
                    ViewSelectedPath_button.IsEnabled = false;
                    ViewSelectedPath_button.Opacity = 0.5;
                }
            }
            else
            {
                if (Item.IsAvailable && Item.Source.Exists)
                {
                    ViewSelectedPath_button.IsEnabled = true;
                    ViewSelectedPath_button.Opacity = 1;
                }
                else
                {
                    ViewSelectedPath_button.IsEnabled = false;
                    ViewSelectedPath_button.Opacity = 0.5;
                }
            }

            #endregion

            #endregion
        }
        #endregion

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
            if (!MessageBox.Show("Are you sure you want to close the program?\nAll processes will be halted!", "Close", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.Yes).Equals(MessageBoxResult.Yes))
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