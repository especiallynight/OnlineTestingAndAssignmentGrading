using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Services
{
    public class JwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string GenerateToken(int user_id, string role)
        {
            var claims = new List<Claim>
{
                new Claim(ClaimTypes.NameIdentifier, user_id.ToString()),
                new Claim("sub", user_id.ToString()),
                new Claim("role", role)
            };

            var secretKey = _configuration["Jwt:SecretKey"];

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey!)
            );

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}