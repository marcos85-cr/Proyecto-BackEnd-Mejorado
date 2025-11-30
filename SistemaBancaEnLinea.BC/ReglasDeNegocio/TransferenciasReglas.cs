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
        
        // Límite de autorización para administradores
        public const decimal LIMITE_AUTORIZACION_ADMIN = 10000000; // 10 millones máximo que un admin puede aprobar

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

        /// <summary>
        /// Valida si el monto excede el límite de autorización del administrador
        /// </summary>
        public static bool ExcedeLimiteAutorizacionAdmin(decimal monto)
        {
            return monto > LIMITE_AUTORIZACION_ADMIN;
        }

        /// <summary>
        /// Valida si una transacción puede ser aprobada por un administrador
        /// </summary>
        public static (bool EsValido, string? Error) ValidarAprobacionAdmin(decimal monto, bool tieneValidacionPrevia)
        {
            if (ExcedeLimiteAutorizacionAdmin(monto))
                return (false, $"El monto excede el límite de autorización del administrador ({LIMITE_AUTORIZACION_ADMIN:N0}).");
            
            if (!tieneValidacionPrevia)
                return (false, "La operación requiere validación previa del cliente o gestor.");
            
            return (true, null);
        }
    }
}