namespace SistemaBancaEnLinea.BC.Entidades
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Rol { get; set; }
        public bool EstadoBloqueado { get; set; }
        public DateTime? FechaBloqueo { get; set; }
        public object ClientesAsignados { get; set; }
    }
}
