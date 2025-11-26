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
    }
}