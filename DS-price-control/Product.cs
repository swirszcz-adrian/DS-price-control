namespace DS_price_control;

struct Product
{
    public Product(uint id, string name, float price, string description = "",  List<string>? tags = null)
    {
        this.Id = id;
        this.Name = name;
        this.Price = price;
        this.Description = description;
        this.tags = tags ?? new List<string>();
        for (int i = 0; i < this.tags.Count; i++)
        {
            this.tags[i].ToLower();
        }
    }

    public bool ContainsTag(List<string> Tags)
    {
        foreach (string Tag in Tags) 
        { 
            string tag = Tag.ToLower();
            if (this.tags.Contains(tag)) return true;
        }

        return false;
    }

    public bool ContainsTag(string Tag)
    {
        string tag = Tag.ToLower();
        if (this.tags.Contains(tag)) return true;

        return false;
    }

    public uint Id { get; }
    public string Name { get; set; }
    public float Price { get; set; }
    public string Description { get; set; }
    private List<string> tags;
    public List<string> Tags 
    { 
        get 
        {
            return this.tags;
        } 
        set 
        { 
            this.tags = value;
            for (int i = 0; i < this.tags.Count; i++)
            {
                this.tags[i].ToLower();
            }
        } 
    }
}
