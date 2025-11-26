namespace SistemaBancaEnLinea.BC.ReglasDeNegocio
{
    /// <summary>
    /// RF-A3: Gestión de datos de cliente
    /// </summary>
    public static class ClientesReglas
    {
        // RF-A3: Validaciones de cliente
        public const int LONGITUD_MINIMA_IDENTIFICACION = 5;
        public const int LONGITUD_MAXIMA_IDENTIFICACION = 50;

        public const int LONGITUD_MINIMA_NOMBRE = 5;
        public const int LONGITUD_MAXIMA_NOMBRE = 200;

        public static bool ValidarIdentificacion(string identificacion)
        {
            if (string.IsNullOrWhiteSpace(identificacion))
                return false;

            return identificacion.Length >= LONGITUD_MINIMA_IDENTIFICACION &&
                   identificacion.Length <= LONGITUD_MAXIMA_IDENTIFICACION;
        }

        public static bool ValidarNombreCompleto(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return false;

            return nombre.Length >= LONGITUD_MINIMA_NOMBRE &&
                   nombre.Length <= LONGITUD_MAXIMA_NOMBRE;
        }

        public static bool ValidarTelefono(string? telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono))
                return true; // Es opcional

            // Validar que contenga solo dígitos y caracteres de formato
            return System.Text.RegularExpressions.Regex.IsMatch(telefono, @"^[\d\-\+\s]+$");
        }

        public static bool ValidarCorreo(string? correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
                return true; // Es opcional

            try
            {
                var addr = new System.Net.Mail.MailAddress(correo);
                return addr.Address == correo;
            }
            catch
            {
                return false;
            }
        }
    }
}