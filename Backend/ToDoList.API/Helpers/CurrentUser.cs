using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ToDoList.API.Helpers
{
    public class CurrentUser
    {
        private readonly HttpContext? _httpContext;

        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContext = httpContextAccessor.HttpContext;
        }

        public string? UserId
        {
            get
            {
                if (_httpContext?.User == null)
                    return null;

                var userId = _httpContext.User.FindFirstValue("uid")
                    ?? _httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? _httpContext.User.FindFirstValue(ClaimTypes.Name);

                return userId;
            }
        }

        public string? Email => _httpContext?.User?.FindFirstValue(ClaimTypes.Email);

        public string? Name => _httpContext?.User?.FindFirstValue(ClaimTypes.Name);

        public bool IsAuthenticated => _httpContext?.User?.Identity?.IsAuthenticated ?? false;

        public string? IpAddress
        {
            get
            {
                if (_httpContext == null) return null;

                if (_httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
                {
                    var ip = forwarded.ToString().Split(',')[0].Trim();
                    if (!string.IsNullOrWhiteSpace(ip)) return ip;
                }

                return _httpContext.Connection.RemoteIpAddress?.ToString();
            }
        }
    }
}
