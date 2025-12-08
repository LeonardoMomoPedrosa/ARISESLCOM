using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Models.Entities;
using ARISESLCOM.Services.interfaces;
using StackExchange.Redis;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace ARISESLCOM.Controllers
{
    public class LoginController(IGerenciaDomainModel gerDomainModel,
                                    IRedisCacheService redis) : BasicController(redis)
    {
        private readonly IGerenciaDomainModel _gerenciaDomainModel = gerDomainModel;

        public IActionResult Index()
        {
           
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(UserModel user)
        {
            var pwd = await _gerenciaDomainModel.GetGerPasswordDBAsync(user.Name);
            var passwordHasher = new PasswordHasher<object>();

            var result = passwordHasher.VerifyHashedPassword(null, pwd, user.Password);
            if (result == PasswordVerificationResult.Success)
            {
                var claims = new List<Claim>()
                {
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Role, "User")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError("", "Login erro");
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
