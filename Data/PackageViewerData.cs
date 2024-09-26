using System;
using Microsoft.UI.Xaml.Media.Imaging;

namespace GMTool.Data;

internal class PackageItem : IComparable
{
    public int ID { get; set; }
    public string DateTime { get; set; }
    public string DisplayName => $"# {ID}\t\t{DateTime}";
    public int PackageType { get; private set; }
    public bool ExtReady { get; private set; }

    public string Size { get; private set; }

    public string Repo { get; private set; }

    public string Region { get; private set; } = string.Empty;

    public BitmapImage Image { get; private set; }

    public PackageItem(int id, int packageType, string dateTime)
    {
        ID = id;
        PackageType = packageType;
        DateTime = dateTime;
    }

    public void SetExtInfo(long size, string repo, string region, BitmapImage image)
    {
        ExtReady = true;

        if (size > 1200 * 1024 * 1024)
        {
            Size = $"{size / 1024d / 1024d / 1024d:F2} GB (Full)";
        }
        else
        {
            Size = $"{size / 1024d / 1024d:F2} MB (Mini)";
        }

        Repo = repo;
        Region = string.IsNullOrEmpty(region) ? "N/A" : region;
        Image = image;
    }

    public int CompareTo(object obj)
    {
        if (obj is PackageItem item)
        {
            return item.ID - ID;
        }
        return 0;
    }
}