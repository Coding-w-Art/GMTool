// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using GMTool.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage;
using UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;
using Windows.ApplicationModel.DataTransfer;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GMTool
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DiffViewer : Page
    {
        private readonly string _trunkRepoPath = "http://192.168.1.67/svn/GrandMaisonHRG/trunk/";
        private readonly string _branchRepoPath = "http://192.168.1.67/svn/GrandMaisonHRG/branches/";
        private readonly string _tagsRepoPath = "http://192.168.1.67/svn/GrandMaisonHRG/tags/";
        private readonly Regex _revisionRegex = new("[0-9]+");


        public ObservableCollection<string> RepoPathList = new ObservableCollection<string>();

        public ObservableCollection<DiffFileItem> DiffFileList = new ObservableCollection<DiffFileItem>();

        private int _revFrom = 0;
        private int _revTo = 0;
        private string _curRepoPath;
        private string _savePath;
        private string _rawData;

        public DiffViewer()
        {
            this.InitializeComponent();
            LoadRepoPathList();
        }

        private async void LoadRepoPathList()
        {
            RepoPathList.Add(_trunkRepoPath);
            cbRepoUrl.SelectedIndex = 0;
            await LoadRepoPath(_branchRepoPath);
            await LoadRepoPath(_tagsRepoPath);
        }

        private async Task LoadRepoPath(string path)
        {
            string args = $"list {path}";
            ProcessStartInfo processInfo = new ProcessStartInfo("svn", args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };
            
            Process process = Process.Start(processInfo);
            if (process == null) return;

            process.EnableRaisingEvents = true;
            process.ErrorDataReceived += delegate
            {
                process.Kill();
                process.Close();
            };

            string content = await process.StandardOutput.ReadToEndAsync();
            if (!string.IsNullOrEmpty(content))
            {
                string[] lines = content.Split("\r\n");
                foreach (string line in lines)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        RepoPathList.Add($"{path}{line}");
                    }
                }
            }
            process.Kill();
            process.Close();
        }

        private async void ButtonView_OnClick(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(tbRevFrom.Text, out int revFrom)) return;
            if (!int.TryParse(tbRevTo.Text, out int revTo)) return;
            _revFrom = Math.Min(revFrom, revTo);
            _revTo = Math.Max(revFrom, revTo);
            tbRevFrom.Text = _revFrom.ToString();
            tbRevTo.Text = _revTo.ToString();

            try
            {
                string repoUrl = RepoPathList[cbRepoUrl.SelectedIndex];
                string args = _revTo == 0
                    ? $"diff {repoUrl} -r {_revFrom} --summarize"
                    : $"diff {repoUrl} -r {_revFrom}:{_revTo} --summarize";
                ProcessStartInfo processInfo = new ProcessStartInfo("svn", args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                Process process = Process.Start(processInfo);
                if (process == null) return;

                process.EnableRaisingEvents = true;
                process.ErrorDataReceived += delegate
                {
                    process.Kill();
                    process.Close();
                };

                _rawData = string.Empty;
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"URL: {repoUrl}");
                sb.AppendLine($"Revision: {_revFrom} - {_revTo}");
                sb.AppendLine("==================================================");

                DiffFileList.Clear();
                int index = 0;
                while (!process.StandardOutput.EndOfStream)
                {
                    string line = await process.StandardOutput.ReadLineAsync();
                    if (!string.IsNullOrEmpty(line))
                    {
                        sb.AppendLine(line);
                        line = line.Replace(repoUrl, "").Trim();
                        DiffFileItem item = new DiffFileItem(index, line);
                        DiffFileList.Add(item);
                        index++;
                    }
                }
                process.Kill();
                process.Close();
                _rawData = sb.ToString();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void ButtonBlame_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && int.TryParse(button.Tag.ToString(), out int index))
            {
                DiffFileItem item = DiffFileList[index];
                if (item == null) return;
                StartSvnCommand("blame", item.FilePath);
            }
        }

        private void ButtonDiff_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && int.TryParse(button.Tag.ToString(), out int index))
            {
                DiffFileItem item = DiffFileList[index];
                if (item == null) return;
                StartSvnCommand("diff", item.FilePath);
            }
        }

        private void ButtonShowLog_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && int.TryParse(button.Tag.ToString(), out int index))
            {
                DiffFileItem item = DiffFileList[index];
                if (item == null) return;
                StartSvnCommand("log", item.FilePath);
            }
        }

        private void StartSvnCommand(string command, string filePath)
        {
            string startRev = $"/endrev:{_revTo}";
            string endRev = $"/startrev:{_revFrom}";
            string args = $"/command:{command} /path:{_curRepoPath}{filePath} {startRev} {endRev}";
            ProcessStartInfo processInfo = new ProcessStartInfo("TortoiseProc.exe", args);
            Process.Start(processInfo);
        }

        private void CbRepoUrl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _curRepoPath = RepoPathList[cbRepoUrl.SelectedIndex];
        }

        private void FileName_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is TextBlock textBlock && int.TryParse(textBlock.Tag.ToString(), out int index))
            {
                DiffFileItem item = DiffFileList[index];
                if (item == null) return;
                StartSvnCommand("diff", item.FilePath);
            }
        }

        private async void ButtonRevFrom_OnClick(object sender, RoutedEventArgs e)
        {
            string version = await SelectVersion();
            if (version == null) return;
            tbRevFrom.Text = version;
        }

        private async void ButtonRevTo_OnClick(object sender, RoutedEventArgs e)
        {
            string version = await SelectVersion();
            if (version == null) return;
            tbRevTo.Text = version;
        }

        private async Task<string> SelectVersion()
        {
            string tmpPath = Path.GetTempPath();
            string fileName = Path.Combine(tmpPath, "DiffViewerRevision");
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            fileName = fileName.Replace("\\", "/");
            ProcessStartInfo processInfo = new ProcessStartInfo("TortoiseProc.exe", $"/command:log /path:{_curRepoPath} /outfile:\"{fileName}\"");
            Process process = Process.Start(processInfo);
            if (process == null) return null;
            await process.WaitForExitAsync();
            if (File.Exists(fileName))
            {
                string revisions = await File.ReadAllTextAsync(fileName);
                Match match = _revisionRegex.Match(revisions);
                if (match.Success)
                {
                    return match.Value;
                }
            }
            return null;
        }

        private void ButtonClear_OnClick(object sender, RoutedEventArgs e)
        {
            DiffFileList.Clear();
        }

        private async void ButtonExport_OnClick(object sender, RoutedEventArgs e)
        {
            if (DiffFileList.Count == 0) return;
            try
            {
                if (string.IsNullOrEmpty(_rawData)) return;
                if (string.IsNullOrEmpty(_savePath) || Directory.Exists(_savePath))
                {
                    _savePath = Environment.SpecialFolder.Desktop.ToString();
                }
                FileSavePicker fileSavePicker = new FileSavePicker();
                fileSavePicker.SuggestedStartLocation = PickerLocationId.Desktop;
                fileSavePicker.FileTypeChoices.Add("ÎÄ±¾ÎÄµµ", new List<string> { ".txt" });
                fileSavePicker.SuggestedFileName = $"diff_changelist_{_revFrom}_{_revTo}.txt";
                MainWindow.CurrentMainWindow.InitializeObject(fileSavePicker);

                StorageFile file = await fileSavePicker.PickSaveFileAsync();
                if (file != null && file.IsAvailable)
                {
                    CachedFileManager.DeferUpdates(file);
                    await FileIO.WriteTextAsync(file, _rawData, UnicodeEncoding.Utf8);
                    await CachedFileManager.CompleteUpdatesAsync(file);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void LvChangeList_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (((FrameworkElement) e.OriginalSource).DataContext is DiffFileItem item)
            {
                StartSvnCommand("log", item.FilePath);
            }
        }

        private void ListViewItem_OnCopyClick(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)e.OriginalSource).DataContext is DiffFileItem item)
            {
                DataPackage package = new DataPackage();
                package.SetText(item.FilePath);
                Clipboard.SetContent(package);
            }
        }

        private void ListViewItem_OnDiffClick(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)e.OriginalSource).DataContext is DiffFileItem item)
            {
                StartSvnCommand("diff", item.FilePath);
            }
        }

        private void ListViewItem_OnBlameClick(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)e.OriginalSource).DataContext is DiffFileItem item)
            {
                StartSvnCommand("blame", item.FilePath);
            }
        }

        private void ListViewItem_OnShowLogClick(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)e.OriginalSource).DataContext is DiffFileItem item)
            {
                StartSvnCommand("log", item.FilePath);
            }
        }

        private void LvChangeList_OnRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (((FrameworkElement)e.OriginalSource).DataContext is DiffFileItem item)
            {
                lvChangeList.SelectedIndex = item.Index;
            }
        }
    }
}
