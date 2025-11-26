using Microsoft.EntityFrameworkCore;
using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.DA;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;

namespace SistemaBancaEnLinea.BW
{
    public class ResultadoLogin
    {
        public bool Exitoso { get; set; }
        public string? Token { get; set; }
        public string? Error { get; set; }
        public Usuario? Usuario { get; set; }
    }

    public class UsuarioServicio : IUsuarioServicio
    {
        private readonly BancaContext _context;
        private const string JWT_SECRET = "mock-jwt-secret-key-2025";
        private const int TOKEN_EXPIRATION_SECONDS = 20 * 60; // 20 minutos

        public UsuarioServicio(BancaContext context)
        {
            _context = context;
        }

        /// <summary>
        /// RF-A1: Registro de usuarios con validación completa
        /// </summary>
        public async Task<Usuario> RegistrarUsuarioAsync(string email, string password, string rol)
        {
            // Validar email único
            if (await _context.Usuarios.AnyAsync(u => u.Email == email))
                throw new InvalidOperationException("El correo electrónico ya está registrado.");

            // Validar formato de contraseña
            if (!AutenticacionReglas.ValidarFormatoPassword(password))
                throw new InvalidOperationException(
                    "La contraseña debe tener mínimo 8 caracteres, incluir al menos 1 mayúscula, 1 número y 1 símbolo.");

            // Validar rol
            if (!AutenticacionReglas.ValidarRol(rol))
                throw new InvalidOperationException("Rol no válido. Use: Administrador, Gestor o Cliente.");

            var usuario = new Usuario
            {
                Email = email,
                PasswordHash = HashPassword(password),
                Rol = rol,
                IntentosFallidos = 0,
                EstaBloqueado = false
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return usuario;
        }

        /// <summary>
        /// RF-A2: Autenticación con generación de JWT (20 minutos)
        /// </summary>
        public async Task<ResultadoLogin> IniciarSesionAsync(string email, string password)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

            if (usuario == null)
                return new ResultadoLogin
                {
                    Exitoso = false,
                    Error = "Credenciales inválidas.",
                    Usuario = null
                };

            // Verificar si está bloqueado
            if (usuario.EstaBloqueado)
            {
                if (usuario.FechaBloqueo.HasValue &&
                    DateTime.UtcNow < usuario.FechaBloqueo.Value.AddMinutes(AutenticacionReglas.MINUTOS_BLOQUEO))
                {
                    var minutosRestantes = AutenticacionReglas.MinutosRestantesBloqueo(usuario);
                    return new ResultadoLogin
                    {
                        Exitoso = false,
                        Error = $"Cuenta bloqueada. Intente en {minutosRestantes} minutos.",
                        Usuario = null
                    };
                }
                else
                {
                    // Desbloquear automáticamente después de 15 minutos
                    usuario.EstaBloqueado = false;
                    usuario.IntentosFallidos = 0;
                    usuario.FechaBloqueo = null;
                }
            }

            // Verificar contraseña
            if (!VerificarPassword(password, usuario.PasswordHash))
            {
                usuario.IntentosFallidos++;

                // RF-A2: Bloquear después de 5 intentos fallidos
                if (usuario.IntentosFallidos >= AutenticacionReglas.INTENTOS_MAXIMOS_FALLIDOS)
                {
                    usuario.EstaBloqueado = true;
                    usuario.FechaBloqueo = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return new ResultadoLogin
                    {
                        Exitoso = false,
                        Error = "Cuenta bloqueada por 15 minutos debido a múltiples intentos fallidos.",
                        Usuario = null
                    };
                }

                await _context.SaveChangesAsync();

                return new ResultadoLogin
                {
                    Exitoso = false,
                    Error = $"Credenciales inválidas. Intentos restantes: {AutenticacionReglas.INTENTOS_MAXIMOS_FALLIDOS - usuario.IntentosFallidos}",
                    Usuario = null
                };
            }

            // Login exitoso - resetear intentos
            usuario.IntentosFallidos = 0;
            await _context.SaveChangesAsync();

            // Generar token JWT
            var token = GenerarToken(usuario);

            return new ResultadoLogin
            {
                Exitoso = true,
                Token = token,
                Usuario = usuario
            };
        }

        /// <summary>
        /// Desbloquea un usuario
        /// </summary>
        public async Task<bool> DesbloquearUsuarioAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null) return false;

            usuario.EstaBloqueado = false;
            usuario.IntentosFallidos = 0;
            usuario.FechaBloqueo = null;
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Verifica si un email ya existe
        /// </summary>
        public async Task<bool> ExisteEmailAsync(string email)
        {
            return await _context.Usuarios.AnyAsync(u => u.Email == email);
        }

        // ========== MÉTODOS PRIVADOS ==========

        /// <summary>
        /// Genera un JWT token mock con expiración de 20 minutos
        /// </summary>
        private string GenerarToken(Usuario usuario)
        {
            try
            {
                var header = new { alg = "HS256", typ = "JWT" };
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var exp = now + TOKEN_EXPIRATION_SECONDS;

                var payload = new
                {
                    sub = usuario.Id.ToString(),
                    email = usuario.Email,
                    role = usuario.Rol,
                    nombre = usuario.Email,
                    iat = now,
                    exp = exp
                };

                var base64Header = Convert.ToBase64String(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(header)));
                var base64Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(payload)));
                var signature = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{base64Header}.{base64Payload}.{JWT_SECRET}"));

                var token = $"{base64Header}.{base64Payload}.{signature}";
                return token;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error al generar token JWT", ex);
            }
        }

        /// <summary>
        /// Valida formato de contraseña
        /// </summary>
        private bool ValidarPassword(string password)
        {
            return AutenticacionReglas.ValidarFormatoPassword(password);
        }

        /// <summary>
        /// Hash de contraseña usando SHA256
        /// </summary>
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Verifica contraseña
        /// </summary>
        private bool VerificarPassword(string password, string hash)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput == hash;
        }
    }
}