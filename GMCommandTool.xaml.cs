using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Globalization.NumberFormatting;
using GMTool.Data;
using Tomlyn;
using Windows.System;
using Newtonsoft.Json.Linq;
using Tomlyn.Model;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GMTool
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GMCommandTool : Page
    {
        private readonly ObservableCollection<ServerItemData> _serverList = new();
        private readonly ObservableCollection<HistoryInfo> _historyList = new();
        private readonly List<CommandCategoryInfo> _commandCategoryList = new();
        private readonly List<Control> _commandParamList = new();

        private readonly List<ParameterOptionInfo> _pidOptions = new();

        private readonly Dictionary<string, string> _languagesDict = new();

        private readonly Dictionary<Control, CommandParameterInfo> _curCommandParamDict = new();

        private string _infoBarMessage = string.Empty;
        private string _playerInfoPath = string.Empty;
        private string _configPath = string.Empty;

        private int _defaultServerId;
        private int _defaultPlayerId;
        private string _defaultPlayerName;
        private int _navigationIndex = -1;
        private bool _autoFillPID = true;

        private bool _initialized = false;

        private Visibility PrivilegeVisibility;


        private readonly List<EnvInfo> _envList = new()
        {
            new("ONE", "NewVersion"),
            new("ONE", "PreRelease"),
            new("BETA", "NewVersion"),
            new("BETA", "PreRelease"),
            new("CHN", "NewVersion"),
            new("CHN", "PreRelease"),
        };

        private readonly List<EnvInfo> _envSpecList = new()
        {
            new("ONE", "NewVersion"),
            new("ONE", "PreRelease"),
            new("ONE", "Review"),
            new("BETA", "NewVersion"),
            new("BETA", "PreRelease"),
            new("BETA", "Review"),
            new("CHN", "NewVersion"),
            new("CHN", "PreRelease"),
            new("CHN", "Review"),
        };

        private readonly Dictionary<string, string> _regionUrl = new()
        {
            {"ONE", "http://grandmaison-one-pre.hrgame.com.cn"},
            {"BETA", "https://chef-sv-pre.huorongames.com"},
            {"CHN", "https://chef-chn-pre.hrgame.com.cn"},
        };

        private readonly Dictionary<string, string> _privateSignKey = new()
        {
            {"ONE", "PrivateSignKey"},
            {"BETA", "SXtT936O6cfE7jPHIt7nxnNCTmDwIdn5"},
            {"CHN", "mk8vZZtSU7uy7NF3X3gLgEstShqKDfNk"},
        };

        private string _curRegion = "ONE";
        private string _curEnv = "PreRelease";

        private string ServerUrl => $"{_regionUrl[_curRegion]}/front/query_servers";
        private string CommandUrl => $"{_regionUrl[_curRegion]}/gm/command";
        private string PrivateSignKey => _privateSignKey[_curRegion];

        private long _defaultServerGroup = 205;

        public GMCommandTool()
        {
            PrivilegeVisibility = CommonData.PrivilegeVisibility;

            this.InitializeComponent();
            Init();
        }

        private async void Init()
        {
            await LoadLanguageConfigs();
            await LoadCommands();
            await LoadPlayerInfo();

            List<HistorySaveInfo> info = await CommonData.LoadCommandHistory();
            if (info != null && info.Count > 0)
            {
                foreach (HistorySaveInfo item in info)
                {
                    StandardUICommand commandCopy = new StandardUICommand();
                    commandCopy.ExecuteRequested += CommandCopy_ExecuteRequested;

                    _historyList.Add(new HistoryInfo
                    {
                        Command = item.Command,
                        Result = item.Result,
                        ErrorCode = item.ErrorCode,
                        CommandCopy = commandCopy,
                    });
                }
                BtnClearHistory.Visibility = Visibility.Visible;
            }

            HistoryListView.ItemsSource = _historyList;
            ServersListView.ItemsSource = _serverList;
            //cbEnvSelect.ItemsSource = _envList;
            cbEnvSelect.ItemsSource = _envSpecList;

            cbEnvSelect.SelectionChanged += OnEnvSelectChanged;
            cbEnvSelect.SelectedIndex = 1;
        }

        private async Task LoadPlayerInfo()
        {
            string playerInfoPath = Path.Combine(CommonData.RepoRootPath, "Tools/GM2/player_info");
            if (File.Exists(playerInfoPath))
            {
                string data = await File.ReadAllTextAsync(playerInfoPath, new UTF8Encoding(false));
                if (!string.IsNullOrEmpty(data))
                {
                    string[] playerInfo = data.Split('|');
                    if (playerInfo.Length == 4)
                    {
                        int serverId = int.Parse(playerInfo[0]);
                        int playerId = int.Parse(playerInfo[1]);
                        string playerName = playerInfo[2];
                        //int playerIndex = int.Parse(playerInfo[3]);

                        if (serverId == _defaultServerId
                            && playerId == _defaultPlayerId
                            && playerName == _defaultPlayerName)
                        {
                            return;
                        }

                        _defaultServerId = serverId;
                        _defaultPlayerId = playerId;
                        _defaultPlayerName = playerName;

                        _pidOptions.Add(new ParameterOptionInfo(playerId.ToString(), playerName));
                    }
                }
            }

            BtnResetServer.Content = _defaultServerId.ToString();
            BtnResetServer.Visibility = _defaultServerId > 0 ? Visibility.Visible : Visibility.Collapsed;
            BtnResetPlayer.Content = $"{_defaultPlayerId} - {_defaultPlayerName}";
            //BtnResetPlayer.Visibility = _defaultPlayerId > 0 ? Visibility.Visible : Visibility.Collapsed;

            FillPID_OnClick(null, null);
        }

        private async Task LoadLanguageConfigs()
        {
            if (CommonData.PrivilegeVisibility == Visibility.Collapsed) return;

            _configPath = Path.Combine(CommonData.RepoRootPath, "Resources\\LogicData\\Dev\\client_json");
            DirectoryInfo dirInfo = new DirectoryInfo(_configPath);
            if (!dirInfo.Exists)
                return;

            _languagesDict.Clear();

            FileInfo[] languageFiles = dirInfo.GetFiles("CfgLanguage*.json", SearchOption.TopDirectoryOnly);
            foreach (FileInfo fileInfo in languageFiles)
            {
                try
                {
                    JArray o = JArray.Parse(await File.ReadAllTextAsync(fileInfo.FullName));
                    foreach (JToken line in o)
                    {
                        string id = (string)line.SelectToken("ID");
                        string newId = (string)line.SelectToken("NewID");
                        string name = (string)line.SelectToken("zh-CN");
                        if (id != null)
                        {
                            _languagesDict.TryAdd(id, name);
                        }
                        if (newId != null)
                        {
                            _languagesDict.TryAdd(newId, name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    TopInfoBar.Message = $"配置解析失败。({ex.Message.ReplaceLineEndings(string.Empty)})";
                    TopInfoBar.Severity = InfoBarSeverity.Error;
                }
            }
        }

        private async Task LoadCommands()
        {
            _commandCategoryList.Clear();
            CommandCategoryInfo allInfo = new CommandCategoryInfo("All", "全部");
            _commandCategoryList.Add(allInfo);
            string commandPath = Path.Combine(CommonData.RepoRootPath, "Tools/GM2/Commands.toml");
            if (!File.Exists(commandPath))
            {
                TopInfoBar.Message = "Commands.toml 文件读取失败。";
                TopInfoBar.Severity = InfoBarSeverity.Error;
                return;
            }
            string file = await File.ReadAllTextAsync(commandPath);
            Tomlyn.Syntax.DocumentSyntax documentSyntax = Toml.Parse(file);
            if (documentSyntax.HasErrors)
            {
                TopInfoBar.Message = $"命令解析失败。{documentSyntax.Diagnostics[0].Message.ReplaceLineEndings(string.Empty)}";
                TopInfoBar.Severity = InfoBarSeverity.Error;
                return;
            }

            try
            {
                TomlTable model = Toml.ToModel(file);
                List<string> categories = model.Keys.ToList();
                if (categories.Count > 0)
                {
                    foreach (string category in categories)
                    {
                        TomlTable table = (TomlTable)model[category];
                        if (table.TryGetValue("title", out var title))
                        {
                            string categoryTitle = (string)title;
                            CommandCategoryInfo info = new CommandCategoryInfo(category, categoryTitle);

                            List<string> keys = table.Keys.ToList();
                            foreach (string key in keys)
                            {
                                if (key == "title") continue;
                                TomlTable command = (TomlTable)table[key];

                                if (!command.TryGetValue("title", out var value1))
                                    continue;

                                bool needConfirm =  false;
                                if (command.TryGetValue("confirm", out var confirm))
                                {
                                    needConfirm = (bool)confirm;
                                }

                                long serverGroup;
                                if (command.TryGetValue("serverGroup", out var group))
                                {
                                    serverGroup = (long)group;
                                }
                                else
                                {
                                    serverGroup = _defaultServerGroup;
                                }

                                CommandInfo commandInfo = new CommandInfo
                                {
                                    Name = key,
                                    Title = (string)value1,
                                    Category = category,
                                    NeedConfirm = needConfirm,
                                    ServerGroup = serverGroup,
                                };

                                if (command.TryGetValue("params", out var paramArray))
                                {
                                    TomlArray parameters = (TomlArray)paramArray;
                                    foreach (TomlTable parameter in parameters)
                                    {
                                        if (!parameter.TryGetValue("name", out var pName) ||
                                            !parameter.TryGetValue("title", out var pTitle))
                                        {
                                            continue;
                                        }
                                        CommandParameterInfo paramInfo = new CommandParameterInfo((string)pName, (string)pTitle);

                                        if (parameter.TryGetValue("options", out var optionsArray))
                                        {
                                            TomlArray options = (TomlArray)optionsArray;
                                            List<ParameterOptionInfo> optionsList = new List<ParameterOptionInfo>();
                                            foreach (TomlTable option in options)
                                            {
                                                string optionValue = (string)option["value"];
                                                string optionTitle = (string)option["title"];
                                                optionsList.Add(new ParameterOptionInfo(optionValue, optionTitle));
                                            }
                                            paramInfo.HasOptions = true;
                                            paramInfo.SetParameterOption(optionsList);
                                        }
                                        else
                                        {
                                            if (parameter.TryGetValue("source", out var source) &&
                                                parameter.TryGetValue("range", out var range))
                                            {
                                                paramInfo.HasOptions = true;
                                                paramInfo.Source = (string)source;
                                                paramInfo.Range = (string)range;
                                                if (parameter.TryGetValue("desc", out var desc))
                                                {
                                                    paramInfo.Desc = (string)desc;
                                                }
                                                else
                                                {
                                                    paramInfo.Desc = string.Empty;
                                                }
                                            }
                                        }

                                        if (parameter.TryGetValue("default", out var defaultValue))
                                        {
                                            paramInfo.DefaultValue = (string)defaultValue;
                                        }
                                        commandInfo.parameters.Add(paramInfo);
                                    }
                                }

                                info.commands.Add(commandInfo);
                                allInfo.commands.Add(commandInfo);
                            }
                            _commandCategoryList.Add(info);
                        }
                    }
                    SelectorTypes.ItemsSource = _commandCategoryList;
                }
            }
            catch (Exception ex)
            {
                TopInfoBar.Message = $"命令列表读取失败。({ex.Message.ReplaceLineEndings(string.Empty)})";
                TopInfoBar.Severity = InfoBarSeverity.Error;
            }
        }


        private void ResetDefaultServerId(object sender, RoutedEventArgs e)
        {
            SetServerId(_defaultPlayerId);
        }

        private void SetServerId(int serverId)
        {
            if (serverId > 0)
            {
                ServersListView.SelectedIndex = -1;
                for (int index = 0; index < _serverList.Count; index++)
                {
                    ServerItemData info = _serverList[index];
                    if (info.ID == _defaultServerId)
                    {
                        ServersListView.SelectedIndex = index;
                        ServersListView.ScrollIntoView(ServersListView.SelectedItem);
                        break;
                    }
                }
            }
        }

        private async Task ReqServerList()
        {
            _infoBarMessage = string.Empty;
            _serverList.Clear();
            try
            {
                HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                ServerReqInfo reqInfo = new ServerReqInfo
                {
                    Channel = 0,
                    DisgardNotOpened = false,
                    Env = _curEnv.ToLower(),
                    ClientVersion = "0.0.1",
                    Privilege = 1,
                    NeedGame = true
                };
                string postData = JsonSerializer.Serialize(reqInfo, typeof(ServerReqInfo));
                HttpResponseMessage response = await client.PostAsync(ServerUrl, new StringContent(postData, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
                string result = await response.Content.ReadAsStringAsync();

                Hashtable content = JsonSerializer.Deserialize<Hashtable>(result);
                if (content.ContainsKey("ErrorCode"))
                {
                    throw new Exception(content["ErrorCode"]?.ToString());
                }

                List<ServerItemData> serverList = new List<ServerItemData>();
                if (content.ContainsKey("Game"))
                {
                    string servers = content["Game"]?.ToString();
                    serverList = JsonSerializer.Deserialize<List<ServerItemData>>(servers);
                }
                serverList.Sort((a, b) => a.ID - b.ID);

                foreach (ServerItemData serverInfo in serverList)
                {      
                    _serverList.Add(serverInfo);
                }

                if (_defaultServerId > 0)
                {
                    SetServerId(_defaultServerId);
                }
            }
            catch (Exception ex)
            {
                _infoBarMessage = $"获取服务器列表失败。({ex.Message.ReplaceLineEndings(string.Empty)})";
                TopInfoBar.Message = _infoBarMessage;
                TopInfoBar.Severity = InfoBarSeverity.Error;
                return;
            }

            if (!_initialized)
            {
                _initialized = true;
            }
        }

        private void ServersListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selecedCount = ServersListView.SelectedItems.Count;
            BtnSend.IsEnabled = selecedCount > 0 && !string.IsNullOrEmpty(TextFinalCommand.Text);

            if (selecedCount == 1)
            {
                ServerItemData info = (ServerItemData)ServersListView.SelectedItem;
                TopInfoBar.Title = $"服务器：[{info.ID}] {info.Name}";
                TopInfoBar.Message = _infoBarMessage;
                TopInfoBar.Severity = InfoBarSeverity.Informational;
            }
            else if (selecedCount > 1)
            {
                TopInfoBar.Title = $"已选择{selecedCount}个服务器";
                TopInfoBar.Message = _infoBarMessage;
                TopInfoBar.Severity = InfoBarSeverity.Informational;
            }
            else
            {
                TopInfoBar.Title = "未选择服务器";
                TopInfoBar.Message = "请选择服务器。";
                TopInfoBar.Severity = InfoBarSeverity.Warning;
            }
        }

        private void TextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            BtnSend.IsEnabled = ServersListView.SelectedItems.Count > 0 && !string.IsNullOrEmpty(TextFinalCommand.Text);
        }

        private void SelectorTypes_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CommandCategoryInfo info = (CommandCategoryInfo)SelectorTypes.SelectedItem;
            if (info != null)
            {
                RefreshSelectorCommand(info.Name);
                SelectorCommands.SelectedIndex = -1;

                if (!SelectorCommands.IsEnabled)
                {
                    SelectorCommands.IsEnabled = true;
                }
            }
        }

        private void RefreshSelectorCommand(string name)
        {
            foreach (CommandCategoryInfo info in _commandCategoryList)
            {
                if (info.Name == name)
                {
                    SelectorCommands.ItemsSource = info.commands;
                    break;
                }
            }
        }

        private async void SelectorCommands_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectorCommands.SelectedItem is CommandInfo info)
            {
                GridParams.Children.Clear();
                _commandParamList.Clear();
                GridParams.ColumnDefinitions.Clear();
                _curCommandParamDict.Clear();
                TextBlockNoParams.Visibility = info.parameters.Count > 0 ? Visibility.Collapsed : Visibility.Visible;

                for (var index = 0; index < info.parameters.Count; index++)
                {
                    CommandParameterInfo parameter = info.parameters[index];
                    Control control;
                    if (parameter.Name == "date")
                    {
                        //DatePicker datePicker = new DatePicker
                        //{
                        //    Header = new TextBlock
                        //    {
                        //        Text = parameter.Title,
                        //        TextWrapping = TextWrapping.NoWrap,
                        //    },
                        //    Margin = new Thickness(10),
                        //    FontFamily = new FontFamily("Assets/JetBrainsMono.ttf#JetBrains Mono"),
                        //    Height = TextFinalCommand.Height,
                        //    //DayFormat = "{day.integer}",
                        //    //MonthFormat = "{month.integer} {month.abbreviated}",
                        //    //YearFormat = "{year.abbreviated(4)}"
                        //    MinWidth = 100,
                        //    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        //    VerticalContentAlignment = VerticalAlignment.Top,
                        //};
                        //datePicker.Date = DateTimeOffset.Now;
                        //ToolTipService.SetToolTip(datePicker, new ToolTip { Content = parameter.Title });
                        //datePicker.DateChanged += (_, args) => { if (args.OldDate != args.NewDate) AssembleCommand(); };
                        //control = datePicker;
                        //GridParams.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(456, GridUnitType.Pixel) });

                        CalendarDatePicker picker = new CalendarDatePicker
                        {
                            Header = new TextBlock
                            {
                                Text = parameter.Title,
                                TextWrapping = TextWrapping.NoWrap,
                            },
                            Margin = new Thickness(10),
                            FontFamily = new FontFamily("Assets/JetBrainsMono.ttf#JetBrains Mono"),
                            Height = TextFinalCommand.Height,
                            Date = DateTimeOffset.Now,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            IsGroupLabelVisible = true,
                        };
                        ToolTipService.SetToolTip(picker, new ToolTip { Content = parameter.Title });
                        picker.DateChanged += (_, args) => { if (args.OldDate != args.NewDate) AssembleCommand(); };
                        control = picker;
                        GridParams.ColumnDefinitions.Add(new ColumnDefinition { MinWidth = 160, Width = new GridLength(1.5d, GridUnitType.Star) });
                    }
                    //else if (parameter.Name == "time")
                    //{
                    //    TimePicker timePicker = new TimePicker
                    //    {
                    //        Header = new TextBlock
                    //        {
                    //            Text = parameter.Title,
                    //            TextWrapping = TextWrapping.NoWrap,
                    //        },
                    //        Margin = new Thickness(10),
                    //        FontFamily = new FontFamily("Assets/JetBrainsMono.ttf#JetBrains Mono"),
                    //        //Height = TextFinalCommand.Height,
                    //        ClockIdentifier = "24HourClock",
                    //        Time = TimeSpan.Zero,
                    //        MinWidth = 100,
                    //        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    //        VerticalContentAlignment = VerticalAlignment.Top,
                    //    };
                    //    timePicker.TimeChanged += (_, args) => { if (args.OldTime != args.NewTime) AssembleCommand(); };
                    //    ToolTipService.SetToolTip(timePicker, new ToolTip { Content = parameter.Title });
                    //    control = timePicker;
                    //    //GridParams.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(242, GridUnitType.Pixel) });
                    //}
                    else if (parameter.Name == "hh" || parameter.Name == "mm" || parameter.Name == "ss")
                    {
                        NumberBox numberBox = new NumberBox
                        {
                            Header = new TextBlock
                            {
                                Text = parameter.Title,
                                TextWrapping = TextWrapping.NoWrap,
                            },
                            Margin = new Thickness(10),
                            FontFamily = new FontFamily("Assets/JetBrainsMono.ttf#JetBrains Mono"),
                            Height = TextFinalCommand.Height,
                            AcceptsExpression = false,
                            IsWrapEnabled = false,
                            PlaceholderText = parameter.Name,
                            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
                            SmallChange = 1,
                            LargeChange = 5,
                            NumberFormatter = new DecimalFormatter { FractionDigits = 0 },
                        };

                        if (parameter.Name == "hh")
                        {
                            numberBox.Minimum = 0;
                            numberBox.Maximum = 23;

                            if (string.IsNullOrEmpty(parameter.DefaultValue))
                            {
                                numberBox.Value = DateTime.Now.Hour;
                            }
                            else
                            {
                                numberBox.Value = int.Parse(parameter.DefaultValue);
                            }
                        }
                        else if (parameter.Name == "mm")
                        {
                            numberBox.Minimum = 0;
                            numberBox.Maximum = 59;

                            if (string.IsNullOrEmpty(parameter.DefaultValue))
                            {
                                numberBox.Value = DateTime.Now.Minute;
                            }
                            else
                            {
                                numberBox.Value = int.Parse(parameter.DefaultValue);
                            }
                        }
                        else if (parameter.Name == "ss")
                        {
                            numberBox.Minimum = 0;
                            numberBox.Maximum = 59;

                            if (string.IsNullOrEmpty(parameter.DefaultValue))
                            {
                                numberBox.Value = 0;
                            }
                            else
                            {
                                numberBox.Value = int.Parse(parameter.DefaultValue);
                            }
                        }

                        ToolTipService.SetToolTip(numberBox, new ToolTip { Content = parameter.Title });
                        numberBox.ValueChanged += (_, args) => { if (args.OldValue != args.NewValue) AssembleCommand(); };
                        control = numberBox;
                        GridParams.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Star) });
                    }
                    else
                    {
                        AutoSuggestBox box = new AutoSuggestBox
                        {
                            Header = new TextBlock
                            {
                                Text = parameter.Title,
                                TextWrapping = TextWrapping.NoWrap,
                            },
                            Margin = new Thickness(10),
                            FontFamily = new FontFamily("Assets/JetBrainsMono.ttf#JetBrains Mono"),
                            Height = TextFinalCommand.Height,
                            PlaceholderText = parameter.Name,
                            DisplayMemberPath = "DisplayName",
                            TextMemberPath = "Value",
                            IsSuggestionListOpen = false,
                        };
                        ToolTipService.SetToolTip(box, new ToolTip { Content = parameter.Title });
                        control = box;
                        GridParams.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Star) });

                        if (parameter.IsPID)
                        {
                            if (_pidOptions.Count > 0)
                            {
                                box.ItemsSource = _pidOptions;
                                //box.QuerySubmitted += OnQuerySubmitted;
                                box.SuggestionChosen += OnSuggestionChosen;
                                box.GotFocus += (_, _) => { box.IsSuggestionListOpen = true; };
                                box.LostFocus += (_, _) => { box.IsSuggestionListOpen = false; };
                            }

                            if (_autoFillPID && _defaultPlayerId != 0)
                            {
                                box.Text = _defaultPlayerId.ToString();
                            }
                        }
                        else
                        {
                            if (parameter.HasOptions)
                            {
                                if (!parameter.ParameterLoaded)
                                {
                                    parameter.SetParameterOption(await LoadParameterOptions(parameter));
                                }

                                if (parameter.Options.Count > 0)
                                {
                                    box.ItemsSource = parameter.Options;
                                    //FontIcon icon = new FontIcon
                                    //{
                                    //    FontFamily = new FontFamily("/Assets/Segoe Fluent Icons.ttf#Segoe Fluent Icons"),
                                    //    Glyph = "\xe721",
                                    //    FontSize = 12
                                    //};
                                    //box.QueryIcon = icon;
                                    //box.QuerySubmitted += OnQuerySubmitted;
                                    box.SuggestionChosen += OnSuggestionChosen;
                                    box.UpdateTextOnSelect = false;

                                    box.GotFocus += (_, _) => { box.IsSuggestionListOpen = true; };
                                    box.LostFocus += (_, _) => { box.IsSuggestionListOpen = false; };
                                }
                            }

                            if (!string.IsNullOrEmpty(parameter.DefaultValue))
                            {
                                box.Text = parameter.DefaultValue;
                            }
                        }

                        if (index == info.parameters.Count - 1)
                        {
                            box.KeyUp += OnLastParamBoxEnterKeyUp;
                        }
                        box.TextChanged += CommandParamTextChanged;
                    }

                    //GridParams.ColumnDefinitions.Add(new ColumnDefinition());
                    GridParams.Children.Add(control);
                    _commandParamList.Add(control);
                    _curCommandParamDict.Add(control, parameter);

                    Grid.SetColumn(control, index);
                }

                AssembleCommand();
            }
        }

        private void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion is ParameterOptionInfo optionInfo)
            {
                //User selected an item, take an action
                sender.Text = optionInfo.Value;
            }
            else
            {
                if (_curCommandParamDict.TryGetValue(sender, out CommandParameterInfo paramInfo) &&
                    paramInfo.HasOptions)
                {
                    if (!string.IsNullOrEmpty(args.QueryText))
                    {
                        List<ParameterOptionInfo> results = new List<ParameterOptionInfo>();
                        foreach (ParameterOptionInfo info in paramInfo.Options)
                        {
                            if (info.Matches(args.QueryText))
                            {
                                results.Add(info);
                            }
                        }

                        if (paramInfo.Options.Count > 0 && results.Count == 0)
                        {
                            results.Add(new ParameterOptionInfo(null, "（无结果）"));
                        }
                        sender.ItemsSource = results;
                    }
                    else
                    {
                        sender.ItemsSource = paramInfo.Options;
                    }
                }
            }
        }

        private void OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (args.SelectedItem is ParameterOptionInfo optionInfo)
            {
                sender.Text = optionInfo.Value;
            }
            else if (args.SelectedItem is string s)
            {
                sender.Text = s;
            }
        }

        private void OnLastParamBoxEnterKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && BtnSend.IsEnabled)
            {
                BtnSend_OnClick(null, null);
            }
        }

        private void CommandParamTextChanged(Control sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (sender is AutoSuggestBox box)
            {
                if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput &&
                    _curCommandParamDict.TryGetValue(box, out CommandParameterInfo paramInfo) &&
                    paramInfo.HasOptions)
                {
                    if (!string.IsNullOrEmpty(box.Text))
                    {
                        List<ParameterOptionInfo> results = new List<ParameterOptionInfo>();
                        foreach (ParameterOptionInfo info in paramInfo.Options)
                        {
                            if (info.Matches(box.Text))
                            {
                                results.Add(info);
                            }
                        }

                        if (paramInfo.Options.Count > 0 && results.Count == 0)
                        {
                            results.Add(new ParameterOptionInfo(null, "（无结果）"));
                        }
                        box.ItemsSource = results;
                    }
                    else
                    {
                        box.ItemsSource = paramInfo.Options;
                    }
                }

                //fix AutoSuggestBox.Text error when only one suggestion available
                if (args.Reason == AutoSuggestionBoxTextChangeReason.SuggestionChosen)
                {
                    if (!string.IsNullOrEmpty(box.Text))
                    {
                        string[] values = box.Text.Split('\t');
                        box.Text = values[0];
                    }
                }
            }
            AssembleCommand();
        }

        private void AssembleCommand()
        {
            string[] commandParamValueList = new string[_commandParamList.Count + 1];
            if (SelectorCommands.SelectedItem is CommandInfo info)
            {
                commandParamValueList[0] = info.Name.Trim();
            }
            for (int i = 0; i < _commandParamList.Count; i++)
            {
                Control control = _commandParamList[i];
                if (control is AutoSuggestBox box)
                {
                    commandParamValueList[i + 1] = box.Text?.Trim() ?? string.Empty;
                }
                else if (control is NumberBox numberBox)
                {
                    commandParamValueList[i + 1] = numberBox.Value.ToString();
                }
                else if (control is CalendarDatePicker cdPicker && cdPicker.Date != null)
                {
                    commandParamValueList[i + 1] = $"{cdPicker.Date.Value.Year} {cdPicker.Date.Value.Month} {cdPicker.Date.Value.Day}";
                }
                //else if (control is DatePicker datePicker)
                //{
                //    commandParamValueList[i] = $"{datePicker.Date.Year} {datePicker.Date.Month} {datePicker.Date.Day}";
                //}
                //else if (control is TimePicker timePicker)
                //{
                //    commandParamValueList[i] = $"{timePicker.Time.Hours} {timePicker.Time.Minutes}";
                //}
            }
            TextFinalCommand.Text = string.Join(' ', commandParamValueList).Trim();
        }

        private async void BtnSend_OnClick(object sender, RoutedEventArgs e)
        {
            string command = TextFinalCommand.Text.Trim();
            if (ServersListView.SelectedRanges.Count > 1) return;

            foreach (ServerItemData item in ServersListView.SelectedItems)
            {
                bool result = await SendCommand(item.ID, item.Name, command);
                if (!result)
                {
                    break;
                }
            }

            BtnClearHistory.Visibility = Visibility.Visible;
            ClearNavigation();
        }

        private async Task<bool> SendCommand(int serverID, string serverName, string command)
        {
            long serverType = 205;
            bool needConfirm = false;
            string commandName = string.Empty;
            if (SelectorCommands.SelectedItem is CommandInfo commandInfo)
            {
                serverType = commandInfo.ServerGroup;
                needConfirm = commandInfo.NeedConfirm;
                commandName = commandInfo.Title;
            }

            if (cbEnvSelect.SelectedItem is EnvInfo { Region: "ONE" })
            {
                needConfirm = false;
            }

            if (needConfirm)
            {
                if (cbEnvSelect.SelectedItem is EnvInfo envInfo)
                {
                    tbConfirmRegion.Text = $"环境和大区：{envInfo.DisplayName}";
                }
                tbConfirmServer.Text = $"服务器：{serverName} ({serverID})";
                if (string.IsNullOrEmpty(_defaultPlayerName))
                {
                    tbConfirmPlayer.Text = $"角色：{_defaultPlayerId}";
                }
                else
                {
                    tbConfirmPlayer.Text = $"角色：{_defaultPlayerName} ({_defaultPlayerId})";
                }

                tbConfirmCommandName.Text = $"命令：{commandName}";
                tbConfirmCommand.Text = command;

                ContentDialogResult result = await cdConfirm.ShowAsync(ContentDialogPlacement.Popup);
                if (result != ContentDialogResult.Primary)
                    return false;
            }

            int time = GetTimeUnix();
            string account = string.Empty;

            string info = $"{serverID}{command}{account}{time}{PrivateSignKey}";
            string sign = CalcMD5(info);

            JsonObject header = new JsonObject
            {
                {"OperationAccount", account},
                {"OperationTS", time},
                {"Sign", sign}
            };
            JsonObject req = new JsonObject
            {
                {"TargetServerID", serverID},
                {"TargetServerType", serverType},
                {"Command", command},
                {"Header", header}
            };
            req.ToJsonString();

            string postData = req.ToJsonString();
            try
            {
                HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                HttpResponseMessage response = await client.PostAsync(CommandUrl, new StringContent(postData, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
                string result = await response.Content.ReadAsStringAsync();
                Hashtable content = JsonSerializer.Deserialize<Hashtable>(result);

                int errorCode = 0;
                if (content.ContainsKey("ErrorCode"))
                {
                    errorCode = int.Parse(content["ErrorCode"].ToString());
                }

                string output = "";
                if (content.ContainsKey("ResponseStr"))
                {
                    output = content["ResponseStr"].ToString().TrimEnd('\n');
                }
                else
                {
                    output = "failed";
                }

                if (errorCode == 0)
                {
                    TopInfoBar.Message = $"命令 [{command}] 发送成功。";
                    TopInfoBar.Severity = InfoBarSeverity.Success;
                }
                else
                {
                    TopInfoBar.Message = $"命令 [{command}] 发送失败。(ErrorCode:{errorCode})";
                    TopInfoBar.Severity = InfoBarSeverity.Error;
                }

                //StandardUICommand commandCopy = new StandardUICommand(StandardUICommandKind.Copy);
                StandardUICommand commandCopy = new StandardUICommand();
                commandCopy.ExecuteRequested += CommandCopy_ExecuteRequested;

                _historyList.Add(new HistoryInfo
                {
                    Command = command,
                    Result = output,
                    ErrorCode = errorCode,
                    CommandCopy = commandCopy,
                });
                CommonData.AddHistoryInfo(command, output, errorCode);

                return errorCode == 0;
            }
            catch (Exception ex)
            {
                TopInfoBar.Message = $"命令 [{command}] 发送失败。({ex.Message.ReplaceLineEndings(string.Empty)})";
                TopInfoBar.Severity = InfoBarSeverity.Error;
            }
            return false;
        }

        private void CommandCopy_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (args.Parameter is HistoryInfo info)
            {
                DataPackage dataPackage = new DataPackage();
                dataPackage.RequestedOperation = DataPackageOperation.Copy;
                dataPackage.SetText(info.Result);
                Clipboard.SetContent(dataPackage);
            }
        }

        private string CalcMD5(string value)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(value);
            using MD5 md5 = MD5.Create();
            byte[] result = md5.ComputeHash(buffer);
            md5.Clear();
            string tmp = "";
            foreach (byte b in result)
            {
                tmp += b.ToString("X").PadLeft(2, '0');
            }
            tmp = tmp.ToLower();
            return tmp;
        }

        private int GetTimeUnix()
        {
            DateTime originalTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan ts = DateTime.Now.ToUniversalTime().Subtract(originalTime);
            return (int)ts.TotalSeconds;
        }

        private async void ShowErrorDialog(string title, string msg)
        {
            ContentDialog dialog = new ContentDialog
            {
                XamlRoot = Root.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = title,
                Content = msg,
                DefaultButton = ContentDialogButton.Close,
                CloseButtonText = "确定"
            };
            await dialog.ShowAsync();
        }

        private void HistoryListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HistoryListView.SelectedItems.Count == 1)
            {
                _navigationIndex = HistoryListView.SelectedIndex;
                HistoryInfo info = (HistoryInfo)HistoryListView.SelectedItem;
                TextFinalCommand.Text = info.Command;
                TryTraceBackHistoryCommand(info.Command);
            }
        }

        private void TextFinalCommand_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            switch (e.OriginalKey)
            {
                case VirtualKey.Enter:
                {
                    if (BtnSend.IsEnabled)
                    {
                        BtnSend_OnClick(null, null);
                    }
                    break;
                }
                //case VirtualKey.Up:
                //    HistoryNavigation(-1);
                //    break;
                //case VirtualKey.Down:
                //    HistoryNavigation(1);
                //    break;
                case VirtualKey.Escape:
                    ClearNavigation();
                    TextFinalCommand.Text = "";
                    break;
            }
        }

        private void HistoryNavigation(int indexShift)
        {
            if (_historyList.Count == 0)
            {
                _navigationIndex = -1;
                return;
            }

            int index = _navigationIndex + indexShift;

            if (index < 0)
            {
                index = _historyList.Count - 1;
            }
            else if (index > _historyList.Count - 1)
            {
                index = 0;
            }

            HistoryListView.SelectedIndex = index;

            if (index >= 0)
            {
                HistoryListView.ScrollIntoView(HistoryListView.SelectedItem);
            }
            else
            {
                HistoryListView.ScrollIntoView(HistoryListView.Items[0]);
            }
            //TryTraceBackHistoryCommand(_historyList[_navigationIndex].Command);
        }


        private void TryTraceBackHistoryCommand(string command)
        {
            try
            {
                if (string.IsNullOrEmpty(command)) return;
                string[] parameters = command.Split(' ');
                if (parameters.Length < 1) return;

                string commandName = parameters[0];
                List<CommandInfo> allCommands = _commandCategoryList[0].commands;
                int allCommandIndex = allCommands.FindIndex(info => info.Name == commandName);

                if (allCommandIndex >= 0)
                {
                    string category = allCommands[allCommandIndex].Category;
                    int categoryIndex = _commandCategoryList.FindIndex(info => info.Name == category);
                    if (categoryIndex >= 0)
                    {
                        int commandIndex = _commandCategoryList[categoryIndex].commands.FindIndex(info => info.Name == commandName);
                        if (commandIndex >= 0)
                        {
                            SelectorTypes.SelectedIndex = categoryIndex;
                            SelectorCommands.SelectedIndex = commandIndex;

                            if (GridParams.Children.Count == 0) return;
                            if (GridParams.Children.Count < parameters.Length)
                            {
                                int paramIndex = 1;
                                for (int controlIndex = 0; controlIndex < GridParams.Children.Count; controlIndex++, paramIndex++)
                                {
                                    if (GridParams.Children[controlIndex] is AutoSuggestBox box)
                                    {
                                        box.Text = parameters[paramIndex];
                                        box.IsSuggestionListOpen = false;
                                    }
                                    else if (GridParams.Children[controlIndex] is CalendarDatePicker cdPicker)
                                    {
                                        int year = int.Parse(parameters[paramIndex]);
                                        paramIndex++;
                                        int month = int.Parse(parameters[paramIndex]);
                                        paramIndex++;
                                        int day = int.Parse(parameters[paramIndex]);
                                        cdPicker.Date = new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);
                                    }
                                    else if (GridParams.Children[controlIndex] is NumberBox numberBox)
                                    {
                                        numberBox.Value = int.Parse(parameters[paramIndex]);
                                    }
                                    //else if (GridParams.Children[index] is DatePicker datePicker)
                                    //{
                                    //    int year = int.Parse(parameters[index + 1]);
                                    //    index++;
                                    //    int month = int.Parse(parameters[index + 1]);
                                    //    index++;
                                    //    int day = int.Parse(parameters[index + 1]);
                                    //    datePicker.Date = new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);
                                    //}
                                    //else if (GridParams.Children[index] is TimePicker timePicker)
                                    //{
                                    //    int hour = int.Parse(parameters[index + 1]);
                                    //    index++;
                                    //    int minute = int.Parse(parameters[index + 1]);
                                    //    timePicker.Time = new TimeSpan(hour, minute, 0);
                                    //}
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void ClearNavigation()
        {
            _navigationIndex = -1;
            HistoryListView.SelectedIndex = _navigationIndex;
            if (HistoryListView.Items.Count > 0)
            {
                HistoryListView.ScrollIntoView(HistoryListView.Items[^1]);
            }
        }

        private void BtnClearHistory_OnClick(object sender, RoutedEventArgs e)
        {
            _navigationIndex = -1;
            _historyList.Clear();
            BtnClearHistory.Visibility = Visibility.Collapsed;
            CommonData.ClearHistoryInfo();
        }

        private void FillPID_OnClick(SplitButton sender, SplitButtonClickEventArgs args)
        {
            if (_defaultPlayerId == 0) return;
            foreach (Control control in _commandParamList)
            {
                if (control is AutoSuggestBox { PlaceholderText: "pid" } box)
                {
                    box.Text = _defaultPlayerId.ToString();
                }
            }
        }

        private void BtnEditPlayerID_OnClick(object sender, RoutedEventArgs e)
        {
            tipEditPlayerID.IsOpen = true;
            if (_defaultPlayerId != 0)
            {
                tbEditPlayerID.Text = _defaultPlayerId.ToString();
            }
        }

        private void BtnEditPlayerIDConfirm_OnClick(TeachingTip sender, object args)
        {
            if (int.TryParse(tbEditPlayerID.Text, out int result))
            {
                _defaultPlayerId = result;
                _defaultPlayerName = "(手动输入)";
                BtnResetPlayer.Content = $"{_defaultPlayerId} - {_defaultPlayerName}";
            }
            else if (string.IsNullOrEmpty(tbEditPlayerID.Text))
            {
                _defaultPlayerId = 0;
                _defaultPlayerName = string.Empty;
                BtnResetPlayer.Content = "无角色";
            }
            tipEditPlayerID.IsOpen = false;

            if (_autoFillPID)
            {
                FillPID_OnClick(null, null);
            }
        }

        private async void BtnResetPlayer_OnClick(object sender, RoutedEventArgs e)
        {
            await LoadPlayerInfo();
        }

        private void BtnCopyPlayerID_OnClick(object sender, RoutedEventArgs e)
        {
            if (_defaultPlayerId > 0)
            {
                DataPackage dataPackage = new DataPackage();
                dataPackage.RequestedOperation = DataPackageOperation.Copy;
                dataPackage.SetText(_defaultPlayerId.ToString());
                Clipboard.SetContent(dataPackage);
            }
        }

        private void MenuFlyoutItemSelectAll_OnClick(object sender, RoutedEventArgs e)
        {
            ServersListView.SelectAll();
        }

        private async Task<List<ParameterOptionInfo>> LoadParameterOptions(CommandParameterInfo info)
        {
            List<ParameterOptionInfo> options = new List<ParameterOptionInfo>();
            if (!string.IsNullOrEmpty(_configPath))
            {
                FileInfo fileInfo = new FileInfo($"{_configPath}/{info.Source}.json");
                if (fileInfo.Exists)
                {
                    try
                    {
                        JArray o = JArray.Parse(await File.ReadAllTextAsync(fileInfo.FullName));
                        foreach (JToken line in o)
                        {
                            string id = (string)line.SelectToken(info.Range);
                            string name = (string)line.SelectToken(info.Desc);
                            string title = name != null && _languagesDict.TryGetValue(name, out string value) ? value : string.Empty;
                            options.Add(new ParameterOptionInfo(id, title));
                        }
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            }
            return options;
        }

        private void AutoFillPID_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleMenuFlyoutItem item)
            {
                _autoFillPID = item.IsChecked;
            }
        }

        private void HistoryListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            MenuFlyout flyout = new MenuFlyout();
            HistoryInfo data = (HistoryInfo)args.Item;

            MenuFlyoutItem item = new MenuFlyoutItem
            {
                Command = data.CommandCopy,
                CommandParameter = data, Text = "复制输出信息",
                Icon = new FontIcon
                {
                    FontFamily = new FontFamily("/Assets/Segoe Fluent Icons.ttf#Segoe Fluent Icons"),
                    Glyph = "\xe8c8",
                }
            };
            flyout.Items.Add(item);
            args.ItemContainer.ContextFlyout = flyout;
        }

        private async void OnEnvSelectChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox { SelectedItem: EnvInfo envInfo })
            {
                _curRegion = envInfo.Region;
                _curEnv = envInfo.Env;
                await ReqServerList();
            }
        }
    }
}
