using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AspNetMvcApp.Models;
using System.Text.Json;

namespace AspNetMvcApp.Controllers;

[ApiController]
[Route("api/cart")]
public class CartApiController : ControllerBase
{
    private readonly AppDbContext _context;
    private const string COOKIE_NAME = "cart";
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public CartApiController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy danh sách sản phẩm và thông tin tổng quan của giỏ hàng hiện tại.
    /// </summary>
    /// <returns>Danh sách sản phẩm trong giỏ hàng và tổng tiền.</returns>
    [HttpGet]
    public async Task<IActionResult> GetCart()
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

        var totalCount = cookieItems.Sum(i => i.Quantity);
        var subtotal = cartViewModel.Sum(i => i.TotalPrice);

        return Ok(new
        {
            items = cartViewModel,
            totalCount = totalCount,
            subtotal = subtotal,
            subtotalVnd = (subtotal * 25000).ToString("#,##0")
        });
    }

    /// <summary>
    /// Model yêu cầu thêm hoặc cập nhật sản phẩm trong giỏ hàng.
    /// </summary>
    public class CartItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    /// <summary>
    /// Thêm sản phẩm vào giỏ hàng.
    /// </summary>
    /// <param name="request">Thông tin sản phẩm và số lượng cần thêm.</param>
    /// <returns>Trạng thái kết quả và tổng số lượng sản phẩm mới trong giỏ hàng.</returns>
    [HttpPost]
    public async Task<IActionResult> AddToCart([FromBody] CartItemRequest request)
    {
        if (request.Quantity <= 0)
        {
            return BadRequest(new { success = false, message = "Số lượng không hợp lệ." });
        }

        var product = await _context.Products.FindAsync(request.ProductId);
        if (product == null)
        {
            return NotFound(new { success = false, message = "Sản phẩm không tồn tại." });
        }

        var cartItems = GetCartFromCookie();
        var existingItem = cartItems.FirstOrDefault(i => i.ProductId == request.ProductId);

        if (existingItem != null)
        {
            existingItem.Quantity += request.Quantity;
        }
        else
        {
            cartItems.Add(new CookieCartItem
            {
                ProductId = request.ProductId,
                Quantity = request.Quantity
            });
        }

        SaveCartToCookie(cartItems);

        int totalCount = cartItems.Sum(i => i.Quantity);
        return Ok(new { success = true, message = $"Đã thêm {request.Quantity} x \"{product.Name}\" vào giỏ hàng thành công!", totalCount });
    }

    /// <summary>
    /// Cập nhật số lượng sản phẩm cụ thể trong giỏ hàng.
    /// </summary>
    /// <param name="request">Thông tin sản phẩm và số lượng mới.</param>
    /// <returns>Tổng tiền cập nhật của sản phẩm đó và của toàn bộ giỏ hàng.</returns>
    [HttpPut]
    public async Task<IActionResult> UpdateQuantity([FromBody] CartItemRequest request)
    {
        if (request.Quantity <= 0)
        {
            return BadRequest(new { success = false, message = "Số lượng phải lớn hơn hoặc bằng 1." });
        }

        var product = await _context.Products.FindAsync(request.ProductId);
        if (product == null)
        {
            return NotFound(new { success = false, message = "Sản phẩm không tồn tại trong hệ thống." });
        }

        var cartItems = GetCartFromCookie();
        var item = cartItems.FirstOrDefault(i => i.ProductId == request.ProductId);

        if (item == null)
        {
            return NotFound(new { success = false, message = "Sản phẩm không có trong giỏ hàng." });
        }

        item.Quantity = request.Quantity;
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
        decimal itemTotal = product.Price * request.Quantity;

        return Ok(new
        {
            success = true,
            newQuantity = request.Quantity,
            itemTotalVnd = (itemTotal * 25000).ToString("#,##0"),
            subtotalVnd = (subtotal * 25000).ToString("#,##0"),
            totalCount
        });
    }

    /// <summary>
    /// Xóa một sản phẩm cụ thể khỏi giỏ hàng.
    /// </summary>
    /// <param name="productId">ID sản phẩm cần xóa.</param>
    /// <returns>Trạng thái kết quả và tổng tiền còn lại của giỏ hàng.</returns>
    [HttpDelete("{productId}")]
    public async Task<IActionResult> RemoveItem(int productId)
    {
        var cartItems = GetCartFromCookie();
        var itemToRemove = cartItems.FirstOrDefault(i => i.ProductId == productId);

        if (itemToRemove == null)
        {
            return NotFound(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng." });
        }

        cartItems.Remove(itemToRemove);
        SaveCartToCookie(cartItems);

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

        return Ok(new
        {
            success = true,
            subtotalVnd = (subtotal * 25000).ToString("#,##0"),
            totalCount
        });
    }

    /// <summary>
    /// Xóa toàn bộ sản phẩm và làm trống giỏ hàng.
    /// </summary>
    /// <returns>Trạng thái kết quả.</returns>
    [HttpDelete]
    public IActionResult Clear()
    {
        Response.Cookies.Delete(COOKIE_NAME);
        return Ok(new { success = true });
    }

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
            Response.Cookies.Delete(COOKIE_NAME);
            return new List<CookieCartItem>();
        }
    }

    private void SaveCartToCookie(List<CookieCartItem> items)
    {
        var json = JsonSerializer.Serialize(items, _jsonOptions);
        var options = new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = false
        };

        Response.Cookies.Append(COOKIE_NAME, json, options);
    }
}
