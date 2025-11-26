using System.ComponentModel.DataAnnotations;

namespace SistemaBancaEnLinea.BC.Entidades
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required,EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string Rol { get; set; }

        public bool IntentosFallidos { get; set; } = 0;

        public bool EstadoBloqueado { get; set; } = false;

        public DateTime? FechaBloqueo { get; set; }

        public int? ClienteId { get; set; }

        public Cliente? ClientesAsociado { get; set; }

        public ICollection<Cliente> ClientesAsignados { get; set; } = new List<Cliente>();
        public object ClienteAsociado { get; set; }
    }
}
