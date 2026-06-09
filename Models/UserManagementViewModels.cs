using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AspNetMvcApp.Models;

public class UserViewModel
{
    public string Id { get; set; } = string.Empty;

    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Tên tài khoản")]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "Vai trò")]
    public List<string> Roles { get; set; } = new List<string>();

    [Display(Name = "Đang bị khóa")]
    public bool IsLockedOut { get; set; }
}

public class CreateUserViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [StringLength(100, ErrorMessage = "Mật khẩu phải từ {2} đến {1} ký tự", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn vai trò")]
    [Display(Name = "Vai trò")]
    public string Role { get; set; } = "User"; // "Admin", "User"
}

public class EditUserViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn vai trò")]
    [Display(Name = "Vai trò")]
    public string SelectedRole { get; set; } = "User"; // "Admin", "User"

    [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới (Bỏ trống nếu không đổi)")]
    public string? NewPassword { get; set; }

    [Display(Name = "Khóa tài khoản này")]
    public bool LockAccount { get; set; }
}
