using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.BC.ReglasDeNegocio
{
    /// <summary>
    /// RF-A1: Registro y autenticación de usuarios
    /// RF-A2: Control de intentos fallidos y bloqueo de cuentas
    /// </summary>
    public static class AutenticacionReglas
    {
        // RF-A1: Validación de contraseña
        public const int LONGITUD_MINIMA_PASSWORD = 8;
        public const string PATRON_PASSWORD = @"(?=.*[A-Z])(?=.*[0-9])(?=.*[!@#$%^&*(),.?""{}|<>])";

        // RF-A2: Bloqueo por intentos fallidos
        public const int INTENTOS_MAXIMOS_FALLIDOS = 5;
        public const int MINUTOS_BLOQUEO = 15;

        // RF-A1: Roles válidos
        public static readonly string[] ROLES_VALIDOS = { "Administrador", "Gestor", "Cliente" };

        public static bool ValidarFormatoPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            if (password.Length < LONGITUD_MINIMA_PASSWORD)
                return false;

            // Validar mayúscula
            if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]"))
                return false;

            // Validar número
            if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[0-9]"))
                return false;

            // Validar símbolo
            if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[!@#$%^&*(),.?""{}|<>]"))
                return false;

            return true;
        }

        public static bool ValidarRol(string rol)
        {
            return ROLES_VALIDOS.Contains(rol);
        }

        public static bool EstaUsuarioBloqueado(Usuario usuario)
        {
            if (!usuario.EstaBloqueado)
                return false;

            if (!usuario.FechaBloqueo.HasValue)
                return true;

            var tiempoTranscurrido = DateTime.UtcNow - usuario.FechaBloqueo.Value;
            return tiempoTranscurrido.TotalMinutes < MINUTOS_BLOQUEO;
        }

        public static int MinutosRestantesBloqueo(Usuario usuario)
        {
            if (!usuario.EstaBloqueado || !usuario.FechaBloqueo.HasValue)
                return 0;

            var tiempoRestante = usuario.FechaBloqueo.Value.AddMinutes(MINUTOS_BLOQUEO) - DateTime.UtcNow;
            return Math.Max(0, (int)tiempoRestante.TotalMinutes);
        }
    }
}
