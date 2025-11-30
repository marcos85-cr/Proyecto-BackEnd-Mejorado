namespace SistemaBancaEnLinea.BC.ReglasDeNegocio
{
    /// <summary>
    /// Constantes generales reutilizables en todo el sistema
    /// </summary>
    public static class ConstantesGenerales
    {
        #region Monedas
        
        public const string MONEDA_COLONES = "CRC";
        public const string MONEDA_DOLARES = "USD";
        
        public static readonly string[] MONEDAS_PERMITIDAS = { MONEDA_COLONES, MONEDA_DOLARES };
        
        public static bool EsMonedaValida(string moneda) => 
            MONEDAS_PERMITIDAS.Contains(moneda, StringComparer.OrdinalIgnoreCase);
        
        #endregion

        #region Estados de Cuenta
        
        public const string ESTADO_CUENTA_ACTIVA = "Activa";
        public const string ESTADO_CUENTA_BLOQUEADA = "Bloqueada";
        public const string ESTADO_CUENTA_CERRADA = "Cerrada";
        
        public static readonly string[] ESTADOS_CUENTA = { ESTADO_CUENTA_ACTIVA, ESTADO_CUENTA_BLOQUEADA, ESTADO_CUENTA_CERRADA };
        
        #endregion

        #region Estados de Cliente
        
        public const string ESTADO_CLIENTE_ACTIVO = "Activo";
        public const string ESTADO_CLIENTE_INACTIVO = "Inactivo";
        public const string ESTADO_CLIENTE_BLOQUEADO = "Bloqueado";
        
        #endregion

        #region Estados de Transacción
        
        public const string ESTADO_TRANSACCION_PENDIENTE = "Pendiente";
        public const string ESTADO_TRANSACCION_COMPLETADA = "Completada";
        public const string ESTADO_TRANSACCION_RECHAZADA = "Rechazada";
        public const string ESTADO_TRANSACCION_CANCELADA = "Cancelada";
        
        #endregion

        #region Tipos de Cuenta
        
        public const string TIPO_CUENTA_AHORRO = "Ahorro";
        public const string TIPO_CUENTA_CORRIENTE = "Corriente";
        
        public static readonly string[] TIPOS_CUENTA = { TIPO_CUENTA_AHORRO, TIPO_CUENTA_CORRIENTE };
        
        public static bool EsTipoCuentaValido(string tipo) => 
            TIPOS_CUENTA.Contains(tipo, StringComparer.OrdinalIgnoreCase);
        
        public static string NormalizarTipoCuenta(string tipo) =>
            TIPOS_CUENTA.FirstOrDefault(t => t.Equals(tipo, StringComparison.OrdinalIgnoreCase)) ?? tipo;
        
        #endregion

        #region Roles de Usuario
        
        public const string ROL_ADMINISTRADOR = "Administrador";
        public const string ROL_GESTOR = "Gestor";
        public const string ROL_CLIENTE = "Cliente";
        
        public static readonly string[] ROLES_PERMITIDOS = { ROL_ADMINISTRADOR, ROL_GESTOR, ROL_CLIENTE };
        
        #endregion

        #region Tipos de Transacción
        
        public const string TIPO_TRANSFERENCIA = "Transferencia";
        public const string TIPO_PAGO_SERVICIO = "PagoServicio";
        
        #endregion
    }
}
