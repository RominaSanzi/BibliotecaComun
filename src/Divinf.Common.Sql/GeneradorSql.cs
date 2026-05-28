using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text.RegularExpressions;
using Divinf.Common.Sql.Internal;

namespace Divinf.Common.Sql
{
    /// <summary>
    /// Query builder portado literal desde loan1\Utils\GeneradorSql.vb (943 LOC).
    /// Mantiene la misma API publica (camelCase) que la version VB para garantizar
    /// que los call sites de Loan funcionen sin cambios al adoptar la libreria.
    /// </summary>
    public class GeneradorSql
    {
        private enum EnumColecciones
        {
            TABLAS = 1,
            COMUMNAS = 2,
            WHERES = 3,
            TABLASESPECIALES = 4,
            JOIN = 5,
            GROUPBY = 6,
            LIMIT = 7,
            HAVING = 8,
            ORDER = 9,
            COMUMNASESPECIALES = 10,
            VALUES = 11,
            VALUESAGRUPADOS = 12,
            SETS = 13
        }

        #region Variables
        private List<string> iColumnas;
        private List<string> iTablas;
        private string iTablaPrincipal = "";
        private List<string> iCondicionesJoin;
        private List<string> iCondicionesWhere;
        private List<string> iCondicionesWhereConOr;
        private List<string> iSet;
        private List<string> iSelect;
        private List<string> iOrden;
        private List<string> iValues;
        private List<string> iValuesAgrupados;
        private List<string> iGroupBy;
        private List<string> iLimit;
        private List<string> iHaving;
        private bool iBloquearTabla;
        private List<ParametroSQL> iParametrosSQL;
        private List<ParametroSQLStoredProcedure> iParametrosSQLStoredProcedure;
        private int iIndiceParametro;
        private string iBaseDeDatos;
        private string iRegion;
        #endregion

        #region Atributos
        public List<ParametroSQL> parametrosSQL
        {
            get { return iParametrosSQL; }
            set { iParametrosSQL = value; }
        }

        public bool bloquearTabla
        {
            get { return iBloquearTabla; }
            set { iBloquearTabla = value; }
        }

        public List<ParametroSQLStoredProcedure> ParametrosSQLStoredProcedure
        {
            get { return iParametrosSQLStoredProcedure; }
            set { iParametrosSQLStoredProcedure = value; }
        }
        #endregion

        #region Constructores
        /// <summary>
        /// Constructor sin parametros. Lee baseDeDatos y Region desde app.config/web.config
        /// via ConfigurationManager.AppSettings. Equivalente al comportamiento del VB legacy.
        /// </summary>
        public GeneradorSql() : this(SqlOptions.Default) { }

        /// <summary>
        /// Constructor principal. Recibe opciones por inyeccion en lugar de leer ConfigurationManager.
        /// </summary>
        public GeneradorSql(SqlOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            iColumnas = new List<string>();
            iTablas = new List<string>();
            iCondicionesJoin = new List<string>();
            iCondicionesWhere = new List<string>();
            iCondicionesWhereConOr = new List<string>();
            iSet = new List<string>();
            iSelect = new List<string>();
            iValues = new List<string>();
            iValuesAgrupados = new List<string>();
            iOrden = new List<string>();
            iLimit = new List<string>();
            iGroupBy = new List<string>();
            iHaving = new List<string>();
            iParametrosSQL = new List<ParametroSQL>();
            iParametrosSQLStoredProcedure = new List<ParametroSQLStoredProcedure>();
            iBaseDeDatos = options.BaseDeDatos;
            iRegion = options.Region;
        }
        #endregion

        #region Helpers internos para preservar semantica VB
        // VB Right(s, n)
        private static string Right(string s, int n)
        {
            if (s == null) return "";
            if (n <= 0) return "";
            if (n >= s.Length) return s;
            return s.Substring(s.Length - n);
        }

        // VB Left(s, n)
        private static string Left(string s, int n)
        {
            if (s == null) return "";
            if (n <= 0) return "";
            if (n >= s.Length) return s;
            return s.Substring(0, n);
        }

        // VB Trim(s) — VB acepta null y devuelve ""; C# String.Trim sobre null tira NRE
        private static string Trim(string s)
        {
            return s == null ? "" : s.Trim();
        }

        // VB Len(s)
        private static int Len(string s)
        {
            return s == null ? 0 : s.Length;
        }

        // VB InStr(s, find) — 1-indexed, 0 si no encuentra
        private static int InStr(string s, string find)
        {
            if (s == null || find == null) return 0;
            return s.IndexOf(find) + 1;
        }

        // VB "x <> Nothing" para String: equivale a no-vacio
        private static bool NotEmpty(string s)
        {
            return !string.IsNullOrEmpty(s);
        }

        // Comodin: el placeholder ("@" para SqlServer, "?" para MySQL/otros)
        private string PlaceholderPrefix
        {
            get { return (iBaseDeDatos == "SQLSERVER" || iBaseDeDatos == "SQLSERVER14") ? "@" : "?"; }
        }
        #endregion

