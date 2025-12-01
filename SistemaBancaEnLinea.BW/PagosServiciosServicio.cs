using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.DA;
using SistemaBancaEnLinea.DA.Acciones;

namespace SistemaBancaEnLinea.BW
{
    public class PagosServiciosServicio : IPagosServiciosServicio
    {
        private readonly BancaContext _context;
        private readonly ProveedorServicioAcciones _proveedorAcciones;
        private readonly CuentaAcciones _cuentaAcciones;
        private readonly TransaccionAcciones _transaccionAcciones;
        private readonly ProgramacionAcciones _programacionAcciones;
        private readonly AuditoriaAcciones _auditoriaAcciones;
        private readonly ILogger<PagosServiciosServicio> _logger;

        public PagosServiciosServicio(
            BancaContext context,
            ProveedorServicioAcciones proveedorAcciones,
            CuentaAcciones cuentaAcciones,
            TransaccionAcciones transaccionAcciones,
            ProgramacionAcciones programacionAcciones,
            AuditoriaAcciones auditoriaAcciones,
            ILogger<PagosServiciosServicio> logger)
        {
            _context = context;
            _proveedorAcciones = proveedorAcciones;
            _cuentaAcciones = cuentaAcciones;
            _transaccionAcciones = transaccionAcciones;
            _programacionAcciones = programacionAcciones;
            _auditoriaAcciones = auditoriaAcciones;
            _logger = logger;
        }

        public async Task<List<ProveedorServicio>> ObtenerProveedoresAsync()
        {
            return await _proveedorAcciones.ObtenerTodosAsync();
        }

        public async Task<ProveedorServicio?> ObtenerProveedorAsync(int id)
        {
            return await _proveedorAcciones.ObtenerPorIdAsync(id);
        }

        public async Task<bool> ValidarNumeroContratoAsync(int proveedorId, string numeroContrato)
        {
            var proveedor = await _proveedorAcciones.ObtenerPorIdAsync(proveedorId);
            if (proveedor == null) return false;

            return PagosServiciosReglas.ValidarNumeroContrato(numeroContrato, proveedor.ReglaValidacionContrato);
        }

