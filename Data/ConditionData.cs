namespace GMTool.Data;

internal class Condition
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string DisplayName => $"{Id}\t{Name}";
    public Condition(string id, string name)
    {
        Id = id;
        Name = name;
    }
}

internal class ReachMode
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string DisplayName => $"{Id}    {Name}";
    public ReachMode(int id, string name)
    {
        Id = id;
        Name = name;
    }
}