        #region Metodos

        public void agregarColumna(string eColumna)
        {
            iColumnas.Add(eColumna);
        }

        public void agregarTabla(string eTabla)
        {
            iTablas.Add(eTabla);
        }

        public void agregarTablaPrincipal(string eTabla)
        {
            iTablaPrincipal = eTabla;
        }

        public void agregarTablaConJoin(string eTabla, string eCondicion, bool esLeft = false)
        {
            if (esLeft)
            {
                iCondicionesJoin.Add(" LEFT JOIN " + eTabla + " ON " + eCondicion);
            }
            else
            {
                iCondicionesJoin.Add(" INNER JOIN " + eTabla + " ON " + eCondicion);
            }
        }

        public void agregarOrden(string eOrden)
        {
            iOrden.Add(eOrden);
        }

        public void agregarGroupBy(string eGroupBy)
        {
            // Le quitamos la palabra asc y/o desc al group by para que no falle cuando migramos a MySql8
            eGroupBy = Regex.Replace(eGroupBy ?? "", @"\bdesc\b", "", RegexOptions.IgnoreCase);
            eGroupBy = Regex.Replace(eGroupBy, @"\basc\b", "", RegexOptions.IgnoreCase);
            eGroupBy = eGroupBy.Trim();
            iGroupBy.Add(eGroupBy);
        }

        public void agregarValue(string eValue, bool eSinGuardarParametros = false)
        {
            ParametroSQL iParametroSQL = new ParametroSQL();

            try
            {
                iIndiceParametro += 1;
                if (!eSinGuardarParametros)
                {
                    // Columnas: la VB Collection es 1-indexed; iColumnas.Item(iValues.Count + 1) en VB
                    // equivale a iColumnas[iValues.Count] en C#.
                    string colName = iColumnas[iValues.Count];
                    iParametroSQL.valor = eValue;
                    iParametroSQL.nombre = colName + iIndiceParametro;
                    iParametroSQL.nombreCampo = colName;
                    iParametrosSQL.Add(iParametroSQL);

                    iValues.Add(PlaceholderPrefix + colName + iIndiceParametro);
                }
                else
                {
                    iValues.Add(eValue);
                }
            }
            catch
            {
                // Comportamiento legacy: swallow.
            }
        }

        public void agregarValueEncriptado(string eValue, string ePalabraClave)
        {
            ParametroSQL iParametroSQL = new ParametroSQL();

            try
            {
                iIndiceParametro += 1;

                string colName = iColumnas[iValues.Count];
                iParametroSQL.valor = HashUtils.ObtenerPassword(eValue, ePalabraClave);
                iParametroSQL.nombre = colName + iIndiceParametro;
                iParametroSQL.nombreCampo = colName;
                iParametrosSQL.Add(iParametroSQL);

                iValues.Add(PlaceholderPrefix + colName + iIndiceParametro);
            }
            catch
            {
            }
        }

        public void agregarSet(string eSet, bool eSinGuardarParametros = false)
        {
            string iDerecha, iIzquierda;
            ParametroSQL iParametroSQL = new ParametroSQL();
            ParametroSQLStoredProcedure iParametroSQLStoreProcedure = new ParametroSQLStoredProcedure();

            try
            {
                iIndiceParametro += 1;

                if (!eSinGuardarParametros && NotEmpty(eSet) && eSet.IndexOf("=") > 0)
                {
                    iDerecha = Trim(Right(eSet, eSet.Length - eSet.IndexOf("=") - 1));
                    iIzquierda = Trim(Left(eSet, eSet.IndexOf("=")));

                    iParametroSQL.valor = iDerecha;
                    iParametroSQL.nombre = iIzquierda + iIndiceParametro;
                    iParametroSQL.nombreCampo = iIzquierda;
                    iParametrosSQL.Add(iParametroSQL);

                    iParametroSQLStoreProcedure = new ParametroSQLStoredProcedure();
                    iParametroSQLStoreProcedure.nombreParametro = "up" + iIzquierda.Replace(".", "");
                    iParametroSQLStoreProcedure.valor = iDerecha;
                    iParametrosSQLStoredProcedure.Add(iParametroSQLStoreProcedure);

                    iSet.Add(iIzquierda + "=" + PlaceholderPrefix + iIzquierda + iIndiceParametro);
                }
                else if (NotEmpty(eSet))
                {
                    iSet.Add(eSet);
                }
            }
            catch
            {
            }
        }

