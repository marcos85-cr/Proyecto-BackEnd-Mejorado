using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using SistemaBancaEnLinea.DA;
using SistemaBancaEnLinea.DA.Acciones;
using SistemaBancaEnLinea.BW;
using SistemaBancaEnLinea.BW.CU;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.API.Middleware;
using SistemaBancaEnLinea.API;
using SistemaBancaEnLinea.BC.Mapping;

var builder = WebApplication.CreateBuilder(args);

// ========== CONFIGURACI�N DE SERVICIOS ==========

// 1. Configurar DbContext
builder.Services.AddDbContext<BancaContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null
        )
    )
);

// 2. Configurar AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// 3. Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 4. Configurar JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key no configurada");
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 5. Registrar Acciones (DA Layer)
builder.Services.AddScoped<UsuarioAcciones>();
builder.Services.AddScoped<ClienteAcciones>();
builder.Services.AddScoped<CuentaAcciones>();
builder.Services.AddScoped<BeneficiarioAcciones>();
builder.Services.AddScoped<TransaccionAcciones>();
builder.Services.AddScoped<ProgramacionAcciones>();
builder.Services.AddScoped<ProveedorServicioAcciones>();
builder.Services.AddScoped<AuditoriaAcciones>();

// 6. Registrar Servicios (BW Layer)
builder.Services.AddScoped<IUsuarioServicio, UsuarioServicio>();
builder.Services.AddScoped<IClienteServicio, ClienteServicio>();
builder.Services.AddScoped<ICuentaServicio, CuentaServicio>();
builder.Services.AddScoped<IBeneficiarioServicio, BeneficiarioServicio>();
builder.Services.AddScoped<ITransferenciasServicio, TransferenciasServicio>();
builder.Services.AddScoped<IPagosServiciosServicio, PagosServiciosServicio>();
builder.Services.AddScoped<IProgramacionServicio, ProgramacionServicio>();
builder.Services.AddScoped<IProveedorServicioServicio, ProveedorServicioServicio>();
builder.Services.AddScoped<IAuditoriaServicio, AuditoriaServicio>();
builder.Services.AddScoped<IReportesServicio, ReportesServicio>();

// 7. Registrar Casos de Uso
builder.Services.AddScoped<GestionCuentasCU>();
builder.Services.AddScoped<GestionUsuariosCU>();
builder.Services.AddScoped<TransferenciasCU>();

// 8. Registrar Background Service para programaciones
builder.Services.AddHostedService<ProgramacionBackgroundService>();

// 9. Configurar Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// 10. Configurar Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sistema Banca en L�nea API",
        Version = "v1",
        Description = "API REST para el sistema de banca en l�nea - Proyecto SOF-18"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Ejemplo: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ========== CONFIGURACI�N DEL PIPELINE ==========

// SIEMPRE habilitar Swagger (Development y Production)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sistema Banca API v1");
    c.RoutePrefix = "swagger"; // Swagger en la ra�z
});


app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Middleware personalizado para validación de JWT
app.UseMiddleware<JwtMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Middleware para respuestas consistentes de autorización
app.UseMiddleware<AuthorizationResponseMiddleware>();
app.MapControllers();

Console.WriteLine("========================================");
Console.WriteLine(" API Sistema Banca en L�nea INICIADA");
Console.WriteLine($" Swagger UI: https://localhost:7500");
Console.WriteLine($" Swagger UI: http://localhost:5500");
Console.WriteLine("========================================");

app.Run();