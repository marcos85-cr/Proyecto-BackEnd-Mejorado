using SistemaBancaEnLinea.BC.Entidades;
using System.ComponentModel.DataAnnotations;

namespace SistemaBancaEnLinea.BC.Modelos
{
    public class Beneficiario
    {
        public int Id { get; set; }

        // RF-C1: Alias (3-30 caracteres, único por cliente)
        [Required, MaxLength(30), MinLength(3)]
        public string Alias { get; set; }

        // RF-C1: Banco
        [Required]
        public string Banco { get; set; }

        // RF-C1: Moneda
        [Required]
        public string Moneda { get; set; }

        // RF-C1: Número de cuenta (12-20 dígitos)
        [Required, MaxLength(20), MinLength(12)]
        public string NumeroCuentaDestino { get; set; }

        // RF-C1: País
        [Required]
        public string Pais { get; set; }

        // RF-C1: Inactivo (inicialmente) / Confirmado
        [Required]
        public string Estado { get; set; } = "Inactivo";

        // Relación con Cliente (FK)
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; }
    }
}