        public void agregarSetEncriptado(string eSet, string ePalabraClave, bool eSinGuardarParametros = false)
        {
            string iDerecha, iIzquierda;
            ParametroSQL iParametroSQL = new ParametroSQL();

            try
            {
                iIndiceParametro += 1;

                if (!eSinGuardarParametros && NotEmpty(eSet) && eSet.IndexOf("=") > 0)
                {
                    iDerecha = Right(eSet, eSet.Length - eSet.IndexOf("=") - 1);
                    iIzquierda = Trim(Left(eSet, eSet.IndexOf("=")));

                    iParametroSQL.valor = HashUtils.ObtenerPassword(iDerecha, ePalabraClave);
                    iParametroSQL.nombre = iIzquierda + iIndiceParametro;
                    iParametroSQL.nombreCampo = iIzquierda;
                    iParametrosSQL.Add(iParametroSQL);

                    iSet.Add(iIzquierda + "=" + PlaceholderPrefix + iIzquierda + iIndiceParametro);
                }
                else if (NotEmpty(eSet))
                {
                    iSet.Add(eSet);
                }
            }
            catch
            {
            }
        }

        public void agregarLimit(string eLimit)
        {
            iLimit.Add(eLimit);
        }

        public void agregarSelect(string eSelect)
        {
            iSelect.Add(eSelect);
        }

        public void agregarValuesAgrupados(string eSelect)
        {
            iValuesAgrupados.Add(eSelect);
        }

        public void agregarHaving(string eHaving)
        {
            iHaving.Add(eHaving);
        }

        public string generarWhereConOr()
        {
            string iConsulta = "";

            // agregamos las condiciones del where
            if (iCondicionesWhereConOr.Count > 0)
            {
                iConsulta = iterarColeccionWhereConOr(iCondicionesWhereConOr);
            }

            vaciarColeccionesWhereOr();

            return iConsulta;
        }

        public string generarWhere()
        {
            string iConsulta = "";

            // agregamos las condiciones del where
            if (iCondicionesWhere.Count > 0)
            {
                iConsulta = iterarColeccionWhere(iCondicionesWhere);
            }

            vaciarColeccionesWhere();

            return iConsulta;
        }

        public string generarSelect()
        {
            string iConsulta;
            // agregamos las columnas
            iConsulta = "SELECT ";
            if (iBaseDeDatos == "SQLSERVER" || iBaseDeDatos == "SQLSERVER14")
            {
                // agregamos el limit
                if (iLimit.Count > 0)
                {
                    iConsulta = iConsulta + " TOP " + iLimit[0] + " ";
                }
            }

            iConsulta += iterarColeccion(iColumnas, EnumColecciones.COMUMNASESPECIALES, ",");

            // agregamos las tablas
            if (iTablaPrincipal.Length > 0)
            {
                iConsulta = iConsulta + " FROM " + iTablaPrincipal;
                // agrego el bloqueo sobre la tabla principal
                if ((iBaseDeDatos == "SQLSERVER" || iBaseDeDatos == "SQLSERVER14") && bloquearTabla)
                {
                    iConsulta = iConsulta + " WITH (UPDLOCK) ";
                }
                // AGREGAMOS JOINS
                iConsulta += iterarColeccion(iCondicionesJoin, EnumColecciones.JOIN, "");
                if (iTablas.Count > 0)
                {
                    iConsulta = iConsulta + ", " + iterarColeccion(iTablas, EnumColecciones.TABLAS, ",");
                }
            }
            else
            {
                iConsulta = iConsulta + " FROM " + iterarColeccion(iTablas, EnumColecciones.TABLASESPECIALES, ",");
                // AGREGAMOS JOINS
                iConsulta += iterarColeccion(iCondicionesJoin, EnumColecciones.JOIN, "");
            }

            // agregamos las condiciones del where
            if (iCondicionesWhere.Count > 0)
            {
                iConsulta = iConsulta + " WHERE " + iterarColeccionWhere(iCondicionesWhere);
            }

            // agregamos las columnas del GROUP BY
            if (iGroupBy.Count > 0)
            {
                iConsulta = iConsulta + " GROUP BY " + iterarColeccion(iGroupBy, EnumColecciones.GROUPBY, ",");
            }

            // agregamos el having
            if (iHaving.Count > 0)
            {
                iConsulta = iConsulta + " HAVING " + iterarColeccionWhere(iHaving);
            }

            // agregamos las columnas del order by
            if (iOrden.Count > 0)
            {
                iConsulta = iConsulta + " ORDER BY " + iterarColeccion(iOrden, EnumColecciones.ORDER, ",");
            }

            if (iBaseDeDatos == "MYSQL")
            {
                // agregamos el limit
                if (iLimit.Count > 0)
                {
                    iConsulta = iConsulta + " LIMIT " + iLimit[0];
                }
            }

            if (iBaseDeDatos == "MYSQL" && iBloquearTabla)
            {
                // agregamos FOR UPDATE
                iConsulta = iConsulta + " FOR UPDATE ";
            }

            vaciarColecciones();

            if (iBaseDeDatos == "SQLSERVER" || iBaseDeDatos == "SQLSERVER14")
            {
                iConsulta = Regex.Replace(iConsulta, @"ifnull\(", "isnull(", RegexOptions.IgnoreCase);
                iConsulta = Regex.Replace(iConsulta, @"length\(", "len(", RegexOptions.IgnoreCase);
                iConsulta = Regex.Replace(iConsulta, @"now\(\)", "getdate()", RegexOptions.IgnoreCase);
            }
            else
            {
                iConsulta = Regex.Replace(iConsulta, @"isnull\(", "ifnull(", RegexOptions.IgnoreCase);
                iConsulta = Regex.Replace(iConsulta, @"len\(", "length(", RegexOptions.IgnoreCase);
                iConsulta = Regex.Replace(iConsulta, @"getdate\(\)", "now()", RegexOptions.IgnoreCase);
            }

            switch (iRegion)
            {
                case "ARGENTINA":
                case "URUGUAY":
                    break;
                case "PARAGUAY":
                    iConsulta = iConsulta.Replace("$", "Gs");
                    break;
                case "COLOMBIA":
                case "ESPANA":
                    break;
            }

            return iConsulta;
        }

