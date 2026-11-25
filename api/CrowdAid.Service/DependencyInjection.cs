using CrowdAid.Data.IRepo;
using CrowdAid.Data.Options;
using CrowdAid.Service.Infrastructure;
using CrowdAid.Service.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CrowdAid.Service;

public static class DependencyInjection
{
    /// <summary>Registers settings, infrastructure and (as you add them) feature services.</summary>
    public static IServiceCollection AddCrowdAidServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<AppSettings>(config.GetSection(AppSettings.Section));
        services.Configure<JwtSettings>(config.GetSection(JwtSettings.Section));
        services.Configure<SmtpSettings>(config.GetSection(SmtpSettings.Section));
        services.Configure<PaystackSettings>(config.GetSection(PaystackSettings.Section));
        services.Configure<AdminSettings>(config.GetSection(AdminSettings.Section));

        services.AddScoped<IReferenceGenerator, ReferenceGenerator>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IActivationTokenFactory, ActivationTokenFactory>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        services.AddHttpClient<IPaystackClient, PaystackClient>((sp, http) =>
        {
            var s = sp.GetRequiredService<IOptions<PaystackSettings>>().Value;
            http.BaseAddress = new Uri(s.BaseUrl.TrimEnd('/') + "/");
            http.Timeout = TimeSpan.FromSeconds(20);
        });

        // Public features
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IDonationService, DonationService>();
        services.AddScoped<ICommunityService, CommunityService>();

        // Sign-in and admin
        services.AddScoped<ActivationLinkService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminApplicationService, AdminApplicationService>();
        services.AddScoped<IAdminInboxService, AdminInboxService>();
        services.AddScoped<IAdminWithdrawalService, AdminWithdrawalService>();

        // Campaigner dashboard
        services.AddScoped<IMeService, MeService>();

        return services;
    }
}
