using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SistemaBancaEnLinea.API.Middleware
{
    public class AuthorizationResponseMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthorizationResponseMiddleware> _logger;

        public AuthorizationResponseMiddleware(RequestDelegate next, ILogger<AuthorizationResponseMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            // Si la respuesta es 401 Unauthorized y no ha sido manejada previamente
            if (context.Response.StatusCode == 401 && !context.Response.HasStarted)
            {
                _logger.LogWarning("Acceso no autorizado para la ruta: {Path}", context.Request.Path);

                var response = new
                {
                    message = "No autorizado. Se requiere un token válido para acceder a este recurso.",
                    error = "UNAUTHORIZED",
                    path = context.Request.Path,
                    timestamp = DateTime.UtcNow
                };

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }

            // Si la respuesta es 403 Forbidden
            if (context.Response.StatusCode == 403 && !context.Response.HasStarted)
            {
                _logger.LogWarning("Acceso prohibido para la ruta: {Path}", context.Request.Path);

                var response = new
                {
                    message = "Acceso prohibido. No tienes los permisos necesarios para este recurso.",
                    error = "FORBIDDEN",
                    path = context.Request.Path,
                    timestamp = DateTime.UtcNow
                };

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        }
    }
}