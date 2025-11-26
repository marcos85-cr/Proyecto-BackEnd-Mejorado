using System.ComponentModel.DataAnnotations;

namespace SistemaBancaEnLinea.BC.Modelos
{
    public class Cliente
    {
        public int Id { get; set; }

        // RF-A3: Identificación única
        [Required]
        public string Identificacion { get; set; } = string.Empty;

        // RF-A3: Nombre completo
        [Required]
        public string NombreCompleto { get; set; } = string.Empty;

        // RF-A3: Teléfono
        public string? Telefono { get; set; }

        // RF-A3: Correo
        [EmailAddress]
        public string? Correo { get; set; }

        // Relación 1:1 con Usuario
        public Usuario? UsuarioAsociado { get; set; }

        // Relación 1:N con Cuentas
        public ICollection<Cuenta> Cuentas { get; set; } = new List<Cuenta>();

        // Relación 1:N con Beneficiarios
        public ICollection<Beneficiario> Beneficiarios { get; set; } = new List<Beneficiario>();

        // Relación N:1 con Gestor asignado
        public int? GestorAsignadoId { get; set; }
        public Usuario? GestorAsignado { get; set; }
    }
}