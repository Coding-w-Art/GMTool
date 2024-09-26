using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using OpenAI;

namespace GMTool.Data;

internal class ChatItemData : INotifyPropertyChanged
{
    public Role MessageRole {get; set; }

    private string _text;

    public string Text
    {
        get => _text;
        set => SetField(ref _text, value);
    }

    public HorizontalAlignment Alignment => MessageRole switch
    {
        Role.System => HorizontalAlignment.Left,
        Role.User => HorizontalAlignment.Right,
        _ => HorizontalAlignment.Center
    };

    public int DisplayFontSize => MessageRole == Role.Assistant ? 12 : 16;

    public CornerRadius Radius => MessageRole switch
    {
        Role.System => new CornerRadius(16, 16, 16, 0),
        Role.User => new CornerRadius(16, 16, 0, 16),
        _ => new CornerRadius(4, 4, 4, 4)
    };

    public ChatItemData(Role role, string text)
    {
        MessageRole = role;
        Text = text;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

internal enum OpenAIRequestType
{
    Chat,
    Image,
    Completion,
}