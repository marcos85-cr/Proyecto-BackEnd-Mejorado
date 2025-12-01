using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.BC.ReglasDeNegocio
{
    /// <summary>
    /// RF-D3: Programación de transferencias
    /// RF-E3: Programación de pagos
    /// </summary>
    public static class ProgramacionReglas
    {
        public const int HORAS_MINIMAS_ANTICIPACION = 1;
        public const int HORAS_ANTES_EJECUCION_CANCELABLE = 24;

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

        public static DateTime CalcularFechaLimiteCancelacion(DateTime fechaProgramada) =>
            fechaProgramada.AddHours(-HORAS_ANTES_EJECUCION_CANCELABLE);

        public static bool ValidarEstadoProgramacion(string estado) =>
            ESTADOS_PROGRAMACION.Contains(estado);

        public static int HorasRestantesParaCancelar(DateTime fechaProgramada)
        {
            var horasRestantes = (fechaProgramada - DateTime.UtcNow).TotalHours;
            return Math.Max(0, (int)horasRestantes);
        }
    }
}