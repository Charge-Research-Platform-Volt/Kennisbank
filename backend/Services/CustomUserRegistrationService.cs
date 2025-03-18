using KnowledgeBank.Models;
using Microsoft.AspNetCore.Identity;
using Serilog;

namespace backend.Services
{
    public class CustomUserRegistrationService
    {
        private readonly UserManager<User> userManager;
        private readonly Serilog.ILogger logger;

        public CustomUserRegistrationService(UserManager<User> userManager)
        {
            this.userManager = userManager;
            this.logger = Log.ForContext<CustomUserRegistrationService>();
        }

        public async Task<IdentityResult> RegisterUserWithDefaultRoleAsync(User user, string password)
        {
            return null;
        }
    }
}
