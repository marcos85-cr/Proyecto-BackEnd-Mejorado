using SistemaBancaEnLinea.BC.Entidades;
using System.ComponentModel.DataAnnotations;

namespace SistemaBancaEnLinea.BC.Modelos
{
    public class Usuario
    {
        public int Id { get; set; }

        // RF-A1: Email único y válido
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        // RF-A1: Contraseña hasheada
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // RF-A1: Rol {Administrador, Gestor, Cliente}
        [Required]
        public string Rol { get; set; } = string.Empty;

        // RF-A2: Control de intentos fallidos
        public int IntentosFallidos { get; set; } = 0;

        // RF-A2: Estado de bloqueo
        public bool EstaBloqueado { get; set; } = false;

        // RF-A2: Fecha de bloqueo (para los 15 minutos)
        public DateTime? FechaBloqueo { get; set; }

        // Relación 1:1 con Cliente (opcional, solo para rol Cliente)
        public int? ClienteId { get; set; }
        public Cliente? ClienteAsociado { get; set; }

        // Relación 1:N Gestor -> Clientes asignados
        public ICollection<Cliente> ClientesAsignados { get; set; } = new List<Cliente>();
    }
}