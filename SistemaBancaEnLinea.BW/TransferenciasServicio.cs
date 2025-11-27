using Microsoft.Extensions.Logging;
using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.DA;
using SistemaBancaEnLinea.DA.Acciones;
using System.Text;

namespace SistemaBancaEnLinea.BW
{
    public class TransferenciasServicio : ITransferenciasServicio
    {
        private readonly BancaContext _context;
        private readonly CuentaAcciones _cuentaAcciones;
        private readonly TransaccionAcciones _transaccionAcciones;
        private readonly BeneficiarioAcciones _beneficiarioAcciones;
        private readonly AuditoriaAcciones _auditoriaAcciones;
        private readonly ProgramacionAcciones _programacionAcciones;
        private readonly ILogger<TransferenciasServicio> _logger;

        public TransferenciasServicio(
            BancaContext context,
            CuentaAcciones cuentaAcciones,
            TransaccionAcciones transaccionAcciones,
            BeneficiarioAcciones beneficiarioAcciones,
            AuditoriaAcciones auditoriaAcciones,
            ProgramacionAcciones programacionAcciones,
            ILogger<TransferenciasServicio> logger)
        {
            _context = context;
            _cuentaAcciones = cuentaAcciones;
            _transaccionAcciones = transaccionAcciones;
            _beneficiarioAcciones = beneficiarioAcciones;
            _auditoriaAcciones = auditoriaAcciones;
            _programacionAcciones = programacionAcciones;
            _logger = logger;
        }

        public async Task<TransferPrecheck> PreCheckTransferenciaAsync(TransferRequest request)
        {
            var resultado = new TransferPrecheck();

            try
            {
                // Validar monto
                if (!TransferenciasReglas.EsMontoValido(request.Monto))
                {
                    resultado.PuedeEjecutar = false;
                    resultado.Errores.Add($"El monto debe ser al menos {TransferenciasReglas.MONTO_MINIMO}");
                    return resultado;
                }

                // Obtener cuenta origen
                var cuentaOrigen = await _cuentaAcciones.ObtenerPorIdAsync(request.CuentaOrigenId);
                if (cuentaOrigen == null)
                {
                    resultado.PuedeEjecutar = false;
                    resultado.Errores.Add("La cuenta origen no existe.");
                    return resultado;
                }

                // Validar que la cuenta esté activa
                if (!CuentasReglas.EsCuentaActiva(cuentaOrigen))
                {
                    resultado.PuedeEjecutar = false;
                    resultado.Errores.Add("La cuenta origen no está activa.");
                    return resultado;
                }

                // Calcular comisión
                bool esTransferenciaPropia = request.CuentaDestinoId.HasValue && !request.BeneficiarioId.HasValue;
                decimal comision = TransferenciasReglas.CalcularComision(esTransferenciaPropia);

                // Validar saldo suficiente
                if (TransferenciasReglas.ExcedeTransferenciasSaldo(cuentaOrigen.Saldo, request.Monto, comision))
                {
                    resultado.PuedeEjecutar = false;
                    resultado.Errores.Add("Saldo insuficiente.");
                    return resultado;
                }

                // Validar límite diario
                var montoTransferidoHoy = await _transaccionAcciones.ObtenerMontoTransferidoHoyAsync(cuentaOrigen.ClienteId);
                if (TransferenciasReglas.ExcedeLimiteDiario(montoTransferidoHoy, request.Monto))
                {
                    resultado.PuedeEjecutar = false;
                    resultado.Errores.Add($"Excede el límite diario. Disponible: {TransferenciasReglas.LIMITE_DIARIO_TRANSFERENCIA - montoTransferidoHoy}");
                    return resultado;
                }

                // Si es transferencia a tercero, validar que esté confirmado
                if (request.BeneficiarioId.HasValue)
                {
                    var beneficiario = await _beneficiarioAcciones.ObtenerPorIdAsync(request.BeneficiarioId.Value);
                    if (beneficiario == null || !BeneficiariosReglas.EstaBeneficiarioConfirmado(beneficiario))
                    {
                        resultado.PuedeEjecutar = false;
                        resultado.Errores.Add("El beneficiario no está confirmado.");
                        return resultado;
                    }
                }

                // Pre-check exitoso
                resultado.PuedeEjecutar = true;
                resultado.SaldoAntes = cuentaOrigen.Saldo;
                resultado.Monto = request.Monto;
                resultado.Comision = comision;
                resultado.MontoTotal = request.Monto + comision;
                resultado.SaldoDespues = cuentaOrigen.Saldo - request.Monto - comision;
                resultado.RequiereAprobacion = TransferenciasReglas.RequiereAprobacion(request.Monto);
                resultado.LimiteDisponible = TransferenciasReglas.LIMITE_DIARIO_TRANSFERENCIA - montoTransferidoHoy;
                resultado.Mensaje = resultado.RequiereAprobacion
                    ? "Esta transferencia requiere aprobación de un administrador."
                    : "Transferencia lista para ejecutar.";

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en PreCheck: {ex.Message}");
                resultado.PuedeEjecutar = false;
                resultado.Errores.Add("Error interno al validar la transferencia.");
                return resultado;
            }
        }

