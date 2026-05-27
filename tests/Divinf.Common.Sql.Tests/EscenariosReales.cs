using System.Collections;
using System.Collections.Generic;

namespace Divinf.Common.Sql.Tests
{
    /// <summary>
    /// Conjunto de escenarios capturados de uso real en Loan para validar paridad.
    /// AGREGAR ESCENARIOS aqui a medida que se identifiquen casos relevantes en el sistema.
    /// Apuntar a cobertura de 30-50 casos antes de cerrar la POC.
    /// </summary>
    public class EscenarioSql
    {
        public string Descripcion { get; set; }
        public string Tabla { get; set; }
        public List<string> Columnas { get; set; } = new List<string>();
        public List<(string tabla, string condicion, bool esLeft)> Joins { get; set; } = new List<(string, string, bool)>();
        public List<string> Wheres { get; set; } = new List<string>();
        public string BaseDeDatos { get; set; } = "MYSQL";
        public string Region { get; set; } = "ARGENTINA";

        public override string ToString() => Descripcion ?? "EscenarioSql";
    }

    public class EscenariosReales : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            // ================== ESCENARIO 1 ==================
            yield return new object[]
            {
                new EscenarioSql
                {
                    Descripcion = "SELECT simple con WHERE",
                    Tabla = "clientes",
                    Columnas = { "id", "nombre" },
                    Wheres = { "estado = 'A'" }
                }
            };

            // ================== ESCENARIO 2 ==================
            yield return new object[]
            {
                new EscenarioSql
                {
                    Descripcion = "SELECT con JOIN interno",
                    Tabla = "clientes c",
                    Columnas = { "c.id", "c.nombre", "d.direccion" },
                    Joins = { ("direcciones d", "d.id_cliente = c.id", false) },
                    Wheres = { "c.estado = 'A'" }
                }
            };

            // ================== ESCENARIO 3 ==================
            yield return new object[]
            {
                new EscenarioSql
                {
                    Descripcion = "SELECT con LEFT JOIN",
                    Tabla = "clientes c",
                    Columnas = { "c.id", "d.direccion" },
                    Joins = { ("direcciones d", "d.id_cliente = c.id", true) }
                }
            };

            // ================== ESCENARIO 4 ==================
            yield return new object[]
            {
                new EscenarioSql
                {
                    Descripcion = "SELECT sin WHERE",
                    Tabla = "productos",
                    Columnas = { "id", "descripcion", "precio" }
                }
            };

            // TODO: agregar mas escenarios hasta llegar a 30-50.
            // Cada escenario que aparezca como discrepante en runtime (logs [Divinf])
            // se agrega aqui como caso de regression.
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
