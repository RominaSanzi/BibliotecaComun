namespace Divinf.Common.Sql
{
    /// <summary>
    /// Parametro SQL nombrado para queries con parametros.
    /// Equivalente al tipo ParametroSQL que vive en Loan/Datos.
    /// Se redefine aqui para que la libreria sea autocontenida.
    /// </summary>
    /// <remarks>
    /// Las propiedades estan en camelCase para mantener paridad con la API VB legacy
    /// (que es case-insensitive). No se exponen aliases PascalCase porque colisionarian
    /// con las camelCase desde el lado VB.
    /// </remarks>
    public class ParametroSQL
    {
        public string nombre { get; set; }
        public object valor { get; set; }
        public string nombreCampo { get; set; }
    }
}
