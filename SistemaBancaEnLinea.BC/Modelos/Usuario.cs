using System.ComponentModel.DataAnnotations;

namespace SistemaBancaEnLinea.BC.Modelos
{
    public class Usuario
    {
        public int Id { get; set; }
 
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
 
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
 
        [Required]
        public string Rol { get; set; } = string.Empty;

        // Datos personales del usuario
        [Required]
        public string Nombre { get; set; } = string.Empty;
        
        public string? Identificacion { get; set; }
        
        public string? Telefono { get; set; }

        // Datos de autenticación y seguridad
        public int IntentosFallidos { get; set; } = 0;
 
        public bool EstaBloqueado { get; set; } = false;
 
        public DateTime? FechaBloqueo { get; set; }
 
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Relación con Cliente
        public int? ClienteId { get; set; }
        public Cliente? ClienteAsociado { get; set; } 
        
        // Clientes asignados (para rol Gestor)
        public ICollection<Cliente> ClientesAsignados { get; set; } = new List<Cliente>();
    }
}