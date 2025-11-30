using Microsoft.EntityFrameworkCore;
using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BC.Modelos.DTOs;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.DA;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace SistemaBancaEnLinea.BW
{
    /// <summary>
    /// Servicio de gestión de usuarios
    /// Implementa operaciones CRUD con validaciones integradas
    /// El controlador solo debe llamar a estos métodos sin lógica adicional
    /// </summary>
    public class UsuarioServicio : IUsuarioServicio
    {
        private readonly BancaContext _context;
        private readonly IConfiguration _configuration;

        // Configuración JWT
        private const int TOKEN_EXPIRATION_SECONDS = 60000;

        public UsuarioServicio(BancaContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        #region Autenticación

        /// <summary>
        /// RF-A2: Autenticación con control de intentos fallidos
        /// Genera JWT con expiración de 20 minutos
        /// </summary>
        public async Task<ResultadoLogin> IniciarSesionAsync(string email, string password)
        {
            var usuario = await ObtenerPorEmailAsync(email);

            if (usuario == null)
                return ResultadoLogin.Fallido("Credenciales inválidas.");

            // Verificar bloqueo usando reglas de AutenticacionReglas
            if (AutenticacionReglas.EstaUsuarioBloqueado(usuario))
            {
                var minutosRestantes = AutenticacionReglas.MinutosRestantesBloqueo(usuario);
                return ResultadoLogin.Fallido($"Cuenta bloqueada. Intente en {minutosRestantes} minutos.");
            }

            // Desbloqueo automático si pasaron los 15 minutos
            if (usuario.EstaBloqueado && !AutenticacionReglas.EstaUsuarioBloqueado(usuario))
            {
                UsuarioReglas.AplicarDesbloqueo(usuario);
            }

            // Verificar contraseña
            if (!VerificarPassword(password, usuario.PasswordHash))
            {
                return await ProcesarIntentoFallido(usuario);
            }

            // Login exitoso
            usuario.IntentosFallidos = 0;
            await _context.SaveChangesAsync();

            // Obtener clienteId si aplica
            int? clienteId = await ObtenerClienteIdAsync(usuario);

            var token = GenerarToken(usuario, clienteId);
            return ResultadoLogin.Exito(token, usuario);
        }

        #endregion

        #region Consultas

        /// <summary>
        /// Obtiene todos los usuarios con clientes asociados
        /// </summary>
        public async Task<List<Usuario>> ObtenerTodosAsync() =>
            await _context.Usuarios
                .Include(u => u.ClienteAsociado)
                .OrderByDescending(u => u.FechaCreacion)
                .ToListAsync();

        /// <summary>
        /// Obtiene un usuario por ID con cliente asociado
        /// </summary>
        public async Task<Usuario?> ObtenerPorIdAsync(int id) =>
            await _context.Usuarios
                .Include(u => u.ClienteAsociado)
                .FirstOrDefaultAsync(u => u.Id == id);

        /// <summary>
        /// Obtiene usuarios por rol específico
        /// </summary>
        public async Task<List<Usuario>> ObtenerPorRolAsync(string rol) =>
            await _context.Usuarios
                .Where(u => u.Rol == rol)
                .Include(u => u.ClienteAsociado)
                .ToListAsync();

        /// <summary>
        /// Verifica si un email ya está registrado
        /// </summary>
        public async Task<bool> ExisteEmailAsync(string email) =>
            await _context.Usuarios.AnyAsync(u => u.Email == email);

        #endregion

        #region Operaciones CRUD con Validaciones Integradas

        /// <summary>
        /// Crea un nuevo usuario con todas las validaciones
        /// Incluye: validación de datos, email único, hash de contraseña
        /// </summary>
        public async Task<ResultadoOperacion<Usuario>> CrearUsuarioAsync(UsuarioRequest request)
        {
            // Validar datos usando reglas de negocio BC
            var validacion = UsuarioReglas.ValidarCreacionUsuario(
                request.Email,
                request.Password ?? "",
                request.Role,
                request.Nombre,
                request.Identificacion,
                request.Telefono);

            if (!validacion.EsValido)
                return ResultadoOperacion<Usuario>.Fallo(validacion.Mensaje);

            // Validar email único en BD
            if (await ExisteEmailAsync(request.Email))
                return ResultadoOperacion<Usuario>.Fallo("El correo electrónico ya está registrado.");

            // Usar ExecutionStrategy para compatibilidad con SqlServerRetryingExecutionStrategy
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Crear usuario
                    var usuario = new Usuario
                    {
                        Email = request.Email,
                        PasswordHash = HashPassword(request.Password!),
                        Rol = request.Role,
                        Nombre = request.Nombre ?? request.Email,
                        Identificacion = request.Identificacion,
                        Telefono = request.Telefono,
                        IntentosFallidos = 0,
                        EstaBloqueado = false,
                        FechaCreacion = DateTime.UtcNow
                    };

                    _context.Usuarios.Add(usuario);
                    await _context.SaveChangesAsync();

                    // Si es rol Cliente, crear un Cliente asociado
                    if (request.Role == "Cliente")
                    {
                        var cliente = new Cliente
                        {
                            Estado = "Activo",
                            FechaRegistro = DateTime.UtcNow,
                            UsuarioAsociado = usuario
                        };
                        _context.Clientes.Add(cliente);
                        await _context.SaveChangesAsync();

                        usuario.ClienteId = cliente.Id;
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                    return ResultadoOperacion<Usuario>.Exito(usuario);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    var innerMessage = ex.InnerException?.Message ?? ex.Message;
                    return ResultadoOperacion<Usuario>.Fallo($"Error al crear usuario: {innerMessage}");
                }
            });
        }

        /// <summary>
        /// Actualiza un usuario con todas las validaciones
        /// Incluye: validación de existencia, email único, campos válidos
        /// Restricción: No puede cambiar su propio rol
        /// </summary>
        public async Task<ResultadoOperacion<Usuario>> ActualizarUsuarioAsync(int id, UsuarioRequest request, int solicitanteId)
        {
            // Buscar usuario existente
            var usuario = await ObtenerPorIdAsync(id);
            if (usuario == null)
                return ResultadoOperacion<Usuario>.Fallo("Usuario no encontrado.");

            // Restricción: No puede cambiar su propio rol
            if (id == solicitanteId && !string.IsNullOrWhiteSpace(request.Role) && request.Role != usuario.Rol)
                return ResultadoOperacion<Usuario>.Fallo("No puede cambiar su propio rol.");

            // Verificar email único si se está cambiando
            bool nuevoEmailExiste = !string.IsNullOrWhiteSpace(request.Email)
                && request.Email != usuario.Email
                && await ExisteEmailAsync(request.Email);

            // Validar usando reglas de negocio BC
            var validacion = UsuarioReglas.ValidarActualizacionUsuario(
                request.Nombre,
                request.Telefono,
                request.Identificacion,
                request.Email,
                usuario.Email,
                nuevoEmailExiste,
                request.Role);

            if (!validacion.EsValido)
                return ResultadoOperacion<Usuario>.Fallo(validacion.Mensaje);

            // Usar ExecutionStrategy para compatibilidad con SqlServerRetryingExecutionStrategy
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Re-obtener usuario dentro de la transacción
                    var usuarioTx = await _context.Usuarios.FindAsync(id);
                    if (usuarioTx == null)
                        return ResultadoOperacion<Usuario>.Fallo("Usuario no encontrado.");

                    // Aplicar actualizaciones usando reglas BC (solo email y rol)
                    UsuarioReglas.AplicarActualizaciones(
                        usuarioTx,
                        request.Email,
                        request.Role);

                    // Actualizar datos personales en usuario
                    if (!string.IsNullOrWhiteSpace(request.Nombre))
                        usuarioTx.Nombre = request.Nombre;
                    if (!string.IsNullOrWhiteSpace(request.Telefono))
                        usuarioTx.Telefono = request.Telefono;
                    if (!string.IsNullOrWhiteSpace(request.Identificacion))
                        usuarioTx.Identificacion = request.Identificacion;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return ResultadoOperacion<Usuario>.Exito(usuarioTx);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    var innerMessage = ex.InnerException?.Message ?? ex.Message;
                    return ResultadoOperacion<Usuario>.Fallo($"Error al actualizar usuario: {innerMessage}");
                }
            });
        }

        /// <summary>
        /// Bloquea o desbloquea un usuario con validaciones de permisos
        /// Incluye: validación de existencia, permisos de admin, estado actual
        /// </summary>
        public async Task<ResultadoOperacion<Usuario>> ToggleBloqueoUsuarioAsync(int usuarioId, int adminId)
        {
            var usuario = await ObtenerPorIdAsync(usuarioId);
            if (usuario == null)
                return ResultadoOperacion<Usuario>.Fallo("Usuario no encontrado.");

            bool nuevoEstado = !usuario.EstaBloqueado;

            if (nuevoEstado)
            {
                // Validar si puede bloquear
                var validacion = UsuarioReglas.PuedeBloquearUsuario(usuario, adminId);
                if (!validacion.EsValido)
                    return ResultadoOperacion<Usuario>.Fallo(validacion.Mensaje);
            }
            else
            {
                // Validar si puede desbloquear
                var validacion = UsuarioReglas.PuedeDesbloquearUsuario(usuario);
                if (!validacion.EsValido)
                    return ResultadoOperacion<Usuario>.Fallo(validacion.Mensaje);
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var usuarioTx = await _context.Usuarios.FindAsync(usuarioId);
                    if (usuarioTx == null)
                        return ResultadoOperacion<Usuario>.Fallo("Usuario no encontrado.");

                    if (nuevoEstado)
                        UsuarioReglas.AplicarBloqueo(usuarioTx);
                    else
                        UsuarioReglas.AplicarDesbloqueo(usuarioTx);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return ResultadoOperacion<Usuario>.Exito(usuarioTx);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    var innerMessage = ex.InnerException?.Message ?? ex.Message;
                    return ResultadoOperacion<Usuario>.Fallo($"Error al cambiar estado de bloqueo: {innerMessage}");
                }
            });
        }

        /// <summary>
        /// Cambia la contraseña con todas las validaciones
        /// Incluye: validación de permisos, contraseña actual, formato nueva contraseña
        /// </summary>
        public async Task<ResultadoOperacion<bool>> CambiarContrasenaAsync(
            int usuarioId,
            int solicitanteId,
            string rolSolicitante,
            string contrasenaActual,
            string nuevaContrasena)
        {
            // Validar permisos: solo el mismo usuario o un admin puede cambiar
            if (solicitanteId != usuarioId && rolSolicitante != "Administrador")
                return ResultadoOperacion<bool>.Fallo("No tiene permisos para cambiar esta contraseña.");

            // Obtener usuario
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
                return ResultadoOperacion<bool>.Fallo("Usuario no encontrado.");

            // Verificar contraseña actual
            if (!VerificarPassword(contrasenaActual, usuario.PasswordHash))
                return ResultadoOperacion<bool>.Fallo("La contraseña actual es incorrecta.");

            // Validar formato de nueva contraseña
            var validacion = UsuarioReglas.ValidarContrasena(nuevaContrasena);
            if (!validacion.EsValido)
                return ResultadoOperacion<bool>.Fallo(validacion.Mensaje);

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var usuarioTx = await _context.Usuarios.FindAsync(usuarioId);
                    if (usuarioTx == null)
                        return ResultadoOperacion<bool>.Fallo("Usuario no encontrado.");

                    usuarioTx.PasswordHash = HashPassword(nuevaContrasena);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return ResultadoOperacion<bool>.Exito(true);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    var innerMessage = ex.InnerException?.Message ?? ex.Message;
                    return ResultadoOperacion<bool>.Fallo($"Error al cambiar contraseña: {innerMessage}");
                }
            });
        }

        /// <summary>
        /// Elimina un usuario validando referencias en otras tablas
        /// </summary>
        public async Task<ResultadoOperacion<bool>> EliminarUsuarioAsync(int usuarioId, int adminId)
        {
            // Validar que no se elimine a sí mismo
            if (usuarioId == adminId)
                return ResultadoOperacion<bool>.Fallo("No puede eliminarse a sí mismo.");

            // Obtener usuario
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
                return ResultadoOperacion<bool>.Fallo("Usuario no encontrado.");

            // Validar que no sea administrador
            if (usuario.Rol == "Administrador")
                return ResultadoOperacion<bool>.Fallo("No se puede eliminar a un administrador.");

            // Verificar referencias en otras tablas
            var referencias = await ValidarReferenciasUsuarioAsync(usuarioId);
            if (!string.IsNullOrEmpty(referencias))
                return ResultadoOperacion<bool>.Fallo($"No se puede eliminar el usuario. {referencias}");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var usuarioTx = await _context.Usuarios.FindAsync(usuarioId);
                    if (usuarioTx == null)
                        return ResultadoOperacion<bool>.Fallo("Usuario no encontrado.");

                    _context.Usuarios.Remove(usuarioTx);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return ResultadoOperacion<bool>.Exito(true);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    var innerMessage = ex.InnerException?.Message ?? ex.Message;
                    return ResultadoOperacion<bool>.Fallo($"Error al eliminar usuario: {innerMessage}");
                }
            });
        }

        /// <summary>
        /// Valida si el usuario tiene referencias en otras tablas
        /// </summary>
        private async Task<string> ValidarReferenciasUsuarioAsync(int usuarioId)
        {
            var referencias = new List<string>();

            // Verificar si tiene cliente asociado
            var tieneCliente = await _context.Clientes
                .AnyAsync(c => c.UsuarioAsociado != null && c.UsuarioAsociado.Id == usuarioId);
            if (tieneCliente)
                referencias.Add("Cliente asociado");

            // Verificar si es gestor de clientes
            var esGestor = await _context.Clientes
                .AnyAsync(c => c.GestorAsignadoId == usuarioId);
            if (esGestor)
                referencias.Add("Clientes asignados como gestor");

            // Verificar registros de auditoría
            var tieneAuditoria = await _context.RegistrosAuditoria
                .AnyAsync(r => r.UsuarioId == usuarioId);
            if (tieneAuditoria)
                referencias.Add("Registros de auditoría");

            if (referencias.Count == 0)
                return string.Empty;

            return $"Tiene vinculaciones con: {string.Join(", ", referencias)}."; 
        }

        #endregion

        #region Métodos Privados - Autenticación

        /// <summary>
        /// Obtiene un usuario por email (uso interno)
        /// </summary>
        private async Task<Usuario?> ObtenerPorEmailAsync(string email) =>
            await _context.Usuarios
                .Include(u => u.ClienteAsociado)
                .FirstOrDefaultAsync(u => u.Email == email);

        /// <summary>
        /// Procesa un intento de login fallido
        /// </summary>
        private async Task<ResultadoLogin> ProcesarIntentoFallido(Usuario usuario)
        {
            usuario.IntentosFallidos++;

            if (usuario.IntentosFallidos >= AutenticacionReglas.INTENTOS_MAXIMOS_FALLIDOS)
            {
                UsuarioReglas.AplicarBloqueo(usuario);
                await _context.SaveChangesAsync();
                return ResultadoLogin.Fallido("Cuenta bloqueada por 15 minutos debido a múltiples intentos fallidos.");
            }

            await _context.SaveChangesAsync();
            var intentosRestantes = AutenticacionReglas.INTENTOS_MAXIMOS_FALLIDOS - usuario.IntentosFallidos;
            return ResultadoLogin.Fallido($"Credenciales inválidas. Intentos restantes: {intentosRestantes}");
        }

        /// <summary>
        /// Obtiene el ID del cliente asociado si el usuario es Cliente
        /// </summary>
        private async Task<int?> ObtenerClienteIdAsync(Usuario usuario)
        {
            if (usuario.Rol != "Cliente") return null;

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.UsuarioAsociado != null && c.UsuarioAsociado.Id == usuario.Id);
            return cliente?.Id;
        }

        #endregion

        #region Métodos Privados - JWT y Seguridad

        /// <summary>
        /// Genera un token JWT con expiración de 20 minutos usando librerías estándar
        /// </summary>
        private string GenerarToken(Usuario usuario, int? clienteId = null)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key no configurada");
            var key = Encoding.UTF8.GetBytes(jwtKey);

            var claims = new List<Claim>
            {
                new Claim("sub", usuario.Id.ToString()),
                new Claim("email", usuario.Email),
                new Claim("role", usuario.Rol),
                new Claim("nombre", usuario.Nombre ?? usuario.Email)
            };

            if (clienteId.HasValue)
                claims.Add(new Claim("client_id", clienteId.Value.ToString()));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddSeconds(TOKEN_EXPIRATION_SECONDS),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        private static bool VerificarPassword(string password, string hash) =>
            HashPassword(password) == hash;

        #endregion
    }
}
