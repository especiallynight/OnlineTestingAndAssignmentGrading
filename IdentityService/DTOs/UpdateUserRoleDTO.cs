using IdentityService.Enums;
using System.Text.Json.Serialization;

namespace IdentityService.DTOs
{
    public class UpdateUserRoleDTO
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserRole Role { get; set; }
    }
}
