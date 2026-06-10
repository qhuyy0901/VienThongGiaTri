using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AspNetMvcApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AspNetMvcApp.Controllers;

public class CartController : Controller
{
    private readonly AppDbContext _context;
    private const string COOKIE_NAME = "cart";
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public CartController(AppDbContext context)
    {
        _context = context;
    }

    // 1. Display Cart Page
    public async Task<IActionResult> Index()
    {
        var cookieItems = GetCartFromCookie();
        var cartViewModel = new List<CartItemViewModel>();

        if (cookieItems.Any())
        {
            var productIds = cookieItems.Select(i => i.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            foreach (var item in cookieItems)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product != null)
                {
                    cartViewModel.Add(new CartItemViewModel
                    {
                        ProductId = product.Id,
                        Name = product.Name,
                        Price = product.Price,
                        ImagePath = product.ImagePath,
                        Quantity = item.Quantity
                    });
                }
            }
        }

        return View(cartViewModel);
    }

    // 2. Add Item to Cart (AJAX API)
    [HttpPost]
    public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
    {
        if (quantity <= 0)
        {
            return Json(new { success = false, message = "Số lượng không hợp lệ." });
        }

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return Json(new { success = false, message = "Sản phẩm không tồn tại." });
        }

        var cartItems = GetCartFromCookie();
        var existingItem = cartItems.FirstOrDefault(i => i.ProductId == productId);

        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            cartItems.Add(new CookieCartItem
            {
                ProductId = productId,
                Quantity = quantity
            });
        }

        SaveCartToCookie(cartItems);

        int totalCount = cartItems.Sum(i => i.Quantity);
        return Json(new { success = true, message = $"Đã thêm {quantity} x \"{product.Name}\" vào giỏ hàng thành công!", totalCount });
    }

    // 3. Update Quantity (AJAX API)
    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
    {
        if (quantity <= 0)
        {
            return Json(new { success = false, message = "Số lượng phải lớn hơn hoặc bằng 1." });
        }

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return Json(new { success = false, message = "Sản phẩm không tồn tại trong hệ thống." });
        }

        var cartItems = GetCartFromCookie();
        var item = cartItems.FirstOrDefault(i => i.ProductId == productId);

        if (item == null)
        {
            return Json(new { success = false, message = "Sản phẩm không có trong giỏ hàng." });
        }

        item.Quantity = quantity;
        SaveCartToCookie(cartItems);

        // Calculate totals
        var productIds = cartItems.Select(i => i.ProductId).ToList();
        var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

        decimal subtotal = 0;
        foreach (var ci in cartItems)
        {
            var p = products.FirstOrDefault(prod => prod.Id == ci.ProductId);
            if (p != null)
            {
                subtotal += p.Price * ci.Quantity;
            }
        }

        int totalCount = cartItems.Sum(i => i.Quantity);
        decimal itemTotal = product.Price * quantity;

        return Json(new { 
            success = true, 
            newQuantity = quantity, 
            itemTotalVnd = (itemTotal * 25000).ToString("#,##0"), 
            subtotalVnd = (subtotal * 25000).ToString("#,##0"), 
            totalCount 
        });
    }

    // 4. Remove Item from Cart (AJAX API)
    [HttpPost]
    public async Task<IActionResult> RemoveItem(int productId)
    {
        var cartItems = GetCartFromCookie();
        var itemToRemove = cartItems.FirstOrDefault(i => i.ProductId == productId);

        if (itemToRemove != null)
        {
            cartItems.Remove(itemToRemove);
            SaveCartToCookie(cartItems);
        }

        // Calculate new subtotal
        var productIds = cartItems.Select(i => i.ProductId).ToList();
        var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

        decimal subtotal = 0;
        foreach (var ci in cartItems)
        {
            var p = products.FirstOrDefault(prod => prod.Id == ci.ProductId);
            if (p != null)
            {
                subtotal += p.Price * ci.Quantity;
            }
        }

        int totalCount = cartItems.Sum(i => i.Quantity);

        return Json(new { 
            success = true, 
            subtotalVnd = (subtotal * 25000).ToString("#,##0"), 
            totalCount 
        });
    }

    // 5. Clear All Items (AJAX/Redirect)
    [HttpPost]
    public IActionResult Clear()
    {
        Response.Cookies.Delete(COOKIE_NAME);
        return Json(new { success = true });
    }

    // 6. Helper: Read Cart from Cookie
    private List<CookieCartItem> GetCartFromCookie()
    {
        var cookie = Request.Cookies[COOKIE_NAME];
        if (string.IsNullOrEmpty(cookie))
        {
            return new List<CookieCartItem>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<CookieCartItem>>(cookie, _jsonOptions) ?? new List<CookieCartItem>();
        }
        catch
        {
            // If cookie is corrupted, delete it and return new list
            Response.Cookies.Delete(COOKIE_NAME);
            return new List<CookieCartItem>();
        }
    }

    // 7. Helper: Save Cart to Cookie
    private void SaveCartToCookie(List<CookieCartItem> items)
    {
        var json = JsonSerializer.Serialize(items, _jsonOptions);
        var options = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = false // false for local HTTP, can set to true on HTTPS production
        };

        Response.Cookies.Append(COOKIE_NAME, json, options);
    }

    // 8. Checkout Form (GET)
    public async Task<IActionResult> Checkout(string selectedProductIds)
    {
        if (string.IsNullOrEmpty(selectedProductIds))
        {
            return RedirectToAction(nameof(Index));
        }

        var ids = selectedProductIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToList();

        if (!ids.Any())
        {
            return RedirectToAction(nameof(Index));
        }

        var cookieItems = GetCartFromCookie();
        var selectedCookieItems = cookieItems.Where(i => ids.Contains(i.ProductId)).ToList();

        if (!selectedCookieItems.Any())
        {
            return RedirectToAction(nameof(Index));
        }

        var products = await _context.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
        var selectedViewModels = new List<CartItemViewModel>();

        decimal subtotal = 0;
        foreach (var item in selectedCookieItems)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product != null)
            {
                var vm = new CartItemViewModel
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    ImagePath = product.ImagePath,
                    Quantity = item.Quantity
                };
                selectedViewModels.Add(vm);
                subtotal += vm.TotalPrice;
            }
        }

        var model = new CheckoutViewModel
        {
            SelectedItems = selectedViewModels,
            Subtotal = subtotal,
            SelectedProductIds = selectedProductIds
        };

        return View(model);
    }

    // 9. Process Checkout (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model)
    {
        // Ignore SelectedItems validation from binding as it's populated on get
        ModelState.Remove("SelectedItems");

        if (ModelState.IsValid)
        {
            var ids = model.SelectedProductIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();

            var cookieItems = GetCartFromCookie();
            var purchasedCookieItems = cookieItems.Where(i => ids.Contains(i.ProductId)).ToList();

            if (!purchasedCookieItems.Any())
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm cần thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            var products = await _context.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
            var purchasedItems = new List<CartItemViewModel>();
            decimal totalAmount = 0;

            foreach (var item in purchasedCookieItems)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product != null)
                {
                    var vm = new CartItemViewModel
                    {
                        ProductId = product.Id,
                        Name = product.Name,
                        Price = product.Price,
                        ImagePath = product.ImagePath,
                        Quantity = item.Quantity
                    };
                    purchasedItems.Add(vm);
                    totalAmount += vm.TotalPrice;
                }
            }

            var orderNumber = "GT-" + DateTime.Now.ToString("yyMMddHHmmss") + "-" + Random.Shared.Next(100, 999);
            var paymentMethod = model.PaymentMethod == "BankTransfer" ? "Chuyển khoản ngân hàng" : "Thanh toán COD (khi nhận hàng)";

            var order = new Order
            {
                OrderNumber = orderNumber,
                UserId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null,
                CustomerName = model.FullName.Trim(),
                Phone = model.Phone.Trim(),
                Address = model.Address.Trim(),
                PaymentMethod = paymentMethod,
                Notes = model.Notes?.Trim() ?? string.Empty,
                TotalAmount = totalAmount,
                Status = "Pending",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Items = purchasedItems.Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.Name,
                    UnitPrice = item.Price,
                    Quantity = item.Quantity,
                    ImagePath = item.ImagePath
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Create Order Receipt
            var receipt = new OrderReceiptViewModel
            {
                OrderId = orderNumber,
                FullName = model.FullName.Trim(),
                Phone = model.Phone.Trim(),
                Address = model.Address.Trim(),
                PaymentMethod = paymentMethod,
                Notes = model.Notes?.Trim() ?? string.Empty,
                Items = purchasedItems,
                TotalAmount = totalAmount,
                OrderDate = DateTime.Now
            };

            // Remove purchased items from Cookie Cart!
            var updatedCookieItems = cookieItems.Where(i => !ids.Contains(i.ProductId)).ToList();
            SaveCartToCookie(updatedCookieItems);

            // Serialize receipt to pass to OrderSuccess view
            var json = JsonSerializer.Serialize(receipt, _jsonOptions);
            return RedirectToAction(nameof(OrderSuccess), new { orderJson = json });
        }

        // Re-populate selected items if form has errors
        var errorIds = model.SelectedProductIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToList();

        var cookieItemsForRe = GetCartFromCookie();
        var reSelectedItems = cookieItemsForRe.Where(i => errorIds.Contains(i.ProductId)).ToList();
        var reProducts = await _context.Products.Where(p => errorIds.Contains(p.Id)).ToListAsync();
        
        model.SelectedItems = new List<CartItemViewModel>();
        decimal reSubtotal = 0;
        foreach (var item in reSelectedItems)
        {
            var product = reProducts.FirstOrDefault(p => p.Id == item.ProductId);
            if (product != null)
            {
                var vm = new CartItemViewModel
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    ImagePath = product.ImagePath,
                    Quantity = item.Quantity
                };
                model.SelectedItems.Add(vm);
                reSubtotal += vm.TotalPrice;
            }
        }
        model.Subtotal = reSubtotal;

        return View(model);
    }

    // 10. Display Order Success Page (GET)
    public IActionResult OrderSuccess(string orderJson)
    {
        if (string.IsNullOrEmpty(orderJson))
        {
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var receipt = JsonSerializer.Deserialize<OrderReceiptViewModel>(orderJson, _jsonOptions);
            if (receipt == null)
            {
                return RedirectToAction(nameof(Index));
            }
            return View(receipt);
        }
        catch
        {
            return RedirectToAction(nameof(Index));
        }
    }
}
