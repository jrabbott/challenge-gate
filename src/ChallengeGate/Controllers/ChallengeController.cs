using ChallengeGate.Configuration;
using ChallengeGate.ViewModels;
using FluentValidation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ChallengeGate.Controllers;

public class ChallengeController(
    IOptions<ChallengeOptions> options, 
    IValidator<ChallengeViewModel> validator,
    IDataProtectionProvider dataProtectionProvider) : Controller
{
    private readonly ChallengeOptions _options = options.Value;
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(ChallengeGateConstants.DataProtectionPurpose);

    [HttpGet]
    public IActionResult Index(string? returnUrl = null)
    {
        if (!_options.Enabled)
        {
            return Redirect(returnUrl ?? "/");
        }

        var model = new ChallengeViewModel
        {
            Layout = _options.Layout,
            Title = _options.Title,
            ReturnUrl = returnUrl
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(ChallengeViewModel model)
    {
        if (!_options.Enabled)
        {
            return Redirect(model.ReturnUrl ?? "/");
        }

        // Always ensure layout and title are set for the view, even if validation fails
        model.Layout = _options.Layout;
        model.Title = _options.Title;

        var validationResult = validator.Validate(model);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return View(model);
        }

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(_options.CookieExpirationMinutes)
        };

        var protectedValue = _protector.Protect($"{ChallengeGateConstants.CookieValuePrefix}:{_options.Password}");
        
        // Tie the cookie to the current password. If the password changes in configuration, 
        // existing cookies will be invalidated by the middleware check.
        Response.Cookies.Append(_options.CookieName, protectedValue, cookieOptions);

        return Redirect(string.IsNullOrEmpty(model.ReturnUrl) ? "/" : model.ReturnUrl);
    }
}
