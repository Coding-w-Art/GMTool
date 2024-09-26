using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GMTool.View;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;

namespace GMTool.Data;

internal class ToolGroupConfig
{
    public string Name { get; }
    public bool Privilege { get; }
    public bool Visible { get; }
    public List<ToolConfig> ToolConfigs { get; }

    public ToolGroupConfig(string name, bool privilege, bool visible, List<ToolConfig> toolConfigs)
    {
        Name = name;
        Privilege = privilege;
        Visible = visible;
        ToolConfigs = toolConfigs;
    }
}

internal class ToolConfig
{
    public string Name { get; }
    public string Icon { get; }
    public string PageName { get; }
    public bool Privilege { get; }
    public bool Visible { get; }
    public string Desc { get; }

    public ToolConfig(string name, string icon, string pageName, bool privilege, bool visible, string desc)
    {
        Name = name;
        Icon = icon;
        PageName = pageName;
        Privilege = privilege;
        Visible = visible;
        Desc = desc;
    }
}

internal class HistorySaveInfo
{
    public string Command { get; set; }
    public string Result { get; set; }
    public int ErrorCode { get; set; }

    public HistorySaveInfo(string command, string result, int errorCode)
    {
        Command = command;
        Result = result;
        ErrorCode = errorCode;
    }
}


internal static class CommonData
{
    public static ElementTheme DefaultTheme; // light, dark or default
    public static ElementTheme StartupTheme; // light or dark
    public static Grid MainWindowGrid { get; set; }

    private static bool _init;
    private static string _settingPath;
    private static Dictionary<string, string> _localSettings;

    public static Visibility PrivilegeVisibility = Visibility.Collapsed;

    public static string LocalApplicationDataPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Chef Squad\\GMTool");
    public static string IdenticonPath => $"{LocalApplicationDataPath}\\identicon_{AccountName}.png";

    public static string RepoRootPath { get; set; } = string.Empty;
    public static string RepoUrlPath { get; set; } = "Unknown Repo";
    public static string AccountName { get; set; }

    private const int HistoryInfoCount = 50;
    private static List<HistorySaveInfo> _historyInfo;

    public static string[] RecentUpdateInfo = {
        "2024/05/14",
        "· 发送关键命令添加二次确认弹窗",
    };

    public static List<ToolGroupConfig> ToolGroupConfigs = new()
    {
        new("通用工具", false, true, new List<ToolConfig>
            {
                new("GM 命令工具", "\xe756", "GMTool.GMCommandTool",
                    false, true, "使用命令修改游戏中的各种数据"),
                new("安装包查看工具", "\xed14", "GMTool.PackageViewer",
                    false, true, "查看最近的安装包记录和安装路径的二维码"),
                new("版本差异比较工具", "\xe89a", "GMTool.DiffViewer",
                    true, true, "比较版本之间的差异，查看修改的文件列表"),
                new("角色数据转移", "\xe748", "GMTool.PlayerTransfer",
                    true, false, "将一个角色的基础数据覆盖到另一个角色上"),
            }
        ),
        new("策划工具", false, true, new List<ToolConfig>
            {
                new("多语言查询工具", "\xf2b7", "GMTool.LanguageSearchTool",
                    true, true, "使用命令修改游戏中的各种数据"),
                new("资源配置检查工具", "\xe7c5", "GMTool.ResourceCheckTool",
                    true, true, "查询表格中配置的资源路径是否存在错误"),
                new("表格锁定工具", "\xe785", "GMTool.View.ExcelLockMgr",
                    true, true, "查看和设置 Excel 表格的锁定状态"),
                new("条件编辑器", "\xf8a6", "GMTool.ConditionEditor",
                    true, false, "生成条件配置参数或查看条件配置的释义"),
            }
        ),
        new("程序工具", true, true, new List<ToolConfig>
            {
                new("日志查看器", "\xEE65", "GMTool.LogcatTool",
                    true, true, "连接安卓设备并记录游戏运行日志"),
                new("时间转换工具", "\xec92", "GMTool.TimeConverter",
                    true, false, "Unix 时间戳转换为 UTC 时间"),
                new("HDR 颜色转换器", "\xe790", "GMTool.ColorConverter",
                    true, false, "将 HDR 颜色转换为 sRGB 颜色"),
            }
        ),
        new("其他", true, true, new List<ToolConfig>
            {
                new("OpenAI", "\xe8f2", "GMTool.ChatBot",
                    true, false, "使用 ChatGPT 和 DALL·E Image 功能"),
                new("反馈建议", "\xed15", "GMTool.View.FeedbackTool",
                    false, true, "如有 Bug 反馈及功能建议可在此填写并发送给相关人员"),
            }
        ),
    };

