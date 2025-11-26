using System.ComponentModel.DataAnnotations;

namespace SistemaBancaEnLinea.BC.Modelos
{
    public class ProveedorServicio
    {
        public int Id { get; set; }

        // RF-E1: Nombre del proveedor
        [Required]
        public string Nombre { get; set; } = string.Empty;

        // RF-E1: Regla de validación del número de contrato (Regex)
        [Required]
        public string ReglaValidacionContrato { get; set; } = string.Empty;

        // FK al Administrador que lo creó
        public int CreadoPorUsuarioId { get; set; }
        public Usuario CreadoPor { get; set; } = null!;
    }
}