using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.BC.ReglasDeNegocio
{
    /// <summary>
    /// Reglas de negocio para la gestión de cuentas bancarias
    /// </summary>
    public static class CuentasReglas
    {
        public const int MAXIMO_CUENTAS_MISMO_TIPO_MONEDA = 3;
        public const int LONGITUD_NUMERO_CUENTA = 12;
        public const decimal LIMITE_DIARIO_DEFAULT = 500000m;

        public static readonly string[] TIPOS_CUENTA_VALIDOS = { "Ahorros", "Corriente", "Ahorro", "Inversión", "Plazo fijo" };
        public static readonly string[] MONEDAS_VALIDAS = { "CRC", "USD" };
        public static readonly string[] ESTADOS_CUENTA = { "Activa", "Bloqueada", "Cerrada" };

        public static bool EsCuentaActiva(Cuenta cuenta) =>
            cuenta.Estado == "Activa";

        public static bool PuedeCerrarse(Cuenta cuenta) =>
            cuenta.Saldo == 0 && cuenta.Estado != "Cerrada";

        public static bool ValidarTipoCuenta(string tipo) =>
            TIPOS_CUENTA_VALIDOS.Any(t => t.Equals(tipo, StringComparison.OrdinalIgnoreCase));

        public static bool ValidarMoneda(string moneda) =>
            MONEDAS_VALIDAS.Contains(moneda.ToUpperInvariant());

        public static bool PuedeBloquearse(Cuenta cuenta) =>
            cuenta.Estado == "Activa";

        public static (bool EsValido, string? Error) ValidarCreacionCuenta(string tipo, string moneda, decimal saldoInicial)
        {
            if (string.IsNullOrWhiteSpace(tipo))
                return (false, "El tipo de cuenta es requerido.");

            if (!ValidarTipoCuenta(tipo))
                return (false, $"Tipo de cuenta inválido: {tipo}. Use: Ahorro o Corriente.");

            if (string.IsNullOrWhiteSpace(moneda))
                return (false, "La moneda es requerida.");

            if (!ValidarMoneda(moneda))
                return (false, $"Moneda inválida: {moneda}. Use: CRC o USD.");

            if (saldoInicial < 0)
                return (false, "El saldo inicial no puede ser negativo.");

            return (true, null);
        }

        public static (bool EsValido, string? Error) ValidarMaximoCuentasMismoTipoMoneda(
            IEnumerable<Cuenta> cuentasExistentes, string tipo, string moneda)
        {
            var cuentasMismoTipoMoneda = cuentasExistentes
                .Count(c => c.Tipo.Equals(tipo, StringComparison.OrdinalIgnoreCase) 
                         && c.Moneda.Equals(moneda, StringComparison.OrdinalIgnoreCase)
                         && c.Estado != "Cerrada" && c.Estado != "Inactiva");

            if (cuentasMismoTipoMoneda >= MAXIMO_CUENTAS_MISMO_TIPO_MONEDA)
                return (false, $"El cliente ya tiene el máximo de {MAXIMO_CUENTAS_MISMO_TIPO_MONEDA} cuentas de tipo {tipo} en {moneda}.");

            return (true, null);
        }
    }
}