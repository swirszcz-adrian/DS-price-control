namespace PriceControl;

struct Product
{
    public Product(uint id, string name, float price, string description = "")
    {
        this.Id = id;
        this.Name = name;
        this.Price = price;
        this.Description = description;
    }

    public uint Id { get; }
    public string Name { get; set; }
    public float Price { get; set; }
    public string Description { get; set; }

}
