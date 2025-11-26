namespace SistemaBancaEnLinea.DA.Entidades
{
    /// <summary>
    /// DTO para resumen de cuenta en extractos
    /// </summary>
    public class ResumenCuenta
    {
        public string NumeroCuenta { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Moneda { get; set; } = string.Empty;
        public decimal SaldoInicial { get; set; }
        public decimal TotalCreditos { get; set; }
        public decimal TotalDebitos { get; set; }
        public decimal TotalComisiones { get; set; }
        public decimal SaldoFinal { get; set; }
        public int CantidadMovimientos { get; set; }
    }
}