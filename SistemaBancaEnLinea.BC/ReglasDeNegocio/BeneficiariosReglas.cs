using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.BC.ReglasDeNegocio
{
    /// <summary>
    /// RF-C1: Gestión de beneficiarios para transferencias a terceros
    /// </summary>
    public static class BeneficiariosReglas
    {
        // RF-C1: Validaciones de alias
        public const int LONGITUD_MINIMA_ALIAS = 3;
        public const int LONGITUD_MAXIMA_ALIAS = 30;

        // RF-C1: Longitud de número de cuenta
        public const int LONGITUD_MINIMA_CUENTA = 12;
        public const int LONGITUD_MAXIMA_CUENTA = 20;

        // RF-C1: Estados del beneficiario
        public static readonly string[] ESTADOS_BENEFICIARIO = { "Inactivo", "Confirmado", "Rechazado", "Inactivo" };

        public static bool ValidarAlias(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias))
                return false;

            if (alias.Length < LONGITUD_MINIMA_ALIAS || alias.Length > LONGITUD_MAXIMA_ALIAS)
                return false;

            return true;
        }

        public static bool ValidarNumeroCuenta(string numeroCuenta)
        {
            if (string.IsNullOrWhiteSpace(numeroCuenta))
                return false;

            if (numeroCuenta.Length < LONGITUD_MINIMA_CUENTA || numeroCuenta.Length > LONGITUD_MAXIMA_CUENTA)
                return false;

            // Validar que sean solo dígitos
            return System.Text.RegularExpressions.Regex.IsMatch(numeroCuenta, @"^\d+$");
        }

        public static bool EstaBeneficiarioConfirmado(Beneficiario beneficiario)
        {
            return beneficiario.Estado == "Confirmado";
        }

        public static bool PuedeActualizarse(Beneficiario beneficiario)
        {
            return beneficiario.Estado == "Inactivo";
        }
    }
}