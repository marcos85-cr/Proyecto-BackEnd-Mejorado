using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaBancaEnLinea.BC.Modelos
{
    public class Transaccion
    {
        public int Id { get; set; }

        // RF-F1: Tipo (Transferencia, PagoServicio)
        [Required]
        public string Tipo { get; set; } = string.Empty;

        // RF-D4: Estados posibles
        [Required]
        public string Estado { get; set; } = string.Empty;

        // Monto de la transacción (debe ser > 0)
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Monto { get; set; }

        // Moneda de la transacción (CRC, USD)
        [Required]
        public string Moneda { get; set; } = string.Empty;

        // Monto de la comisión aplicada
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Comision { get; set; } = 0;

        // RF-D2: Cabecera Idempotency-Key para evitar duplicados
        [Required, MaxLength(50)]
        public string IdempotencyKey { get; set; } = string.Empty;

        // Fecha en que se creó la transacción
        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Fecha en que se ejecutó
        public DateTime? FechaEjecucion { get; set; }

        // RF-F3: Número de referencia único del comprobante
        public string? ComprobanteReferencia { get; set; }

        // Descripción o concepto de la transacción
        public string? Descripcion { get; set; }

        // Saldo antes de la transacción
        [Column(TypeName = "decimal(18, 2)")]
        public decimal SaldoAnterior { get; set; }

        // Saldo después de la transacción
        [Column(TypeName = "decimal(18, 2)")]
        public decimal SaldoPosterior { get; set; }

        // --- Relaciones ---

        // Cuenta Origen (FK) - Obligatoria
        [Required]
        public int CuentaOrigenId { get; set; }
        public Cuenta CuentaOrigen { get; set; } = null!;

        // Cuenta Destino (para transferencias propias)
        public int? CuentaDestinoId { get; set; }
        public Cuenta? CuentaDestino { get; set; }

        // Beneficiario (para transferencias a terceros)
        public int? BeneficiarioId { get; set; }
        public Beneficiario? Beneficiario { get; set; }

        // Proveedor de Servicio (para pagos)
        public int? ProveedorServicioId { get; set; }
        public ProveedorServicio? ProveedorServicio { get; set; }

        // Número de contrato para pagos de servicios (RF-E2)
        public string? NumeroContrato { get; set; }

        // Detalle adicional del destino
        public string? DetalleDestino { get; set; }

        // Cliente que realiza la transacción
        [Required]
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;

        // Programación (RF-D3, RF-E3)
        public Programacion? Programacion { get; set; }
    }
}