using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AspNetMvcApp.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;

namespace AspNetMvcApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductManagementController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public ProductManagementController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // GET: Admin/ProductManagement
    public async Task<IActionResult> Index(int? page, string? searchTerm)
    {
        int pageNumber = page ?? 1;
        if (pageNumber < 1) pageNumber = 1;
        int pageSize = 10;

        IQueryable<Product> query = _context.Products.Include(p => p.Category);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.Trim();
            query = query.Where(p => p.Name.Contains(searchTerm) || (p.Category != null && p.Category.Name.Contains(searchTerm)));
        }

        var totalProducts = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

        var products = await query
            .OrderBy(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = pageNumber;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalProducts = totalProducts;
        ViewBag.PageSize = pageSize;
        ViewBag.SearchTerm = searchTerm;

        return View(products);
    }

    // GET: Admin/ProductManagement/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
        return View();
    }

    // POST: Admin/ProductManagement/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product, List<IFormFile> uploadImages, string mainImage)
    {
        // Remove navigation properties from validation
        ModelState.Remove("Category");
        ModelState.Remove("uploadImages");
        ModelState.Remove("mainImage");

        if (ModelState.IsValid)
        {
            await ProcessUploadedImages(product, uploadImages, mainImage);

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
        return View(product);
    }

    // GET: Admin/ProductManagement/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _context.Products.Include(p => p.ProductImages).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return NotFound();
        }

        ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
        return View(product);
    }

    // POST: Admin/ProductManagement/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product product, List<IFormFile> uploadImages, string mainImage, List<string> deletedImages)
    {
        if (id != product.Id)
        {
            return NotFound();
        }

        // Remove navigation properties from validation
        ModelState.Remove("Category");
        ModelState.Remove("uploadImages");
        ModelState.Remove("mainImage");
        ModelState.Remove("deletedImages");

        if (ModelState.IsValid)
        {
            try
            {
                // We need to handle image updates
                // First get the existing product to update its images
                var existingProduct = await _context.Products.Include(p => p.ProductImages).FirstOrDefaultAsync(p => p.Id == id);
                if (existingProduct == null) return NotFound();

                // Update basic info
                existingProduct.Name = product.Name;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.Price = product.Price;
                existingProduct.Rating = product.Rating;
                existingProduct.StockStatus = product.StockStatus;
                existingProduct.Description = product.Description;
                existingProduct.IsFeatured = product.IsFeatured;

                // Handle deleted images first
                if (deletedImages != null && deletedImages.Count > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "product", "imges");
                    foreach (var imgName in deletedImages)
                    {
                        if (string.IsNullOrEmpty(imgName)) continue;

                        // Check if it's the main image
                        if (existingProduct.ImagePath == imgName)
                        {
                            existingProduct.ImagePath = string.Empty;
                        }
                        else
                        {
                            // Check if it's in the ProductImages collection
                            var dbImg = existingProduct.ProductImages.FirstOrDefault(pi => pi.ImagePath == imgName);
                            if (dbImg != null)
                            {
                                existingProduct.ProductImages.Remove(dbImg);
                                _context.ProductImages.Remove(dbImg);
                            }
                        }

                        // Try to physically delete the file from product/imges
                        try
                        {
                            string filePath = Path.Combine(uploadsFolder, imgName);
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                        }
                        catch (Exception)
                        {
                            // Keep going if physical deletion fails
                        }

                        // Also try to physically delete from old imager folder just in case
                        try
                        {
                            string oldFilePath = Path.Combine(_env.WebRootPath, "imager", imgName);
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }
                        catch (Exception)
                        {
                            // Keep going
                        }
                    }
                }

                // Process new images and main image
                await ProcessUploadedImages(existingProduct, uploadImages, mainImage);

                _context.Update(existingProduct);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Products.AnyAsync(p => p.Id == id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", product.CategoryId);
        return View(product);
    }

    private async Task ProcessUploadedImages(Product product, List<IFormFile> uploadImages, string mainImage)
    {
        string uploadsFolder = Path.Combine(_env.WebRootPath, "product", "imges");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        bool mainImageUpdated = false;

        if (uploadImages != null && uploadImages.Count > 0)
        {
            foreach (var file in uploadImages)
            {
                if (file.Length > 0)
                {
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    // Check if this is the chosen main image
                    if (!string.IsNullOrEmpty(mainImage) && file.FileName == mainImage)
                    {
                        product.ImagePath = uniqueFileName;
                        mainImageUpdated = true;
                    }
                    else
                    {
                        product.ProductImages.Add(new ProductImage
                        {
                            ImagePath = uniqueFileName
                        });
                    }
                }
            }
        }

        // If the user selected an existing image as the main image in the Edit view
        if (!mainImageUpdated && !string.IsNullOrEmpty(mainImage))
        {
            // If the selected mainImage is already an existing image filename
            if (product.ImagePath != mainImage)
            {
                // We need to swap them or simply set it
                var existingImage = product.ProductImages.FirstOrDefault(pi => pi.ImagePath == mainImage);
                if (existingImage != null)
                {
                    // Move current main image to ProductImages
                    if (!string.IsNullOrEmpty(product.ImagePath))
                    {
                        product.ProductImages.Add(new ProductImage { ImagePath = product.ImagePath });
                    }
                    
                    // Set new main image
                    product.ImagePath = mainImage;
                    product.ProductImages.Remove(existingImage);
                }
            }
        }
        else if (string.IsNullOrEmpty(product.ImagePath) && product.ProductImages.Any())
        {
            // Fallback if no main image selected but there are images
            var firstImg = product.ProductImages.First();
            product.ImagePath = firstImg.ImagePath;
            product.ProductImages.Remove(firstImg);
        }
    }

    // POST: Admin/ProductManagement/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";
        }
        return RedirectToAction(nameof(Index));
    }
}
