// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using GMTool.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Path = System.IO.Path;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GMTool.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ExcelLockMgr : Page
    {
        private readonly Regex _regexFileName = new("(?<=Name: )(.+)");
        private readonly Regex _regexLockOwner = new("(?<=Lock Owner: )(.+)");
        private readonly Regex _regexLockComment = new("(?<=Lock Comment)(.|\r|\n)+");

        private readonly string _excelPath;

        private readonly Dictionary<string, LockFileInfo> _fileDict = new();
        private readonly ObservableCollection<ExcelFileInfo> _fileList = new();
        private readonly Dictionary<string, bool> _starredList = new();
        private readonly Dictionary<string, int> _fileListIndex = new();

        private Visibility UnlockVisibility;
        private Visibility LockVisibility;
        private Visibility UnlockAllVisibility;
        private Visibility LockAllVisibility;

        public static List<string> StarredList;

        public ExcelLockMgr()
        {
            this.InitializeComponent();
            lvFileList.ItemsSource = _fileList;
            _excelPath = Path.Combine(CommonData.RepoRootPath, "Excels\\Dev");
            Init();
        }

        private async void Init()
        {
            LoadUsername();
            await LoadStarredExcel();
            await LoadFileLockInfo();
            LoadExcelFileList();
        }

        private void LoadUsername()
        {
            tbUsername.Text = string.IsNullOrEmpty(CommonData.AccountName) ? "当前用户：（空）" : $"当前用户：{CommonData.AccountName}";
        }

        private async Task LoadStarredExcel()
        {
            StarredList = await CommonData.LoadStarredExcel() ?? new();
            if (StarredList == null)
            {
                StarredList = new();
            }
            else
            {
                foreach (string fileName in StarredList)
                {
                    _starredList.Add(fileName, true);
                }
            }
        }

        private void LoadExcelFileList()
        {
            _fileList.Clear();
            List<ExcelFileInfo> fileList = new List<ExcelFileInfo>();
            DirectoryInfo dir = new DirectoryInfo(_excelPath);
            if (dir.Exists)
            {
                FileInfo[] fileInfos = dir.GetFiles("*.xlsx");
                foreach (FileInfo fileInfo in fileInfos)
                {
                    if (fileInfo.Name.StartsWith('~')) continue;
                    if (!_starredList.TryGetValue(fileInfo.Name, out bool starred))
                    {
                        _starredList[fileInfo.Name] = starred;
                    }
                    ExcelFileInfo excelFileInfo = new ExcelFileInfo(fileInfo.Name, fileInfo.FullName, starred);
                    if (_fileDict.TryGetValue(fileInfo.Name, out LockFileInfo lockFileInfo))
                    {
                        excelFileInfo.SetLockInfo(CommonData.AccountName, lockFileInfo);
                    }
                    fileList.Add(excelFileInfo);
                }
            }
            fileList.Sort();

            for (int index = 0; index < fileList.Count; index++)
            {
                ExcelFileInfo info = fileList[index];
                _fileListIndex[info.FileName] = index;
                _fileList.Add(info);
            }
        }

        private void ReOrder(string fileName)
        {
            if (!_fileListIndex.TryGetValue(fileName, out int oldIndex)) return;
            List<ExcelFileInfo> fileList = _fileList.ToList();
            fileList.Sort();
            for (int index = 0; index < fileList.Count; index++)
            {
                ExcelFileInfo info = fileList[index];
                _fileListIndex[info.FileName] = index;
            }
            if (!_fileListIndex.TryGetValue(fileName, out int newIndex)) return;
            _fileList.Move(oldIndex, newIndex);
            lvFileList.SelectedIndex = newIndex;
            lvFileList.ScrollIntoView(_fileList[newIndex], ScrollIntoViewAlignment.Default);
        }

        private async Task LoadFileLockInfo()
        {
            _fileDict.Clear();
            (bool success, string content) = await GetSvnCommandOutput("info -R http://192.168.1.67/svn/GrandMaisonHRG/trunk/Excels/Dev");
            if (!success || string.IsNullOrEmpty(content)) return;
            foreach (string chunk in content.Split("\r\n\r\n"))
            {
                if (string.IsNullOrEmpty(chunk)) continue;
                Match matchName = _regexFileName.Match(chunk);
                if (!matchName.Success) continue;
                Match matchOwner = _regexLockOwner.Match(chunk);
                if (!matchOwner.Success) continue;
                string name = matchName.Value.TrimEnd('\r', '\n', ' ');
                string owner = matchOwner.Value.TrimEnd('\r', '\n', ' ');

                Match matchComment = _regexLockComment.Match(chunk);
                string result = matchComment.Value.TrimEnd('\r', '\n', ' ');
                string comment = matchComment.Success ? result.Split("\r\n")[^1] : null;
                _fileDict.TryAdd(name, new LockFileInfo(name, owner, comment));
            }
        }

        public static async Task<(bool, string)> GetSvnCommandOutput(string args)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo("svn", args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            Process process = Process.Start(processInfo);
            if (process != null)
            {
                process.EnableRaisingEvents = true;
                process.ErrorDataReceived += delegate
                {
                    process.Kill();
                    process.Close();
                };

                string content = await process.StandardOutput.ReadToEndAsync();
                content = content.TrimEnd('\r', '\n', ' ');

                process.Kill();
                process.Close();
                return (true, content);
            }
            else
            {
                return (false, string.Empty);
            }
        }

        private async void BtUpdate_OnClick(object sender, RoutedEventArgs e)
        {
            foSvnUpdate.Hide();
            string path1 = Path.Combine(CommonData.RepoRootPath, "Excels");
            string path2 = Path.Combine(CommonData.RepoRootPath, "Resources/LogicData");
            string path3 = Path.Combine(CommonData.RepoRootPath, "Resources/Toml");
            string path4 = Path.Combine(CommonData.RepoRootPath, "Resources/LuaLib/cfg_mgr.lua");
            string path5 = Path.Combine(CommonData.RepoRootPath, "Resources/Lua/json_data/json_configs.lua");
            string args = $"/command:update /path:{path1}*{path2}*{path3}*{path4}*{path5}";
            ProcessStartInfo processInfo = new ProcessStartInfo("TortoiseProc.exe", args);
            Process process = Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
            }
            BtRefresh_OnClick(null, null);
        }

        private async void BtRefresh_OnClick(object sender, RoutedEventArgs e)
        {
            await LoadFileLockInfo();
            LoadExcelFileList();
        }


        private void BtHelp_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is HyperlinkButton button)
            {
                tipHelp.Target = button;
                tipHelp.IsOpen = true;
            }
        }

        private void TipHelp_OnActionButtonClick(TeachingTip sender, object args)
        {
            tipHelp.IsOpen = false;
        }

        private void ButtonStarred_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is HyperlinkButton {DataContext: ExcelFileInfo info})
            {
                _starredList[info.FileName] = false;
                StarredList.Remove(info.FileName);
                info.SetStarred(false);
                ReOrder(info.FileName);
            }
        }

        private void ButtonUnStarred_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is HyperlinkButton { DataContext: ExcelFileInfo info })
            {
                _starredList[info.FileName] = true;
                if (!StarredList.Contains(info.FileName))
                {
                    StarredList.Add(info.FileName);
                }
                info.SetStarred(true);
                ReOrder(info.FileName);
            }
        }

        private void ListViewItem_OnUpdateClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem { DataContext: ExcelFileInfo info })
            {
                string args = $"/command:update /path:{info.FullPath}";
                ProcessStartInfo processInfo = new ProcessStartInfo("TortoiseProc.exe", args);
                Process.Start(processInfo);
            }
        }

        private void ListViewItem_OnShowLogClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem {DataContext: ExcelFileInfo info})
            {
                string args = $"/command:log /path:{info.FullPath}";
                ProcessStartInfo processInfo = new ProcessStartInfo("TortoiseProc.exe", args);
                Process.Start(processInfo);
            }
        }

        private void ListViewItem_OnDoubleClick(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is TextBlock { DataContext: ExcelFileInfo info })
            {
                string args = $"/command:log /path:{info.FullPath}";
                ProcessStartInfo processInfo = new ProcessStartInfo("TortoiseProc.exe", args);
                Process.Start(processInfo);
            }
        }

        private void ListViewItemFlyout_OnOpened(object sender, object e)
        {
            MenuFlyout flyout = sender as MenuFlyout;
            if (flyout == null) return;
            if (flyout is { Target: ListViewItem {DataContext: ExcelFileInfo info}})
            {
                if (lvFileList.SelectedItem == null ||
                    (lvFileList.SelectedItems.Count == 1 && lvFileList.SelectedItem != info))
                {
                    lvFileList.SelectedItem = info;
                }
            }

            int canUnlockCount = 0;
            int canLockCount = 0;
            foreach (ExcelFileInfo item in lvFileList.SelectedItems)
            {
                if (item.LockedBySelf)
                {
                    canUnlockCount++;
                }

                if (!item.LockedBySelf && !item.LockedByOthers)
                {
                    canLockCount++;
                }
            }

            flyout.Items[0].Visibility = lvFileList.SelectedItems.Count == 1 ? Visibility.Visible : Visibility.Collapsed;

            flyout.Items[1].Visibility = canLockCount == 1 ? Visibility.Visible : Visibility.Collapsed;
            flyout.Items[2].Visibility = canUnlockCount == 1 ? Visibility.Visible : Visibility.Collapsed;
            flyout.Items[3].Visibility = canLockCount > 1 ? Visibility.Visible : Visibility.Collapsed;
            flyout.Items[4].Visibility = canUnlockCount > 1 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtGenerate_OnClick(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo process = new ProcessStartInfo(Path.Combine(CommonData.RepoRootPath, "Tools/AutoGen/gen_cfg_client.bat"))
            {
                WorkingDirectory = Path.Combine(CommonData.RepoRootPath, "Tools/AutoGen"),
            };
            Process.Start(process);
        }

        private async void BtCommit_OnClick(object sender, RoutedEventArgs e)
        {
            string path1 = Path.Combine(CommonData.RepoRootPath, "Excels");
            string path2 = Path.Combine(CommonData.RepoRootPath, "Resources/LogicData");
            string path3 = Path.Combine(CommonData.RepoRootPath, "Resources/Toml");
            string path4 = Path.Combine(CommonData.RepoRootPath, "Resources/LuaLib/cfg_mgr.lua");
            string path5 = Path.Combine(CommonData.RepoRootPath, "Resources/Lua/json_data/json_configs.lua");
            string args = $"/command:commit /path:{path1}*{path2}*{path3}*{path4}*{path5}";
            ProcessStartInfo processInfo = new ProcessStartInfo("TortoiseProc.exe", args);
            Process process = Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
            }
            BtRefresh_OnClick(null, null);
        }

        private void ListViewItem_OnLockClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: ExcelFileInfo info })
            {
                ShowFileDialog("lock", info.FileName);
            }
        }

        private void ListViewItem_OnUnlockClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: ExcelFileInfo info })
            {
                ShowFileDialog("unlock", info.FileName);
            }
        }

        private void ListViewItem_OnLockAllClick(object sender, RoutedEventArgs e)
        {
            List<string> fileName = new List<string>();
            foreach (ExcelFileInfo item in lvFileList.SelectedItems)
            {
                if (!item.LockedByOthers && !item.LockedBySelf)
                {
                    fileName.Add($"\"{Path.Combine(_excelPath, item.FileName)}\"");
                }
            }
            ShowAllFileDialog("lock", fileName);
        }

        private void ListViewItem_OnUnlockAllClick(object sender, RoutedEventArgs e)
        {
            List<string> fileName = new List<string>();
            foreach (ExcelFileInfo item in lvFileList.SelectedItems)
            {
                if (item.LockedBySelf)
                {
                    fileName.Add($"\"{Path.Combine(_excelPath, item.FileName)}\"");
                }
            }
            ShowAllFileDialog("unlock", fileName);
        }

        private async void ShowFileDialog(string operation, string fileName, string comment = null)
        {
            cdSetLock.XamlRoot = XamlRoot;
            if (operation == "lock")
            {
                cdSetLock.Title = $"锁定表格 {fileName}";
                cdSetLock.PrimaryButtonText = "锁定";
                tbComment.Visibility = Visibility.Visible;
            }
            else
            {
                cdSetLock.Title = $"解锁表格 {fileName}";
                cdSetLock.PrimaryButtonText = "解锁";
                tbComment.Visibility = Visibility.Collapsed;
            }
            tbComment.Text = comment ?? string.Empty;
            ContentDialogResult result = await cdSetLock.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                LockFile(operation, $"\"{Path.Combine(_excelPath, fileName)}\"");
            }
        }

        private async void ShowAllFileDialog(string operation, List<string> fileNames, string comment = null)
        {
            cdSetLock.XamlRoot = XamlRoot;
            if (operation == "lock")
            {
                cdSetLock.Title = $"锁定表格（已选择{fileNames.Count}个表格）";
                cdSetLock.PrimaryButtonText = "全部锁定";
                tbComment.Visibility = Visibility.Visible;
            }
            else
            {
                cdSetLock.Title = $"解锁表格（已选择{fileNames.Count}个表格）";
                cdSetLock.PrimaryButtonText = "全部解锁";
                tbComment.Visibility = Visibility.Collapsed;
            }
            tbComment.Text = comment ?? string.Empty;
            ContentDialogResult result = await cdSetLock.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                LockFile(operation, string.Join(' ', fileNames));
            }
        }

        private async void LockFile(string operation, string args)
        {
            string command = $"{operation} {args}";
            if (operation == "lock" && !string.IsNullOrEmpty(tbComment.Text))
            {
                command = $"{command} -m \"{tbComment.Text}\"";
            }
            (bool success, string content) = await GetSvnCommandOutput(command);
            if (success)
            {
                BtRefresh_OnClick(null, null);
            }
        }

        private async void ButtonOpenExcel_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is HyperlinkButton {DataContext: ExcelFileInfo info})
            {
                try
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(info.FullPath);
                    if (file != null)
                    {
                        await Launcher.LaunchFileAsync(file);
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        private void ButtonShowLog_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is HyperlinkButton { DataContext: ExcelFileInfo info })
            {
                string args = $"/command:log /path:{info.FullPath}";
                ProcessStartInfo processInfo = new ProcessStartInfo("TortoiseProc.exe", args);
                Process.Start(processInfo);
            }
        }

        private Visibility CanUnlockAll()
        {
            int lockedCount = 0;
            foreach (ExcelFileInfo item in lvFileList.SelectedItems)
            {
                if (item.LockedBySelf)
                {
                    lockedCount++;
                }
            }
            return lockedCount > 1 ? Visibility.Visible : Visibility.Collapsed;
        }

        private Visibility CanLockAll()
        {
            int unlockedCount = 0;
            foreach (ExcelFileInfo item in lvFileList.SelectedItems)
            {
                if (!item.LockedByOthers && !item.LockedBySelf)
                {
                    unlockedCount++;
                }
            }
            return unlockedCount > 1 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
