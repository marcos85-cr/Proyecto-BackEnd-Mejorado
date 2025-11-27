using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaBancaEnLinea.BC.Modelos
{
    public class Cuenta
    {
        public int Id { get; set; }

        // RF-B1: Número de 12 dígitos, único
        [Required, MinLength(12), MaxLength(12)]
        public string Numero { get; set; } = string.Empty;

        // RF-B1: Tipo (Ahorros, Corriente, Inversión, Plazo fijo)
        [Required]
        public string Tipo { get; set; } = string.Empty;

        // RF-B1: Moneda (CRC, USD)
        [Required]
        public string Moneda { get; set; } = string.Empty;

        // RF-B1: Saldo. Debe ser >= 0
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Saldo { get; set; }

        // RF-B1: Estado (Activa, Bloqueada, Cerrada)
        [Required]
        public string Estado { get; set; } = "Activa";

        // Fecha de apertura de la cuenta
        public DateTime? FechaApertura { get; set; }

        // Relación con Cliente (FK)
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;
    }
}