using SistemaBancaEnLinea.BC.Entidades;
using System.ComponentModel.DataAnnotations;

namespace SistemaBancaEnLinea.BC.Modelos
{
    public class Beneficiario
    {
        public int Id { get; set; }

        [Required, MinLength(3), MaxLength(30)]
        public string Alias { get; set; }

        [Required]
        public string Banco { get; set; }

        [Required]
        public string Moneda { get; set; }

        [Required, MinLength(12), MaxLength(20)]
        public string NumeroCuentaDestino { get; set; }

        [Required]
        public string Pais { get; set; }

        // RF-C1: Inactivo inicialmente, luego Confirmado
        [Required]
        public string Estado { get; set; } = "Inactivo";

        // FK a Cliente
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; }
    }
}