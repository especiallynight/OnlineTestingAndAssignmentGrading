
using IdentityService.Data;
using IdentityService.DTOs;
using IdentityService.Enums;
using IdentityService.Models;
using IdentityService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;



namespace IdentityService.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IdentityDbContext _context;
        private readonly JwtService _jwtService;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthController(
            IdentityDbContext context,
            JwtService jwtService,
            IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult> Register(LoginDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest("Email и пароль обязательны");
            }

            var existing_user = await _context.Users
                .AnyAsync(user => user.Email == dto.Email);

            if (existing_user)
                return Conflict("Пользователь с таким email уже существует");

            var user = new User
            {
                Email = dto.Email,
                Role = IdentityService.Enums.UserRole.Student
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("Регистрация прошла успешно");
        }

        [HttpPost("teacher-application")]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult> ApplyForTeacher()
        {
            var user_id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (user_id == null || !int.TryParse(user_id, out var id))
                return Unauthorized();

            var has_application = await _context.TeacherApplications
                .AnyAsync(a => a.UserId == id &&
                    a.Status == TeacherApplicationStatus.Pending);

            if (has_application)
                return Conflict("У вас уже есть заявка на рассмотрении");

            var application = new TeacherApplication
            {
                UserId = id,
                Status = TeacherApplicationStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.TeacherApplications.Add(application);
            await _context.SaveChangesAsync();

            return Ok("Заявка на получение роли преподавателя отправлена");
        }


        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDTO>> Login(LoginDTO dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(user => user.Email == dto.Email);

            if (user == null)
                return Unauthorized("Неверный email или пароль");

            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                dto.Password
            );

            if (result == PasswordVerificationResult.Failed)
                return Unauthorized("Неверный email или пароль");

            var now = DateTime.UtcNow;
            var expires_at = now.AddHours(1);

            var access_token = _jwtService.GenerateToken(user.UserId, user.Role.ToString());
            var refresh_token = GenerateRefreshToken();

            var refresh_token_entity = new RefreshToken
            {
                UserId = user.UserId,
                TokenHash = HashToken(refresh_token),
                ExpiresAt = now.AddDays(7),
                CreatedAt = now
            };

            _context.RefreshTokens.Add(refresh_token_entity);
            await _context.SaveChangesAsync();

            return Ok(new AuthResponseDTO
            {
                AccessToken = access_token,
                RefreshToken = refresh_token,
                ExpiresAt = expires_at
            });
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDTO>> RefreshToken(RefreshTokenDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
                return BadRequest("Refresh token не указан");

            var token_hash = HashToken(dto.RefreshToken);

            var refresh_token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == token_hash);

            var now = DateTime.UtcNow;

            if (refresh_token == null ||
                refresh_token.RevokedAt != null ||
                refresh_token.ExpiresAt <= now)
            {
                return Unauthorized("Недействительный refresh token");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == refresh_token.UserId);

            if (user == null)
                return Unauthorized("Пользователь не найден");

            refresh_token.RevokedAt = now;

            var expires_at = now.AddHours(1);
            var access_token = _jwtService.GenerateToken(user.UserId, user.Role.ToString());
            var new_refresh_token = GenerateRefreshToken();

            var newRefreshTokenEntity = new RefreshToken
            {
                UserId = user.UserId,
                TokenHash = HashToken(new_refresh_token),
                ExpiresAt = now.AddDays(7),
                CreatedAt = now
            };

            _context.RefreshTokens.Add(newRefreshTokenEntity);

            await _context.SaveChangesAsync();

            return Ok(new AuthResponseDTO
            {
                AccessToken = access_token,
                RefreshToken = new_refresh_token,
                ExpiresAt = expires_at
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult> Logout()
        {
            var user_id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (user_id == null || !int.TryParse(user_id, out var id))
                return Unauthorized();

            var now = DateTime.UtcNow;

            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == id && rt.RevokedAt == null)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.RevokedAt = now;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Выход выполнен успешно" });
        }

        [HttpGet("teacher-applications")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> GetTeacherApplications()
        {
            var applications = await _context.TeacherApplications
                .Select(a => new
                {
                    a.TeacherApplicationId,
                    a.UserId,
                    Email = a.User.Email,
                    a.Status,
                    a.CreatedAt,
                    a.ReviewedAt
                })
                .ToListAsync();

            return Ok(applications);
        }

        [HttpPut("teacher-applications/{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> ApproveTeacherApplication(int id)
        {
            var application = await _context.TeacherApplications
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.TeacherApplicationId == id);

            if (application == null)
                return NotFound("Заявка не найдена");

            if (application.Status != TeacherApplicationStatus.Pending)
                return Conflict("Заявка уже рассмотрена");

            var admin_id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(admin_id, out var reviewer_id))
                return Unauthorized();

            if (application.UserId == reviewer_id)
                return BadRequest("Нельзя рассматривать собственную заявку");

            application.Status = TeacherApplicationStatus.Approved;
            application.ReviewedAt = DateTimeOffset.UtcNow;
            application.ReviewedBy = reviewer_id;
            application.User.Role = UserRole.Teacher;

            await _context.SaveChangesAsync();

            return Ok("Заявка одобрена.");
        }

        [HttpPut("teacher-applications/{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> RejectTeacherApplication(int id)
        {
            var application = await _context.TeacherApplications
                .FirstOrDefaultAsync(a => a.TeacherApplicationId == id);

            if (application == null)
                return NotFound("Заявка не найдена");

            if (application.Status != TeacherApplicationStatus.Pending)
                return Conflict("Заявка уже рассмотрена");

            var admin_id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(admin_id, out var reviewer_id))
                return Unauthorized();

            if (application.UserId == reviewer_id)
                return BadRequest("Нельзя рассматривать собственную заявку");

            application.Status = TeacherApplicationStatus.Rejected;
            application.ReviewedAt = DateTimeOffset.UtcNow;
            application.ReviewedBy = reviewer_id;

            await _context.SaveChangesAsync();

            return Ok("Заявка отклонена");
        }

        private string GenerateRefreshToken()
        {
            var random_bytes = new byte[64];

            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(random_bytes);

            return Convert.ToBase64String(random_bytes);
        }

        private string HashToken(string token)
        {
            using var sha256 = SHA256.Create();

            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));

            return Convert.ToBase64String(bytes);
        }
    }
}
