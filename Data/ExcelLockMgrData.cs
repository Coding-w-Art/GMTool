using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;

namespace GMTool.Data;

internal class LockFileInfo
{
    public string FileName { get; }
    public string LockOwner { get; }
    public string LockComment { get; }

    public LockFileInfo(string fileName, string lockOwner, string lockComment)
    {
        FileName = fileName;
        LockOwner = lockOwner;
        LockComment = lockComment;
    }
}

internal class ExcelFileInfo: IEquatable<ExcelFileInfo>, IComparable<ExcelFileInfo>, INotifyPropertyChanged
{
    public string FileName { get; }
    public string FullPath { get; }
    public bool LockedBySelf { get; private set; }
    public bool LockedByOthers { get; private set; }
    public string LockOwner { get; private set; }
    public string LockComment { get; private set; }
    public Visibility LockOtherVisibility => LockedByOthers ? Visibility.Visible : Visibility.Collapsed;
    public Visibility LockSelfVisibility => LockedBySelf ? Visibility.Visible : Visibility.Collapsed;
    public Visibility UnlockVisibility => LockedByOthers || LockedBySelf ? Visibility.Collapsed : Visibility.Visible;
    public bool Starred { get; private set; }
    public string LockDisplayInfo
    {
        get
        {
            if (LockedBySelf)
            {
                return $"已锁定: {LockComment ?? "(无备注信息)"}";
            }
            else if (LockedByOthers)
            {
                return $"已被 {LockOwner} 锁定: {LockComment ?? "(无备注信息)"}";
            }
            else
            {
                return string.Empty;
            }
        }
    }

    public Visibility StarredVisibility { get; private set; }
    public Visibility UnStarredVisibility { get; private set; }

    public void SetStarred(bool starred)
    {
        Starred = starred;
        (StarredVisibility, UnStarredVisibility) = Starred ? (Visibility.Visible, Visibility.Collapsed) : (Visibility.Collapsed, Visibility.Visible);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("StarredVisibility"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UnStarredVisibility"));
    }

    public int SorOrder
    {
        get
        {
            if (Starred) return 3;
            if (LockedBySelf) return 2;
            if (LockedByOthers) return 1;
            return 0;
        }
    }

    public ExcelFileInfo(string fileName, string fullPath, bool starred)
    {
        FileName = fileName;
        FullPath = fullPath;
        Starred = starred;
        (StarredVisibility, UnStarredVisibility) = Starred ? (Visibility.Visible, Visibility.Collapsed) : (Visibility.Collapsed, Visibility.Visible);
    }

    public void SetLockInfo(string username, LockFileInfo lockFileInfo)
    {
        LockedBySelf = lockFileInfo.LockOwner == username;
        LockedByOthers = lockFileInfo.LockOwner != username;
        LockOwner = lockFileInfo.LockOwner;
        LockComment = lockFileInfo.LockComment;
    }

    public int CompareTo(ExcelFileInfo other)
    {
        if (other == null) return -1;

        if (SorOrder == other.SorOrder)
        {
            return string.Compare(FileName, other.FileName, StringComparison.Ordinal);
        }
        else
        {
            return other.SorOrder - SorOrder;
        }
    }

    public bool Equals(ExcelFileInfo other)
    {
        return other != null && other.FullPath == FullPath;
    }

    public event PropertyChangedEventHandler PropertyChanged;
}

