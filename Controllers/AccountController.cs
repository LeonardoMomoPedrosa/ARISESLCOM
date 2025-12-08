using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ARISESLCOM.Models.Entities;
using Microsoft.AspNetCore.Identity;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Services.interfaces;
using Microsoft.EntityFrameworkCore;
using ARISESLCOM.Data;
using ARISESLCOM.Models.Domains;

namespace ARISESLCOM.Controllers
{
    public class AccountController(IDBContext dBContext,
                                    IGerenciaDomainModel gerDomainModel,
                                    IRedisCacheService redis) : BasicController(redis)
    {
        private readonly IDBContext _dbContext = dBContext;
        private readonly IGerenciaDomainModel _gerenciaDomainModel = gerDomainModel;

        public IActionResult Login()
        {
            string param1 = Request.Query["param1"];
            if (param1 != null && param1.Length > 0)
            {
            var passwordHasher = new PasswordHasher<object>();
            var hashedPassword = passwordHasher.HashPassword(new object(), param1);
                ViewBag.Hash = hashedPassword;
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(UserModel user)
        {
            _dbContext.GetSqlConnection().Open();
            _gerenciaDomainModel.SetContext(_dbContext);

            var pwd = await _gerenciaDomainModel.GetGerPasswordDBAsync(user.Name);
            var passwordHasher = new PasswordHasher<object>();

            var result = passwordHasher.VerifyHashedPassword(new object(), pwd, user.Password);
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
                ViewBag.Name = user.Name;
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}
