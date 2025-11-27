using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SistemaBancaEnLinea.BW;
using SistemaBancaEnLinea.BW.CU;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.DA;
using SistemaBancaEnLinea.DA.Acciones;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ========== CONFIGURACIÓN DE SERVICIOS ==========

// Configuración de Entity Framework con SQL Server
builder.Services.AddDbContext<BancaContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuración de JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "TuClaveSecretaMuyLargaYSegura1234567890!@#$%";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SistemaBancaEnLinea";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SistemaBancaEnLinea";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ========== REGISTRO DE ACCIONES (Data Access) ==========
builder.Services.AddScoped<UsuarioAcciones>();
builder.Services.AddScoped<ClienteAcciones>();
builder.Services.AddScoped<CuentaAcciones>();
builder.Services.AddScoped<BeneficiarioAcciones>();
builder.Services.AddScoped<TransaccionAcciones>();
builder.Services.AddScoped<AuditoriaAcciones>();
builder.Services.AddScoped<ProveedorServicioAcciones>();
builder.Services.AddScoped<ProgramacionAcciones>();

// ========== REGISTRO DE SERVICIOS (Business Logic) ==========
builder.Services.AddScoped<IUsuarioServicio, UsuarioServicio>();
builder.Services.AddScoped<IClienteServicio, ClienteServicio>();
builder.Services.AddScoped<ICuentaServicio, CuentaServicio>();
builder.Services.AddScoped<IBeneficiarioServicio, BeneficiarioServicio>();
builder.Services.AddScoped<ITransferenciasServicio, TransferenciasServicio>();
builder.Services.AddScoped<IPagosServiciosServicio, PagosServiciosServicio>();
builder.Services.AddScoped<IProgramacionServicio, ProgramacionServicio>();
builder.Services.AddScoped<IAuditoriaServicio, AuditoriaServicio>();
builder.Services.AddScoped<IProveedorServicioServicio, ProveedorServicioServicio>();

// ========== REGISTRO DE CASOS DE USO ==========
builder.Services.AddScoped<GestionCuentasCU>();
builder.Services.AddScoped<GestionUsuariosCU>();
builder.Services.AddScoped<TransferenciasCU>();

// ========== CONFIGURACIÓN DE CONTROLLERS ==========
builder.Services.AddControllers();

// ========== CONFIGURACIÓN DE CORS ==========
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ========== CONFIGURACIÓN DE SWAGGER ==========
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sistema Banca en Línea API",
        Version = "v1",
        Description = "API para el sistema de banca en línea"
    });

    // Configuración de autenticación JWT en Swagger
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

    // Incluir comentarios XML
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// ========== CONFIGURACIÓN DEL PIPELINE ==========

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Banca en Línea API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ========== INICIALIZACIÓN DE BASE DE DATOS ==========
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BancaContext>();
    try
    {
        context.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al inicializar la base de datos");
    }
}

app.Run();