using System.ComponentModel.DataAnnotations;

namespace SistemaBancaEnLinea.BC.Modelos
{
    public class RegistroAuditoria
    {
        public int Id { get; set; }

        [Required]
        public DateTime FechaHora { get; set; } = DateTime.UtcNow;

        // RF-G2: Tipo de operación
        [Required]
        public string TipoOperacion { get; set; } = string.Empty;

        // Detalle de la acción
        [Required]
        public string Descripcion { get; set; } = string.Empty;

        // Usuario que realizó la acción
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; } = null!;

        // Estado del objeto antes del cambio (JSON)
        public string? DetalleJson { get; set; }
    }
}