        public string generarInsert()
        {
            string iConsulta;

            // agregamos la tabla
            iConsulta = "INSERT INTO " + iterarColeccion(iTablas, EnumColecciones.TABLAS, ",");

            // agregamos las columnas
            iConsulta = iConsulta + " (" + iterarColeccion(iColumnas, EnumColecciones.COMUMNAS, ",") + ")";

            // agregamos los values
            if (iValues.Count > 0) iConsulta = iConsulta + " VALUES " + " (" + iterarColeccion(iValues, EnumColecciones.VALUES, ",") + ")";

            // agregamos los values agrupados
            if (iValuesAgrupados.Count > 0) iConsulta = iConsulta + " VALUES " + iterarColeccion(iValuesAgrupados, EnumColecciones.VALUESAGRUPADOS, ",");

            vaciarColecciones();
            // Finalmente devolvemos la consulta
            return iConsulta;
        }

        public string generarValues()
        {
            string iConsulta;
            // agregamos los values
            iConsulta = "(" + iterarColeccion(iValues, EnumColecciones.VALUES, ",") + ")";

            vaciarColeccionValues();
            return iConsulta;
        }

        public string generarInsertConSelect()
        {
            string iConsulta;

            iConsulta = "INSERT INTO " + iterarColeccion(iTablas, EnumColecciones.TABLAS, ",");
            iConsulta = iConsulta + " (" + iterarColeccion(iColumnas, EnumColecciones.COMUMNAS, ",") + ")";
            iConsulta = iConsulta + " " + iterarColeccion(iValues, EnumColecciones.VALUES, ",");

            vaciarColecciones();
            return iConsulta;
        }

        public string generarUpdate()
        {
            string iConsulta;

            // agregamos las tablas
            if (iTablaPrincipal.Length > 0)
            {
                iConsulta = " UPDATE " + iTablaPrincipal;
                // AGREGAMOS JOINS
                iConsulta += iterarColeccion(iCondicionesJoin, EnumColecciones.JOIN, "");
                if (iTablas.Count > 0)
                {
                    iConsulta = iConsulta + ", " + iterarColeccion(iTablas, EnumColecciones.TABLAS, ",");
                }
            }
            else
            {
                iConsulta = " UPDATE " + iterarColeccion(iTablas, EnumColecciones.TABLAS, ",");
                iConsulta += iterarColeccion(iCondicionesJoin, EnumColecciones.JOIN, "");
            }

            // agregamos el set
            iConsulta = iConsulta + " SET " + iterarColeccion(iSet, EnumColecciones.SETS, ",");

            // agregamos la condicion
            if (iCondicionesWhere.Count > 0)
            {
                iConsulta = iConsulta + " WHERE " + iterarColeccionWhere(iCondicionesWhere);
            }

            vaciarColecciones();
            return iConsulta;
        }

        public string generarUnion()
        {
            string iConsulta = "";
            int i;

            // agregamos la tabla
            if (iSelect.Count > 1)
            {
                for (i = 1; i <= iSelect.Count; i++)
                {
                    if (i == iSelect.Count)
                    {
                        iConsulta = iConsulta + "(" + iSelect[i - 1] + ")";
                    }
                    else
                    {
                        iConsulta = iConsulta + "(" + iSelect[i - 1] + ")" + " union ";
                    }
                }
            }
            else if (iSelect.Count == 1)
            {
                iConsulta = iSelect[0];
            }

            // agregamos las columnas del order by
            if (iOrden.Count > 0)
            {
                iConsulta = iConsulta + " ORDER BY " + iterarColeccion(iOrden, EnumColecciones.ORDER, ",");
            }

            vaciarColecciones();
            while (iSelect.Count > 0)
            {
                iSelect.RemoveAt(0);
            }
            return iConsulta;
        }

