using SistemaBancaEnLinea.BC.Modelos;

namespace SistemaBancaEnLinea.BW.Interfaces.BW
{
    public interface IBeneficiarioServicio
    {
        Task<List<Beneficiario>> ObtenerMisBeneficiariosAsync(int clienteId);
        Task<Beneficiario?> ObtenerBeneficiarioAsync(int id);
        Task<Beneficiario> CrearBeneficiarioAsync(Beneficiario beneficiario);
        Task<Beneficiario> ActualizarBeneficiarioAsync(int id, string alias);
        Task EliminarBeneficiarioAsync(int id);
        Task<Beneficiario> ConfirmarBeneficiarioAsync(int id);
    }
}