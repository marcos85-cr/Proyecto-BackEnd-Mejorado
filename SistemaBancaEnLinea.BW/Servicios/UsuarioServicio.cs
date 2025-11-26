using Microsoft.EntityFrameworkCore;
using SistemaBancaEnLinea.BC.Entidades;
using SistemaBancaEnLinea.DA;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SistemaBancaEnLinea.BW.Servicios
{
    public class UsuarioServicio : IUsuarioServicio
    {
        private readonly BancaContext _context;

        public UsuarioServicio(BancaContext context)
        {
            _context = context;
        }

        // RF-A1: Registro de usuarios
        public async Task<Usuario> RegistrarUsuarioAsync(string email, string password, string rol)
        {
            // Validar email único
            if (await _context.Usuarios.AnyAsync(u => u.Email == email))
                throw new InvalidOperationException("El correo electrónico ya está registrado.");

            // Validar formato de contraseña: mínimo 8 caracteres, 1 mayúscula, 1 número, 1 símbolo
            if (!ValidarPassword(password))
                throw new InvalidOperationException("La contraseña debe tener mínimo 8 caracteres, incluir al menos 1 mayúscula, 1 número y 1 símbolo.");

            // Validar rol
            var rolesValidos = new[] { "Administrador", "Gestor", "Cliente" };
            if (!rolesValidos.Contains(rol))
                throw new InvalidOperationException("Rol no válido.");

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

        // RF-A2: Autenticación
        public async Task<(bool Exitoso, string? Token, string? Error)> IniciarSesionAsync(string email, string password)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

            if (usuario == null)
                return (false, null, "Credenciales inválidas.");

            // Verificar si está bloqueado
            if (usuario.EstaBloqueado)
            {
                if (usuario.FechaBloqueo.HasValue &&
                    DateTime.UtcNow < usuario.FechaBloqueo.Value.AddMinutes(15))
                {
                    var minutosRestantes = (usuario.FechaBloqueo.Value.AddMinutes(15) - DateTime.UtcNow).Minutes;
                    return (false, null, $"Cuenta bloqueada. Intente en {minutosRestantes} minutos.");
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
                if (usuario.IntentosFallidos >= 5)
                {
                    usuario.EstaBloqueado = true;
                    usuario.FechaBloqueo = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return (false, null, "Cuenta bloqueada por 15 minutos debido a múltiples intentos fallidos.");
                }

                await _context.SaveChangesAsync();
                return (false, null, $"Credenciales inválidas. Intentos restantes: {5 - usuario.IntentosFallidos}");
            }

            // Login exitoso - resetear intentos
            usuario.IntentosFallidos = 0;
            await _context.SaveChangesAsync();

            // Aquí se generaría el JWT Token (se implementará después)
            var token = GenerarToken(usuario);

            return (true, token, null);
        }

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

        // Helpers
        private bool ValidarPassword(string password)
        {
            if (password.Length < 8) return false;
            if (!Regex.IsMatch(password, @"[A-Z]")) return false; // Al menos 1 mayúscula
            if (!Regex.IsMatch(password, @"[0-9]")) return false; // Al menos 1 número
            if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?""{}|<>]")) return false; // Al menos 1 símbolo
            return true;
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private bool VerificarPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }

        private string GenerarToken(Usuario usuario)
        {
            // Placeholder - Se implementará JWT completo después
            return $"token_{usuario.Id}_{DateTime.UtcNow.Ticks}";
        }
    }
}