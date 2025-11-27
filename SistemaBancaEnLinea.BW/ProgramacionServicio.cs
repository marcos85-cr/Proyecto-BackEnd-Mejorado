using Microsoft.Extensions.Logging;
using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BC.ReglasDeNegocio;
using SistemaBancaEnLinea.BW.Interfaces.BW;
using SistemaBancaEnLinea.DA;
using SistemaBancaEnLinea.DA.Acciones;

namespace SistemaBancaEnLinea.BW
{
    public class ProgramacionServicio : IProgramacionServicio
    {
        private readonly BancaContext _context;
        private readonly ProgramacionAcciones _programacionAcciones;
        private readonly TransaccionAcciones _transaccionAcciones;
        private readonly CuentaAcciones _cuentaAcciones;
        private readonly AuditoriaAcciones _auditoriaAcciones;
        private readonly ILogger<ProgramacionServicio> _logger;

        public ProgramacionServicio(
            BancaContext context,
            ProgramacionAcciones programacionAcciones,
            TransaccionAcciones transaccionAcciones,
            CuentaAcciones cuentaAcciones,
            AuditoriaAcciones auditoriaAcciones,
            ILogger<ProgramacionServicio> logger)
        {
            _context = context;
            _programacionAcciones = programacionAcciones;
            _transaccionAcciones = transaccionAcciones;
            _cuentaAcciones = cuentaAcciones;
            _auditoriaAcciones = auditoriaAcciones;
            _logger = logger;
        }

        public async Task<List<Programacion>> ObtenerProgramacionesClienteAsync(int clienteId)
        {
            return await _programacionAcciones.ObtenerPorClienteAsync(clienteId);
        }

        public async Task<Programacion?> ObtenerProgramacionAsync(int transaccionId)
        {
            return await _programacionAcciones.ObtenerPorIdAsync(transaccionId);
        }

        public async Task<bool> CancelarProgramacionAsync(int transaccionId, int clienteId)
        {
            var programacion = await _programacionAcciones.ObtenerPorIdAsync(transaccionId);
            if (programacion == null)
                throw new InvalidOperationException("Programación no encontrada.");

            if (programacion.Transaccion.ClienteId != clienteId)
                throw new InvalidOperationException("No tiene permiso para cancelar esta programación.");

            if (!ProgramacionReglas.PuedeCancelarse(programacion.FechaProgramada))
                throw new InvalidOperationException(
                    $"Solo puede cancelar programaciones con al menos {ProgramacionReglas.HORAS_ANTES_EJECUCION_CANCELABLE} horas de anticipación.");

            programacion.EstadoJob = "Cancelado";
            programacion.Transaccion.Estado = "Cancelada";

            await _programacionAcciones.ActualizarAsync(programacion);
            await _transaccionAcciones.ActualizarAsync(programacion.Transaccion);

            await _auditoriaAcciones.RegistrarAsync(
                clienteId,
                "CancelacionProgramacion",
                $"Programación {transaccionId} cancelada"
            );

            _logger.LogInformation($"Programación {transaccionId} cancelada por cliente {clienteId}");
            return true;
        }

        public async Task EjecutarProgramacionesPendientesAsync()
        {
            var programacionesPendientes = await _programacionAcciones.ObtenerPendientesAsync();

            foreach (var programacion in programacionesPendientes)
            {
                try
                {
                    await EjecutarProgramacionAsync(programacion);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error ejecutando programación {programacion.TransaccionId}: {ex.Message}");
                    programacion.EstadoJob = "Fallido";
                    programacion.Transaccion.Estado = "Fallida";
                    await _programacionAcciones.ActualizarAsync(programacion);
                }
            }
        }

        private async Task EjecutarProgramacionAsync(Programacion programacion)
        {
            var transaccion = programacion.Transaccion;
            var cuentaOrigen = await _cuentaAcciones.ObtenerPorIdAsync(transaccion.CuentaOrigenId);

            if (cuentaOrigen == null || !CuentasReglas.EsCuentaActiva(cuentaOrigen))
            {
                throw new InvalidOperationException("Cuenta origen no disponible.");
            }

            decimal montoTotal = transaccion.Monto + transaccion.Comision;

            if (cuentaOrigen.Saldo < montoTotal)
            {
                throw new InvalidOperationException("Saldo insuficiente.");
            }

            using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Actualizar saldo cuenta origen
                cuentaOrigen.Saldo -= montoTotal;
                await _cuentaAcciones.ActualizarAsync(cuentaOrigen);

                // Si es transferencia, acreditar cuenta destino
                if (transaccion.Tipo == "Transferencia" && transaccion.CuentaDestinoId.HasValue)
                {
                    var cuentaDestino = await _cuentaAcciones.ObtenerPorIdAsync(transaccion.CuentaDestinoId.Value);
                    if (cuentaDestino != null)
                    {
                        cuentaDestino.Saldo += transaccion.Monto;
                        await _cuentaAcciones.ActualizarAsync(cuentaDestino);
                    }
                }

                // Actualizar transacción y programación
                transaccion.Estado = "Exitosa";
                transaccion.FechaEjecucion = DateTime.UtcNow;
                transaccion.SaldoPosterior = cuentaOrigen.Saldo;
                await _transaccionAcciones.ActualizarAsync(transaccion);

                programacion.EstadoJob = "Ejecutado";
                await _programacionAcciones.ActualizarAsync(programacion);

                await dbTransaction.CommitAsync();

                await _auditoriaAcciones.RegistrarAsync(
                    transaccion.ClienteId,
                    "EjecucionProgramacion",
                    $"Programación {programacion.TransaccionId} ejecutada exitosamente"
                );

                _logger.LogInformation($"Programación {programacion.TransaccionId} ejecutada exitosamente");
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                throw;
            }
        }
    }
}
