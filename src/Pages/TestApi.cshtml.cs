using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MyAzureWebApp.Pages
{
    public class TestApiModel : PageModel
    {
        public IActionResult OnGet()
        {
            return new JsonResult(new
            {
                message = "Hello from the API!",
                timestamp = DateTime.UtcNow,
                userAgent = Request.Headers.UserAgent.ToString(),
                ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                method = Request.Method,
                path = Request.Path.Value
            });
        }

        public IActionResult OnPost()
        {
            return new JsonResult(new
            {
                message = "POST request received",
                timestamp = DateTime.UtcNow,
                userAgent = Request.Headers.UserAgent.ToString(),
                ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                method = Request.Method,
                path = Request.Path.Value
            });
        }
    }
}
