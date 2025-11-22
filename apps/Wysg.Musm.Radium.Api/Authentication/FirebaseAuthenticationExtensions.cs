using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Wysg.Musm.Radium.Api.Authentication
{
    /// <summary>
    /// Extension methods for Firebase authentication setup
    /// </summary>
    public static class FirebaseAuthenticationExtensions
    {
        /// <summary>
        /// Add Firebase JWT authentication to the service collection
        /// </summary>
        public static IServiceCollection AddFirebaseAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Bind Firebase settings
            services.Configure<Configuration.FirebaseSettings>(
                configuration.GetSection("Firebase"));

            // Add JWT Bearer authentication with Firebase handler
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddScheme<JwtBearerOptions, FirebaseAuthenticationHandler>(
                    JwtBearerDefaults.AuthenticationScheme,
                    options => { });

            services.AddAuthorizationBuilder()
                .AddPolicy("RequireFirebaseAuth", policy =>
                {
                    policy.RequireAuthenticatedUser();
                });

            return services;
        }
    }
}
