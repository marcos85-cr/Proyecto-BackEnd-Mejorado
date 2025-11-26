using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.DA.Config
{
    public class ClienteConfig : IEntityTypeConfiguration<Cliente>
    {
        public void Configure(EntityTypeBuilder<Cliente> builder)
        {
            builder.ToTable("Clientes");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Identificacion)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(c => c.Identificacion)
                .IsUnique();

            builder.Property(c => c.NombreCompleto)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(c => c.Telefono)
                .HasMaxLength(20);

            builder.Property(c => c.Correo)
                .HasMaxLength(256);

            // Relación 1:N Gestor -> Clientes
            builder.HasOne(c => c.GestorAsignado)
                .WithMany(u => u.ClientesAsignados)
                .HasForeignKey(c => c.GestorAsignadoId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}