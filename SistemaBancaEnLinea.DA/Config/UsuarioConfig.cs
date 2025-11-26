using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.DA.Config
{
    public class UsuarioConfig : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("Usuarios");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(256);

            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(512);

            builder.Property(u => u.Rol)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(u => u.IntentosFallidos)
                .HasDefaultValue(0);

            builder.Property(u => u.EstaBloqueado)
                .HasDefaultValue(false);

            // Relación 1:1 Usuario <-> Cliente
            builder.HasOne(u => u.ClienteAsociado)
                .WithOne(c => c.UsuarioAsociado)
                .HasForeignKey<Usuario>(u => u.ClienteId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}