using AspNetMvcApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AspNetMvcApp.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AccountController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null, string? externalError = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Products");
        }

        ViewData["ReturnUrl"] = returnUrl;
        if (!string.IsNullOrWhiteSpace(externalError))
        {
            ModelState.AddModelError(string.Empty, $"{externalError} đăng nhập thất bại. Vui lòng kiểm tra Client ID, Client Secret và Redirect URI.");
        }

        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return RedirectToLocal(returnUrl);
            }

            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
        }

        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Products");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            var user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToLocal(returnUrl);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Products");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        if (!IsExternalProviderConfigured(provider))
        {
            ViewData["ReturnUrl"] = returnUrl;
            ModelState.AddModelError(string.Empty, $"Chưa cấu hình {provider} hoặc vẫn đang dùng giá trị mẫu. Vui lòng thay bằng ClientId/AppId và ClientSecret/AppSecret thật.");
            return View("Login", new LoginViewModel());
        }

        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (!string.IsNullOrEmpty(remoteError))
        {
            ViewData["ReturnUrl"] = returnUrl;
            ModelState.AddModelError(string.Empty, $"Lỗi từ nhà cung cấp đăng nhập: {remoteError}");
            return View("Login", new LoginViewModel());
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        var result = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: true);

        if (result.Succeeded)
        {
            return RedirectToLocal(returnUrl);
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            ViewData["ReturnUrl"] = returnUrl;
            ModelState.AddModelError(string.Empty, "Không lấy được email từ tài khoản mạng xã hội.");
            return View("Login", new LoginViewModel());
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                FullName = info.Principal.FindFirstValue(ClaimTypes.Name),
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                ViewData["ReturnUrl"] = returnUrl;
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View("Login", new LoginViewModel());
            }

            await _userManager.AddToRoleAsync(user, "User");
        }

        var addLoginResult = await _userManager.AddLoginAsync(user, info);
        if (!addLoginResult.Succeeded && !addLoginResult.Errors.Any(error => error.Code == "LoginAlreadyAssociated"))
        {
            ViewData["ReturnUrl"] = returnUrl;
            foreach (var error in addLoginResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View("Login", new LoginViewModel());
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToLocal(returnUrl);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Products");
    }

    private bool IsExternalProviderConfigured(string provider)
    {
        return provider switch
        {
            "Google" => HasValue("Authentication:Google:ClientId") && HasValue("Authentication:Google:ClientSecret"),
            _ => false
        };
    }

    private bool HasValue(string key)
    {
        var value = _configuration[key];
        return !string.IsNullOrWhiteSpace(value)
            && !value.StartsWith("TODO", StringComparison.OrdinalIgnoreCase)
            && !value.EndsWith("_CLIENT_ID", StringComparison.OrdinalIgnoreCase)
            && !value.EndsWith("_CLIENT_SECRET", StringComparison.OrdinalIgnoreCase)
            && !value.EndsWith("_APP_ID", StringComparison.OrdinalIgnoreCase)
            && !value.EndsWith("_APP_SECRET", StringComparison.OrdinalIgnoreCase);
    }
}
