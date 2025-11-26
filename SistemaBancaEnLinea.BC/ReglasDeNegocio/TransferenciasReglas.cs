namespace SistemaBancaEnLinea.BC.ReglasDeNegocio
{
    /// <summary>
    /// RF-D1: Pre-check de transferencia
    /// RF-D2: Ejecución de transferencia con idempotency
    /// RF-D3: Programación de transferencias
    /// RF-D4: Estados de transacciones
    /// </summary>
    public static class TransferenciasReglas
    {
        // RF-D1: Límites de transferencia
        public const decimal LIMITE_DIARIO_TRANSFERENCIA = 5000000; // 5 millones
        public const decimal UMBRAL_APROBACION = 1000000; // 1 millón requiere aprobación
        public const decimal MONTO_MINIMO = 100; // Monto mínimo permitido

        // RF-D4: Estados de transacción
        public static readonly string[] ESTADOS_TRANSACCION =
        {
            "Exitosa",
            "PendienteAprobacion",
            "Cancelada",
            "Fallida",
            "Programada"
        };

        // Tipos de transacción
        public static readonly string[] TIPOS_TRANSACCION = { "Transferencia", "PagoServicio" };

        // RF-D3: Comisiones por tipo de transferencia
        public const decimal COMISION_TRANSFERENCIA_PROPIA = 0; // Sin comisión
        public const decimal COMISION_TRANSFERENCIA_TERCERO = 500; // 500 CRC

        public static bool RequiereAprobacion(decimal monto)
        {
            return monto > UMBRAL_APROBACION;
        }

        public static bool EsMontoValido(decimal monto)
        {
            return monto > 0 && monto >= MONTO_MINIMO;
        }

        public static decimal CalcularComision(bool esTransferenciaPropia)
        {
            return esTransferenciaPropia ? COMISION_TRANSFERENCIA_PROPIA : COMISION_TRANSFERENCIA_TERCERO;
        }

        public static bool ExcedeTransferenciasSaldo(decimal saldo, decimal monto, decimal comision)
        {
            return (monto + comision) > saldo;
        }

        public static bool ExcedeLimiteDiario(decimal montoTransferidoHoy, decimal montoNuevo)
        {
            return (montoTransferidoHoy + montoNuevo) > LIMITE_DIARIO_TRANSFERENCIA;
        }

        public static bool ValidarTipoTransaccion(string tipo)
        {
            return TIPOS_TRANSACCION.Contains(tipo);
        }

        public static bool ValidarEstadoTransaccion(string estado)
        {
            return ESTADOS_TRANSACCION.Contains(estado);
        }
    }
}