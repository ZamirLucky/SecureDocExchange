using FileStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;

namespace FileStore.Filters
{
    public class DownloadAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly IWebHostEnvironment _environment;

        private const string UploadFolder = "App_Data/SecureUploads";

        public DownloadAuthorizeAttribute(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Extract action arguments
            if (!context.ActionArguments.TryGetValue("vm", out var vmObj) 
                || vmObj is not DownloadViewModel vm
                || string.IsNullOrWhiteSpace(vm.LawyerEmail)
                || !Guid.TryParse(vm.Code, out var code))
            {
                context.Result = new ForbidResult();
                return;
            }

            var lawyerEmail = vm.LawyerEmail.Trim();

            // Load metadata.json
            var metaPath = Path.Combine(
                _environment.ContentRootPath,
                UploadFolder,
                "metadata.json");

            if (!System.IO.File.Exists(metaPath))
            {
                context.Result = new ForbidResult();
                return;
            }

            var json = await System.IO.File.ReadAllTextAsync(metaPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                context.Result = new ForbidResult();
                return;
            }

            List<JsonElement> entries;
            try
            {
                entries = JsonSerializer.Deserialize<List<JsonElement>>(json)!
                          ?? new List<JsonElement>();
            }
            catch (JsonException)
            {
                context.Result = new ForbidResult();
                return;
            }

            // Check for a matching entry
            var match = entries.Any(e =>
                e.GetProperty("LawyerEmail").GetString()?.Equals(
                    lawyerEmail, StringComparison.OrdinalIgnoreCase) == true
                && Guid.TryParse(
                    e.GetProperty("Code").GetString(),
                    out var storedCode)
                && storedCode == code
            );

            //Reject unauthorized
            if (!match)
            {
                context.Result = new ForbidResult();
                return;
            }

            // Proceed with the action
            await next();

        }
    }
}
