namespace Divinf.Common.Sql
{
    /// <summary>
    /// Parametro SQL para stored procedures.
    /// Equivalente al tipo ParametrosSQLStoredProcedureVO que vive en Loan/Datos.
    /// </summary>
    public class ParametroSQLStoredProcedure
    {
        public string nombreParametro { get; set; }
        public object valor { get; set; }
    }
}
