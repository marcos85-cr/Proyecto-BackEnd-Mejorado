using SistemaBancaEnLinea.BC.Modelos;
using SistemaBancaEnLinea.DA.Acciones;

namespace SistemaBancaEnLinea.BW.CU
{
   
    public class GestionUsuariosCU
    {
        private readonly UsuarioAcciones _usuarioAcciones;
        private readonly AuditoriaAcciones _auditoriaAcciones;

        public GestionUsuariosCU(UsuarioAcciones usuarioAcciones, AuditoriaAcciones auditoriaAcciones)
        {
            _usuarioAcciones = usuarioAcciones;
            _auditoriaAcciones = auditoriaAcciones;
        }

        public async Task<List<Usuario>> ObtenerTodosLosUsuariosAsync()
        {
            return await _usuarioAcciones.ObtenerTodosAsync();
        }

        public async Task<List<Usuario>> ObtenerGestoresAsync()
        {
            return await _usuarioAcciones.ObtenerPorRolAsync("Gestor");
        }

        public async Task<Usuario?> ObtenerUsuarioPorIdAsync(int id)
        {
            return await _usuarioAcciones.ObtenerPorIdAsync(id);
        }
    }
}