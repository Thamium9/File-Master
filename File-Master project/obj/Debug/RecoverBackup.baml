   M S B A M L   `   `   `  ����F  BFile-Master, Version=0.8.5.0, Culture=neutral, PublicKeyToken=null9!clr-namespace:File_Master_projectFile_Master_project  5   	   '    !File_Master_project.RecoverBackup   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   3local!clr-namespace:File_Master_project     5   	   -   5          �Name$  BackupRecovery_window��5   	    � Title$ RecoverBackup��5   	   $	��250��6   $	��600��6,   ���	#FF0A0A0A � WindowStyle$
 None=�6O    � 
ResizeMode$ NoResize=�6b    � WindowStartupLocation$ CenterScreen=�6x   $��1,1,1,1q�6�   ���	#FF969696.��5	      � $��0,0,0,0q�6   .+�5
   
   � ���	#FF0A0A0A$��	0,0,0,220q�6&   .+�5      �� $��Backup Recovery��6   $
��Left=�6.   $��Center=�6I   ���	#FFACACAC$
��Bold"�6{   $��16(�6�   $��5,2,5,0q�6�   $��28��6�   5      � 5   
   ���	#FF151515$��0,30,0,0q�6&   .+�5      �� -   5         ��Name$ RecoveryPath_textbox��5      $
��Left=�64   $��44��6O   $��10,0,0,0q�6[   $��Center=�6m   $	��520��6�   ���	#FF0A0A0A���	#FFACACAC$��Center=�6�   $��5,0,0,0q�6�   $
��Bold"�6�   6  �� 5      $8��2Select the recovery location for the backup files!��6   $
��Left=�6Q   $��	10,10,0,0q�6l   $	��Top=�6   ���	#FFACACAC$��14(�6�   �� 5      -   5         ��Name$ Recover_button��5      $��Recover data��6-   $��Right=�6D   $��
0,176,10,0q�6`   $	��Top=�6t   $	��120��6�   ���	#FFACACAC���	#FF474747$��2q�6�   $��34��6�   $
��Bold"�6�   $��14(�6�   $��Italic$�6   J� Opacity$	 0.5Q�6  {�.  6E  �� 5      -   5        $ CancelRecovery_button��5      $��Cancel��64   $��Right=�6E   $��0,176,151,0q�6a   $	��Top=�6v   $	��120��6�   ���	#FFACACAC���	#FF474747$��2q�6�   $��34��6�   $
��Bold"�6�   $��14(�6�   $��Italic$�6	  6  �� 5      -   5        $# BrowseRecoveryLoaction_button��5      $��Browse��6<   $
��Left=�6M   $��	530,0,0,0q�6h   $��Center=�6{   $��60��6�   ���	#FFACACAC���	#FF474747$��2q�6�   $��44��6�   $
��Bold"�6�   $��14(�6  $��Italic$�6  6#  �� 5      $I��CBy default it will be the original location of the backed up files.��6   $
��Left=�6b   $��	10,39,0,0q�6}   $	��Top=�6�   ���	#FFACACAC$��14(�6�   �� 5      -   5         ��Name$ RecoveryLocError_label��5      $3��-The specified target location doesn't exists!��64   $
��Left=�6l   $��
10,131,0,0q�6�   $	��Top=�6�   ���	#FFDC0000$��14(�6�   5      5      5      