using System.Collections.Generic;

namespace AspNetMvcApp.Models;

public class CheckoutViewModel
{
    public List<CartItemViewModel> SelectedItems { get; set; } = new List<CartItemViewModel>();
    public decimal Subtotal { get; set; }
    public string SelectedProductIds { get; set; } = string.Empty;

    // Billing details
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "COD"; // "COD", "BankTransfer"
    public string Notes { get; set; } = string.Empty;
}