        public async Task<Transaccion> EjecutarTransferenciaAsync(TransferRequest request)
        {
            // Verificar idempotency
            if (await _transaccionAcciones.ExisteIdempotencyKeyAsync(request.IdempotencyKey))
            {
                var existente = await _transaccionAcciones.ObtenerPorIdempotencyKeyAsync(request.IdempotencyKey);
                return existente!;
            }

            // Pre-check
            var preCheck = await PreCheckTransferenciaAsync(request);
            if (!preCheck.PuedeEjecutar)
            {
                throw new InvalidOperationException(string.Join(", ", preCheck.Errores));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var cuentaOrigen = await _cuentaAcciones.ObtenerPorIdAsync(request.CuentaOrigenId);

                // Determinar estado inicial
                string estadoInicial;
                if (request.Programada && request.FechaProgramada.HasValue)
                {
                    estadoInicial = "Programada";
                }
                else if (preCheck.RequiereAprobacion)
                {
                    estadoInicial = "PendienteAprobacion";
                }
                else
                {
                    estadoInicial = "Exitosa";
                }

                // Crear transacción
                var transaccion = new Transaccion
                {
                    Tipo = "Transferencia",
                    Estado = estadoInicial,
                    Monto = request.Monto,
                    Moneda = request.Moneda,
                    Comision = preCheck.Comision,
                    IdempotencyKey = request.IdempotencyKey,
                    FechaCreacion = DateTime.UtcNow,
                    SaldoAnterior = preCheck.SaldoAntes,
                    SaldoPosterior = preCheck.SaldoDespues,
                    CuentaOrigenId = request.CuentaOrigenId,
                    CuentaDestinoId = request.CuentaDestinoId,
                    BeneficiarioId = request.BeneficiarioId,
                    ClienteId = request.ClienteId,
                    Descripcion = request.Descripcion,
                    ComprobanteReferencia = GenerarReferenciaComprobante()
                };

                // Solo actualizar saldos si es ejecución inmediata
                if (estadoInicial == "Exitosa")
                {
                    cuentaOrigen!.Saldo -= (request.Monto + preCheck.Comision);
                    await _cuentaAcciones.ActualizarAsync(cuentaOrigen);

                    // Acreditar cuenta destino (si es interna)
                    if (request.CuentaDestinoId.HasValue)
                    {
                        var cuentaDestino = await _cuentaAcciones.ObtenerPorIdAsync(request.CuentaDestinoId.Value);
                        if (cuentaDestino != null)
                        {
                            cuentaDestino.Saldo += request.Monto;
                            await _cuentaAcciones.ActualizarAsync(cuentaDestino);
                        }
                    }

                    transaccion.FechaEjecucion = DateTime.UtcNow;
                }

                var transaccionCreada = await _transaccionAcciones.CrearAsync(transaccion);

                // Si es programada, crear registro de programación
                if (request.Programada && request.FechaProgramada.HasValue)
                {
                    var programacion = new Programacion
                    {
                        TransaccionId = transaccionCreada.Id,
                        FechaProgramada = request.FechaProgramada.Value,
                        FechaLimiteCancelacion = ProgramacionReglas.CalcularFechaLimiteCancelacion(request.FechaProgramada.Value),
                        EstadoJob = "Pendiente"
                    };
                    await _programacionAcciones.CrearAsync(programacion);
                }

                await transaction.CommitAsync();

                // Auditoría
                await _auditoriaAcciones.RegistrarAsync(
                    request.ClienteId,
                    "Transferencia",
                    $"Transferencia de {request.Monto} {request.Moneda} desde cuenta {cuentaOrigen!.Numero}. Estado: {estadoInicial}"
                );

                _logger.LogInformation($"Transferencia {transaccionCreada.Id} creada con estado {estadoInicial}");
                return transaccionCreada;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error ejecutando transferencia: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Transaccion>> ObtenerMisTransaccionesAsync(int clienteId)
        {
            return await _transaccionAcciones.ObtenerPorClienteAsync(clienteId);
        }

        public async Task<Transaccion?> ObtenerTransaccionAsync(int id)
        {
            return await _transaccionAcciones.ObtenerPorIdAsync(id);
        }

        public async Task<Transaccion> AprobarTransaccionAsync(int transaccionId, int aprobadorId)
        {
            var transaccion = await _transaccionAcciones.ObtenerPorIdAsync(transaccionId);
            if (transaccion == null)
                throw new InvalidOperationException("Transacción no encontrada.");

            if (transaccion.Estado != "PendienteAprobacion")
                throw new InvalidOperationException("La transacción no está pendiente de aprobación.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Actualizar saldos
                var cuentaOrigen = await _cuentaAcciones.ObtenerPorIdAsync(transaccion.CuentaOrigenId);
                cuentaOrigen!.Saldo -= (transaccion.Monto + transaccion.Comision);
                await _cuentaAcciones.ActualizarAsync(cuentaOrigen);

                if (transaccion.CuentaDestinoId.HasValue)
                {
                    var cuentaDestino = await _cuentaAcciones.ObtenerPorIdAsync(transaccion.CuentaDestinoId.Value);
                    if (cuentaDestino != null)
                    {
                        cuentaDestino.Saldo += transaccion.Monto;
                        await _cuentaAcciones.ActualizarAsync(cuentaDestino);
                    }
                }

                transaccion.Estado = "Exitosa";
                transaccion.FechaEjecucion = DateTime.UtcNow;
                await _transaccionAcciones.ActualizarAsync(transaccion);

                await transaction.CommitAsync();

                await _auditoriaAcciones.RegistrarAsync(
                    aprobadorId,
                    "AprobacionTransferencia",
                    $"Transferencia {transaccionId} aprobada"
                );

                return transaccion;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Transaccion> RechazarTransaccionAsync(int transaccionId, int aprobadorId, string razon)
        {
            var transaccion = await _transaccionAcciones.ObtenerPorIdAsync(transaccionId);
            if (transaccion == null)
                throw new InvalidOperationException("Transacción no encontrada.");

            if (transaccion.Estado != "PendienteAprobacion")
                throw new InvalidOperationException("La transacción no está pendiente de aprobación.");

            transaccion.Estado = "Cancelada";
            transaccion.Descripcion = $"{transaccion.Descripcion} | Rechazada: {razon}";
            await _transaccionAcciones.ActualizarAsync(transaccion);

            await _auditoriaAcciones.RegistrarAsync(
                aprobadorId,
                "RechazoTransferencia",
                $"Transferencia {transaccionId} rechazada. Razón: {razon}"
            );

            return transaccion;
        }

        public async Task<byte[]> DescargarComprobanteAsync(int transaccionId)
        {
            var transaccion = await _transaccionAcciones.ObtenerPorIdAsync(transaccionId);
            if (transaccion == null)
                throw new InvalidOperationException("Transacción no encontrada.");

            // Generar comprobante simple en texto
            var sb = new StringBuilder();
            sb.AppendLine("===========================================");
            sb.AppendLine("       COMPROBANTE DE TRANSFERENCIA");
            sb.AppendLine("===========================================");
            sb.AppendLine();
            sb.AppendLine($"Referencia: {transaccion.ComprobanteReferencia}");
            sb.AppendLine($"Fecha: {transaccion.FechaCreacion:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($"Estado: {transaccion.Estado}");
            sb.AppendLine();
            sb.AppendLine($"Cuenta Origen: {transaccion.CuentaOrigen?.Numero}");
            sb.AppendLine($"Monto: {transaccion.Moneda} {transaccion.Monto:N2}");
            sb.AppendLine($"Comisión: {transaccion.Moneda} {transaccion.Comision:N2}");
            sb.AppendLine($"Total: {transaccion.Moneda} {transaccion.Monto + transaccion.Comision:N2}");
            sb.AppendLine();
            sb.AppendLine($"Descripción: {transaccion.Descripcion}");
            sb.AppendLine();
            sb.AppendLine("===========================================");

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private string GenerarReferenciaComprobante()
        {
            return $"TRF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        }
    }
}