        public async Task<Transaccion> RealizarPagoAsync(PagoServicioRequest request)
        {
            // Verificar idempotency
            if (await _transaccionAcciones.ExisteIdempotencyKeyAsync(request.IdempotencyKey))
            {
                var existente = await _transaccionAcciones.ObtenerPorIdempotencyKeyAsync(request.IdempotencyKey);
                return existente!;
            }

            // Validar proveedor
            var proveedor = await _proveedorAcciones.ObtenerPorIdAsync(request.ProveedorServicioId);
            if (proveedor == null)
                throw new InvalidOperationException("Proveedor de servicio no encontrado.");

            // Validar número de contrato
            if (!PagosServiciosReglas.ValidarNumeroContrato(request.NumeroContrato, proveedor.ReglaValidacionContrato))
                throw new InvalidOperationException("El número de contrato no es válido para este proveedor.");

            // Validar cuenta origen
            var cuentaOrigen = await _cuentaAcciones.ObtenerPorIdAsync(request.CuentaOrigenId);
            if (cuentaOrigen == null)
                throw new InvalidOperationException("Cuenta origen no encontrada.");

            if (!CuentasReglas.EsCuentaActiva(cuentaOrigen))
                throw new InvalidOperationException("La cuenta origen no está activa.");

            // Usar ExecutionStrategy para compatibilidad con SqlServerRetryingExecutionStrategy
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Validar saldo suficiente dentro de la transacción
                    if (cuentaOrigen.Saldo < request.Monto)
                        throw new InvalidOperationException("Saldo insuficiente.");

                    // Crear transacción
                    var transaccion = new Transaccion
                    {
                        Tipo = "PagoServicio",
                        Estado = "Exitosa",
                        Monto = request.Monto,
                        Moneda = cuentaOrigen.Moneda,
                        Comision = 0,
                        IdempotencyKey = request.IdempotencyKey,
                        FechaCreacion = DateTime.UtcNow,
                        FechaEjecucion = DateTime.UtcNow,
                        SaldoAnterior = cuentaOrigen.Saldo,
                        SaldoPosterior = cuentaOrigen.Saldo - request.Monto,
                        CuentaOrigenId = request.CuentaOrigenId,
                        ProveedorServicioId = request.ProveedorServicioId,
                        NumeroContrato = request.NumeroContrato,
                        ClienteId = request.ClienteId,
                        Descripcion = request.Descripcion ?? $"Pago a {proveedor.Nombre}",
                        ComprobanteReferencia = $"PAG-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}"
                    };

                    var transaccionCreada = await _transaccionAcciones.CrearAsync(transaccion);

                    // Actualizar saldo (después de crear la transacción para tener registro)
                    cuentaOrigen.Saldo -= request.Monto;
                    await _cuentaAcciones.ActualizarAsync(cuentaOrigen);

                    // Confirmar la transacción solo si todo fue exitoso
                    await transaction.CommitAsync();

                    // Auditoría (fuera de la transacción para no afectar rollback si falla)
                    try
                    {
                        await _auditoriaAcciones.RegistrarAsync(
                            request.ClienteId,
                            "PagoServicio",
                            $"Pago de {request.Monto} a {proveedor.Nombre}. Contrato: {request.NumeroContrato}"
                        );
                    }
                    catch (Exception auditEx)
                    {
                        _logger.LogWarning($"Error registrando auditoría pero pago fue exitoso: {auditEx.Message}");
                    }

                    _logger.LogInformation($"Pago de servicio {transaccionCreada.Id} realizado exitosamente");
                    return transaccionCreada;
                }
                catch (Exception ex)
                {
                    // Intentar rollback con manejo de errores
                    try
                    {
                        await transaction.RollbackAsync();
                        _logger.LogWarning($"Transacción de pago revertida debido a error: {ex.Message}");
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError($"Error crítico haciendo rollback de pago: {rollbackEx.Message}");
                    }

                    _logger.LogError($"Error realizando pago: {ex.Message}");
                    throw new InvalidOperationException($"Error en pago de servicio: {ex.Message}", ex);
                }
            });
        }

        public async Task<Transaccion> ProgramarPagoAsync(PagoServicioRequest request)
        {
            if (!request.FechaProgramada.HasValue)
                throw new InvalidOperationException("Debe especificar una fecha para el pago programado.");

            if (!ProgramacionReglas.PuedeProgramarse(request.FechaProgramada.Value))
                throw new InvalidOperationException("La fecha programada debe ser al menos 1 hora en el futuro.");

            // Validar proveedor
            var proveedor = await _proveedorAcciones.ObtenerPorIdAsync(request.ProveedorServicioId);
            if (proveedor == null)
                throw new InvalidOperationException("Proveedor de servicio no encontrado.");

            // Validar número de contrato
            if (!PagosServiciosReglas.ValidarNumeroContrato(request.NumeroContrato, proveedor.ReglaValidacionContrato))
                throw new InvalidOperationException("El número de contrato no es válido para este proveedor.");

            // Validar cuenta origen
            var cuentaOrigen = await _cuentaAcciones.ObtenerPorIdAsync(request.CuentaOrigenId);
            if (cuentaOrigen == null)
                throw new InvalidOperationException("Cuenta origen no encontrada.");

            // Usar ExecutionStrategy para compatibilidad con SqlServerRetryingExecutionStrategy
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Crear transacción programada
                    var transaccion = new Transaccion
                    {
                        Tipo = "PagoServicio",
                        Estado = "Programada",
                        Monto = request.Monto,
                        Moneda = cuentaOrigen.Moneda,
                        Comision = 0,
                        IdempotencyKey = request.IdempotencyKey,
                        FechaCreacion = DateTime.UtcNow,
                        SaldoAnterior = cuentaOrigen.Saldo,
                        SaldoPosterior = cuentaOrigen.Saldo,
                        CuentaOrigenId = request.CuentaOrigenId,
                        ProveedorServicioId = request.ProveedorServicioId,
                        NumeroContrato = request.NumeroContrato,
                        ClienteId = request.ClienteId,
                        Descripcion = request.Descripcion ?? $"Pago programado a {proveedor.Nombre}",
                        ComprobanteReferencia = $"PAG-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}"
                    };

                    var transaccionCreada = await _transaccionAcciones.CrearAsync(transaccion);

                    var programacion = new Programacion
                    {
                        TransaccionId = transaccionCreada.Id,
                        FechaProgramada = request.FechaProgramada.Value,
                        FechaLimiteCancelacion = ProgramacionReglas.CalcularFechaLimiteCancelacion(request.FechaProgramada.Value),
                        EstadoJob = "Pendiente"
                    };
                    await _programacionAcciones.CrearAsync(programacion);

                    // Confirmar la transacción solo si todo fue exitoso
                    await transaction.CommitAsync();

                    // Auditoría (fuera de la transacción)
                    try
                    {
                        await _auditoriaAcciones.RegistrarAsync(
                            request.ClienteId,
                            "ProgramacionPagoServicio",
                            $"Pago programado de {request.Monto} a {proveedor.Nombre} para {request.FechaProgramada:dd/MM/yyyy}"
                        );
                    }
                    catch (Exception auditEx)
                    {
                        _logger.LogWarning($"Error registrando auditoría pero programación fue exitosa: {auditEx.Message}");
                    }

                    return transaccionCreada;
                }
                catch (Exception ex)
                {
                    // Intentar rollback con manejo de errores
                    try
                    {
                        await transaction.RollbackAsync();
                        _logger.LogWarning($"Transacción de programación revertida debido a error: {ex.Message}");
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError($"Error crítico haciendo rollback de programación: {rollbackEx.Message}");
                    }

                    _logger.LogError($"Error programando pago: {ex.Message}");
                    throw new InvalidOperationException($"Error programando pago de servicio: {ex.Message}", ex);
                }
            });
        }

        public async Task<List<Transaccion>> ObtenerHistorialPagosAsync(int clienteId)
        {
            var transacciones = await _transaccionAcciones.ObtenerPorClienteAsync(clienteId);
            return transacciones.Where(t => t.Tipo == "PagoServicio").ToList();
        }
    }
}
