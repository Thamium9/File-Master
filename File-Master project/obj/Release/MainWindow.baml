   M S B A M L   `   `   `  ���� F  BFile-Master, Version=0.8.4.0, Culture=neutral, PublicKeyToken=null9!clr-namespace:File_Master_projectFile_Master_project  $    File_Master_project.MainWindow   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     3local!clr-namespace:File_Master_project     _ [Hardcodet.NotifyIcon.Wpf, Version=1.1.0.0, Culture=neutral, PublicKeyToken=682384a853a08aad)tb http://www.hardcodet.net/taskbar  -     �Name$  Main_window�� � Title$ File-Master��$	��700��$
��1025�����	#FF0A0A0A
���   � � WindowStartupLocation$ CenterScreen=����	#FF0A0A0A$��0,0,0,0q�$y�Visible=� � WindowStyle$
 None=� � Icon$ Icons/icon-recycle-10.jpg�� � 
ResizeMode$ CanMinimize=�$��0,0,0,0q�$��0,0,0,0q�.��� -    �Name$ 	Main_grid��$��0,30,0,0q����	#FF1A1A1A.+�3  -Hardcodet.Wpf.TaskbarNotification.TaskbarIcon  -     Name$ NotifyIcon_Taskbar��   
IconSource$  /Icons/icon-recycle-10.ico��	   ToolTipText$	 File-Master���� -   
 ��Name$
 debug_label��$�� ��$
��Left=�$��303,-32,0,0q�$	��Top=�$	��155��$��31�����	#FFACACAC$��Italic$�$y�Hidden=��� -    ��Name$ 
Icon_image��$
��Left=�$��25��$��	5,-27,0,0q�$	��Top=�$��25��$��Icons/icon-recycle-10.jpg���� $��File-Master��$
��Left=�$��
31,-32,0,0q�$	��Top=����	#FFACACAC$
��Bold"�$��16(��� $��v0.8.4��$
��Left=�$��126,-28,0,0q�$	��Top=����	#FFACACAC�� -    ��Name$" Minimizebackground_rectangle�����	#FF404040$
��Left=�$��24��$��950,-28,0,0q�
���   �$	��Top=�$��24��$y�Hidden=��� -   $ Closebackground_rectangle�����	#FFC80000$
��Left=�$��24��$��987,-28,0,0q�
���   �$	��Top=�$��24��$y�Hidden=��� -   $ Close_image��$
��Left=�$��32��$��983,-32,0,0q�$	��Top=�$��32��$(��"Icons/icons8-close-window-64v2.png�� J� RenderTransformOrigin$ 1.156,0.236&��� -	   $ Minimize_image��$
��Left=�$��32��$��946,-32,0,0q�$	��Top=�$��32��$+��%Icons/icons8-minimize-window-64v2.png���� -
   $��Debug��$
��Left=�$��205,-28,0,0q�$	��Top=�$��75��$y�Hidden=�]� $
��Left=�$	��670����.  $	��Top=�$	��160�����	#FF141414o� -    o�Name$ Categorization_menu��$	��100��$	��Top=�$	��160��$��	0,240,0,0q�$
��Left=�$��Center=����	#FF141414���	#FF303030$��0q��� -   $ FCleanup_image��$
��Left=�$��40��$��
60,260,0,0q�$	��Top=�$��40��$��Icons/cleanup2.png�� J� Opacity$
 0.25Q�$y�Hidden=��� -   $
 Fc_label��$��File-Cleanup��$
��Left=�$	��Top=����	#FFACACAC$��16(�$
��Bold"�$��Center=�$	��100��$��	0,240,0,0q�$	��160��{�.  $
 0.25Q�$��5,40,5,5q�$��Center=�$y�Hidden=�o� -   $ FileCleanup_menu��$	��100��$	��Top=�$	��160��$��	0,120,0,0q�$
��Left=�$��Center=����	#FF141414���	#FF303030$��0q��� -   $ Categorization_image��$
��Left=�$��40��$��
60,140,0,0q�$	��Top=�$��40��$��Icons/categorization4.png��$
 0.25Q�$y�Hidden=��� -   $
 	Ctg_label��$��Categorization��$
��Left=�$	��Top=����	#FFACACAC$��16(�$
��Bold"�$��Center=�$	��100��$��	0,120,0,0q�$	��160��{�.  $��Center=�$��5,40,5,5q�$
 0.25Q�$y�Hidden=�o� -   $ Backup_menu��$	��100��$	��Top=�$	��160��$
��Left=�$��Center=����	#FF1A1A1A���	#FF303030$��0q��� -   $ Backup_image��$
��Left=�$��40��$��	60,20,0,0q�$	��Top=�$��40��$��Icons/backup3.png���� -   $
 Bc_label��$��Backup��$
��Left=�$	��Top=����	#FFACACAC$��16(�$
��Bold"�$��Center=�$	��100��$	��160��$��Center=�$��5,40,5,5q�o� -   $ Settings_menu��$��50��$	��Top=�$	��160��$��	0,620,0,0q�$
��Left=�$��Center=����	#FF141414���	#FF303030$��0q�� -   $ Backup_grid��$
��Left=�$	��670��$��	160,0,0,0q�$	��Top=�$	��865��.+��� $��Manage selected item��$��Right=�$	��288��$��	0,0,35,35q�$��Bottom=�$	��800�����	#FFACACAC$��1q����	#FFBBBBBB$��14(�.��� -   $ ManageBackupItem_grid��$
��Left=�$	��264��$	��Top=�$	��790��$��0,0,0,0q�$y�Visible=�.+�� -   $ StoredBacups_grid��$��Center=�$��Center=�$��0,0,0,0q�$	��790��$	��264��$y�Hidden=�.+��� -    ��Name$  StoredBackups_scrollviewer��$��Visible=�$��Hidden=�$	��Top=�$	��200��$��0,64,0,0q����	#FF151515.���� -    ��Name$ StoredBackups_stackpanel�� �� Orientation$ 
Horizontal=�$��LeftToRight=� � UseLayoutRounding .  $
��Left=�.+���    NewestBackup_Border#���   $��	10,0,10,0q�$��5,5,5,5q�$	��160��._�]� $	��160��$	��150��$��Stretch=����	#FF212121.+��� $��Full backup��$��Center=�$	��Top=����	#FFACACAC$��15(�$
��Bold"�$	��Top=�$��0,0,0,1q����	#FFABA8A8$��5,3,5,2q��� $��
Files: 502��$��Center=�$��Bottom=����	#FFACACAC$��14(�$
��Bold"�$	��Top=�$��0,10,0,0q��� $��Size: 22 GB��$��Center=�$��Bottom=����	#FFACACAC$��14(�$
��Bold"�$	��Top=��� $��Created: 2022.12.24��$��Center=�$��Bottom=����	#FFACACAC$��14(�$
��Bold"�$	��Top=��� $��Recover��$��80��$��Right=�$��25��$��Right=�$��Bottom=����	#FF373737���	#FFEDEDED�� $��Delete��$��80��$
��Left=�$��25��$��Bottom=����	#FF373737���	#FFEDEDED  CustomButtonStyle1#���  ]� $	��160��$	��150��$��Stretch=����	#FF212121$��	10,0,10,0q�.+��� $��Partial backup��$��Center=�$	��Top=����	#FFACACAC$��15(�$
��Bold"�$	��Top=�$��0,0,0,1q����	#FFABA8A8$��5,3,5,2q��� $��Size: 22 GB��$��Center=�$��Bottom=����	#FFACACAC$��14(�$
��Bold"�$	��Top=�$��0,35,0,0q��� $��Created: 2022.12.25��$��Center=�$��Bottom=����	#FFACACAC$��14(�$
��Bold"�$	��Top=��� $��Recover��$��80��$��Right=�$��25��$��Right=�$��Bottom=����	#FF373737���	#FFEDEDED�� $��Delete��$��80��$
��Left=�$��25��$��Bottom=����	#FF373737���	#FFEDEDED�� -    ��Name$ StoredBackupsBack_button��$
��Back��$��Right=�$��
0,18,700,0q�$	��Top=�$��80�����	#FFACACAC���	#FF474747$��2q�$��30��$
��Bold"�$��14(��� -   $#
 SelectedNumberOfBackups_label��$��Number of backups: 0�����	#FFACACAC$��14(�$
��Bold"�$��	455,1,0,0q�$
��Left=�$	��Top=��� -   $#
 SelectedNumberOfAllowed_label��$��Number of allowed: 0�����	#FFACACAC$��14(�$
��Bold"�$��
455,35,0,0q�$
��Left=�$	��Top=��� -   $$
 SelectedTotalBackupSpace_label��$��Total backup space: 0 GB�����	#FFACACAC$��14(�$
��Bold"�$��
222,18,0,0q�$
��Left=�$	��Top=��� -   $ DeleteAllBackup_button��$��Delete all backups��$��Right=�$��	0,18,10,0q�$	��Top=�$	��134�����	#FFACACAC���	#FF474747$��2q�$��30��$
��Bold"�$��14(�� -   $ BackupProgress_grid��$��Center=�$��Center=�$��0,0,0,0q�$	��790��$	��264��$y�Hidden=�.+�� -     �Name$  BackupProgress_progressbar��$��Center=�$��20��$��Center=�$	��300��$��0,0,0,0q��� -!   $$
 BackupProgressPercentage_label��$��Loading...
��$��Center=�$	��Top=����	#FFACACAC$��14(�$
��Bold"�$��0,87,0,0q�$��30��$	��300���� -"   $
 FileBackupSource_label��$��Source��$
��Left=�$	��Top=����	#FFACACAC$��14(�$
��Bold"�$��	10,34,0,0q�$	��200��$��Center=�$y�Hidden=��� -#   $!
 FileBackupDestination_label��$��Destination��$
��Left=�$	��Top=����	#FFACACAC$��14(�$
��Bold"�$��
570,34,0,0q�$	��200��$��Center=�$y�Hidden=��� -$   $" CancelBackupOperation_button��$��Cancel operation��$��Center=�$��0,0,0,10q�$��Bottom=�$	��130�����	#FFACACAC���	#FF474747$��2q�$��34��$
��Bold"�$��14(�$ 0.582,0.444&�$��Italic$�{�.  $	 0.5Q��� -%   $
 BackupOperation_label��$��Backup is in progress...��$��Center=�$	��Top=����	#FFACACAC$��16(�$
��Bold"�$��0,10,0,0q��� -&   $
 BackupProgressData_label��$�� ��$��Center=�$	��Top=����	#FFACACAC$��14(�$��Normal"�$��	0,147,0,0q�$��69��$	��300��� -'   $ BackupManaging_grid��$��Center=�$��Center=�$��0,0,0,0q�$	��790��$	��264��$y�Hidden=�.+��� -(   $
 Status_label��$)��#Status info: No items are selected!��$
��Left=�$��28,81,282,0q�$	��Top=����	#FFE29A06$��14(�$
��Bold"�$	��480���� -)   $
 Interval_label��$��Save interval: ��$
��Left=�$��
28,115,0,0q�$	��Top=����	#FFACACAC$��14(�$	��250��$
��Bold"��� -*   $
 OnlySaveOnChange_label��$&�� Only save if data is modified: -��$
��Left=�$��303,149,237,0q�$	��Top=����	#FFACACAC$��14(�{�.  $	 0.5Q�$
��Bold"�$	��250���� -+   $
 Lastsaved_label��$��Last saved:��$
��Left=�$��28,183,512,0q�$	��Top=����	#FFACACAC$��14(�{�.  $
��Bold"�$	��250���� -,   $
 Backupfilesize_label��$��Backup file size: ��$
��Left=�$��28,217,512,0q�$	��Top=����	#FFACACAC$��14(�{�.  $
��Bold"�$	��250���� --   $ DeleteTask_button��$��Delete task��$��Right=�$��0,5,22,0q�$	��Top=�$	��120�����	#FFACACAC���	#FF474747$��2q�$��34��$
��Bold"�$��14(�$ 0.582,0.444&�$��Italic$�{�.  $	 0.5Q��� -.   $ Enablebackup_button��$��Enable task��$��Right=�$��	0,58,22,0q�$	��Top=�$	��120�����	#FFACACAC���	#FF474747$��2q�$��34��$
��Bold"�$��14(�$	 0.5Q�{�.  $��Italic$��� -/   $ Disablebackup_button��$��Disable task��$��Right=�$��	0,58,22,0q�$	��Top=�$	��120�����	#FFACACAC���	#FF474747$��2q�$��34��$
��Bold"�$��14(�$��Italic$�$y�Hidden=��� -0   $ Modification_button��$��Modify task��$��Right=�$��
0,110,22,0q�$	��Top=�$	��120�����	#FFACACAC���	#FF474747$��2q�$��34��$��Italic$�$
��Bold"�$��14(�$	 0.5Q�{�.  �� -1   $ Restorefiles_button��$��Restore files��$��Right=�$��
0,160,22,0q�$	��Top=�$	��120�����	#FFACACAC���	#FF474747$��2q�$��34��$
��Bold"�$��14(�$��Italic$�$	 0.5Q�{�.  �� -2   $ Manualsave_button��$��Manual save��$��Right=�$��
0,212,22,0q�$	��Top=�$	��120�����	#FFACACAC���	#FF474747$��2q�$��34��$
��Bold"�$��14(�$��Italic$�$	 0.5Q�{�.  �� -3   $ Settings_image��$��Right=�$��24��$��0,115,147,0q�$	��Top=�$��24��$ 
2.44,0.576&�$"��Icons/icons8-settings-48.png��$y�Hidden=��� -4    ��Name$ ItemPath_textbox��$
��Left=�$��44��$��28,39,282,0q�$	��Top=�$	��480�����	#FF151515$��Select a source folder!�����	#FFE29A06$��Center=�$��5,0,0,0q�$
��Bold"� �� IsReadOnlyCaretVisible .  �� 
IsReadOnly . �� -5   $ ViewSelectedPath_button��$
��View��$
��Left=�$��
508,40,0,0q�$	��Top=�$��45�����	#FFACACAC���	#FF474747$��2q�$��43��$��14(�$	 0.5Q�{�.  $��Italic$��� -6   $
 NumberOfBackups_label��$ ��Current number of backups:��$
��Left=�$��28,149,512,0q�$	��Top=����	#FFACACAC$��14(�{�.  $
��Bold"�$	��250���� -7   $
 Smartsave_label_Copy1��$��File compression: -��$
��Left=�$��303,217,237,0q�$	��Top=����	#FFACACAC$��14(�{�.  $	 0.5Q�$
��Bold"�$	��250���� -8   $
 Method_label��$��Method: ��$
��Left=�$��303,115,0,0q�$	��Top=����	#FFACACAC$��14(�{�.  $
��Bold"�$	��250���� -9   $
 Smartsave_label_Copy3��$��Storage limit: -��$
��Left=�$��303,183,237,0q�$	��Top=����	#FFACACAC$��14(�{�.  $	 0.5Q�$
��Bold"�$	��250���� -:    ��Name$  ViewPathSelection_Combobox��$
��Left=�$��28,9,0,0q�$	��Top=�$	��120�� �� SelectedIndex$ 0��.@��� $��Destination���� $��Source��� -;   $ NoSelectedTask_grid��.+��� -<   $
 Interval_labl��$��No task is selected!��$��Center=�$��Center=����	#FFACACAC$��20(�$	��250��$��Center=�$	 0.5Q��� $��Backup drives/tasks:��$
��Left=�$��	30,17,0,0q�$	��Top=����	#FFACACAC$��14(�$
��Bold"��� -=    ��Name$ Backuptask_listbox��$
��Left=�$	��280��$��	30,51,0,0q�$	��Top=�$	��800�����	#FF151515$��5,0,0,0q����	#FFACACAC$
��Bold"� �� IsTextSearchEnabled .  $
��Auto=�|�.  �� ->   $ SaveAll_image��$��Right=�$��24��$��0,301,269,0q�$	��Top=�$��24��$ 
2.44,0.576&�$(��"Icons/icons8-save(orange-dark).png��$	 0.5Q�$y�Hidden=��� -?   $ SaveAll2_image��$��Right=�$��24��$��
0,301,35,0q�$	��Top=�$��24��$ 
2.44,0.576&�$(��"Icons/icons8-save(orange-dark).png��$	 0.5Q�$y�Hidden=��� -@   $ ManageBackupDrives_button��$��Manage backup drives��$
��Left=�$��
630,14,0,0q�$	��Top=�$	��200�����	#FFACACAC���	#FF474747$��2q�$��34��$
��Bold"�$��14(��� -A   $ temp_button_Copy��$��Backup settings��$��Right=�$��
0,247,64,0q�$	��Top=�$	��200�����	#FFACACAC���	#FF474747$��2q�$��34��$
��Bold"�$��14(�$	 0.5Q�{�.  $y�Hidden=��� -B   $ Manualsaveall_button��$��Manual save ALL��$��Right=�$��
0,296,64,0q�$	��Top=�$	��200�����	#FFACACAC���	#FF474747$��2q�$��34��$
��Bold"�$��14(�$	 0.5Q�{�.  $y�Hidden=�� -C   $ Backupsubmenu1_grid��$
��Left=�$	��670��$��	160,0,0,0q�$	��Top=�$	��865��$y�Hidden=�.+��� $��Settings��$
��Left=�$	��334��$��
21,111,0,0q�$	��Top=�$	��799�����	#FFACACAC$��1q����	#FFBBBBBB� -D   $" Backupsubmenu1_settings_grid��$
��Left=�$	��319��$	��Top=�$	��799��$��
21,126,0,0q�.+��� $��Method:��$
��Left=�$��	30,45,0,0q�$	��Top=����	#FFACACAC$��14(�$ 0.647,1.625&�	� -E    	�Name$" BackupMethodFull_radiobutton��$
��Full��$
��Left=�$��
107,28,0,0q�$	��Top=����	#FFACACAC$��14(�$ -0.177,-0.389&� d� 	IsChecked$
 TrueY�
������� 	� 	GroupName$ method��$��4,-3,0,0q�	� -F   $) #BackupMethodIncremental_radiobutton��$��Incremential��$
��Left=�$��
107,51,0,0q�$	��Top=����	#FFACACAC$��14(�$ -0.177,-0.389&�
�������$	 0.5Q�{�.  $ method��$��4,-3,0,0q�	� -G   $* $BackupMethodDifferential_radiobutton��$��Differential��$
��Left=�$��
107,74,0,0q�$	��Top=����	#FFACACAC$��14(�$ -0.177,-0.389&�
�������$	 0.5Q�{�.  $ method��$��4,-3,0,0q��� $��Buckup interval:��$
��Left=�$��
30,199,0,0q�$	��Top=����	#FFACACAC$��14(��� -H   $  Intervalselection_combobox��$
��Left=�$��217,203,0,0q�$	��Top=�$��66��  �� 
IsReadOnly  . 
���   �$��6,3,5,3q�$ 2��.@��� $��minute��
! � Tag$! minute���� $
��hour��$
! hour���� $	��day��$	! day���� -I   $
 	NoC_label��$��Number of cycles:��$
��Left=�$��
31,107,0,0q�$	��Top=����	#FFACACAC$��14(�$��5,5,0,5q�	� -J   $ 
temp_Copy2��$��Manual determination��$
��Left=�$��1000,60,0,0q�$	��Top=����	#FFACACAC$��14(�$ -0.177,-0.389&�
�������$ forget files��{�.  $
 0.25Q�	� -K   $ 
temp_Copy3��$!��Store them in 'temp' folder��$
��Left=�$��1000,83,0,0q�$	��Top=����	#FFACACAC$��14(�$ -0.177,-0.389&�$
 TrueY�
�������$ forget files��{�.  $
 0.25Q��� -L   " ��Name$" OnlySaveOnChange_checkbox��$3��-Only save when data is modified (recommended)��$
��Left=�$��
30,265,0,0q�$	��Top=����	#FFACACAC$��14(�$
 TrueY�{�.  $	 0.5Q�$��4,-3,0,0q��� -M   $" temp1_Copy1��$��Use backup size limit��$
��Left=�$��
430,37,0,0q�$	��Top=����	#FFACACAC$��14(�{�.  $	 0.5Q�$��4,-3,0,0q��� $
��Left=�$��23��$��
502,60,0,0q�# �� TextWrapping$
# Wrap=�$��0��$	��Top=�$��57�����	#FF151515���	#FFACACAC$��Right=�$��0,2,3,0q�{�.  $	 0.5Q��� $��MB��$
��Left=�$��
559,57,0,0q�$	��Top=����	#FFACACAC$��14(�$	 0.5Q��� $��Limit:��$
��Left=�$��
459,57,0,0q�$	��Top=����	#FFACACAC$��14(�$	 0.5Q��� -N   $" Compress_checkbox��$"��Compress backup files (.zip)��$
��Left=�$��430,265,0,0q�$	��Top=����	#FFACACAC$��14(�{�.  $	 0.5Q�$��4,-3,0,0q��� $��Action on failed attempt:��$
��Left=�$��430,107,0,0q�$	��Top=����	#FFACACAC$��14(�$	 0.5Q��� -O   $% Help_OnlySaveWhenModified_image��$
��Left=�$��20��$��357,263,0,0q�$	��Top=�$��20��$"��/Icons/Help icon mini IV.png��$	 0.5Q��� ���	#FFA0A0A0$��1��$��	0,10,0,20q�$��Center=��� -P   $" temp1_Copy2��$4��.Send popup message notification after last try��$
��Left=�$��459,196,0,0q�$	��Top=����	#FFACACAC$��14(�$��4,-3,0,0q�{�.  $	 0.5Q��� $(��"Wait time until the next attempt: ��$
��Left=�$��459,136,0,0q�$	��Top=����	#FFACACAC$��14(�$	 0.5Q��� -Q   $ Help_NumberOfCycles_image��$
��Left=�$��20��$��217,113,0,0q�$	��Top=�$��20��$"��/Icons/Help icon mini IV.png��$ ^� ShowDuration$$ 15000��% � ToolTip% _� .���� �� $Z�TThis determines how many full backups can be stored at once maximum. (0 = unlimited)���� �� $J�DIf the limit is reached, the oldest full backup will be overwritten.���� -R   $ NumberOfCycles_textbox��$
��Left=�$��22��$��152,110,0,0q�$
# Wrap=�$��1��$	��Top=�$��60�����	#FF151515���	#FFACACAC$��Center=�$��0,2,0,0q��� -S   $
 NoC_label_Copy��$��Cycle length:��$
��Left=�$��
31,152,0,0q�$	��Top=����	#FFACACAC$��14(�$��5,5,0,5q�$	 0.2Q��� $
��Left=�$��20��$��217,158,0,0q�$	��Top=�$��20��$"��/Icons/Help icon mini IV.png��$	 0.5Q��� $
��Left=�$��22��$��152,154,0,0q�$
# Wrap=�$��1��$	��Top=�$��60�����	#FF151515���	#FFACACAC$��Center=�{�.  $��0,2,0,0q�$	 0.2Q��� -T   $ BackupInterval_textbox��$
��Left=�$��22��$��152,203,0,0q�$
# Wrap=�$��1��$	��Top=�$��60�����	#FF151515���	#FFACACAC$��Center=�$��0,2,0,0q��� -U   $ BackupTaskLabel_textbox��$
��Left=�$��44��$��	21,44,0,0q�$	��Top=�$	��232�����	#FF151515���	#FFACACAC$��Center=�$��5,0,0,0q�$
��Bold"����	#FFAAAAAA�� $��Task label:��$
��Left=�$��	21,10,0,0q�$	��Top=����	#FFACACAC$��14(��� $
��Left=�$��20��$��	93,14,0,0q�$	��Top=�$��20��$"��/Icons/Help icon mini IV.png��$$ 15000��% _� .���� �� $Y�SThe task label is what will show up in the backptask list as the name of this task.���� �� $I�CThis will also be the name of the root directory of the backuptask.���� $*��$Source path (can be file or folder):��$
��Left=�$��
258,10,0,0q�$	��Top=����	#FFACACAC$��14(��� -V   $ Sourceinput_textbox��$
��Left=�$��44��$��
258,44,0,0q�$	��Top=�$	��530�����	#FF151515���	#FFACACAC$��Center=�$��5,0,0,0q�$
��Bold"����	#FFAAAAAA�� -W   $ Newitemapply_button��$��Apply��$��Right=�$��	0,0,45,35q�$��Bottom=�$��96��$��32��$
��Bold"�$��14(����	#FFACACAC$y�Hidden=��� -X   $ Modifyitemapply_button��$��Modify��$��Right=�$��	0,0,45,35q�$��Bottom=�$��96��$��32��$
��Bold"�$��14(����	#FFACACAC$y�Hidden=��� -Y   $! Backupsubmenu1cancel_button��$��Cancel��$��Right=�$��
0,0,170,35q�$��Bottom=�$��96��$��32��$
��Bold"�$��14(����	#FFACACAC�� -Z   $ Sourcepathselect_button��$	��...��$
��Left=�$��
788,44,0,0q�$	��Top=�$��32��$��44��$
��Bold"�$��14(����	#FFACACAC .  $��1,1,1,5q�	� -[   $ 
temp_Copy4��$��Several copies��$
��Left=�$��1038,348,-166,0q�$	��Top=����	#FFACACAC$��14(�$ -0.177,-0.389&�
�������$ Destination type��	� -\   $ 
temp_Copy5��$��Custom ��$
��Left=�$��1038,389,-129,0q�$	��Top=����	#FFACACAC$��14(�$ -0.177,-0.389&�
�������$ Destination type���� $ ��Destination path (folder):��$
��Left=�$��258,471,0,0q�$	��Top=����	#FFACACAC$��14(��� -]   $ Destinationinput_textbox��$
��Left=�$��44��$��258,505,0,0q�$	��Top=�$	��530�����	#FF151515���	#FFACACAC$��Center=�$��5,0,0,0q�$
��Bold"����	#FFAAAAAA�� -^   $" Destinationpathselect_button��$	��...��$
��Left=�$��788,504.334,0,0q�$	��Top=�$��32��$��44��$
��Bold"�$��14(����	#FFACACAC .  $��1,1,1,5q��� $��Destination drive:��$
��Left=�$��
21,471,0,0q�$	��Top=����	#FFACACAC$��14(�$��5,5,0,5q��� $
��Left=�$��20��$��139,477,0,0q�$	��Top=�$��20��$"��/Icons/Help icon mini IV.png��$$ 15000��% _� .���� �� $C�=Choose which drive this backuptask will store its backups on.���� �� $`�Z(If a drive is not listed below, then try to enable it in the 'Manage backup drives' menu)���� $.��(Available free space on selected drive: ��$
��Left=�$��
21,574,0,0q�$	��Top=����	#FFACACAC$��14(�$��5,5,0,5q�$��Normal"��� $-��'Available allocated space for backups: ��$
��Left=�$��
21,608,0,0q�$	��Top=����	#FFACACAC$��14(�$��5,5,0,5q�$��Normal"��� -_   $ 
 AvailableFreeSpaceBD_label��$��-��$
��Left=�$��269,574,0,0q�$	��Top=����	#FFACACAC$��14(�$��5,5,0,5q�$
��Bold"��� -`   $#
 AvailableAllocatedSpace_label��$��-��$
��Left=�$��268,608,0,0q�$	��Top=����	#FFACACAC$��14(�$��5,5,0,5q�$
��Bold"��� -a   $  Backupdriveselect_combobox��$
��Left=�$��
21,514,0,0q�$	��Top=�$	��200��  . 
���   �$��30��$ 0��$��6,6,5,3q�.@��� $��Select a backup drive!���� -b   $" BrowseForFile_checkbox��$��Browse for file��$
��Left=�$��
712,16,0,0q�$	��Top=����	#FFACACAC$��14(�$��4,-3,0,0q�� -c   $ Backupsubmenu2_grid��$
��Left=�$	��669��$��	160,1,0,0q�$	��Top=�$	��865��$y�Hidden=�.+��� -d   $ Alldrives_scrollviewer��$
��Left=�$	��494��$��	33,70,0,0q�$	��Top=�$	��780�����	#FF151515$	��0,5q�$y�Visible=��� -e   $" Showunavailable_checkbox��$�� ��$
��Left=�$��
631,36,0,0q�$	��Top=�$��29��$��Center=�$��Center=�$��17��$ FalseY��� $��Detected drives:��$
��Left=�$��	33,36,0,0q�$	��Top=����	#FFACACAC$��14(�$
��Bold"��� $��Show unavailable:��$
��Left=�$��
502,36,0,0q�$	��Top=����	#FFACACAC$��14(�$
��Bold"��� -f   $! Backupsubmenu2cancel_button��$
��Back��$��Right=�$��	0,0,69,35q�$��Bottom=�$��96��$��32��$
��Bold"�$��14(����	#FFACACAC�� $��Drive information ��$
��Left=�$��
154,70,0,0q�$	��Top=����	#FFACACAC$��14(�$��Italic$����	#FF151515$	��241��$��Center=����	#FFACACAC$	��1,0q��� $��Set space limit��$
��Left=�$��
395,70,0,0q�$	��Top=����	#FFACACAC$��14(�$��Italic$����	#FF151515$	��192��$��Center=����	#FFACACAC$	��1,0q��� $#��Drive avtivation/deactivation��$
��Left=�$��
587,70,0,0q�$	��Top=����	#FFACACAC$��14(�$��Italic$����	#FF151515$	��209��$��Center=����	#FFACACAC$��1,0,0,0q��� $��
Drive icon��$
��Left=�$��	33,70,0,0q�$	��Top=����	#FFACACAC$��14(�$��Italic$����	#FF151515$	��121��$��Center=����	#FFACACAC$��0,0,1,0q�� -g   $ Fileexplorer_grid��$
��Left=�$	��670��$��	160,0,0,0q�$	��Top=�$	��865��$y�Hidden=�.+��� $��Currently selected path:��$
��Left=�$��	20,10,0,0q�$	��Top=����	#FFACACAC$��14(��� -h   $ Path_textbox��$
��Left=�$��44��$��	20,44,0,0q�$
# Wrap=�$	��Top=�$	��768�����	#FF151515���	#FFACACAC$��Center=�$��5,0,0,0q�$
��Bold"����	#FFAAAAAA���� *file+& �� Mode$& OneWay=�&   System.Windows.Controls.DataGrid  -i   $��FileExplorer2_datagrid��'   
IsReadOnly' . (   AutoGenerateColumns( .  $��20,113,77,138q����	#FF151515���	#FFAAAAAA)   Columns) 0  *System.Windows.Controls.DataGridTextColumn  ,  &System.Windows.Controls.DataGridColumn*   Header$
* Name��1  +System.Windows.Controls.DataGridBoundColumn+   Binding+ �� *Files+$& OneWay=�  $
* Size��+ �� *FileExplorer+  $
* Type��+ �� *FileExplorer+  $* Changed��+ �� *FileExplorer+  -j   $��FileExplorer_datagrid��$��20,113,77,138q�( .  ���	#FF151515
���   �$��1q�,   HorizontalGridLinesBrush
, �   �-   VerticalGridLinesBrush
- �   �.   GridLinesVisibility$. 
Horizontal=�/   HeadersVisibility$/ Column=�0   ColumnHeaderHeight$0 25Q�1   	RowHeight$1 20Q�2   RowBackground2 �	#FF464646��Z� 3 � 	Resources3 =  7System.Windows.Controls.Primitives.DataGridColumnHeader4 �� BasedOn5 �� 
TargetType6 �� Property7 �� Value��%>   '        7 �� 84   "5  .��� 6 � ��7 �	#FFAAAAAA8   RowStyle8 �� )  #System.Windows.Controls.DataGridRow"5  .��� 6 � ��7 �	#FF151515�� 6 � ��
7 �����9 �� Triggers9 P� : P� Property: �@J�IsMouseOver; P� Value; . . ��� 6 � ��7 �	#FF343434)   $
* Name��<   MinWidth$	< 250Q�+ �� *Name+  $
* Size��$< 80Q�+ �� *Size+  $
* Type��$	< 100Q�+ �� *Type+  $* Changed��$	< 150Q�+ �� *Name+� -k   $ 
Debug_grid��$
��Left=�$	��670��$��	160,0,0,0q�$	��Top=�$	��865��$y�Hidden=�.+��� -l   $ Debugwindow_textbox��$
��Left=�$	��587��$��	33,38,0,0q�$
# Wrap=�$	��Top=�$	��785�����	#FF141414���	#FF23FF00$
��Bold"�3 = �� 
TargetType> �� Padding? �� Triggers@ �� 
TargetName  CustomButtonStyle2A �� CornerRadiusB �� EndPointC �� 
StartPointD � ColorE � Offset��%�  &	       &	 $    &	  :    �� "5 ��.��� 6 �@�OverridesDefaultStyle7 . �� 6 � ��7 �� "= ��.R��� $��border��$��1q�$	> 4,2q�
�������#��z��._��� $��Center=�$��Center=�? P� : �@J�IsMouseOver; . . ��� $@ border��6 � ��
7 ������� $@ border��6 � ��7 �	#FF4B4B4B�� "5 ��.��� 6 �@�OverridesDefaultStyle7 . �� 6 � ��$7 5q��� 6 � ��7 �� "= ��.R��� $��border��$��1q�$	> 4,2q�
�������$A 3��#��z��._��� $��Center=�$��Center=�? P� : �@J�IsMouseOver; . . ��� $@ border��6 � ��
7 �   ��� $B 0.5,1&�$C 0.5,0&�.��� $D 	#FF05FA05��� $D 	#FF056405��$E 1Q