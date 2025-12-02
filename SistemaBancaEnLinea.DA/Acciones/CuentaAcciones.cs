using Microsoft.EntityFrameworkCore;
using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.DA.Acciones
{
    public class CuentaAcciones
    {
        private readonly BancaContext _context;

        public CuentaAcciones(BancaContext context)
        {
            _context = context;
        }

        public async Task<Cuenta?> ObtenerPorIdAsync(int id)
        {
            return await _context.Cuentas
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        /// <summary>
        /// Obtiene una cuenta por ID con relaciones completas (Cliente, Usuario, Gestor)
        /// </summary>
        public async Task<Cuenta?> ObtenerPorIdConRelacionesAsync(int id)
        {
            return await _context.Cuentas
                .Include(c => c.Cliente)
                    .ThenInclude(cl => cl.UsuarioAsociado)
                .Include(c => c.Cliente)
                    .ThenInclude(cl => cl.GestorAsignado)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Cuenta?> ObtenerPorNumeroAsync(string numero)
        {
            return await _context.Cuentas
                .Include(c => c.Cliente)
                .FirstOrDefaultAsync(c => c.Numero == numero);
        }

        public async Task<bool> ExisteNumeroAsync(string numero)
        {
            return await _context.Cuentas.AnyAsync(c => c.Numero == numero);
        }

        public async Task<Cuenta> CrearAsync(Cuenta cuenta)
        {
            _context.Cuentas.Add(cuenta);
            await _context.SaveChangesAsync();
            return cuenta;
        }

        public async Task ActualizarAsync(Cuenta cuenta)
        {
            _context.Cuentas.Update(cuenta);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Cuenta>> ObtenerPorClienteAsync(int clienteId)
        {
            return await _context.Cuentas
                .Where(c => c.ClienteId == clienteId)
                .OrderByDescending(c => c.FechaApertura)
                .ToListAsync();
        }

        public async Task<int> ContarCuentasPorTipoYMonedaAsync(int clienteId, string tipo, string moneda)
        {
            return await _context.Cuentas
                .CountAsync(c => c.ClienteId == clienteId &&
                                 c.Tipo == tipo &&
                                 c.Moneda == moneda);
        }

        public async Task<List<Cuenta>> FiltrarCuentasAsync(int? clienteId, string? tipo, string? moneda, string? estado)
        {
            var query = _context.Cuentas.AsQueryable();

            if (clienteId.HasValue)
                query = query.Where(c => c.ClienteId == clienteId.Value);

            if (!string.IsNullOrEmpty(tipo))
                query = query.Where(c => c.Tipo == tipo);

            if (!string.IsNullOrEmpty(moneda))
                query = query.Where(c => c.Moneda == moneda);

            if (!string.IsNullOrEmpty(estado))
                query = query.Where(c => c.Estado == estado);

            return await query.Include(c => c.Cliente).ToListAsync();
        }

        /// <summary>
        /// Obtiene todas las cuentas con relaciones completas (Cliente, Usuario, Gestor)
        /// </summary>
        public async Task<List<Cuenta>> ObtenerTodasConRelacionesAsync()
        {
            return await _context.Cuentas
                .Include(c => c.Cliente)
                    .ThenInclude(cl => cl.UsuarioAsociado)
                .Include(c => c.Cliente)
                    .ThenInclude(cl => cl.GestorAsignado)
                .OrderByDescending(c => c.FechaApertura)
                .ToListAsync();
        }

        public async Task<bool> TieneTransaccionesAsync(int cuentaId)
        {
            return await _context.Transacciones
                .AnyAsync(t => t.CuentaOrigenId == cuentaId || t.CuentaDestinoId == cuentaId);
        }

        /// <summary>
        /// Verifica si la cuenta tiene operaciones pendientes (programaciones activas o transacciones en proceso)
        /// </summary>
        public async Task<bool> TieneOperacionesPendientesAsync(int cuentaId)
        {
            // Verificar transacciones en estado "Pendiente" o "En Proceso"
            var tieneTansaccionesPendientes = await _context.Transacciones
                .AnyAsync(t => (t.CuentaOrigenId == cuentaId || t.CuentaDestinoId == cuentaId)
                            && (t.Estado == "Pendiente" || t.Estado == "En Proceso"));

            // Verificar programaciones activas (EstadoJob = Pendiente)
            var tieneProgramacionesActivas = await _context.Programaciones
                .AnyAsync(p => p.Transaccion != null
                            && (p.Transaccion.CuentaOrigenId == cuentaId || p.Transaccion.CuentaDestinoId == cuentaId)
                            && p.EstadoJob == "Pendiente");

            return tieneTansaccionesPendientes || tieneProgramacionesActivas;
        }
    }
}