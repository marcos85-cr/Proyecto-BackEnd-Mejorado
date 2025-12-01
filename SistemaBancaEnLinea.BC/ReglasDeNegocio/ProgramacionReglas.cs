using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.BC.Modelos.DTOs;

namespace SistemaBancaEnLinea.BC.ReglasDeNegocio
{
    /// <summary>
    /// RF-D3: Programación de transferencias
    /// RF-E3: Programación de pagos
    /// </summary>
    public static class ProgramacionReglas
    {
        // Horas mínimas para programar una transferencia
        public const int HORAS_MINIMAS_ANTICIPACION = 1;

        // Horas antes de la ejecución en que se puede cancelar
        public const int HORAS_ANTES_EJECUCION_CANCELABLE = 24;

        // Estados de programación
        public static readonly string[] ESTADOS_PROGRAMACION = { "Pendiente", "Ejecutado", "Fallido", "Cancelado" };

        public static bool PuedeProgramarse(DateTime fechaProgramada)
        {
            var tiempoMinimo = DateTime.UtcNow.AddHours(HORAS_MINIMAS_ANTICIPACION);
            return fechaProgramada > tiempoMinimo;
        }

        public static bool PuedeCancelarse(DateTime fechaProgramada)
        {
            var ahora = DateTime.UtcNow;
            var horasRestantes = (fechaProgramada - ahora).TotalHours;
            return horasRestantes > HORAS_ANTES_EJECUCION_CANCELABLE;
        }

        public static DateTime CalcularFechaLimiteCancelacion(DateTime fechaProgramada)
        {
            return fechaProgramada.AddHours(-HORAS_ANTES_EJECUCION_CANCELABLE);
        }

        public static bool ValidarEstadoProgramacion(string estado)
        {
            return ESTADOS_PROGRAMACION.Contains(estado);
        }

        public static int HorasRestantesParaCancelar(DateTime fechaProgramada)
        {
            var horasRestantes = (fechaProgramada - DateTime.UtcNow).TotalHours;
            return Math.Max(0, (int)horasRestantes);
        }

        #region ========== MAPEO DTOs ==========

        public static ProgramacionListaDto MapearAListaDto(Programacion p) =>
            new(p.TransaccionId, p.Transaccion?.Tipo, p.Transaccion?.Monto,
                p.Transaccion?.Moneda, p.Transaccion?.Descripcion,
                p.FechaProgramada, p.FechaLimiteCancelacion, p.EstadoJob,
                p.EstadoJob == "Pendiente" && DateTime.UtcNow < p.FechaLimiteCancelacion);

        public static IEnumerable<ProgramacionListaDto> MapearAListaDto(IEnumerable<Programacion> programaciones) =>
            programaciones.Select(MapearAListaDto);

        public static ProgramacionResumenDto MapearAResumenDto(Programacion p) =>
            new(p.TransaccionId, p.Transaccion?.Tipo, p.Transaccion?.Monto,
                p.Transaccion?.Moneda, p.FechaProgramada, p.EstadoJob);

        public static IEnumerable<ProgramacionResumenDto> MapearAResumenDto(IEnumerable<Programacion> programaciones) =>
            programaciones.Select(MapearAResumenDto);

        public static ProgramacionDetalleDto MapearADetalleDto(Programacion p) =>
            new(p.TransaccionId, p.TransaccionId, p.Transaccion?.Tipo, p.Transaccion?.Monto,
                p.Transaccion?.Moneda, p.Transaccion?.Descripcion,
                p.FechaProgramada, p.FechaLimiteCancelacion, p.EstadoJob,
                p.EstadoJob == "Pendiente" && DateTime.UtcNow < p.FechaLimiteCancelacion,
                p.Transaccion?.CuentaOrigen?.Numero, p.Transaccion?.CuentaDestino?.Numero);

        #endregion
    }
}