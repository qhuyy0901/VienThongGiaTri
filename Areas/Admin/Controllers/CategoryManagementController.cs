using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AspNetMvcApp.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetMvcApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoryManagementController : Controller
{
    private readonly AppDbContext _context;

    public CategoryManagementController(AppDbContext context)
    {
        _context = context;
    }

    // GET: Admin/CategoryManagement
    public async Task<IActionResult> Index(int? page, string? searchTerm)
    {
        int pageNumber = page ?? 1;
        if (pageNumber < 1) pageNumber = 1;
        int pageSize = 10;

        IQueryable<Category> query = _context.Categories.Include(c => c.Products);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.Trim();
            query = query.Where(c => c.Name.Contains(searchTerm));
        }

        var totalCategories = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCategories / pageSize);

        var categories = await query
            .OrderBy(c => c.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = pageNumber;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCategories = totalCategories;
        ViewBag.PageSize = pageSize;
        ViewBag.SearchTerm = searchTerm;

        return View(categories);
    }

    // GET: Admin/CategoryManagement/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Admin/CategoryManagement/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category category)
    {
        ModelState.Remove("Products");

        if (ModelState.IsValid)
        {
            // Check duplicate name
            var exists = await _context.Categories.AnyAsync(c => c.Name.ToLower() == category.Name.Trim().ToLower());
            if (exists)
            {
                ModelState.AddModelError("Name", "Tên danh mục này đã tồn tại.");
                return View(category);
            }

            category.Name = category.Name.Trim();
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm danh mục mới thành công!";
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    // GET: Admin/CategoryManagement/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound();
        }
        return View(category);
    }

    // POST: Admin/CategoryManagement/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category category)
    {
        if (id != category.Id)
        {
            return NotFound();
        }

        ModelState.Remove("Products");

        if (ModelState.IsValid)
        {
            try
            {
                // Check duplicate name excluding current one
                var exists = await _context.Categories.AnyAsync(c => c.Id != id && c.Name.ToLower() == category.Name.Trim().ToLower());
                if (exists)
                {
                    ModelState.AddModelError("Name", "Tên danh mục này đã tồn tại.");
                    return View(category);
                }

                var existingCategory = await _context.Categories.FindAsync(id);
                if (existingCategory == null) return NotFound();

                existingCategory.Name = category.Name.Trim();
                _context.Update(existingCategory);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Categories.AnyAsync(c => c.Id == id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    // POST: Admin/CategoryManagement/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (category != null)
        {
            // Note: Since DB is configured to Cascade delete, deleting this category will delete all related products.
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Xóa danh mục \"{category.Name}\" và {category.Products.Count} sản phẩm liên kết thành công!";
        }
        return RedirectToAction(nameof(Index));
    }
}
