using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SistemaBancaEnLinea.BC.Entidades
{
    public class Cliente
    {
        public string Id { get; set; }
        [Required]
        public string Identificacion { get; set; }
        [Required]
        public string NombreCompleto { get; set; }
        public string Telefono { get; set; }
        public string Correo { get; set; }
        public Usuario UsuarioAsociado { get; set; }
        public ICollection<Cuenta> Cuentas { get; set; } = new List<Cuenta>();
        public ICollection<Beneficiario> Beneficiarios { get; set; } = new List<Beneficiario>();
        public int? GestorAsignadoId { get; set; }
        public Usuario GestorAsignado { get; set; }

    }
}
