namespace PriceControl;

struct Product
{
    public Product(uint id, string name, string description = "", List<string>? tags = null)
    {
        this.Id = id;
        this.Name = name;
        this.Description = description;
        this.Tags = tags ?? new List<string>();
        for (int i = 0; i < this.Tags.Count; i++)
        {
            this.Tags[i].ToLower().Trim().Replace(' ', '_');
        }
    }

    public override string ToString()
    {
        string nameStr = this.Name.Length < 10 ? this.Name : this.Name.Substring(0, 10 - 3) + "...";
        string descriptionStr = this.Description.Length < 20 ? this.Description : this.Description.Substring(0, 20 - 3) + "...";

        string tagsStr = string.Empty;
        foreach (string tag in this.Tags) { tagsStr += "<" + tag + "> "; }
        tagsStr = tagsStr.Length < 20 ? tagsStr : tagsStr.Substring(0, 20 - 3) + "...";

        string str = string.Format("{0,4} | {1, 10} | {2, 20} | {3, 20}\n", this.Id, nameStr, descriptionStr, tagsStr);
        return str;
    }

    public bool ContainsTag(List<string> tags)
    {
        foreach (string tag in tags)
        {
            if (this.Tags.Contains(tag.ToLower().Trim().Replace(' ', '_'))) { return true; }
        }

        return false;
    }

    public bool ContainsTag(string tag)
    {
        if (this.Tags.Contains(tag.ToLower().Trim().Replace(' ', '_'))) { return true; }

        return false;
    }

    public uint Id { get; }
    public string Name { get; }
    public string Description { get; }
    public List<string> Tags { get; }
}
