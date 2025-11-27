using System.ComponentModel.DataAnnotations;

namespace SistemaBancaEnLinea.BC.Modelos
{
    public class Beneficiario
    {
        public int Id { get; set; }

        // RF-C1: Alias (3-30 caracteres, único por cliente)
        [Required, MinLength(3), MaxLength(30)]
        public string Alias { get; set; } = string.Empty;

        // RF-C1: Banco
        [Required]
        public string Banco { get; set; } = string.Empty;

        // RF-C1: Moneda
        [Required]
        public string Moneda { get; set; } = string.Empty;

        // RF-C1: Número de cuenta (12-20 dígitos)
        [Required, MinLength(12), MaxLength(20)]
        public string NumeroCuentaDestino { get; set; } = string.Empty;

        // RF-C1: País
        [Required]
        public string Pais { get; set; } = string.Empty;

        // RF-C1: Inactivo inicialmente, luego Confirmado
        [Required]
        public string Estado { get; set; } = "Inactivo";

        // Fecha de creación
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // FK a Cliente
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;
    }
}