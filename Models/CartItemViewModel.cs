namespace AspNetMvcApp.Models;

public class CartItemViewModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public int Quantity { get; set; }

    public decimal TotalPrice => Price * Quantity;
}
