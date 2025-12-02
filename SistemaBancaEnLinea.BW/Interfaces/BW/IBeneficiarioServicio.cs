using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.BW.Interfaces.BW
{
    public interface IBeneficiarioServicio
    {
        Task<List<Beneficiario>> ObtenerMisBeneficiariosAsync(int clienteId);
        Task<Beneficiario?> ObtenerBeneficiarioAsync(int id);
        Task<Beneficiario> CrearBeneficiarioAsync(Beneficiario beneficiario);
        Task<Beneficiario>  CrearBeneficiarioParaUsuarioAsync(Beneficiario beneficiario, int usuarioId);
        Task<Beneficiario> ActualizarBeneficiarioAsync(int id, string alias);
        Task<Beneficiario> ActualizarBeneficiarioConAccesoAsync(int id, string alias, int usuarioId, string rol);
        Task EliminarBeneficiarioAsync(int id);
        Task EliminarBeneficiarioConAccesoAsync(int id, int usuarioId, string rol);
        Task<Beneficiario> ConfirmarBeneficiarioAsync(int id);
        Task<Beneficiario> ConfirmarBeneficiarioConAccesoAsync(int id, int usuarioId, string rol);
        Task<(Beneficiario? beneficiario, bool tieneOperaciones)> ObtenerBeneficiarioConAccesoAsync(int id, int usuarioId, string rol);
        Task<bool> TieneOperacionesPendientesAsync(int beneficiarioId);
    }
}