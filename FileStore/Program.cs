using FileStore.Filters;
using FileStore.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace FileStore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            // Add MVC with global authorization policy
            builder.Services.AddControllersWithViews(options =>
            {
                // Require authenticated users by default
                var policy = new AuthorizationPolicyBuilder()
                                 .RequireAuthenticatedUser()
                                 .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            });

            // Register in-memory user service & Register the Filter in DI
            builder.Services.AddSingleton<IUserService,  UserService>();
            builder.Services.AddScoped<DownloadAuthorizeAttribute>();

            // Configure JSON metadata provider
            //builder.Services.AddControllers(options =>
            //{
            //    options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
            //});

            // Configure Authentication & Cookie settings
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                   .AddCookie(options =>
                   {
                       options.LoginPath = "/Account/Login";
                       options.AccessDeniedPath = "/Account/AccessDenied";
                       options.ExpireTimeSpan = TimeSpan.FromHours(1);
                       options.SlidingExpiration = true;
                   });

            var app = builder.Build();

            // Configure the HTTP request or middleware pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Enable authentication and authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // Route configuration
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
