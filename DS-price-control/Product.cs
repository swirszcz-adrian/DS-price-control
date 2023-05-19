namespace PriceControl;

struct Product
{
    public Product(uint id, string name, string description = "", List<string>? tags = null)
    {
        this.Id = id;
        this.Name = name;
        this.Description = description;
        this._Tags = tags ?? new List<string>();
        for (int i = 0; i < this._Tags.Count; i++)
        {
            this._Tags[i].ToLower().Trim().Replace(' ', '_');
        }
    }

    public bool ContainsTag(List<string> tags)
    {
        foreach (string tag in tags)
        {
            if (this._Tags.Contains(tag.ToLower().Trim().Replace(' ', '_'))) { return true; }
        }

        return false;
    }

    public bool ContainsTag(string tag)
    {
        if (this._Tags.Contains(tag.ToLower().Trim().Replace(' ', '_'))) { return true; }

        return false;
    }

    public uint Id { get; }
    public string Name { get; set; }
    public string Description { get; set; }
    private List<string> _Tags;
    public List<string> Tags
    {
        get { return this._Tags; }
        set
        {
            this._Tags = value;
            for (int i = 0; i < this._Tags.Count; i++)
            {
                this._Tags[i].ToLower().Trim().Replace(' ', '_');
            }
        }
    }

}
