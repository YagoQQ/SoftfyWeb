using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SoftfyWeb.Modelos;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SoftfyWeb.Services
{
    public class JwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerarToken(Usuario usuario, string rol)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id),
                new Claim(JwtRegisteredClaimNames.UniqueName, usuario.UserName),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim("TipoUsuario", usuario.TipoUsuario),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, usuario.Email),
                // 2. Claim de Rol único
                new Claim(ClaimTypes.Role, rol)
            };

            // 3. Clave secreta y credenciales
            var keyBytes = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);
            var signingKey = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            // 4. Tiempos del token
            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(_config.GetValue<double>("Jwt:ExpireMinutes"));

            // 5. Crear y firmar el token
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
