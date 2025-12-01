using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BC.Modelos.DTOs;

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
        public const decimal LIMITE_DIARIO_DEFAULT = 500000m;

        // RF-B1: Tipos de cuenta válidos
        public static readonly string[] TIPOS_CUENTA_VALIDOS = { "Ahorros", "Corriente", "Ahorro", "Inversión", "Plazo fijo" };

        // RF-B1: Monedas válidas
        public static readonly string[] MONEDAS_VALIDAS = { "CRC", "USD" };

        // RF-B1: Estados de cuenta
        public static readonly string[] ESTADOS_CUENTA = { "Activa", "Bloqueada", "Cerrada" };

        // ==================== VALIDACIONES ====================

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

        /// <summary>
        /// Valida si un cliente puede abrir otra cuenta del mismo tipo y moneda
        /// Restricción: No puede abrir más de 3 cuentas del mismo tipo y moneda
        /// </summary>
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

        // ==================== MAPEOS ====================

        public static CuentaListaDto MapearAListaDto(Cuenta cuenta) =>
            new(
                cuenta.Id,
                cuenta.Numero,
                cuenta.Tipo,
                cuenta.Moneda,
                cuenta.Saldo,
                cuenta.Estado,
                cuenta.FechaApertura,
                cuenta.Cliente?.UsuarioAsociado?.Nombre,
                cuenta.ClienteId,
                LIMITE_DIARIO_DEFAULT,
                cuenta.Saldo
            );

        public static IEnumerable<CuentaListaDto> MapearAListaDto(IEnumerable<Cuenta> cuentas) =>
            cuentas.Select(MapearAListaDto);

        public static CuentaDetalleDto MapearADetalleDto(Cuenta cuenta) =>
            new(
                cuenta.Id,
                cuenta.Numero,
                cuenta.Tipo,
                cuenta.Moneda,
                cuenta.Saldo,
                cuenta.Estado,
                cuenta.FechaApertura,
                cuenta.ClienteId,
                cuenta.Cliente?.UsuarioAsociado?.Nombre ?? "N/A"
            );

        public static CuentaCreacionDto MapearACreacionDto(Cuenta cuenta) =>
            new(
                cuenta.Id,
                cuenta.Numero,
                cuenta.Tipo,
                cuenta.Moneda,
                cuenta.Saldo,
                cuenta.Estado
            );

        public static CuentaBalanceDto MapearABalanceDto(Cuenta cuenta) =>
            new(
                cuenta.Saldo,
                cuenta.Saldo,
                cuenta.Moneda
            );

        public static CuentaEstadoDto MapearAEstadoDto(Cuenta cuenta, string mensaje) =>
            new(
                cuenta.Id,
                cuenta.Numero,
                cuenta.Estado,
                mensaje
            );

        public static CuentaCompletaDto MapearACompletaDto(Cuenta cuenta) =>
            new(
                cuenta.Id,
                cuenta.Numero,
                cuenta.Tipo,
                cuenta.Moneda,
                cuenta.Saldo,
                cuenta.Estado,
                cuenta.FechaApertura,
                cuenta.Cliente != null ? new CuentaRelacionClienteDto(
                    cuenta.Cliente.Id,
                    cuenta.Cliente.Direccion,
                    cuenta.Cliente.FechaNacimiento,
                    cuenta.Cliente.Estado,
                    cuenta.Cliente.FechaRegistro
                ) : null,
                cuenta.Cliente?.UsuarioAsociado != null ? new CuentaRelacionUsuarioDto(
                    cuenta.Cliente.UsuarioAsociado.Id,
                    cuenta.Cliente.UsuarioAsociado.Nombre,
                    cuenta.Cliente.UsuarioAsociado.Email,
                    cuenta.Cliente.UsuarioAsociado.Telefono,
                    cuenta.Cliente.UsuarioAsociado.Identificacion,
                    cuenta.Cliente.UsuarioAsociado.Rol
                ) : null,
                cuenta.Cliente?.GestorAsignado != null ? new CuentaRelacionGestorDto(
                    cuenta.Cliente.GestorAsignado.Id,
                    cuenta.Cliente.GestorAsignado.Nombre,
                    cuenta.Cliente.GestorAsignado.Email
                ) : null
            );

        public static IEnumerable<CuentaCompletaDto> MapearACompletaDto(IEnumerable<Cuenta> cuentas) =>
            cuentas.Select(MapearACompletaDto);
    }
}