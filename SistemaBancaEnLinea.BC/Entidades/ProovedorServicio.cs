using SistemaBancaEnLinea.BC.Entidades;
using System.ComponentModel.DataAnnotations;

namespace SistemaBancaEnLinea.BC.Modelos
{
    public class ProveedorServicio
    {
        public int Id { get; set; }

        // RF-E1: Nombre del proveedor
        [Required]
        public string Nombre { get; set; }

        // RF-E1: Regla de validación del número de contrato (e.g., Regex string)
        [Required]
        public string ReglaValidacionContrato { get; set; }

        // FK al Administrador que lo creó (opcional para trazabilidad)
        public int CreadoPorUsuarioId { get; set; }
        public Usuario CreadoPor { get; set; }
    }
}
