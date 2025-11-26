using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.BC.ReglasDeNegocio
{
    /// <summary>
    /// RF-B1: Apertura de cuentas
    /// RF-B2: Consulta de saldo
    /// RF-B3: Bloqueo y cierre de cuentas
    /// </summary>
    public static class CuentasReglas
    {
        // RF-B1: Límites de cuentas
        public const int MAXIMO_CUENTAS_MISMO_TIPO_MONEDA = 3;
        public const int LONGITUD_NUMERO_CUENTA = 12;

        // RF-B1: Tipos de cuenta válidos
        public static readonly string[] TIPOS_CUENTA_VALIDOS = { "Ahorros", "Corriente", "Inversión", "Plazo fijo" };

        // RF-B1: Monedas válidas
        public static readonly string[] MONEDAS_VALIDAS = { "CRC", "USD" };

        // RF-B1: Estados de cuenta
        public static readonly string[] ESTADOS_CUENTA = { "Activa", "Bloqueada", "Cerrada" };

        public static bool EsCuentaActiva(Cuenta cuenta)
        {
            return cuenta.Estado == "Activa";
        }

        public static bool PuedeCerrarse(Cuenta cuenta)
        {
            // Solo se puede cerrar si saldo es 0 y no tiene operaciones pendientes
            return cuenta.Saldo == 0 && cuenta.Estado != "Cerrada";
        }

        public static bool ValidarTipoCuenta(string tipo)
        {
            return TIPOS_CUENTA_VALIDOS.Contains(tipo);
        }

        public static bool ValidarMoneda(string moneda)
        {
            return MONEDAS_VALIDAS.Contains(moneda);
        }

        public static bool PuedeBloquearse(Cuenta cuenta)
        {
            return cuenta.Estado == "Activa";
        }
    }
}