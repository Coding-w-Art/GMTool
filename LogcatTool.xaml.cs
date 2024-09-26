// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Documents;
using GMTool.Data;
using Windows.Storage;
using DispatcherQueuePriority = Microsoft.UI.Dispatching.DispatcherQueuePriority;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Microsoft.UI.Xaml.Input;
using UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding;
using Windows.System;
using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GMTool
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LogcatTool : Page
    {
        private string _savePath;
        private readonly List<string> _outputLines;
        private string _deviceName = string.Empty;
        private readonly List<string> _deviceNames;
        private readonly string _adbCommandName = string.Empty;
        private bool _paused;
        private bool _clearHistory = true;

        private readonly bool _adbInstalled;
        private string _filterKeywords = string.Empty;

        public LogcatTool()
        {
            this.InitializeComponent();

            _outputLines = new List<string>();
            _deviceNames = new List<string>();

            string envPath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);
            if (!string.IsNullOrEmpty(envPath))
            {
                string[] paths = envPath.Split(";");
                if (paths.Length > 0)
                {
                    foreach (string path in paths)
                    {
                        if (string.IsNullOrEmpty(path)) continue;
                        DirectoryInfo dirInfo = new DirectoryInfo(path);
                        if (dirInfo.Exists)
                        {
                            FileInfo[] fileInfos = dirInfo.GetFiles("adb.exe", SearchOption.TopDirectoryOnly);
                            if (fileInfos.Length > 0)
                            {
                                _adbInstalled = true;
                                _adbCommandName = "adb";
                                break;
                            }
                        }
                    }
                }
            }

            if (!_adbInstalled)
            {
                _adbCommandName = Path.Combine(CommonData.RepoRootPath, "Tools\\adb\\adb.exe");
            }

            if (_adbInstalled)
            {
                SetInfoBar(InfoBarSeverity.Informational, "未连接", "检测到 ADB 工具。连接设备并开始。");
            }
            else if (File.Exists(_adbCommandName))
            {
                SetInfoBar(InfoBarSeverity.Informational, "未连接", "未检测到 ADB 工具，将使用内置 ADB 工具。连接设备并开始。");
            }
            else
            {
                btStart.Visibility = Visibility.Collapsed;
                btStop.Visibility = Visibility.Collapsed;
                btResume.Visibility = Visibility.Collapsed;
                btPause.Visibility = Visibility.Collapsed;
                SetInfoBar(InfoBarSeverity.Error, "未连接", "未检测到 ADB 工具，请先安装 ADB 工具。");
            }
        }

        private async void BtStart_OnClick(object sender, RoutedEventArgs e)
        {
            SetInfoBar(InfoBarSeverity.Warning, "连接中", "重启 ADB 进程...");
            _paused = false;
            btStart.Visibility = Visibility.Collapsed;
            btStop.Visibility = Visibility.Visible;
            btSave.IsEnabled = false;

            Process[] processList = Process.GetProcessesByName("adb");
            if (processList.Length > 0)
            {
                foreach (Process p in processList)
                {
                    p.Exited -= ProcessOnExited;
                    p.Kill(true);
                    await p.WaitForExitAsync();
                }
            }

            SetInfoBar(InfoBarSeverity.Warning, "连接中", "正在连接设备...");
            // check devices
            _deviceName = string.Empty;
            _deviceNames.Clear();
            ProcessStartInfo deviceInfo = new ProcessStartInfo(_adbCommandName)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                Arguments = "shell settings get global device_name",
                StandardOutputEncoding = new UTF8Encoding(false),
            };

            Process processDevice = Process.Start(deviceInfo);
            if (processDevice != null)
            {
                processDevice.EnableRaisingEvents = true;
                processDevice.OutputDataReceived += ProcessDeviceDataReceived;
                processDevice.BeginOutputReadLine();
                await processDevice.WaitForExitAsync();
            }
            else
            {
                SetInfoBar(InfoBarSeverity.Error, "连接失败", "无法连接到设备。");
                return;
            }

            if (_deviceNames.Count == 0)
            {
                SetInfoBar(InfoBarSeverity.Error, "连接失败", "未发现设备。");
                return;
            }

            if (_deviceNames.Count != 1)
            {
                SetInfoBar(InfoBarSeverity.Error, "连接失败", "发现多台设备。");
                return;
            }
            _deviceName = _deviceNames[0];

            if (_clearHistory)
            {
                SetInfoBar(InfoBarSeverity.Warning, $"已连接 {_deviceName}", "清除日志历史记录...");
                // clear logcat
                ProcessStartInfo clearLogcat = new ProcessStartInfo(_adbCommandName)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    Arguments = "logcat -c"
                };
                Process processClear = Process.Start(clearLogcat);
                if (processClear != null)
                {
                    await processClear.WaitForExitAsync();
                }
                await Task.Delay(2000);
            }

            // start logcat
            ProcessStartInfo info = new ProcessStartInfo(_adbCommandName)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                Arguments = "logcat -s Unity",
                StandardOutputEncoding = new UTF8Encoding(false)
            };
            Process process = Process.Start(info);
            if (process != null)
            {
                SetInfoBar(InfoBarSeverity.Success, $"已连接 {_deviceName}", "正在记录日志...");
                process.EnableRaisingEvents = true;
                process.OutputDataReceived += ProcessOutputDataReceived;
                process.BeginOutputReadLine();
                process.Exited += ProcessOnExited;
            }

            btPause.Visibility = Visibility.Visible;
            btResume.Visibility = Visibility.Collapsed;
        }

        private void ProcessOnExited(object sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
            {
                btStop.Visibility = Visibility.Collapsed;
                btStart.Visibility = Visibility.Visible;
                btPause.Visibility = Visibility.Collapsed;
                btResume.Visibility = Visibility.Collapsed;
                btSave.IsEnabled = true;
                SetInfoBar(InfoBarSeverity.Error, "已断开", "连接已断开，请重新开始");
            });
        }

        private void ProcessDeviceDataReceived(object sender, DataReceivedEventArgs args)
        {
            string line = args.Data;
            if (!string.IsNullOrEmpty(line) && !line.StartsWith("*") && !line.StartsWith("error"))
            {
                _deviceNames.Add(line);
            }
        }

        private void ProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (_paused) return;
            string line = e.Data;
            if (!string.IsNullOrEmpty(line))
            {
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
                {
                    AddOutputLineData(line);
                });
            }
        }

        private void AddOutputLineData(string line)
        {
            _outputLines.Add(line);

            if (_filterKeywords != string.Empty &&
                !line.Contains(_filterKeywords, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Paragraph p = LineTextToParagraph(line);
            tbConsole.Blocks.Add(p);

            //scrollViewer.ScrollToVerticalOffset(scrollViewer.ScrollableHeight);
        }

        private Paragraph LineTextToParagraph(string line)
        {
            Paragraph p = new Paragraph();

            string[] substrings;
            string separator;
            Color color;
            //if (line.IndexOf("I Unity", StringComparison.Ordinal) > 0)
            //{
            //    separator = "I Unity";
            //    substrings = line.Split(separator);
            //    color = Colors.AliceBlue;
            //}
            //else
            if (line.IndexOf("W Unity", StringComparison.Ordinal) > 0)
            {
                separator = "W Unity";
                substrings = line.Split(separator);
                color = Colors.Gold;
            }
            else if (line.IndexOf("E Unity", StringComparison.Ordinal) > 0)
            {
                separator = "E Unity";
                substrings = line.Split(separator);
                color = Colors.OrangeRed;
            }
            else
            {
                p.Inlines.Add(new Run { Text = line });
                return p;
            }

            if ( substrings.Length == 2)
            {
                p.Inlines.Add(new Run { Text = substrings[0] });
                p.Inlines.Add(new Run { Text = separator, Foreground = new SolidColorBrush(color) });
                p.Inlines.Add(new Run { Text = substrings[1] });
                return p;
            }
            else
            {
                p.Inlines.Add(new Run { Text = line });
                return p;
            }
        }

        private async void BtStop_OnClick(object sender, RoutedEventArgs e)
        {
            Process[] processList = Process.GetProcessesByName("adb");
            if (processList.Length > 0)
            {
                foreach (Process p in processList)
                {
                    p.Kill(true);
                    await p.WaitForExitAsync();
                }
            }

            btStop.Visibility = Visibility.Collapsed;
            btStart.Visibility = Visibility.Visible;
            btPause.Visibility = Visibility.Collapsed;
            btResume.Visibility = Visibility.Collapsed;
            btSave.IsEnabled = true;
            SetInfoBar(InfoBarSeverity.Informational, "已停止", "连接设备并重新开始运行。");
        }

        private void BtPause_OnClick(object sender, RoutedEventArgs e)
        {
            btPause.Visibility = Visibility.Collapsed;
            btResume.Visibility = Visibility.Visible;
            btSave.IsEnabled = true;
            _paused = true;
        }

        private void BtResume_OnClick(object sender, RoutedEventArgs e)
        {
            btPause.Visibility = Visibility.Visible;
            btResume.Visibility = Visibility.Collapsed;
            btSave.IsEnabled = false;
            _paused = false;
        }

        private void SetInfoBar(InfoBarSeverity severity, string title, string message)
        {
            infoBar.Severity = severity;
            infoBar.Title = title;
            infoBar.Message = message;
        }

        private void BtClear_OnClick(object sender, RoutedEventArgs e)
        {
            tbConsole.Blocks.Clear();
            _outputLines.Clear();
        }

        private async void BtSave_OnClick(object sender, RoutedEventArgs e)
        {
            if (_outputLines.Count == 0) return;

            if (string.IsNullOrEmpty(_savePath) || Directory.Exists(_savePath))
            {
                _savePath = Environment.SpecialFolder.Desktop.ToString();
            }
            FileSavePicker fileSavePicker = new FileSavePicker();
            fileSavePicker.SuggestedStartLocation = PickerLocationId.Desktop;
            fileSavePicker.FileTypeChoices.Add("日志文件", new List<string> { ".log" });
            fileSavePicker.SuggestedFileName = $"adb_logcat_{DateTime.Now.ToShortDateString()}_{DateTime.Now.ToLongTimeString()}.log";

            MainWindow.CurrentMainWindow.InitializeObject(fileSavePicker);

            StorageFile file = await fileSavePicker.PickSaveFileAsync();
            if (file != null && file.IsAvailable)
            {
                CachedFileManager.DeferUpdates(file);
                await FileIO.WriteLinesAsync(file, _outputLines, UnicodeEncoding.Utf8);
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                if (status == FileUpdateStatus.Complete)
                {
                    SetInfoBar(InfoBarSeverity.Informational, "日志已保存", $"文件路径：{file.Path}");
                }
                else
                {
                    SetInfoBar(InfoBarSeverity.Error, "日志保存失败", $"文件路径：{file.Path}");
                }
            }
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

        private void TextFilter_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (sender is TextBox textBox && e.Key == VirtualKey.Enter)
            {
                string input = textBox.Text;
                if (string.IsNullOrEmpty(input))
                {
                    if (_filterKeywords != string.Empty)
                    {
                        _filterKeywords = string.Empty;
                        UpdateOutputFilter();
                    }
                }
                else if (_filterKeywords != input)
                {
                    _filterKeywords = input;
                    UpdateOutputFilter();
                }
            }
        }

        private void TextFilter_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrEmpty(textBox.Text))
            {
                if (_filterKeywords != string.Empty)
                {
                    _filterKeywords = string.Empty;
                    UpdateOutputFilter();
                }
            }
        }

        private void UpdateOutputFilter()
        {
            tbConsole.Blocks.Clear();
            if (_filterKeywords == string.Empty)
            {
                foreach (string line in _outputLines)
                {
                    Paragraph p = LineTextToParagraph(line);
                    tbConsole.Blocks.Add(p);
                }
            }
            else
            {
                foreach (string line in _outputLines)
                {
                    if (line.Contains(_filterKeywords, StringComparison.OrdinalIgnoreCase))
                    {
                        Paragraph p = LineTextToParagraph(line);
                        tbConsole.Blocks.Add(p);
                    }
                }
            }
        }

        private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            _clearHistory = true;
        }

        private void ToggleButton_OnUnchecked(object sender, RoutedEventArgs e)
        {
            _clearHistory = false;
        }
    }
}
