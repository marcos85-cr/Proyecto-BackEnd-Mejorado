using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace SistemaBancaEnLinea.API.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtMiddleware> _logger;

        public JwtMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<JwtMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var token = ExtractTokenFromRequest(context);

            if (token != null)
            {
                try
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);

                    var validationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = true,
                        ValidIssuer = _configuration["Jwt:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = _configuration["Jwt:Audience"],
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };

                    // Validar el token
                    var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                    // Agregar el usuario autenticado al contexto
                    context.User = principal;

                    _logger.LogDebug("Token JWT validado exitosamente para el usuario: {UserId}",
                        principal.FindFirst("sub")?.Value ?? principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                }
                catch (SecurityTokenExpiredException)
                {
                    _logger.LogWarning("Token JWT expirado");
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new {
                        message = "Token ha expirado",
                        error = "TOKEN_EXPIRED"
                    });
                    return;
                }
                catch (SecurityTokenInvalidSignatureException)
                {
                    _logger.LogWarning("Firma del token JWT inválida");
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new {
                        message = "Firma del token inválida",
                        error = "INVALID_SIGNATURE"
                    });
                    return;
                }
                catch (SecurityTokenInvalidIssuerException)
                {
                    _logger.LogWarning("Issuer del token JWT inválido");
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new {
                        message = "Issuer del token inválido",
                        error = "INVALID_ISSUER"
                    });
                    return;
                }
                catch (SecurityTokenInvalidAudienceException)
                {
                    _logger.LogWarning("Audience del token JWT inválido");
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new {
                        message = "Audience del token inválido",
                        error = "INVALID_AUDIENCE"
                    });
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al validar token JWT");
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new {
                        message = "Token inválido",
                        error = "INVALID_TOKEN"
                    });
                    return;
                }
            }

            await _next(context);
        }

        private string? ExtractTokenFromRequest(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (authHeader != null && authHeader.StartsWith("Bearer "))
            {
                return authHeader.Substring("Bearer ".Length);
            }

            // También revisar si viene en los parámetros de consulta (para casos específicos)
            return context.Request.Query["access_token"].FirstOrDefault();
        }
    }
}