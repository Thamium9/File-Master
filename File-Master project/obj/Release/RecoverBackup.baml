   M S B A M L   `   `   `  ���� F  BFile-Master, Version=0.8.4.0, Culture=neutral, PublicKeyToken=null9!clr-namespace:File_Master_projectFile_Master_project  '    !File_Master_project.RecoverBackup   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     3local!clr-namespace:File_Master_project     -     �Name$  BackupRecovery_window�� � Title$ RecoverBackup��$	��250��$	��600�����	#FF0A0A0A � WindowStyle$
 None=� � 
ResizeMode$ NoResize=� � WindowStartupLocation$ CenterScreen=�$��1,1,1,1q����	#FF969696.��� $��0,0,0,0q�.+�� ���	#FF0A0A0A$��	0,0,0,220q�.+��� $��Backup Recovery��$
��Left=�$��Center=����	#FFACACAC$
��Bold"�$��16(�$��5,2,5,0q�$��28��� ���	#FF151515$��0,30,0,0q�.+��� -    ��Name$ RecoveryPath_textbox��$
��Left=�$��44��$��10,0,0,0q�$��Center=�$	��520�����	#FF0A0A0A���	#FFACACAC$��Center=�$��5,0,0,0q�$
��Bold"��� $8��2Select the recovery location for the backup files!��$
��Left=�$��	10,10,0,0q�$	��Top=����	#FFACACAC$��14(��� -    ��Name$ Recover_button��$��Recover data��$��Right=�$��
0,176,10,0q�$	��Top=�$	��120�����	#FFACACAC���	#FF474747$��2q�$��34��$
��Bold"�$��14(�$��Italic$� J� Opacity$	 0.5Q�{�.  �� -   $ CancelRecovery_button��$��Cancel��$��Right=�$��0,176,151,0q�$	��Top=�$	��120�����	#FFACACAC���	#FF474747$��2q�$��34��$
��Bold"�$��14(�$��Italic$��� -   $# BrowseRecoveryLoaction_button��$��Browse��$
��Left=�$��	530,0,0,0q�$��Center=�$��60�����	#FFACACAC���	#FF474747$��2q�$��44��$
��Bold"�$��14(�$��Italic$��� $I��CBy default it will be the original location of the backed up files.��$
��Left=�$��	10,39,0,0q�$	��Top=����	#FFACACAC$��14(��� -    ��Name$ RecoveryLocError_label��$3��-The specified target location doesn't exists!��$
��Left=�$��
10,131,0,0q�$	��Top=����	#FFDC0000$��14(