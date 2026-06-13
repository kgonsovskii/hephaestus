using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using model;

namespace cp.Controllers;

[AllowAnonymous]
[Route("[controller]")]
public class AuthController : BaseController
{
    public AuthController(ServerService serverService, IConfiguration configuration, IMemoryCache memoryCache) : base(
        serverService, configuration, memoryCache)
    {
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        if (OsAuthentication.IsValidUser(username, password, out var msg))
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

            var cookiesHeader = HttpContext.Response.Headers["Set-Cookie"];
            var cookieString = string.Join("; ", cookiesHeader.Select(c => c.Split(';')[0]));

            ViewData["RedirectFlag"] = true;
            ViewData["CookieString"] = cookieString;
            ViewData["LoginFailed"] = "Success. Redirect.";

            return View("Index");
        }

        ViewData["LoginFailed"] = msg;
        return View("Index");
    }

    [HttpPost]
    [Route("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Auth");
    }
}
