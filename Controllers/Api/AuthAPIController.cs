using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ARISESLCOM.Data;
using ARISESLCOM.DTO.Api.Request;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Models.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ARISESLCOM.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IDBContext dBContext,
                                IGerenciaDomainModel gerencia) : ControllerBase
    {
        private readonly IDBContext _dbContext = dBContext;
        private readonly IGerenciaDomainModel _gerencia = gerencia;

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginAPIRequest model)
        {
            // Exemplo: validar usu�rio fixo (substituir por banco)
            var resultDB = await LoginAsync(model.Username, model.Password);

            if (!resultDB)
            {
                return Unauthorized();
            }

            var jwtSecret = Environment.GetEnvironmentVariable("XPJ");
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret));

            var claims = new[]
             {
                new Claim(ClaimTypes.Name, model.Username),
                new Claim(ClaimTypes.Role, "Admin") // exemplo, pode puxar do banco
            };

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1), // expira em 1h
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                token = tokenString,
                expires = token.ValidTo
            });
        }

        private async Task<bool> LoginAsync(String user, String pwd)
        {
            _dbContext.GetSqlConnection().Open();

            PasswordVerificationResult result;
            try
            {
                _gerencia.SetContext(_dbContext);
                var pwdHash = await _gerencia.GetGerPasswordDBAsync(user);

                if (string.IsNullOrEmpty(pwdHash))
                {
                    return false;
                }

                var passwordHasher = new PasswordHasher<object>();

                result = passwordHasher.VerifyHashedPassword(null, pwdHash, pwd);
            } finally
            {
                await _dbContext.CloseAsync();
            }

            return result == PasswordVerificationResult.Success;
        }
    }
}
