namespace SistemaBancaEnLinea.BC.ReglasDeNegocio
{
    /// <summary>
    /// Validaciones comunes utilizadas en toda la aplicación
    /// </summary>
    public static class ValidacionesComunes
    {
        public static bool ValidarMonto(decimal monto)
        {
            return monto > 0 && monto <= decimal.MaxValue;
        }

        public static bool ValidarSaldo(decimal saldo)
        {
            return saldo >= 0;
        }

        public static bool ValidarEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static bool ValidarFecha(DateTime fecha)
        {
            return fecha != default && fecha >= DateTime.UtcNow;
        }

        public static string NormalizarEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }
    }
}