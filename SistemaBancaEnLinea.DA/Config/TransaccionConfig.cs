using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.DA.Config
{
    public class TransaccionConfig : IEntityTypeConfiguration<Transaccion>
    {
        public void Configure(EntityTypeBuilder<Transaccion> builder)
        {
            builder.ToTable("Transacciones");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Tipo)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(t => t.Estado)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(t => t.Monto)
                .HasPrecision(18, 2);

            builder.Property(t => t.Moneda)
                .IsRequired()
                .HasMaxLength(3);

            builder.Property(t => t.Comision)
                .HasPrecision(18, 2)
                .HasDefaultValue(0);

            builder.Property(t => t.IdempotencyKey)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(t => t.IdempotencyKey)
                .IsUnique();

            builder.Property(t => t.FechaCreacion)
                .IsRequired();

            builder.Property(t => t.ComprobanteReferencia)
                .HasMaxLength(100);

            builder.Property(t => t.Descripcion)
                .HasMaxLength(500);

            builder.Property(t => t.SaldoAnterior)
                .HasPrecision(18, 2);

            builder.Property(t => t.SaldoPosterior)
                .HasPrecision(18, 2);

            builder.Property(t => t.NumeroContrato)
                .HasMaxLength(50);

            builder.Property(t => t.DetalleDestino)
                .HasMaxLength(1000);

            // Relación Cuenta Origen (Obligatoria)
            builder.HasOne(t => t.CuentaOrigen)
                .WithMany()
                .HasForeignKey(t => t.CuentaOrigenId)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Cuenta Destino (Opcional)
            builder.HasOne(t => t.CuentaDestino)
                .WithMany()
                .HasForeignKey(t => t.CuentaDestinoId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Beneficiario (Opcional)
            builder.HasOne(t => t.Beneficiario)
                .WithMany()
                .HasForeignKey(t => t.BeneficiarioId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación ProveedorServicio (Opcional)
            builder.HasOne(t => t.ProveedorServicio)
                .WithMany()
                .HasForeignKey(t => t.ProveedorServicioId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Cliente (Obligatoria)
            builder.HasOne(t => t.Cliente)
                .WithMany()
                .HasForeignKey(t => t.ClienteId)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            // Índices para búsquedas
            builder.HasIndex(t => t.FechaCreacion);
            builder.HasIndex(t => t.Estado);
            builder.HasIndex(t => t.ClienteId);
        }
    }
}