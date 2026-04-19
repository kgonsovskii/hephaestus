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
    
    public AuthController(ServerService serverService, IConfiguration configuration, IMemoryCache memoryCache) : base(
        serverService, configuration, memoryCache)
    {
    }

    [AllowAnonymous]
    [HttpGet]
    
    public IActionResult Index()
    {
        return View();
    }

    
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
