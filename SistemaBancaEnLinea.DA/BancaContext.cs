using Microsoft.EntityFrameworkCore;
using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.DA.Config;

namespace SistemaBancaEnLinea.DA
{
    public class BancaContext : DbContext
    {
        public BancaContext(DbContextOptions<BancaContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Cuenta> Cuentas { get; set; }
        public DbSet<Beneficiario> Beneficiarios { get; set; }
        public DbSet<Transaccion> Transacciones { get; set; }
        public DbSet<Programacion> Programaciones { get; set; }
        public DbSet<ProveedorServicio> ProveedoresServicios { get; set; }
        public DbSet<RegistroAuditoria> RegistrosAuditoria { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Aplicar todas las configuraciones desde la carpeta Config
            modelBuilder.ApplyConfiguration(new UsuarioConfig());
            modelBuilder.ApplyConfiguration(new ClienteConfig());
            modelBuilder.ApplyConfiguration(new CuentaConfig());
            modelBuilder.ApplyConfiguration(new BeneficiarioConfig());
            modelBuilder.ApplyConfiguration(new TransaccionConfig());
            modelBuilder.ApplyConfiguration(new ProgramacionConfig());
            modelBuilder.ApplyConfiguration(new ProveedorServicioConfig());
            modelBuilder.ApplyConfiguration(new RegistroAuditoriaConfig());

            base.OnModelCreating(modelBuilder);
        }
    }
}