using Microsoft.EntityFrameworkCore;
using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.DA;
using System.Security.Cryptography;
using System.Text;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;

namespace SistemaBancaEnLinea.BW
{
    public class UsuarioServicio : IUsuarioServicio
    {
        private readonly BancaContext _context;
        private const string JWT_SECRET = "TuClaveSecretaMuyLargaYSegura1234567890!@#$%";
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
                EstaBloqueado = false,
                FechaCreacion = DateTime.UtcNow
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
            var usuario = await _context.Usuarios
                .Include(u => u.ClienteAsociado)
                .FirstOrDefaultAsync(u => u.Email == email);

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

            // Obtener el clienteId si existe
            int? clienteId = null;
            if (usuario.Rol == "Cliente")
            {
                var cliente = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.UsuarioAsociado != null && c.UsuarioAsociado.Id == usuario.Id);
                clienteId = cliente?.Id;
            }

            // Generar token JWT con clienteId
            var token = GenerarToken(usuario, clienteId);

            // Actualizar información del usuario con datos del cliente si existe
            if (usuario.ClienteAsociado != null)
            {
                usuario.Nombre = usuario.ClienteAsociado.NombreCompleto;
                usuario.Identificacion = usuario.ClienteAsociado.Identificacion;
                usuario.Telefono = usuario.ClienteAsociado.Telefono;
            }

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

        /// <summary>
        /// Obtiene un usuario por su ID
        /// </summary>
        public async Task<Usuario?> ObtenerPorIdAsync(int id)
        {
            return await _context.Usuarios
                .Include(u => u.ClienteAsociado)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        /// <summary>
        /// Obtiene un usuario por su Email
        /// </summary>
        public async Task<Usuario?> ObtenerPorEmailAsync(string email)
        {
            return await _context.Usuarios
                .Include(u => u.ClienteAsociado)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        /// <summary>
        /// Obtiene todos los usuarios
        /// </summary>
        public async Task<List<Usuario>> ObtenerTodosAsync()
        {
            return await _context.Usuarios
                .Include(u => u.ClienteAsociado)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene usuarios por rol
        /// </summary>
        public async Task<List<Usuario>> ObtenerPorRolAsync(string rol)
        {
            return await _context.Usuarios
                .Where(u => u.Rol == rol)
                .Include(u => u.ClienteAsociado)
                .ToListAsync();
        }

        /// <summary>
        /// Actualiza un usuario
        /// </summary>
        public async Task<Usuario> ActualizarUsuarioAsync(Usuario usuario)
        {
            var existente = await _context.Usuarios.FindAsync(usuario.Id);
            if (existente == null)
                throw new InvalidOperationException("Usuario no encontrado.");

            existente.Nombre = usuario.Nombre;
            existente.Telefono = usuario.Telefono;
            existente.Identificacion = usuario.Identificacion;

            await _context.SaveChangesAsync();
            return existente;
        }

        // ========== MÉTODOS PRIVADOS ==========

        /// <summary>
        /// Genera un JWT token con expiración de 20 minutos
        /// </summary>
        private string GenerarToken(Usuario usuario, int? clienteId = null)
        {
            try
            {
                var header = new { alg = "HS256", typ = "JWT" };
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var exp = now + TOKEN_EXPIRATION_SECONDS;

                var payload = new Dictionary<string, object>
                {
                    { "sub", usuario.Id.ToString() },
                    { "email", usuario.Email },
                    { "role", usuario.Rol },
                    { "nombre", usuario.Nombre ?? usuario.Email },
                    { "iat", now },
                    { "exp", exp }
                };

                // Agregar client_id si es un cliente
                if (clienteId.HasValue)
                {
                    payload.Add("client_id", clienteId.Value.ToString());
                }

                var base64Header = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(header)))
                    .Replace('+', '-').Replace('/', '_').TrimEnd('=');

                var base64Payload = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(payload)))
                    .Replace('+', '-').Replace('/', '_').TrimEnd('=');

                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(JWT_SECRET));
                var input = Encoding.UTF8.GetBytes($"{base64Header}.{base64Payload}");
                var hash = hmac.ComputeHash(input);
                var signature = Convert.ToBase64String(hash)
                    .Replace('+', '-').Replace('/', '_').TrimEnd('=');

                return $"{base64Header}.{base64Payload}.{signature}";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error al generar token JWT", ex);
            }
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