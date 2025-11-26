using SistemaBancaEnLinea.BC.Entidades;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaBancaEnLinea.BC.Modelos
{
    public class Cuenta
    {
        public int Id { get; set; }

        // RF-B1: Número de 12 dígitos, único
        [Required, MaxLength(12), MinLength(12)]
        public string Numero { get; set; }

        // RF-B1: Tipo (Ahorros, Corriente, Inversión, Plazo fijo)
        [Required]
        public string Tipo { get; set; }

        // RF-B1: Moneda (CRC, USD)
        [Required]
        public string Moneda { get; set; }

        // RF-B1: Saldo. Debe ser >= 0
        [Required]
        [Column(TypeName = "decimal(18, 2)")] // Configuración de precisión para EF Core
        public decimal Saldo { get; set; }

        // RF-B1: Estado (Activa, Bloqueada, Cerrada)
        [Required]
        public string Estado { get; set; }

        // Relación con Cliente (FK)
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; }
    }
}
