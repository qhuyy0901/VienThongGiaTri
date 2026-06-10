using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AspNetMvcApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AspNetMvcApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AccountManagementController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AccountManagementController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // GET: Admin/AccountManagement
    public async Task<IActionResult> Index(int? page, string? searchTerm)
    {
        int pageNumber = page ?? 1;
        if (pageNumber < 1) pageNumber = 1;
        int pageSize = 10;

        IQueryable<AppUser> query = _userManager.Users;

        if (!string.IsNullOrEmpty(searchTerm))
        {
            searchTerm = searchTerm.Trim();
            query = query.Where(u => (u.Email != null && u.Email.Contains(searchTerm)) || (u.UserName != null && u.UserName.Contains(searchTerm)));
        }

        var totalUsers = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalUsers / pageSize);

        var usersList = await query
            .OrderBy(u => u.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var viewModels = new List<UserViewModel>();
        foreach (var user in usersList)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var isLocked = await _userManager.IsLockedOutAsync(user);
            
            viewModels.Add(new UserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Username = user.UserName ?? string.Empty,
                Roles = roles.ToList(),
                IsLockedOut = isLocked
            });
        }

        ViewBag.CurrentPage = pageNumber;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalUsers = totalUsers;
        ViewBag.PageSize = pageSize;
        ViewBag.SearchTerm = searchTerm;

        return View(viewModels);
    }

    // GET: Admin/AccountManagement/Create
    public IActionResult Create()
    {
        return View(new CreateUserViewModel());
    }

    // POST: Admin/AccountManagement/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (ModelState.IsValid)
        {
            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng bởi một tài khoản khác.");
                return View(model);
            }

            var user = new AppUser
            {
                UserName = model.Email.Trim(),
                Email = model.Email.Trim(),
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Ensure target role exists, otherwise create it
                if (!await _roleManager.RoleExistsAsync(model.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.Role));
                }

                // Add to selected role
                await _userManager.AddToRoleAsync(user, model.Role);

                TempData["SuccessMessage"] = $"Tạo tài khoản \"{user.Email}\" thành công!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    // GET: Admin/AccountManagement/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var selectedRole = roles.FirstOrDefault() ?? "User";
        var isLocked = await _userManager.IsLockedOutAsync(user);

        var model = new EditUserViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            SelectedRole = selectedRole,
            LockAccount = isLocked
        };

        return View(model);
    }

    // POST: Admin/AccountManagement/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EditUserViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Guard: Prevent locking current user
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (user.Id == currentUserId && model.LockAccount)
            {
                ModelState.AddModelError("LockAccount", "Bạn không thể tự khóa tài khoản của chính mình!");
                return View(model);
            }

            // Check duplicate email
            var emailExists = await _userManager.Users.AnyAsync(u => u.Id != id && u.Email == model.Email.Trim());
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng bởi một tài khoản khác.");
                return View(model);
            }

            user.Email = model.Email.Trim();
            user.UserName = model.Email.Trim();

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var err in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                return View(model);
            }

            // Update Roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(model.SelectedRole))
            {
                // Guard: Prevent changing current user's role if they are the only Admin (simplified here as warning or check)
                if (user.Id == currentUserId && model.SelectedRole != "Admin")
                {
                    ModelState.AddModelError("SelectedRole", "Bạn không thể tự tước quyền Admin của chính mình!");
                    return View(model);
                }

                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!await _roleManager.RoleExistsAsync(model.SelectedRole))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.SelectedRole));
                }
                await _userManager.AddToRoleAsync(user, model.SelectedRole);
            }

            // Reset password if provided
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                if (!resetResult.Succeeded)
                {
                    foreach (var err in resetResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, err.Description);
                    }
                    return View(model);
                }
            }

            // Lock / Unlock account
            if (model.LockAccount)
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
            }

            TempData["SuccessMessage"] = $"Cập nhật tài khoản \"{user.Email}\" thành công!";
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // POST: Admin/AccountManagement/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        // Guard: Prevent self-deletion
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (user.Id == currentUserId)
        {
            TempData["ErrorMessage"] = "Bạn không thể tự xóa tài khoản của chính mình!";
            return RedirectToAction(nameof(Index));
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = $"Xóa tài khoản \"{user.Email}\" thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = "Lỗi xảy ra khi xóa tài khoản.";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/AccountManagement/ToggleLock/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        // Guard: Prevent self-lock
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (user.Id == currentUserId)
        {
            TempData["ErrorMessage"] = "Bạn không thể tự khóa tài khoản của chính mình!";
            return RedirectToAction(nameof(Index));
        }

        var isLocked = await _userManager.IsLockedOutAsync(user);
        if (isLocked)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            TempData["SuccessMessage"] = $"Đã mở khóa tài khoản \"{user.Email}\"!";
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            TempData["SuccessMessage"] = $"Đã khóa tài khoản \"{user.Email}\"!";
        }

        return RedirectToAction(nameof(Index));
    }
}
