
namespace GMTool.Data;

enum ErrorReason
{
    None = 0,
    DirNotExists = 1,
    FileNotExists = 2,
    CasesNotMatch = 3,
}

internal class ResourceCheckData
{
    public string FileName { get; }
    public string ID { get; }
    public string Field { get; }
    public string Path { get; }
    public int Index { get; set; }
    public ErrorReason ErrorReason { get; set; }

    public ResourceCheckData(int index, string fileName, string id, string field, string path, ErrorReason errorReason)
    {
        Index = index;
        FileName = fileName;
        ID = id;
        Field = field;
        Path = path;
        ErrorReason = errorReason;
    }

    public string Reason => ErrorReason switch
    {
        ErrorReason.DirNotExists => "目录不存在",
        ErrorReason.FileNotExists => "文件不存在",
        ErrorReason.CasesNotMatch => "大小写错误",
        _ => string.Empty
    };
}

internal class CfgItemInfo
{
    public string FileName { get; }
    public string FilePath { get; }
    public bool CheckAll { get; }

    public override string ToString()
    {
        if (CheckAll)
        {
            return "全部";
        }
        else
        {
            return FileName;
        }
    }

    public CfgItemInfo()
    {
        CheckAll = true;
    }

    public CfgItemInfo(string fileName, string filePath)
    {
        CheckAll = false;
        FileName = fileName;
        FilePath = filePath;
    }
}