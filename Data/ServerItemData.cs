using System.Collections.Generic;
using Microsoft.UI.Xaml;
using ICommand = System.Windows.Input.ICommand;

namespace GMTool.Data;

internal class EnvInfo
{
    public string Region { get; set; }
    public string Env { get; set; }
    public string DisplayName => $"{Region} - {Env}";

    public EnvInfo(string region, string env)
    {
        Region = region;
        Env = env;
    }
}

internal class ServerReqInfo
{
    public int Channel { get; set; }
    public int Privilege { get; set; }
    public bool DisgardNotOpened { get; set; }
    public string Env { get; set; }
    public string ClientVersion { get; set; }
    public bool NeedGame { get; set; }
}


internal class ServerItemData
{
    public string Name { get; set; }
    public int ID { get; set; }
    public int Group { get; set; }
    public int GeoID { get; set; }
    public long OpenTime { get; set; }
    public string Env { get; set; }

    public string DisplayName => $"{ID}        {Name}";
}

internal class HistoryInfo
{
    public string Command { get; set; }
    public string Result { get; set; }
    public int ErrorCode { get; set; }

    public ICommand CommandCopy { get; set; }

    //public string Input => $"cmd req > {Command}";
    //public string Output => $"cmd rsp > {Result}";
    public string Icon => ErrorCode == 0 ? "\xf13e" : "\xf13d";

    public Visibility SuccessVisibility => ErrorCode == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility FailedVisibility => ErrorCode == 0 ? Visibility.Collapsed : Visibility.Visible;
}

internal class ParameterOptionInfo
{
    public string Value { get; set; }
    public string Title { get; set; }
    public string DisplayName => string.IsNullOrEmpty(Title) ? Value : $"{Value}  -  {Title}";

    public ParameterOptionInfo(string value, string title)
    {
        Value = value ?? string.Empty;
        Title = title ?? string.Empty;
    }

    public bool Matches(string query)
    {
        return Value.Contains(query) || Title.Contains(query);
    }
}

internal class CommandParameterInfo
{
    public string Name { get; set; }
    public string Title { get; set; }
    public string Source { get; set; }
    public string Range { get; set; }
    public string Desc { get; set; }
    public string DefaultValue { get; set; }
    public bool HasOptions { get; set; }
    public bool ParameterLoaded { get; private set; }
    public List<ParameterOptionInfo> Options { get; private set; }

    public bool IsPID => Name == "pid";

    public CommandParameterInfo(string name, string title)
    {
        Name = name;
        Title = title;
        Options = new List<ParameterOptionInfo>();
    }

    public void SetParameterOption(List<ParameterOptionInfo> parameterOptions)
    {
        Options = parameterOptions;
        ParameterLoaded = true;
    }

    public void ClearParameterOption()
    {
        Options.Clear();
        ParameterLoaded = false;
    }
}

internal class CommandInfo
{
    public string Name { get; set; }
    public string Title { get; set; }
    public string Category { get; set; }
    public bool NeedConfirm { get; set; }
    public long ServerGroup { get; set; }
    public List<CommandParameterInfo> parameters { get; set; }

    public string DisplayName => $"{Title}  {Name}";
    public string FullDisplayName => $"{Category} | {Title}  {Name}";

    public CommandInfo()
    {
        parameters = new List<CommandParameterInfo>();
    }
}

internal class CommandCategoryInfo
{
    public string Name { get; set; }
    public string Title { get; set; }
    public List<CommandInfo> commands { get; set; }

    public string DisplayName => $"{Title}  {Name}";

    public CommandCategoryInfo(string name, string title)
    {
        Name = name;
        Title = title;
        commands = new List<CommandInfo>();
    }
}