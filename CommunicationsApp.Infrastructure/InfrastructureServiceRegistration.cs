using CommunicationsApp.Application.Interfaces;
using CommunicationsApp.Application.Notifications;
using CommunicationsApp.Infrastructure.CosmosDb;
using CommunicationsApp.Infrastructure.Notifications;
using CommunicationsApp.Infrastructure.Services;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommunicationsApp.Infrastructure
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHybridCache(options =>
            {
                options.DefaultEntryOptions = new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(10),
                    LocalCacheExpiration = TimeSpan.FromMinutes(30)
                };
            });

            services.AddScoped<ICommunicationsNotificationService, CommunicationsNotificationService>();
            services.AddScoped<IServerRepository, ServerRepository>();
            services.AddScoped<IServerService, ServerService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<CosmosDbFactory>();
            services.AddScoped<ICosmosDbService, CosmosDbService>();
            services.AddScoped<IMediaService, MediaService>();
            services.AddScoped<IAuthService, AuthService>();

            return services;
        }
    }
}