        public string generarUnion(bool eUnionAll)
        {
            string iConsulta = "";
            int i;

            for (i = 1; i <= iSelect.Count; i++)
            {
                if (i == iSelect.Count)
                {
                    iConsulta = iConsulta + "(" + iSelect[i - 1] + ")";
                }
                else
                {
                    iConsulta = iConsulta + "(" + iSelect[i - 1] + ")" + " union all ";
                }
            }

            if (iOrden.Count > 0)
            {
                iConsulta = iConsulta + " ORDER BY " + iterarColeccion(iOrden, EnumColecciones.ORDER, ",");
            }

            vaciarColecciones();
            while (iSelect.Count > 0)
            {
                iSelect.RemoveAt(0);
            }
            return iConsulta;
        }

        public void agregarCondicionWhereConOr(string eCondicionWhereConOr, bool eSinGuardarParametros = false)
        {
            string iDerecha, iIzquierda;
            ParametroSQL iParametroSQL = new ParametroSQL();
            ParametroSQLStoredProcedure iParametroSQLStoredProcedure = new ParametroSQLStoredProcedure();
            string iCaracterEncontrado = "";

            try
            {
                iIndiceParametro += 1;

                if (eCondicionWhereConOr != null && eCondicionWhereConOr.ToLower().IndexOf(" and ") > 0)
                {
                    eSinGuardarParametros = true;
                }
                else if (eCondicionWhereConOr != null && eCondicionWhereConOr.ToLower().IndexOf(" or ") > 0)
                {
                    eSinGuardarParametros = true;
                }
                else if (eCondicionWhereConOr != null && eCondicionWhereConOr.IndexOf(">=") > 0)
                {
                    iCaracterEncontrado = ">=";
                }
                else if (eCondicionWhereConOr != null && eCondicionWhereConOr.IndexOf("<=") > 0)
                {
                    iCaracterEncontrado = "<=";
                }
                else if (eCondicionWhereConOr != null && eCondicionWhereConOr.IndexOf("<>") > 0)
                {
                    iCaracterEncontrado = "<>";
                }
                else if (eCondicionWhereConOr != null && eCondicionWhereConOr.IndexOf(">") > 0)
                {
                    iCaracterEncontrado = ">";
                }
                else if (eCondicionWhereConOr != null && eCondicionWhereConOr.IndexOf("<") > 0)
                {
                    iCaracterEncontrado = "<";
                }
                else if (eCondicionWhereConOr != null && eCondicionWhereConOr.IndexOf("=") > 0)
                {
                    iCaracterEncontrado = "=";
                }

                if (!eSinGuardarParametros && NotEmpty(eCondicionWhereConOr) && Len(iCaracterEncontrado) > 0)
                {
                    iDerecha = Trim(Right(eCondicionWhereConOr, eCondicionWhereConOr.Length - eCondicionWhereConOr.IndexOf(iCaracterEncontrado) - Len(iCaracterEncontrado)));
                    if (InStr(iDerecha, ".") <= 0 || InStr(iDerecha, "'") > 0)
                    {
                        iIzquierda = Trim(Left(eCondicionWhereConOr, eCondicionWhereConOr.IndexOf(iCaracterEncontrado)));
                        iParametroSQL.valor = iDerecha;
                        iParametroSQL.nombre = "condicion" + iIndiceParametro;
                        iParametroSQL.nombreCampo = iIzquierda;
                        iParametrosSQL.Add(iParametroSQL);

                        iParametroSQLStoredProcedure = new ParametroSQLStoredProcedure();
                        iParametroSQLStoredProcedure.nombreParametro = iIzquierda.Replace(".", "");
                        iParametroSQLStoredProcedure.valor = iDerecha;
                        iParametrosSQLStoredProcedure.Add(iParametroSQLStoredProcedure);

                        iCondicionesWhereConOr.Add(iIzquierda + iCaracterEncontrado + PlaceholderPrefix + "condicion" + iIndiceParametro);
                    }
                    else
                    {
                        iCondicionesWhereConOr.Add(eCondicionWhereConOr);
                    }
                }
                else if (!eSinGuardarParametros && eCondicionWhereConOr != null && eCondicionWhereConOr.IndexOf("like") > 0)
                {
                    iIzquierda = Trim(Left(eCondicionWhereConOr, eCondicionWhereConOr.IndexOf("like")));
                    iDerecha = Trim(Right(eCondicionWhereConOr, eCondicionWhereConOr.Length - eCondicionWhereConOr.IndexOf("'")));
                    iParametroSQL.valor = iDerecha;
                    iParametroSQL.nombre = "condicion" + iIndiceParametro;
                    iParametroSQL.nombreCampo = iIzquierda;
                    iParametrosSQL.Add(iParametroSQL);

                    iCondicionesWhereConOr.Add(iIzquierda + " like " + PlaceholderPrefix + "condicion" + iIndiceParametro);
                }
                else if (NotEmpty(eCondicionWhereConOr))
                {
                    iCondicionesWhereConOr.Add(eCondicionWhereConOr);
                }
            }
            catch
            {
            }
        }

