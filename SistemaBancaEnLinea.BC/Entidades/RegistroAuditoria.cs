using SistemaBancaEnLinea.BC.Entidades;
using System.ComponentModel.DataAnnotations;

namespace SistemaBancaEnLinea.BC.Modelos
{
    public class RegistroAuditoria
    {
        public int Id { get; set; }

        [Required]
        public DateTime FechaHora { get; set; } = DateTime.Now;

        // RF-G2: Tipo de operación (Creación de usuarios, apertura de cuentas, etc.)
        [Required]
        public string TipoOperacion { get; set; }

        // Detalle de la acción (e.g., "Se aprobó la transferencia #12345")
        [Required]
        public string Descripcion { get; set; }

        // Identificador del usuario que realizó la acción
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; }

        // Puede usarse para guardar el estado del objeto antes del cambio
        public string DetalleJson { get; set; }
    }
}
