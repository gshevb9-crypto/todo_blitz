using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using ToDoList.Domain.Entities;

namespace ToDoList.Application.Common.Interfaces
{
    public interface IJwtService
    {
        Task<(string Token, DateTime Expiration)> GenerateAccessTokenAsync(ApplicationUser user, UserManager<ApplicationUser> userManager);

        (string Token, DateTime Expiration) GenerateRefreshToken();
    }
}