        public void agregarCondicionWhere(string eCondicionWhere, bool eSinGuardarParametros = false)
        {
            string iDerecha, iIzquierda;
            ParametroSQL iParametroSQL = new ParametroSQL();
            ParametroSQLStoredProcedure iParametroSQLStoredProcedure = new ParametroSQLStoredProcedure();
            string iCaracterEncontrado = "";

            try
            {
                iIndiceParametro += 1;

                if (eCondicionWhere != null && eCondicionWhere.ToLower().IndexOf(" and ") > 0)
                {
                    eSinGuardarParametros = true;
                }
                else if (eCondicionWhere != null && eCondicionWhere.ToLower().IndexOf(" or ") > 0)
                {
                    eSinGuardarParametros = true;
                }
                else if (eCondicionWhere != null && eCondicionWhere.IndexOf(">=") > 0)
                {
                    iCaracterEncontrado = ">=";
                }
                else if (eCondicionWhere != null && eCondicionWhere.IndexOf("<=") > 0)
                {
                    iCaracterEncontrado = "<=";
                }
                else if (eCondicionWhere != null && eCondicionWhere.IndexOf("<>") > 0)
                {
                    iCaracterEncontrado = "<>";
                }
                else if (eCondicionWhere != null && eCondicionWhere.IndexOf(">") > 0)
                {
                    iCaracterEncontrado = ">";
                }
                else if (eCondicionWhere != null && eCondicionWhere.IndexOf("<") > 0)
                {
                    iCaracterEncontrado = "<";
                }
                else if (eCondicionWhere != null && eCondicionWhere.IndexOf("=") > 0)
                {
                    iCaracterEncontrado = "=";
                }

                if (!eSinGuardarParametros && NotEmpty(eCondicionWhere) && Len(iCaracterEncontrado) > 0)
                {
                    iDerecha = Trim(Right(eCondicionWhere, eCondicionWhere.Length - eCondicionWhere.IndexOf(iCaracterEncontrado) - Len(iCaracterEncontrado)));
                    if (InStr(iDerecha, ".") <= 0 || InStr(iDerecha, "'") > 0)
                    {
                        iIzquierda = Trim(Left(eCondicionWhere, eCondicionWhere.IndexOf(iCaracterEncontrado)));
                        iParametroSQL.valor = iDerecha;
                        iParametroSQL.nombre = "condicion" + iIndiceParametro;
                        iParametroSQL.nombreCampo = iIzquierda;
                        iParametrosSQL.Add(iParametroSQL);

                        iParametroSQLStoredProcedure = new ParametroSQLStoredProcedure();
                        iParametroSQLStoredProcedure.nombreParametro = iIzquierda.Replace(".", "");
                        iParametroSQLStoredProcedure.valor = iDerecha;
                        iParametrosSQLStoredProcedure.Add(iParametroSQLStoredProcedure);

                        iCondicionesWhere.Add(iIzquierda + iCaracterEncontrado + PlaceholderPrefix + "condicion" + iIndiceParametro);
                    }
                    else
                    {
                        iCondicionesWhere.Add(eCondicionWhere);
                    }
                }
                else if (!eSinGuardarParametros && eCondicionWhere != null && eCondicionWhere.IndexOf("like") > 0)
                {
                    iIzquierda = Trim(Left(eCondicionWhere, eCondicionWhere.IndexOf("like")));
                    iDerecha = Trim(Right(eCondicionWhere, eCondicionWhere.Length - eCondicionWhere.IndexOf("'")));
                    iParametroSQL.valor = iDerecha;
                    iParametroSQL.nombre = "condicion" + iIndiceParametro;
                    iParametroSQL.nombreCampo = iIzquierda;
                    iParametrosSQL.Add(iParametroSQL);

                    iCondicionesWhere.Add(iIzquierda + " like " + PlaceholderPrefix + "condicion" + iIndiceParametro);
                }
                else if (NotEmpty(eCondicionWhere))
                {
                    iCondicionesWhere.Add(eCondicionWhere);
                }
            }
            catch
            {
            }
        }

