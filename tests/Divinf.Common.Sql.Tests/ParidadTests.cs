using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace Divinf.Common.Sql.Tests
{
    /// <summary>
    /// Tests de paridad entre la implementacion C# nueva (Divinf.Common.Sql.GeneradorSql)
    /// y la implementacion VB legacy (Loan1.Utils.GeneradorSql).
    ///
    /// REQUISITO: copiar Loan1.Utils.dll a tests\refs\ antes de correr.
    /// Path tipico de origen:
    ///   C:\Users\Usuarioi7\source\repos\loan1\Utils\bin\Debug\Utils.dll
    /// </summary>
    public class ParidadTests
    {
        private static readonly bool LegacyDllPresente = File.Exists(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "refs", "Loan1.Utils.dll"));

        [Theory]
        [ClassData(typeof(EscenariosReales))]
        public void GeneradorSql_PortC_GeneraMismoSelectQueLegacy(EscenarioSql e)
        {
            // === Implementacion nueva ===
            string sqlNuevo = ConstruirNuevo(e).generarSelect();

            // === Implementacion legacy via reflection (vive en otro assembly) ===
            if (!LegacyDllPresente)
            {
                // Cuando Loan1.Utils.dll no esta presente, saltamos el assert contra legacy
                // y solo verificamos que la nueva impl no tire excepcion.
                Assert.False(string.IsNullOrWhiteSpace(sqlNuevo),
                    "[Skip] Loan1.Utils.dll no encontrado en refs/. La nueva impl deberia al menos producir SQL no vacio.");
                return;
            }

            string sqlLegacy = ConstruirLegacy(e, "generarSelect");
            Assert.Equal(Normalizar(sqlLegacy), Normalizar(sqlNuevo));
        }

        // === Helpers ===

        private static GeneradorSql ConstruirNuevo(EscenarioSql e)
        {
            var gen = new GeneradorSql(new SqlOptions { BaseDeDatos = e.BaseDeDatos, Region = e.Region });
            gen.agregarTablaPrincipal(e.Tabla);
            foreach (var col in e.Columnas) gen.agregarColumna(col);
            foreach (var j in e.Joins) gen.agregarTablaConJoin(j.tabla, j.condicion, j.esLeft);
            foreach (var w in e.Wheres) gen.agregarCondicionWhere(w, false);
            return gen;
        }

        private static string ConstruirLegacy(EscenarioSql e, string metodoGenerar)
        {
            // Cargar el tipo VB legacy por nombre completo
            Assembly legacyAsm = Assembly.LoadFrom(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "refs", "Loan1.Utils.dll"));

            Type legacyType = legacyAsm.GetTypes().FirstOrDefault(t => t.Name == "GeneradorSql");
            Assert.NotNull(legacyType);

            dynamic legacy = Activator.CreateInstance(legacyType);
            legacy.agregarTablaPrincipal(e.Tabla);
            foreach (var col in e.Columnas) legacy.agregarColumna(col);
            foreach (var j in e.Joins) legacy.agregarTablaConJoin(j.tabla, j.condicion, j.esLeft);
            foreach (var w in e.Wheres) legacy.agregarCondicionWhere(w, false);

            MethodInfo metodo = legacyType.GetMethod(metodoGenerar);
            Assert.NotNull(metodo);
            return (string)metodo.Invoke(legacy, null);
        }

        private static string Normalizar(string sql)
        {
            if (sql == null) return "";
            return Regex.Replace(sql, @"\s+", " ").Trim();
        }
    }
}
