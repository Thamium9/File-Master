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

                Reset_Backupmenu();
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
        public BackupTask GetSelectedBackupTask()
        {
            BackupTask Item = null;
            ListBoxItem Selection = (ListBoxItem)Backuptask_listbox.SelectedItem;
            if(Selection != null && Selection.Tag.GetType() == typeof(BackupTask))
            {
                Item = (BackupTask)Selection.Tag;
            }
            return Item;
        }
        #endregion

        #region Events
        private void Backuptask_listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)//changes when you select an item from the source list
        {
            if (Backuptask_listbox.SelectedIndex != -1)//if the index is -1 there is no item selected
            {
                BackupTask Item = GetSelectedBackupTask();
                if (Item != null)
                {
                    Display_BackupTask(Item);
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

        private async void DeleteTask_button_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete this task? \nIt will be deleted permanently along with the associated backups!", "Deletion", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel).Equals(MessageBoxResult.Yes))
            {
                try
                {
                    BackupTask Item = GetSelectedBackupTask();
                    var Deletion = Task.Run(() => { Item.BackupDriveOfItem.RemoveBackupTask(Item); });
                    DeleteTask_button.Content = "Deleting task";
                    await Deletion;
                    DeleteTask_button.Content = "Delete task";
                    MessageBox.Show("The backup task was successfully deleted", "Deletion", MessageBoxButton.OK, MessageBoxImage.Information);
                    Reset_Backupmenu();
                    BackupProcess.Upload_BackupInfo();
                }
                catch (Exception)
                {
                    MessageBox.Show("The deletion of the backup task failed!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    //LOG
                }

            }
        }

        private void Restorefiles_button_Click(object sender, RoutedEventArgs e)
        {
            BackupManaging_grid.Visibility = Visibility.Hidden;
            BackupProgress_grid.Visibility = Visibility.Hidden;
            StoredBacups_grid.Visibility = Visibility.Visible;
            StoredBackups_stackpanel.Children.Clear();

            BackupTask Selected = GetSelectedBackupTask();
            if(Selected.Backups.Count > 0)
            {
                foreach (var Backup in Selected.Backups.Reverse<Backup>())
                {
                    #region DockPanel
                    DockPanel dp = new DockPanel();
                    dp.Width = 160;
                    dp.Height = 150;
                    dp.HorizontalAlignment = HorizontalAlignment.Stretch;
                    dp.Background = new SolidColorBrush(Color.FromRgb(33, 33, 33));
                    
                    #endregion

                    #region Backup label
                    Label label = new Label();
                    string format = "000";
                    if (Backup.Partial) label.Content = $"Partial backup [{Backup.NumberID.ToString(format)}]";
                    else label.Content = $"Full bakcup [{Backup.NumberID.ToString(format)}]";
                    label.HorizontalAlignment = HorizontalAlignment.Center;
                    label.VerticalAlignment = VerticalAlignment.Top;
                    label.Foreground = new SolidColorBrush(Color.FromRgb(172, 172, 172));
                    label.FontSize = 15;
                    label.FontWeight = FontWeights.Bold;
                    label.BorderThickness = new Thickness(0, 0, 0, 1);
                    label.BorderBrush = new SolidColorBrush(Color.FromRgb(172, 172, 172));
                    label.Padding = new Thickness(5, 3, 5, 2);
                    DockPanel.SetDock(label, Dock.Top);
                    dp.Children.Add(label);
                    #endregion

                    #region Files label
                    label = new Label();
                    label.Content = $"Files: {Backup.Files.Count}";
                    label.HorizontalAlignment = HorizontalAlignment.Center;
                    label.VerticalAlignment = VerticalAlignment.Bottom;
                    label.FontSize = 14;
                    label.FontWeight = FontWeights.Bold;
                    label.Margin = new Thickness(0, 10, 0, 0);
                    label.Foreground = new SolidColorBrush(Color.FromRgb(172, 172, 172));
                    DockPanel.SetDock(label, Dock.Top);
                    dp.Children.Add(label);
                    #endregion

                    #region Size label   
                    label = new Label();
                    label.Content = $"Size: {Backup.Size.Humanize()}";
                    label.HorizontalAlignment = HorizontalAlignment.Center;
                    label.VerticalAlignment = VerticalAlignment.Bottom;
                    label.FontSize = 14;
                    label.FontWeight = FontWeights.Bold;
                    label.Foreground = new SolidColorBrush(Color.FromRgb(172, 172, 172));
                    DockPanel.SetDock(label, Dock.Top);
                    dp.Children.Add(label);
                    #endregion

                    #region Created label
                    label = new Label();
                    label.Content = $"Created: {Backup.Creation.ToString("yyyy.MM.dd")}";
                    label.HorizontalAlignment = HorizontalAlignment.Center;
                    label.VerticalAlignment = VerticalAlignment.Bottom;
                    label.FontSize = 14;
                    label.FontWeight = FontWeights.Bold;
                    label.Foreground = new SolidColorBrush(Color.FromRgb(172, 172, 172));
                    DockPanel.SetDock(label, Dock.Top);
                    dp.Children.Add(label);
                    #endregion

                    #region Delete button
                    Button DeleteButton = new Button();
                    DeleteButton.Content = "Delete";
                    DeleteButton.Height = 25;
                    DeleteButton.Width = 80;
                    DeleteButton.Background = new SolidColorBrush(Color.FromRgb(55, 55, 55));
                    DeleteButton.Foreground = new SolidColorBrush(Color.FromRgb(235,235,235));
                    DeleteButton.Style = this.FindResource("CustomButtonStyle1") as Style;
                    DeleteButton.VerticalAlignment = VerticalAlignment.Bottom;
                    DeleteButton.Focusable = false;
                    DeleteButton.Click += async (ds, de) => 
                    {
                        if (MessageBox.Show("Are you sure you want to delete this backup?\nThis action is irreversable!", "Delete backup", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes) == MessageBoxResult.Yes)
                        {
                            var Deletion = Task.Run(() => { Selected.DeleteBackup(Backup); });
                            DeleteButton.Content = "Deleting";
                            await Deletion;
                            Update_Backupmenu();
                        }
                    };
                    DockPanel.SetDock(DeleteButton, Dock.Left);
                    dp.Children.Add(DeleteButton);
                    #endregion

                    #region Recover button
                    Button RecoverButton = new Button();
                    RecoverButton = new Button();
                    RecoverButton.Content = "Recover";
                    RecoverButton.Height = 25;
                    RecoverButton.Width = 80;
                    RecoverButton.Background = new SolidColorBrush(Color.FromRgb(55, 55, 55));
                    RecoverButton.Foreground = new SolidColorBrush(Color.FromRgb(235, 235, 235));
                    RecoverButton.Style = this.FindResource("CustomButtonStyle1") as Style;
                    RecoverButton.VerticalAlignment = VerticalAlignment.Bottom;
                    RecoverButton.Focusable = false;
                    RecoverButton.Click += (ds, de) =>
                    {
                        if(Backup.Creation == Selected.LastSaved || MessageBox.Show("The selected backup is not the latest one. \nAre you sure you want to recover this one?", 
                            "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            RecoverBackup RBwindow = new RecoverBackup(Selected, Backup);
                            RBwindow.Owner = this;
                            RBwindow.Show();
                            InactivateWindow();
                            RecoverButton.Content = "Recovering";
                        }
                    };
                    DockPanel.SetDock(RecoverButton, Dock.Right);
                    dp.Children.Add(RecoverButton);
                    #endregion

                    #region Border
                    Border ObjectContainer = new Border();
                    if (Backup.Creation == Selected.LastSaved)
                    {
                        ObjectContainer.BorderThickness = new Thickness(5);
                        ObjectContainer.BorderBrush = this.FindResource("NewestBackup_Border") as LinearGradientBrush;
                        ObjectContainer.Margin = new Thickness(10, 0, 10, 0);
                        ObjectContainer.Height = 160;
                        ObjectContainer.Child = dp;
                    }
                    else
                    {
                        ObjectContainer.Margin = new Thickness(10, 0, 10, 0);
                        ObjectContainer.Height = 160;
                        ObjectContainer.Child = dp;
                    }
                    #endregion

                    StoredBackups_stackpanel.Children.Add(ObjectContainer);
                }
            }
            else
            {
                DockPanel dp = new DockPanel
                {
                    Width = 780,
                    Height = 180
                };

                Label label = new Label
                {
                    Content = "There are no backups stored yet!",
                    FontWeight = FontWeights.Normal,
                    FontSize = 15,
                    Opacity = 0.5,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.FromRgb(172, 172, 172))
                };
                dp.Children.Add(label);
                StoredBackups_stackpanel.Children.Add(dp);
            }
        }

        private void Modification_button_Click(object sender, RoutedEventArgs e)
        {
            HideAllMenu();
            Backupsubmenu1_grid.Visibility = Visibility.Visible;
            Menu = "Backup.sub1";
            Modifyitemapply_button.Visibility = Visibility.Visible;

            #region Data load
            BackupTask BTask = GetSelectedBackupTask();
            Destinationinput_textbox.Text = BTask.Destination.FullName;
            Sourceinput_textbox.Text = BTask.Source.FullName;
            BackupTaskLabel_textbox.Text = BTask.Label;
            NumberOfCycles_textbox.Text = BTask.Configuration.NumberOfCycles.ToString();
            foreach (ComboBoxItem item in Intervalselection_combobox.Items)
            {
                if(item.Tag.ToString() == BTask.Configuration.CycleInterval.GetTime())
                {
                    Intervalselection_combobox.SelectedItem = item;
                }
            }
            BackupDrive Drive = BTask.BackupDriveOfItem;
            ComboBoxItem CI = new ComboBoxItem();
            CI.Content = $"({Drive.GetDriveLetter()}:) {Drive.GetVolumeLabel()}";
            CI.Tag = Drive;
            Backupdriveselect_combobox_Refresh(CI);
            Backupdriveselect_combobox.IsEnabled = false;
            Backupdriveselect_combobox.Opacity = 0.5;
            #endregion
        }

        private void Enablebackup_button_Click(object sender, RoutedEventArgs e)
        {
            BackupTask Item = GetSelectedBackupTask();
            foreach (var Drive in BackupProcess.BackupDrives)
            {
                Drive.SetBackupTaskState(true, Item);
            }
            Update_Backupmenu();
            BackupProcess.Upload_BackupInfo();
        }

        private void Disablebackup_button_Click(object sender, RoutedEventArgs e)
        {
            BackupTask Item = GetSelectedBackupTask();
            foreach (var Drive in BackupProcess.BackupDrives)
            {
                Drive.SetBackupTaskState(false, Item);
            }
            Update_Backupmenu();
            BackupProcess.Upload_BackupInfo();
        }

        private async void Manualsave_button_Click(object sender, RoutedEventArgs e)
        {
            if (Warning_Save())
            {
                BackupTask Item = GetSelectedBackupTask();
                var Backup = Task.Run(() => BackupProcess.Manualsave_Async(Item));
                System.Threading.Thread.Sleep(100);
                Update_Backupmenu();
                await Backup;
                Update_Backupmenu();
                BackupProcess.Upload_BackupInfo();
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
                    Process.Start(GetSelectedBackupTask().RootDirectoty);
                }
                else
                {
                    FileSystemInfo Source = GetSelectedBackupTask().Source;
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

        private void CancelBackupOperation_button_Click(object sender, RoutedEventArgs e)
        {
            GetSelectedBackupTask().CancelBackup.Cancel();
        }

        private void StoredBackupsBack_button_Click(object sender, RoutedEventArgs e)
        {
            BackupManaging_grid.Visibility = Visibility.Visible;
            BackupProgress_grid.Visibility = Visibility.Hidden;
            StoredBacups_grid.Visibility = Visibility.Hidden;
        }

        private void DeleteAllBackup_button_Click(object sender, RoutedEventArgs e)
        {
            if(MessageBox.Show("Are you sure you want to delete every backup associated with the selected backup task?", "Delete backups", MessageBoxButton.YesNo, MessageBoxImage.Warning).Equals(MessageBoxResult.Yes))
            {
                GetSelectedBackupTask().DeleteBackups();
                Update_Backupmenu();
            }
        }
        #endregion

        #region Submenu1

        #region Events

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
            foreach (var Drive in BackupProcess.BackupDrives)
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
                BackupDrive SelectedDrive = (BackupDrive)selected.Tag;
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
            else
            {
                AvailableFreeSpaceBD_label.Content = "-";
                AvailableAllocatedSpace_label.Content = "-";
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
                    ComboBoxItem CI = (ComboBoxItem)Backupdriveselect_combobox.SelectedItem;
                    BackupDrive Target = (BackupDrive)CI.Tag;
                    Target.AddBackupTask(CreateBackupitem());
                    BackupProcess.Upload_BackupInfo();
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
                BackupTask SelectedItem = GetSelectedBackupTask();
                BackupTaskConfiguration Settings = CreateBackupConfiguration();
                bool update = false;
                if(SelectedItem.Backups.Count > 0)
                {
                    if (SelectedItem.Configuration.SourcePath != Settings.SourcePath ||
                        SelectedItem.Configuration.Method != Settings.Method ||
                        SelectedItem.Configuration.CycleLength != Settings.CycleLength ||
                        SelectedItem.Configuration.FileCompression != Settings.FileCompression)
                    {
                        if (MessageBox.Show("You have made changes that are not compatible with the currently stored backups! The backups associated with this backup task will be deleted permanently! Do you still want to proceed?", "Modify", MessageBoxButton.YesNo, MessageBoxImage.Warning).Equals(MessageBoxResult.Yes))
                        {
                            SelectedItem.DeleteBackups();
                            update = true;
                        }
                    }
                    else if (Destinationinput_textbox.Text != SelectedItem.Destination.FullName)
                    {
                        MessageBoxResult mbr = MessageBox.Show("You have changed the destination of the backup task! \nWould you like to move the stored backups to this new location? \n\nClick 'yes' to move the backups \nClick 'no' to delete the stored backups.", "Modify", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                        if (mbr == MessageBoxResult.Yes)
                        {
                            update = true;
                        }
                        else if (mbr == MessageBoxResult.No && MessageBox.Show("Are you sure you want to delete all stored backups associated with this task? This action is permanent!", "Delete backups", MessageBoxButton.YesNo, MessageBoxImage.Warning).Equals(MessageBoxResult.Yes))
                        {
                            SelectedItem.DeleteBackups();
                            update = true;
                        }
                    }
                    else if(SelectedItem.Label != SelectedItem.Label)
                    {
                        if (MessageBox.Show("You have changed the label of the backup task! \nThe backup containing folder will be renamed.", "Modify", MessageBoxButton.YesNo, MessageBoxImage.Warning).Equals(MessageBoxResult.Yes))
                        {
                            update = true;
                        }
                    }
                    else
                    {
                        update = true;
                    }
                }
                else
                {                    
                    update = true;
                }
                if(update)
                {
                    InactivateWindow();
                    //make this async!!!!
                    string Label = BackupTaskLabel_textbox.Text;
                    if (Label == "") Label = $"BACKUP_{new FileInfo(Sourceinput_textbox.Text).Name}";
                    SelectedItem.UpdateConfiguration(Destinationinput_textbox.Text, Label, Settings);
                    ActivateWindow();
                    BackupProcess.Upload_BackupInfo();
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
            int NumberOfCycles;
            if (!int.TryParse(NumberOfCycles_textbox.Text, out NumberOfCycles)) NumberOfCycles = -1;
            BackupDrive Target = null;
            if (Selection.Tag != null && Selection.Tag.GetType() == typeof(BackupDrive))
            {
                Target = (BackupDrive)Selection.Tag;
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
            else if (NumberOfCycles < 0)
            {
                MessageBox.Show("The number of cycles must be a valid integer number greater than -1!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                return true;
            }
            return false;
        }

        private BackupTask CreateBackupitem()
        {
            BackupTaskConfiguration Settings = CreateBackupConfiguration();
            string Destination = Destinationinput_textbox.Text;
            string Label = BackupTaskLabel_textbox.Text;
            if (Label == "") Label = $"BACKUP_{new FileInfo(Sourceinput_textbox.Text).Name}";
            BackupTask Item = new BackupTask(Destination, Label, Settings);
            return Item;
        }

        private BackupTaskConfiguration CreateBackupConfiguration()
        {
            string Source = Sourceinput_textbox.Text;
            char Method = 'F';
            int CycleLength = 1;
            int NumberofCycles = int.Parse(NumberOfCycles_textbox.Text);
            ComboBoxItem CI = (ComboBoxItem)Intervalselection_combobox.SelectedItem;
            Interval CycleInterval = new Interval(CI.Tag.ToString());
            DiskSpace MaxStorageData = new DiskSpace(0);
            Interval RetryWaitTime = new Interval("5 minute");
            int MaxNumberOfRetries = 3;
            bool PopupOnFail = false;
            BackupTaskConfiguration Settings = new BackupTaskConfiguration(
                Source,
                Method,
                CycleLength,
                NumberofCycles,
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

        private void Reset_BackupSubmenu1Settings()
        {
            BackupMethodFull_radiobutton.IsChecked = true;
        }

        #endregion

        #region Menu control
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
            Backupdriveselect_combobox.IsEnabled = true;
            Backupdriveselect_combobox.Opacity = 1;
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
            BackupProcess.ActivateBackupDrive(serial, Space);
            if (BackupProcess.GetBackupDriveFromSerial(serial).SizeLimitCheck(out double result))
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
                BackupProcess.GetBackupDriveFromSerial(serial).SizeLimit.Gigabytes = limit;
                if(BackupProcess.GetBackupDriveFromSerial(serial).SizeLimitCheck(out double result))
                {
                    MessageBox.Show("The set limit cannot be this big!\nIt will be set to the maximum allowed amount!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Main.BackupDriveSizeLimits[serial].Text = result.ToString();
                }
            }
            else
            {
                BackupProcess.GetBackupDriveFromSerial(serial).SizeLimit.Gigabytes = 0;
                MessageBox.Show("Invalid limit!\nThe limit has been removed!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                Main.BackupDriveSizeLimits[serial].Text = "";
            }
            BackupProcess.Upload_BackupInfo();
            UpdateSubmenu2_Async();
        }

        private void DeactivateDrive_Click(object sender, RoutedEventArgs e)
        {
            BackupProcess.DeactivateBackupDrive(((Image)sender).Tag.ToString());
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

                if (MediaType == "") continue; // skips unsupported drive type from the list
                string ThisDriveSerial = ThisDrive.Key;
                DriveInfo ThisDriveInfo = ThisDrive.Value.DriveInformation;

                bool isBackupEnabled = BackupProcess.IsBackupdrive(ThisDriveSerial);

                double AvailableSpaceRatio = (double)ThisDriveInfo.AvailableFreeSpace / (double)ThisDriveInfo.TotalSize;
                double BackupUsedSpaceRatio = 0;
                double BackupSpaceRatio = 0;
                if (isBackupEnabled)
                {
                    BackupUsedSpaceRatio += (double)BackupProcess.GetBackupDriveFromSerial(ThisDriveSerial).GetBackupSize().Bytes / (double)ThisDriveInfo.TotalSize;
                    BackupSpaceRatio += ((double)BackupProcess.GetBackupDriveFromSerial(ThisDriveSerial).SizeLimit.Bytes / (double)ThisDriveInfo.TotalSize) - BackupUsedSpaceRatio;
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
                string volumelabel = ThisDriveInfo.VolumeLabel;
                if(volumelabel == "")
                {
                    if (ThisDriveInfo.DriveType == DriveType.Fixed) volumelabel = "Local Disk";
                    else volumelabel = "Drive";
                }
                TextBlock infotext = new TextBlock();
                infotext.Text = $"{volumelabel} ({ThisDriveInfo.Name})";
                Info.Content = infotext;
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
                    else DiskSpaceLimit.Text = BackupProcess.GetBackupDriveFromSerial(ThisDriveSerial).SizeLimit.Gigabytes.ToString();
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
                    if (BackupProcess.GetBackupDriveFromSerial(ThisDriveSerial).SizeLimit.Gigabytes == limit)
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
            foreach (var ThisDrive in BackupProcess.BackupDrives)
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
                    TextBlock infotext = new TextBlock();
                    infotext.Text = $"{ThisDrive.GetVolumeLabel()} ({ThisDrive.GetDriveLetter()})";
                    Info.Content = infotext;
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

        #region Menu control
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

        #region Menu control
        public void Reset_Backupmenu()
        {
            Backuptask_listbox.SelectedIndex = -1;         
            ItemPath_textbox.Foreground = new SolidColorBrush(Color.FromRgb(226, 154, 6));
            ItemPath_textbox.Text = "Select a source folder!";
            Interval_label.Content = "Save interval: ";
            NumberOfBackups_label.Content = $"Current number of backups: ";
            Method_label.Content = "Method: ";
            Status_label.Content = "Status info: No items are selected!";
            Status_label.Foreground = new SolidColorBrush(Color.FromRgb(226, 154, 6));
            OnlySaveOnChange_label.Content = "Only save if data is modified: -";
            Lastsaved_label.Content = "Last saved:";
            Backupfilesize_label.Content = "Backup file size: ";
            DeleteTask_button.IsEnabled = false;
            DeleteTask_button.Opacity = 0.5;
            Restorefiles_button.IsEnabled = false;
            Restorefiles_button.Opacity = 0.5;
            Modification_button.IsEnabled = false;
            Modification_button.Opacity = 0.5;
            Modification_button.Visibility = Visibility.Visible;
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
            BackupProgress_grid.Visibility = Visibility.Hidden;
            StoredBacups_grid.Visibility = Visibility.Hidden;
            BackupManaging_grid.Visibility = Visibility.Visible;
            Display_Backups();
        }

        public void Update_Backupmenu()
        {
            if(Backuptask_listbox.SelectedIndex!=-1)
            {
                BackupTask Item = GetSelectedBackupTask();
                Display_Backups();
                for (int i = 0; i < Backuptask_listbox.Items.Count; i++)
                {
                    ListBoxItem temp = (ListBoxItem)Backuptask_listbox.Items[i];
                    var ThisItem = temp.Tag;
                    if (ThisItem == Item) Backuptask_listbox.SelectedIndex = i;
                }
                Display_BackupTask(Item);
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
            ListBoxItem ListItem;
            foreach (var Drive in BackupProcess.BackupDrives)
            {
                Drive.Update();
                //Backupdrive
                Backuptask_listbox.Items.Add(GetBackupDriveLBI(Drive));

                //Backupitems
                foreach (var Backupitem in Drive.BackupTasks)
                {
                    ListItem = GetBackupItemLBI(Backupitem);
                    Backuptask_listbox.Items.Add(ListItem);
                }
            }
            #region Add 'Add new task' button
            if(BackupProcess.BackupDrives.Count()>0)
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
                    Height = Backuptask_listbox.Height,
                    Width = Backuptask_listbox.Width - 25,
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

        private void Display_BackupTask(BackupTask Item)
        {
            StoredBacups_grid.Visibility = Visibility.Hidden;
            if(Item.ActiveTask)
            {
                BackupProgress_grid.Visibility = Visibility.Visible;
                BackupManaging_grid.Visibility = Visibility.Hidden;
                BackupProcess.DisplayBackupProgress(Item);
            }
            else
            {
                BackupProgress_grid.Visibility = Visibility.Hidden;
                BackupManaging_grid.Visibility = Visibility.Visible;
                if(Item.IsAvailable)
                {
                    #region Loads interval
                    Item.Configuration.CycleInterval.Humanize();
                    Interval_label.Content = $"Save interval: {Item.Configuration.CycleInterval.GetTime()}";
                    #endregion

                    #region Loads current number of backup
                    NumberOfBackups_label.Content = $"Current number of backups: {Item.Backups.Count}";
                    #endregion

                    #region Loads Method
                    string method;
                    switch (Item.Configuration.Method)
                    {
                        case 'F': 
                            method = "Full backup";
                            break;
                        case 'I':
                            method = "Incremental backup";
                            break;
                        case 'D':
                            method = "Differential backup";
                            break;
                        default:
                            method = "Unknown";
                            break;
                    }
                    Method_label.Content = $"Method: {method}";
                    #endregion

                    #region Loads OSC
                    //if (Item.Configuration.OnlySaveOnChange) OnlySaveOnChange_label.Content = "Only save if data is modified: ON";
                    //else OnlySaveOnChange_label.Content = "Only save if data is modified: OFF";
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

                    #region Loads backup file size
                    Backupfilesize_label.Content = $"Backup file size: {Item.BackupsSize.Humanize()}";
                    #endregion

                    #region Buttons
                    #region Enable/Disable
                    if (Item.IsAvailable && !Item.IsOutOfSpace && Item.Source.Exists)
                    {
                        if (!Item.IsEnabled)
                        {
                            Disablebackup_button.Visibility = Visibility.Hidden;
                            Enablebackup_button.Visibility = Visibility.Visible;
                            Enablebackup_button.IsEnabled = true;
                            Enablebackup_button.Opacity = 1;
                        }
                        else
                        {
                            Disablebackup_button.Visibility = Visibility.Visible;
                            Enablebackup_button.Visibility = Visibility.Hidden;
                        }
                    }
                    else
                    {
                        Disablebackup_button.Visibility = Visibility.Hidden;
                        Enablebackup_button.Visibility = Visibility.Visible;
                        Enablebackup_button.IsEnabled = false;
                        Enablebackup_button.Opacity = 0.5;
                    }
                    #endregion
                    #region Modification
                    Modification_button.Opacity = 1;
                    Modification_button.IsEnabled = true;
                    #endregion
                    #region Restore
                    Restorefiles_button.IsEnabled = true;
                    Restorefiles_button.Opacity = 1;
                    #endregion
                    #region Manual save
                    if (Item.ActiveTask)
                    {
                        Manualsave_button.IsEnabled = false;
                        Manualsave_button.Opacity = 0.5;
                        Manualsave_button.Content = "Saving...";
                    }
                    else
                    {
                        if (Item.IsAvailable && !Item.IsOutOfSpace && Item.Source.Exists)
                        {
                            Manualsave_button.IsEnabled = true;
                            Manualsave_button.Opacity = 1;
                            Manualsave_button.Content = "Manual save";
                        }
                        else
                        {
                            Manualsave_button.IsEnabled = false;
                            Manualsave_button.Opacity = 0.5;
                            Manualsave_button.Content = "Manual save";
                        }
                    }
                    #endregion
                    #endregion
                }
                #region Buttons
                #region Remove
                DeleteTask_button.IsEnabled = true;
                DeleteTask_button.Opacity = 1;
                #endregion
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

                #region Loads path
                bool dest = false;
                ItemPath_textbox.Foreground = new SolidColorBrush(Color.FromRgb(0, 230, 120));
                if (ViewPathSelection_Combobox.SelectedIndex == 0)
                {
                    ItemPath_textbox.Text = Item.Destination.FullName;
                    dest = true;
                }
                else
                {
                    if (Item.IsAvailable) ItemPath_textbox.Text = Item.Source.FullName;
                    else ItemPath_textbox.Text = "The source is unknown!";
                }

                if(!Item.IsAvailable)
                {
                    ItemPath_textbox.Foreground = new SolidColorBrush(Color.FromRgb(230, 0, 0));
                }
                else if (Item.IsOutOfSpace)
                {
                    if (BackupProcess.Settings.IsTempFolderEnabled)
                    {
                        if(dest) ItemPath_textbox.Text = BackupProcess.Settings.TempFolder.FullName;
                        ItemPath_textbox.Foreground = new SolidColorBrush(Color.FromRgb(225, 225, 0));
                    }
                    else
                    {
                        ItemPath_textbox.Foreground = new SolidColorBrush(Color.FromRgb(230, 0, 0));
                    }
                }
                #endregion

                #region Get status
                #region Default status
                Status_label.Foreground = new SolidColorBrush(Color.FromRgb(0, 230, 120));
                Status_label.Content = "Status: OK!";
                #endregion

                if(!Item.IsAvailable)
                {
                    Status_label.Foreground = new SolidColorBrush(Color.FromRgb(230, 0, 0));
                    Status_label.Content = "Status info: The destination is unreachable!";
                }
                else if(Item.IsOutOfSpace)
                {
                    if (BackupProcess.Settings.IsTempFolderEnabled)//Can save to temp-drive temp
                    {
                        Status_label.Foreground = new SolidColorBrush(Color.FromRgb(225, 225, 0));
                        Status_label.Content = "Status info: An alternative destination is used!";
                    }
                    else
                    {
                        Status_label.Foreground = new SolidColorBrush(Color.FromRgb(230, 0, 0));
                        Status_label.Content = "Status info: The backup drive has reached its space limit!";
                    }
                }
                else if (!Item.Source.Exists)
                {
                    Status_label.Foreground = new SolidColorBrush(Color.FromRgb(200, 0, 180));
                    Status_label.Content = "Status info: The source is missing!";
                }
                else if (!Item.IsEnabled)
                {
                    Status_label.Foreground = new SolidColorBrush(Color.FromRgb(240, 70, 0));
                    Status_label.Content = "Status info: The backup item is disabled!";
                }
                #endregion
            }

        }

        #region Process data

        private ListBoxItem GetBackupItemLBI(BackupTask Item)
        {
            ListBoxItem LBI = new ListBoxItem();
            LBI.Opacity = 0.8;
            LBI.Content = $"◍ {Item.GetBackupType()}: {Item.Label} - ({Item.BackupsSize.Humanize()})";
            LBI.Tag = Item;
            LBI.Padding = new Thickness(15, 0, 0, 0);           

            #region Status color    

            //Defaults
            if (Item.ActiveTask)
            {
                LBI.Foreground = new SolidColorBrush(Color.FromRgb(0, 145, 250));
                LBI.Content = $"◍ {Item.GetBackupType()}: {Item.Source.FullName} - (saving...)";
            }
            else
            {
                LBI.Foreground = new SolidColorBrush(Color.FromRgb(0, 230, 120));
            }

            //Issues
            if (!Item.IsAvailable)
            {
                LBI.Foreground = new SolidColorBrush(Color.FromRgb(230, 0, 0));
            }
            else if(Item.IsOutOfSpace)
            {
                if (BackupProcess.Settings.IsTempFolderEnabled)//Can save to temp-drive temp
                {
                    LBI.Foreground = new SolidColorBrush(Color.FromRgb(225, 225, 0));
                }
                else
                {
                    LBI.Foreground = new SolidColorBrush(Color.FromRgb(230, 0, 0));
                }
            }
            else if (!Item.Source.Exists)
            {
                LBI.Foreground = new SolidColorBrush(Color.FromRgb(200, 0, 180));
            }
            else if (!Item.IsEnabled)
            {
                LBI.Foreground = new SolidColorBrush(Color.FromRgb(240, 70, 0));
            }
            #endregion

            return LBI;
        }

        private ListBoxItem GetBackupDriveLBI(BackupDrive Drive)
        {
            #region Initialization
            ListBoxItem LBI = new ListBoxItem();
            DockPanel Drive_dockpanel = new DockPanel();
            TextBox drivename = new TextBox();
            TextBox drivespace = new TextBox();
            #endregion

            #region Drivespace + Drivename
            drivename.Text = $"Backup drive: {Drive.GetVolumeLabel()} ({Drive.GetDriveLetter()}:)";
            if (Drive.SizeLimit.Gigabytes == 0) drivespace.Text = $"{Drive.GetBackupSize().Humanize()}";
            else drivespace.Text = $"{Drive.GetBackupSize().Humanize()} / {Drive.SizeLimit.Gigabytes} GB";
            Drive_dockpanel.Children.Add(drivename);
            Drive_dockpanel.Children.Add(drivespace);

            #region Customization
            DockPanel.SetDock(drivespace, Dock.Right);
            Drive_dockpanel.Width = Backuptask_listbox.Width - 25;
            Drive_dockpanel.LastChildFill = false;
            drivename.Background = Brushes.Transparent;
            drivespace.Background = Brushes.Transparent;
            drivename.Foreground = new SolidColorBrush(Color.FromRgb(172, 172, 172));
            drivespace.Foreground = new SolidColorBrush(Color.FromRgb(172, 172, 172));
            drivename.BorderThickness = new Thickness(0);
            drivespace.BorderThickness = new Thickness(0);
            drivename.Focusable = false;
            drivespace.Focusable = false;
            LBI.BorderThickness = new Thickness(0, 0, 0, 1);
            LBI.Margin = new Thickness(0, 5, 0, 0);
            #endregion
            #endregion

            #region Status color
            #region Default
            drivespace.Foreground = new SolidColorBrush(Color.FromRgb(0, 230, 120));
            drivename.Foreground = new SolidColorBrush(Color.FromRgb(0, 230, 120));
            LBI.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 230, 120));
            #endregion

            if (Drive.IsAvailable == false)
            {
                drivename.Foreground = new SolidColorBrush(Color.FromRgb(230, 0, 0));
                drivespace.Foreground = new SolidColorBrush(Color.FromRgb(230, 0, 0));
                LBI.BorderBrush = new SolidColorBrush(Color.FromRgb(230, 0, 0));
            }

            else if (Drive.IsOutOfSpace == true)
            {
                drivespace.Foreground = new SolidColorBrush(Color.FromRgb(230, 0, 0));
            }
            #endregion

            LBI.Content = Drive_dockpanel;
            LBI.Tag = Drive.DriveID;
            return LBI;
        }

        #endregion

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

        #region Window Control
        public void InactivateWindow()
        {
            Main_grid.Opacity = 0.5;
            this.IsHitTestVisible = false;
        }

        public void ActivateWindow()
        {
            this.IsHitTestVisible = true;
            this.Main_grid.Opacity = 1;
            this.Activate();
        }
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