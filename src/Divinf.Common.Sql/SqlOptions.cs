using System.Configuration;

namespace Divinf.Common.Sql
{
    /// <summary>
    /// Opciones de configuracion del GeneradorSql. Reemplaza la lectura directa
    /// de ConfigurationManager.AppSettings que hacia la version VB legacy.
    /// El consumidor (Loan, Collection) es quien lee su propia configuracion y la pasa.
    /// </summary>
    public class SqlOptions
    {
        /// <summary>
        /// Devuelve una instancia con valores leídos de app.config/web.config
        /// (claves "baseDeDatos" y "Region").
        /// </summary>
        public static SqlOptions Default => new SqlOptions
        {
            BaseDeDatos = ConfigurationManager.AppSettings["baseDeDatos"],
            Region = ConfigurationManager.AppSettings["Region"]
        };

        /// <summary>
        /// Motor de base de datos. Valores conocidos:
        /// "MYSQL", "SQLSERVER", "SQLSERVER14".
        /// Equivalente a ConfigurationManager.AppSettings("baseDeDatos") en VB legacy.
        /// </summary>
        public string BaseDeDatos { get; set; }

        /// <summary>
        /// Region operativa. Valores conocidos:
        /// "ARGENTINA", "URUGUAY", "PARAGUAY", "COLOMBIA", "ESPANA".
        /// Equivalente a ConfigurationManager.AppSettings("Region") en VB legacy.
        /// </summary>
        public string Region { get; set; }
    }
}