        public void agregarCondicionWhereEncriptado(string eCondicionWhere, bool eSinGuardarParametros = false)
        {
            string iDerecha, iIzquierda;
            ParametroSQL iParametroSQL = new ParametroSQL();
            string iCaracterEncontrado = "";

            try
            {
                iIndiceParametro += 1;

                if (eCondicionWhere != null && eCondicionWhere.ToLower().IndexOf(" and ") > 0)
                {
                    eSinGuardarParametros = true;
                }
                else if (eCondicionWhere != null && eCondicionWhere.ToLower().IndexOf(" or ") > 0)
                {
                    eSinGuardarParametros = true;
                }
                else if (eCondicionWhere != null && eCondicionWhere.IndexOf(">=") > 0)
                {
                    iCaracterEncontrado = ">=";
                }
                else if (eCondicionWhere != null && eCondicionWhere.IndexOf("<=") > 0)
                {
                    iCaracterEncontrado = "<=";
                }
                else if (eCondicionWhere != null && eCondicionWhere.IndexOf("<>") > 0)
                {
                    iCaracterEncontrado = "<>";
                }
                else if (eCondicionWhere != null && eCondicionWhere.IndexOf(">") > 0)
                {
                    iCaracterEncontrado = ">";
                }
                else if (eCondicionWhere != null && eCondicionWhere.IndexOf("<") > 0)
                {
                    iCaracterEncontrado = "<";
                }
                else if (eCondicionWhere != null && eCondicionWhere.IndexOf("=") > 0)
                {
                    iCaracterEncontrado = "=";
                }

                if (!eSinGuardarParametros && NotEmpty(eCondicionWhere) && Len(iCaracterEncontrado) > 0)
                {
                    iDerecha = Trim(Right(eCondicionWhere, eCondicionWhere.Length - eCondicionWhere.IndexOf(iCaracterEncontrado) - Len(iCaracterEncontrado)));
                    if (InStr(iDerecha, ".") <= 0 || InStr(iDerecha, "'") > 0)
                    {
                        iIzquierda = Trim(Left(eCondicionWhere, eCondicionWhere.IndexOf(iCaracterEncontrado)));
                        iParametroSQL.valor = HashUtils.SqlMD5(iDerecha, iBaseDeDatos);
                        iParametroSQL.nombre = "condicion" + iIndiceParametro;
                        iParametroSQL.nombreCampo = iIzquierda;
                        iParametrosSQL.Add(iParametroSQL);

                        iCondicionesWhere.Add(iIzquierda + iCaracterEncontrado + PlaceholderPrefix + "condicion" + iIndiceParametro);
                    }
                    else
                    {
                        iCondicionesWhere.Add(eCondicionWhere);
                    }
                }
                else if (!eSinGuardarParametros && eCondicionWhere != null && eCondicionWhere.IndexOf("like") > 0)
                {
                    iIzquierda = Trim(Left(eCondicionWhere, eCondicionWhere.IndexOf("like")));
                    iDerecha = Trim(Right(eCondicionWhere, eCondicionWhere.Length - eCondicionWhere.IndexOf("'")));
                    iParametroSQL.valor = HashUtils.SqlMD5(iDerecha, iBaseDeDatos);
                    iParametroSQL.nombre = "condicion" + iIndiceParametro;
                    iParametroSQL.nombreCampo = iIzquierda;
                    iParametrosSQL.Add(iParametroSQL);

                    iCondicionesWhere.Add(iIzquierda + " like " + PlaceholderPrefix + "condicion" + iIndiceParametro);
                }
                else if (NotEmpty(eCondicionWhere))
                {
                    iCondicionesWhere.Add(eCondicionWhere);
                }
            }
            catch
            {
            }
        }

        public string generarDelete()
        {
            string iConsulta;

            // agregamos la tabla
            iConsulta = "DELETE FROM " + iterarColeccion(iTablas, EnumColecciones.TABLAS, ",");

            // agregamos la condicion
            if (iCondicionesWhere.Count > 0)
            {
                iConsulta = iConsulta + " WHERE " + iterarColeccionWhere(iCondicionesWhere);
            }

            vaciarColecciones();
            return iConsulta;
        }

