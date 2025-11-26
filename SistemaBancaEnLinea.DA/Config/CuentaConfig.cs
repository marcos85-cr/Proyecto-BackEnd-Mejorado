using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.DA.Config
{
    public class CuentaConfig : IEntityTypeConfiguration<Cuenta>
    {
        public void Configure(EntityTypeBuilder<Cuenta> builder)
        {
            builder.ToTable("Cuentas");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Numero)
                .IsRequired()
                .HasMaxLength(12)
                .IsFixedLength();

            builder.HasIndex(c => c.Numero)
                .IsUnique();

            builder.Property(c => c.Tipo)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(c => c.Moneda)
                .IsRequired()
                .HasMaxLength(3);

            builder.Property(c => c.Saldo)
                .HasPrecision(18, 2);

            builder.Property(c => c.Estado)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Activa");

            // Relación 1:N Cliente -> Cuentas
            builder.HasOne(c => c.Cliente)
                .WithMany(cl => cl.Cuentas)
                .HasForeignKey(c => c.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}