namespace AspNetMvcApp.Models;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string ImagePath { get; set; } = string.Empty;

    public decimal TotalPrice => UnitPrice * Quantity;

    public Order? Order { get; set; }
    public Product? Product { get; set; }
}
