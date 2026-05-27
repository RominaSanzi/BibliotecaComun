using System;
using Divinf.Common.Sql;

namespace Divinf.Common.SmokeTest
{
    /// <summary>
    /// Smoke test minimo. Valida que la libreria se puede consumir desde un proyecto
    /// .NET Framework 4.8 (mismo runtime que Loan/Web) y genera SQL valido.
    ///
    /// Despues de la POC: cambiar el ProjectReference por PackageReference y
    /// ejecutar este ejecutable para confirmar que el ciclo
    /// "pack -> push al feed -> restore -> consumir" funciona end-to-end.
    /// </summary>
    public class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("=== Divinf.Common.SmokeTest ===");
            Console.WriteLine($"Runtime: {Environment.Version}, OS: {Environment.OSVersion}");
            Console.WriteLine();

            int errores = 0;

            // === Caso 1: SELECT simple ===
            errores += Validar("SELECT simple", () =>
            {
                var g = new GeneradorSql(new SqlOptions { BaseDeDatos = "MYSQL", Region = "ARGENTINA" });
                g.agregarTablaPrincipal("clientes");
                g.agregarColumna("id");
                g.agregarColumna("nombre");
                g.agregarCondicionWhere("estado = 'A'", false);
                return g.generarSelect();
            });

            // === Caso 2: SELECT con JOIN ===
            errores += Validar("SELECT con JOIN", () =>
            {
                var g = new GeneradorSql(new SqlOptions { BaseDeDatos = "MYSQL", Region = "ARGENTINA" });
                g.agregarTablaPrincipal("clientes c");
                g.agregarColumna("c.id");
                g.agregarColumna("d.direccion");
                g.agregarTablaConJoin("direcciones d", "d.id_cliente = c.id", false);
                return g.generarSelect();
            });

            // === Caso 3: INSERT con values ===
            errores += Validar("INSERT", () =>
            {
                var g = new GeneradorSql(new SqlOptions { BaseDeDatos = "MYSQL", Region = "ARGENTINA" });
                g.agregarTabla("clientes");
                g.agregarColumna("nombre");
                g.agregarColumna("apellido");
                g.agregarValue("Juan", false);
                g.agregarValue("Perez", false);
                return g.generarInsert();
            });

            // === Caso 4: UPDATE con SET y WHERE ===
            errores += Validar("UPDATE", () =>
            {
                var g = new GeneradorSql(new SqlOptions { BaseDeDatos = "MYSQL", Region = "ARGENTINA" });
                g.agregarTablaPrincipal("clientes");
                g.agregarSet("nombre = 'Maria'", false);
                g.agregarCondicionWhere("id = 42", false);
                return g.generarUpdate();
            });

            // === Caso 5: DELETE ===
            errores += Validar("DELETE", () =>
            {
                var g = new GeneradorSql(new SqlOptions { BaseDeDatos = "MYSQL", Region = "ARGENTINA" });
                g.agregarTabla("clientes");
                g.agregarCondicionWhere("id = 99", false);
                return g.generarDelete();
            });

            Console.WriteLine();
            Console.WriteLine(errores == 0
                ? "[OK] Todos los casos generaron SQL no vacio."
                : $"[FAIL] {errores} caso(s) fallaron.");

            return errores;
        }

        private static int Validar(string nombre, Func<string> generar)
        {
            try
            {
                string sql = generar();
                bool ok = !string.IsNullOrWhiteSpace(sql);
                Console.WriteLine($"[{(ok ? "OK" : "FAIL")}] {nombre}: {sql}");
                return ok ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAIL] {nombre}: EXCEPTION {ex.Message}");
                return 1;
            }
        }
    }
}
