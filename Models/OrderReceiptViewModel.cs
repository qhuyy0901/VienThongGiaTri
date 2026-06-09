using System;
using System.Collections.Generic;

namespace AspNetMvcApp.Models;

public class OrderReceiptViewModel
{
    public string OrderId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    
    public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
}
