using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using model;

namespace cp.Controllers;

[Route("[controller]")]
public class AuthController : BaseController
{
    // GET: /auth
    public AuthController(ServerService serverService, IConfiguration configuration, IMemoryCache memoryCache) : base(
        serverService, configuration, memoryCache)
    {
    }

    [AllowAnonymous]
    [HttpGet]
    // Exact route for login page
    public IActionResult Index()
    {
        return View();
    }

    // POST: /auth/login
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        if (RemoteAuthentication.IsValidUser(username, password, Server, out var msg))
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Ensure the SignInAsync is awaited
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

            // Extract the "Set-Cookie" header after signing in
            var cookiesHeader = HttpContext.Response.Headers["Set-Cookie"];

            // Get cookie string (for this example we assume a simple cookie)
            var cookieString = string.Join("; ", cookiesHeader.Select(c => c.Split(';')[0]));

            // Set a flag in ViewData for the redirect script
            ViewData["RedirectFlag"] = true;
            ViewData["CookieString"] = cookieString;
            ViewData["LoginFailed"] = "Success. Redirect.";
            // Return the Index view
            return View("Index");
        }
        
        ViewData["LoginFailed"] = msg;
        return View("Index");
    }

    // POST: /auth/logout
    [HttpPost]
    [Route("logout")] // Exact route for logout action
    public async Task<IActionResult> Logout()
    {
        // Sign out the user and clear the session
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Index", "Auth"); // Redirect to login page
    }
}