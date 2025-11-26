using SistemaBancaEnLinea.BC.Entidades;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaBancaEnLinea.BC.Modelos
{
    public class Transaccion
    {
        public int Id { get; set; }

        // RF-F1: Tipo (Transferencia, Pago de Servicio)
        [Required]
        public string Tipo { get; set; }

        // RF-D4: Estado (Pendiente Aprobacion, Programada, Exitosa, Fallida, Cancelada, Rechazada)
        [Required]
        public string Estado { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Monto { get; set; }

        // Monto de la comisión aplicada
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Comision { get; set; } = 0;

        // RF-D2: Cabecera Idempotency-Key
        [Required, MaxLength(50)]
        public string IdempotencyKey { get; set; }

        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaEjecucion { get; set; }

        // RF-F3: Número de referencia del comprobante
        public string ComprobanteReferencia { get; set; }

        // --- Relaciones de Origen y Destino ---

        // Cuenta Origen (FK)
        public int CuentaOrigenId { get; set; }
        public Cuenta CuentaOrigen { get; set; }

        // Destino (Puede ser una Cuenta Propia/Tercero o un Pago de Servicio)
        public string DetalleDestino { get; set; } // Guarda JSON o texto del destino final (e.g., Num. Contrato y Proveedor)

        // FK al cliente/usuario que realiza la transacción
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; }

        // Propiedad de Navegación para operaciones programadas (RF-D3, RF-E3)
        public Programacion Programacion { get; set; }
    }
}