using System.Text;

namespace GMTool.Data;

internal class FeedbackMarkdown
{
    public string content { get; set; }

    public FeedbackMarkdown(string user, string category, string detail)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("#### GM工具反馈建议");
        sb.AppendLine($"<font color=\"comment\">类别：{category}</font>");
        sb.AppendLine($"<font color=\"comment\">来自：{user}</font>");
        sb.AppendLine(detail);
        content = sb.ToString();
    }
}

internal class FeedbackInfo
{
    public string msgtype { get; set; } = "markdown";
    public FeedbackMarkdown markdown { get; set; }

    public FeedbackInfo(string user, string category, string detail)
    {
        markdown = new FeedbackMarkdown(user, category, detail);
    }
}