    public static async Task SaveLocalSetting(string key, string value, bool saveToFile = true)
    {
        await InitLocalSettings();
        _localSettings[key] = value;
        if (saveToFile)
        {
            await SaveLocalSettings();
        }
    }

    public static async Task<string> GetLocalSetting(string key)
    {
        await InitLocalSettings();
        if (_localSettings.TryGetValue(key, out string value))
        {
            return value;
        }
        return null;
    }

    private static async Task InitLocalSettings()
    {
        if (_init) return;
        _init = true;

        _localSettings = new Dictionary<string, string>();

        _settingPath = $"{LocalApplicationDataPath}\\localSettings";
        if (!Directory.Exists(LocalApplicationDataPath))
        {
            Directory.CreateDirectory(LocalApplicationDataPath);
        }

        if (File.Exists(_settingPath))
        {
            string[] settings = await File.ReadAllLinesAsync(_settingPath, new UTF8Encoding(false));
            foreach (string s in settings)
            {
                string[] kvp = s.Split('=');
                if (kvp.Length == 2)
                {
                    string k = kvp[0];
                    string v = kvp[1];
                    _localSettings.TryAdd(k, v);
                }
            }
        }
    }

    private static async Task SaveLocalSettings()
    {
        if (!_init) return;

        StringBuilder sb = new StringBuilder();
        foreach (KeyValuePair<string, string> p in _localSettings)
        {
            sb.AppendLine($"{p.Key}={p.Value}");
        }
        await File.WriteAllTextAsync(_settingPath, sb.ToString(), new UTF8Encoding(false));
    }

    public static void AddHistoryInfo(string command, string result, int errorCode)
    {
        if (_historyInfo != null)
        {
            _historyInfo.Add(new HistorySaveInfo(command, result, errorCode));
        }
    }

    public static void ClearHistoryInfo()
    {
        if (_historyInfo != null)
        {
            _historyInfo.Clear();
        }
    }

    private static async Task<T> LoadPrefs<T>(string fileName)
    {
        try
        {
            string path = $"{LocalApplicationDataPath}\\{fileName}";
            if (!File.Exists(path))
            {
                return default;
            }
            string content = await File.ReadAllTextAsync(path, new UTF8Encoding(false));
            if (!string.IsNullOrEmpty(content))
            {
                return JsonConvert.DeserializeObject<T>(content);
            }
            return default;

        }
        catch (Exception)
        {
            return default;
        }
    }

    private static async Task SavePrefs(string fileName, object content)
    {
        try
        {
            if (!Directory.Exists(LocalApplicationDataPath))
            {
                Directory.CreateDirectory(LocalApplicationDataPath);
            }
            string path = $"{LocalApplicationDataPath}\\{fileName}";
            string result = JsonConvert.SerializeObject(content);
            if (!string.IsNullOrEmpty(result))
            {
                await File.WriteAllTextAsync(path, result, new UTF8Encoding(false));
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public static async Task<List<HistorySaveInfo>> LoadCommandHistory()
    {
        _historyInfo = await LoadPrefs<List<HistorySaveInfo>>("commandHistory");
        if (_historyInfo != null && _historyInfo.Count > HistoryInfoCount)
        {
            _historyInfo = _historyInfo.TakeLast(HistoryInfoCount).ToList();
        }
        return _historyInfo;
    }

    public static async Task SaveCommandHistory()
    {
        if (_historyInfo == null) return;
        if (_historyInfo.Count > HistoryInfoCount)
        {
            _historyInfo = _historyInfo.TakeLast(HistoryInfoCount).ToList();
        }
        await SavePrefs("commandHistory", _historyInfo);
    }

    public static async Task<List<string>> LoadStarredExcel()
    {
        return await LoadPrefs<List<string>>("starredExcels");
    }

    public static async Task SaveStarredExcel()
    {
        if (ExcelLockMgr.StarredList == null) return;
        await SavePrefs("starredExcels", ExcelLockMgr.StarredList);
    }
}