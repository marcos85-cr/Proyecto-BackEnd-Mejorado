using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.DA;
using SistemaBancaEnLinea.DA.Acciones;

namespace SistemaBancaEnLinea.BW.CU
{
    /// <summary>
    /// Caso de Uso: Transferencias (RF-D1, RF-D2, RF-D3, RF-D4)
    /// </summary>
    public class TransferenciasCU
    {
        private readonly BancaContext _context;
        private readonly CuentaAcciones _cuentaAcciones;
        private readonly TransaccionAcciones _transaccionAcciones;
        private readonly BeneficiarioAcciones _beneficiarioAcciones;
        private readonly AuditoriaAcciones _auditoriaAcciones;

        // Configuración de límites (deberían venir de configuración)
        private const decimal LIMITE_DIARIO = 5000000; // 5 millones
        private const decimal UMBRAL_APROBACION = 1000000; // 1 millón requiere aprobación

        public TransferenciasCU(
            BancaContext context,
            CuentaAcciones cuentaAcciones,
            TransaccionAcciones transaccionAcciones,
            BeneficiarioAcciones beneficiarioAcciones,
            AuditoriaAcciones auditoriaAcciones)
        {
            _context = context;
            _cuentaAcciones = cuentaAcciones;
            _transaccionAcciones = transaccionAcciones;
            _beneficiarioAcciones = beneficiarioAcciones;
            _auditoriaAcciones = auditoriaAcciones;
        }

        /// <summary>
        /// RF-D1: Pre-check de transferencia
        /// </summary>
        public async Task<PreCheckResult> PreCheckTransferenciaAsync(
            int cuentaOrigenId,
            int? cuentaDestinoId,
            int? beneficiarioId,
            decimal monto)
        {
            var resultado = new PreCheckResult();

            // Obtener cuenta origen
            var cuentaOrigen = await _cuentaAcciones.ObtenerPorIdAsync(cuentaOrigenId);
            if (cuentaOrigen == null)
            {
                resultado.EsValido = false;
                resultado.Errores.Add("La cuenta origen no existe.");
                return resultado;
            }

            // Validar que la cuenta esté activa
            if (cuentaOrigen.Estado != "Activa")
            {
                resultado.EsValido = false;
                resultado.Errores.Add("La cuenta origen no está activa.");
                return resultado;
            }

            // Validar saldo suficiente
            if (cuentaOrigen.Saldo < monto)
            {
                resultado.EsValido = false;
                resultado.Errores.Add("Saldo insuficiente.");
                return resultado;
            }

            // Validar límite diario
            var montoTransferidoHoy = await _transaccionAcciones.ObtenerMontoTransferidoHoyAsync(cuentaOrigen.ClienteId);
            if (montoTransferidoHoy + monto > LIMITE_DIARIO)
            {
                resultado.EsValido = false;
                resultado.Errores.Add($"Excede el límite diario. Disponible: {LIMITE_DIARIO - montoTransferidoHoy}");
                return resultado;
            }

            // Si es transferencia a tercero, validar que esté confirmado
            if (beneficiarioId.HasValue)
            {
                var beneficiario = await _beneficiarioAcciones.ObtenerPorIdAsync(beneficiarioId.Value);
                if (beneficiario == null || beneficiario.Estado != "Confirmado")
                {
                    resultado.EsValido = false;
                    resultado.Errores.Add("El beneficiario no está confirmado.");
                    return resultado;
                }
            }

            // Calcular valores
            resultado.EsValido = true;
            resultado.SaldoAntes = cuentaOrigen.Saldo;
            resultado.MontoADebitar = monto;
            resultado.Comision = 0; // Calcular según reglas de negocio
            resultado.SaldoDespues = cuentaOrigen.Saldo - monto - resultado.Comision;
            resultado.RequiereAprobacion = monto > UMBRAL_APROBACION;

            return resultado;
        }

        /// <summary>
        /// RF-D2: Ejecutar transferencia
        /// </summary>
        public async Task<Transaccion> EjecutarTransferenciaAsync(
            int clienteId,
            int cuentaOrigenId,
            int? cuentaDestinoId,
            int? beneficiarioId,
            decimal monto,
            string moneda,
            string idempotencyKey,
            string? descripcion = null)
        {
            // Verificar idempotency
            if (await _transaccionAcciones.ExisteIdempotencyKeyAsync(idempotencyKey))
            {
                var existente = await _transaccionAcciones.ObtenerPorIdempotencyKeyAsync(idempotencyKey);
                return existente!;
            }

            // Pre-check
            var preCheck = await PreCheckTransferenciaAsync(cuentaOrigenId, cuentaDestinoId, beneficiarioId, monto);
            if (!preCheck.EsValido)
            {
                throw new InvalidOperationException(string.Join(", ", preCheck.Errores));
            }

            // Usar transacción de base de datos para atomicidad
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var cuentaOrigen = await _cuentaAcciones.ObtenerPorIdAsync(cuentaOrigenId);

                // Determinar estado inicial
                var estadoInicial = preCheck.RequiereAprobacion ? "PendienteAprobacion" : "Exitosa";

                // Crear transacción
                var transaccion = new Transaccion
                {
                    Tipo = "Transferencia",
                    Estado = estadoInicial,
                    Monto = monto,
                    Moneda = moneda,
                    Comision = preCheck.Comision,
                    IdempotencyKey = idempotencyKey,
                    FechaCreacion = DateTime.UtcNow,
                    SaldoAnterior = preCheck.SaldoAntes,
                    SaldoPosterior = preCheck.SaldoDespues,
                    CuentaOrigenId = cuentaOrigenId,
                    CuentaDestinoId = cuentaDestinoId,
                    BeneficiarioId = beneficiarioId,
                    ClienteId = clienteId,
                    Descripcion = descripcion,
                    ComprobanteReferencia = GenerarReferenciaComprobante()
                };

                // Solo actualizar saldos si no requiere aprobación
                if (!preCheck.RequiereAprobacion)
                {
                    // Debitar cuenta origen
                    cuentaOrigen!.Saldo -= (monto + preCheck.Comision);
                    await _cuentaAcciones.ActualizarAsync(cuentaOrigen);

                    // Acreditar cuenta destino (si es interna)
                    if (cuentaDestinoId.HasValue)
                    {
                        var cuentaDestino = await _cuentaAcciones.ObtenerPorIdAsync(cuentaDestinoId.Value);
                        if (cuentaDestino != null)
                        {
                            cuentaDestino.Saldo += monto;
                            await _cuentaAcciones.ActualizarAsync(cuentaDestino);
                        }
                    }

                    transaccion.FechaEjecucion = DateTime.UtcNow;
                }

                var transaccionCreada = await _transaccionAcciones.CrearAsync(transaccion);

                await transaction.CommitAsync();

                // Auditoría
                await _auditoriaAcciones.RegistrarAsync(
                    clienteId,
                    "Transferencia",
                    $"Transferencia de {monto} {moneda} desde cuenta {cuentaOrigen!.Numero}"
                );

                return transaccionCreada;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private string GenerarReferenciaComprobante()
        {
            return $"TRF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        }
    }

    // Resultado del pre-check de transferencia
    public class PreCheckResult
    {
        public bool EsValido { get; set; }
        public List<string> Errores { get; set; } = new();
        public decimal SaldoAntes { get; set; }
        public decimal MontoADebitar { get; set; }
        public decimal Comision { get; set; }
        public decimal SaldoDespues { get; set; }
        public bool RequiereAprobacion { get; set; }
    }
}