        private string iterarColeccion(List<string> eColeccion, EnumColecciones eConcepto, string eSeparador)
        {
            string iCadena = "";
            int i;

            for (i = 1; i <= eColeccion.Count; i++)
            {
                if ((iBaseDeDatos == "SQLSERVER" || iBaseDeDatos == "SQLSERVER14") && eConcepto == EnumColecciones.COMUMNASESPECIALES && iGroupBy.Count > 0)
                {
                    string iColumna = eColeccion[i - 1];
                    bool yaEsAgregacion =
                        (iColumna.Length > 3 && iColumna.ToLower().Substring(0, 4) == "max(") ||
                        iColumna.ToLower().Contains("min(") ||
                        (!iColumna.ToLower().Contains("case ") && iColumna.ToLower().Contains("max(")) ||
                        iColumna.ToLower().Contains("sum(") ||
                        iColumna.ToLower().Contains("count(") ||
                        (!iColumna.ToLower().Contains("case ") && iColumna.ToLower().Contains("null ")) ||
                        iColumna.ToLower().Contains("group_concat_d");

                    if (!yaEsAgregacion)
                    {
                        if (iColumna.ToLower().Contains(" as "))
                        {
                            int lastAs = iColumna.ToLower().LastIndexOf(" as ");
                            iColumna = "min(" + Left(iColumna, lastAs) + ") " + Right(iColumna, iColumna.Length - lastAs);
                        }
                        else
                        {
                            if (iColumna.ToLower().Contains("."))
                            {
                                int lastDot = iColumna.ToLower().LastIndexOf(".");
                                iColumna = "min(" + iColumna + ") as " + Right(iColumna, iColumna.Length - lastDot - 1);
                            }
                            else
                            {
                                iColumna = "min(" + iColumna + ") as " + iColumna;
                            }
                        }
                        if (i == eColeccion.Count)
                        {
                            iCadena = iCadena + iColumna;
                        }
                        else
                        {
                            iCadena = iCadena + iColumna + eSeparador;
                        }
                    }
                    else
                    {
                        if (i == eColeccion.Count)
                        {
                            iCadena = iCadena + eColeccion[i - 1];
                        }
                        else
                        {
                            iCadena = iCadena + eColeccion[i - 1] + eSeparador;
                        }
                    }
                }
                else
                {
                    if (i == eColeccion.Count)
                    {
                        iCadena = iCadena + eColeccion[i - 1];
                    }
                    else
                    {
                        iCadena = iCadena + eColeccion[i - 1] + eSeparador;
                    }
                }

                if ((iBaseDeDatos == "SQLSERVER" || iBaseDeDatos == "SQLSERVER14") && eConcepto == EnumColecciones.TABLASESPECIALES && i == 1 && iBloquearTabla && iTablaPrincipal.Length == 0)
                {
                    if (eColeccion.Count > 1)
                    {
                        iCadena = Left(iCadena, iCadena.Length - 1) + "  WITH (UPDLOCK) , ";
                    }
                    else
                    {
                        iCadena = iCadena + "  WITH (UPDLOCK) ";
                    }
                }
            }

            // Reemplazo las palabras reservadas para SQL SERVER
            if (iBaseDeDatos == "SQLSERVER" || iBaseDeDatos == "SQLSERVER14")
            {
                iCadena = Regex.Replace(iCadena, @"as plan ", "as [plan] ", RegexOptions.IgnoreCase);
                iCadena = Regex.Replace(iCadena, @"as plan\)", "as [plan])", RegexOptions.IgnoreCase);
                iCadena = Regex.Replace(iCadena, @"as plan,", "as [plan],", RegexOptions.IgnoreCase);
            }

            switch (iRegion)
            {
                case "ARGENTINA":
                case "URUGUAY":
                    break;
                case "PARAGUAY":
                    iCadena = iCadena.Replace("$", "Gs");
                    break;
                case "COLOMBIA":
                case "ESPANA":
                    break;
            }

            return iCadena;
        }

        private string iterarColeccionWhere(List<string> eColeccion)
        {
            string iCadena = "";
            string ultimo = eColeccion.Count > 0 ? eColeccion[eColeccion.Count - 1] : null;

            foreach (string iNewItem in eColeccion)
            {
                iCadena = iCadena + "(" + iNewItem;
                if (iNewItem != ultimo)
                {
                    iCadena = iCadena + ") AND ";
                }
                else
                {
                    iCadena = iCadena + ")";
                }
            }

            return iCadena;
        }

        private string iterarColeccionWhereConOr(List<string> eColeccion)
        {
            string iCadena = "";
            string ultimo = eColeccion.Count > 0 ? eColeccion[eColeccion.Count - 1] : null;

            foreach (string iNewItem in eColeccion)
            {
                iCadena = iCadena + "(" + iNewItem;
                if (iNewItem != ultimo)
                {
                    iCadena = iCadena + ") Or ";
                }
                else
                {
                    iCadena = iCadena + ")";
                }
            }

            return iCadena;
        }

        /// <summary>
        /// Equivalente al destructor() VB legacy. Limpia las listas.
        /// En C# es innecesario por el GC; se mantiene por compatibilidad de API.
        /// </summary>
        public void destructor()
        {
            iColumnas = null;
            iTablas = null;
            iCondicionesWhere = null;
            iOrden = null;
            iLimit = null;
            iSelect = null;
            iGroupBy = null;
            iHaving = null;
            iParametrosSQL = null;
        }

        private void vaciarColecciones()
        {
            iColumnas.Clear();
            iLimit.Clear();
            iCondicionesJoin.Clear();
            iCondicionesWhere.Clear();
            iGroupBy.Clear();
            iOrden.Clear();
            iSet.Clear();
            iTablas.Clear();
            iValues.Clear();
            iValuesAgrupados.Clear();
            iHaving.Clear();
            iTablaPrincipal = "";
        }

        private void vaciarColeccionesWhereOr()
        {
            iCondicionesWhereConOr.Clear();
        }

        private void vaciarColeccionValues()
        {
            iValues.Clear();
        }

        private void vaciarColeccionesWhere()
        {
            iCondicionesWhere.Clear();
        }

        public string generarSelectSecuencia(string eSecuencia)
        {
            // Esta funcion retorna un string para obtener el proximo id a otorgar
            return "select nextval (" + "'\"" + eSecuencia + "\"'" + ") as id";
        }

        #endregion
    }
}
