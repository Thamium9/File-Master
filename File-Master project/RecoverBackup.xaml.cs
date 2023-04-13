using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace File_Master_project
{
    /// <summary>
    /// Interaction logic for RecoverBackup.xaml
    /// </summary>
    public partial class RecoverBackup : Window
    {
        private BackupTask Selected;
        private Backup BC;

        public RecoverBackup(BackupTask selected, Backup backup)
        {
            BC = backup;
            Selected = selected;
            InitializeComponent();
        }

        private void BackupRecovery_window_Loaded(object sender, RoutedEventArgs e)
        {
            RecoveryPath_textbox.Text = Selected.Source.FullName.Replace($@"\{Selected.Source.Name}", "");
            //RecoveryPath_textbox.Text = "D:\\Backups\\Recovery";
        }

        private async void Recover_button_Click(object sender, RoutedEventArgs e)
        {
            MainWindow Main = System.Windows.Application.Current.MainWindow as MainWindow;
            string Destination = RecoveryPath_textbox.Text;           
            var Recovery = Task.Run(() =>  Selected.RecoveryRequest(BC, Destination));
            this.Close();
            Main.ActivateWindow();
            Main.Update_Backupmenu();
            await Recovery;
            Main.Update_Backupmenu();
        }

        private void CancelRecovery_button_Click(object sender, RoutedEventArgs e)
        {
            MainWindow Main = System.Windows.Application.Current.MainWindow as MainWindow;
            Main.IsHitTestVisible = true;
            Main.Main_grid.Opacity = 1;
            Main.Update_Backupmenu();
            this.Close();
            Main.Activate();
        }

        private void BrowseRecoveryLoaction_button_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog PathSelect = new FolderBrowserDialog();
            PathSelect.RootFolder = Environment.SpecialFolder.MyComputer;
            PathSelect.SelectedPath = RecoveryPath_textbox.Text;
            if (PathSelect.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                RecoveryPath_textbox.Text = PathSelect.SelectedPath;
            }
        }

        private void RecoveryPath_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(RecoveryPath_textbox.Text != "")
            {
                try
                {
                    DirectoryInfo Path = new DirectoryInfo(RecoveryPath_textbox.Text);
                    if (Path.Exists)
                    {
                        string temp = $@"{Path}\{Selected.Source.Name}";
                        if (!File.Exists(temp) && !Directory.Exists(temp))
                        {
                            Recover_button.IsEnabled = true;
                            Recover_button.Opacity = 1;
                            RecoveryLocError_label.Content = "";
                        }
                        else
                        {
                            Recover_button.IsEnabled = false;
                            Recover_button.Opacity = 0.5;
                            RecoveryLocError_label.Content = "The specified location already contains an item with the same name!";
                        }
                    }
                    else
                    {
                        Recover_button.IsEnabled = false;
                        Recover_button.Opacity = 0.5;
                        RecoveryLocError_label.Content = "The specified location doesn't exists!";
                    }
                }
                catch (Exception)
                {
                    Recover_button.IsEnabled = false;
                    Recover_button.Opacity = 0.5;
                    RecoveryLocError_label.Content = "The specified path is incorrect!";
                }
                
            }
            else
            {
                Recover_button.IsEnabled = false;
                Recover_button.Opacity = 0.5;
                RecoveryLocError_label.Content = "You have to provide a recovery location!";
            }
        }
    }
}
