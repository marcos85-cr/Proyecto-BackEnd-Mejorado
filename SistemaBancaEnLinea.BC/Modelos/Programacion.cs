using System.ComponentModel.DataAnnotations;

namespace SistemaBancaEnLinea.BC.Modelos
{
    public class Programacion
    {
        // Se usa la misma PK que la Transaccion para una relación 1:1
        [Key]
        public int TransaccionId { get; set; }
        public Transaccion Transaccion { get; set; } = null!;

        // RF-D3: Fecha en que se debe ejecutar
        [Required]
        public DateTime FechaProgramada { get; set; }

        // RF-D3, RF-E3: Se pueden cancelar hasta 24 horas antes
        [Required]
        public DateTime FechaLimiteCancelacion { get; set; }

        // Estado del job (Pendiente, Ejecutado, Fallido, Cancelado)
        [Required]
        public string EstadoJob { get; set; } = "Pendiente";
    }
}