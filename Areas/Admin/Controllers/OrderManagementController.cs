using AspNetMvcApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspNetMvcApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrderManagementController : Controller
{
    private static readonly string[] AllowedStatuses = ["Pending", "Processing", "Completed", "Cancelled"];
    private readonly AppDbContext _context;

    public OrderManagementController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? page, string? searchTerm, string? status)
    {
        var pageNumber = page.GetValueOrDefault(1);
        if (pageNumber < 1) pageNumber = 1;

        const int pageSize = 10;
        var query = _context.Orders.Include(o => o.Items).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();
            query = query.Where(o =>
                o.OrderNumber.Contains(searchTerm) ||
                o.CustomerName.Contains(searchTerm) ||
                o.Phone.Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(status) && AllowedStatuses.Contains(status))
        {
            query = query.Where(o => o.Status == status);
        }

        var totalOrders = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalOrders / pageSize);
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = pageNumber;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalOrders = totalOrders;
        ViewBag.PageSize = pageSize;
        ViewBag.SearchTerm = searchTerm;
        ViewBag.Status = status;
        ViewBag.Statuses = AllowedStatuses;

        return View(orders);
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        ViewBag.Statuses = AllowedStatuses;
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? returnUrl = null)
    {
        if (!AllowedStatuses.Contains(status))
        {
            TempData["ErrorMessage"] = "Trạng thái đơn hàng không hợp lệ.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var order = await _context.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        order.Status = status;
        order.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã cập nhật đơn {order.OrderNumber}.";

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
