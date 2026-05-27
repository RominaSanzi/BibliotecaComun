using System;
using System.Security.Cryptography;
using System.Text;

namespace Divinf.Common.Sql.Internal
{
    /// <summary>
    /// Replica las utilidades sqlMD5 y obtenerPassword de Loan/Comun/FuncionComun.
    /// Se mantienen INTERNAS para no exponer detalles de implementacion al consumidor
    /// y para que las firmas publicas del legacy queden encapsuladas.
    /// </summary>
    internal static class HashUtils
    {
        /// <summary>
        /// Devuelve la expresion SQL para calcular MD5 sobre un campo, segun el motor de BD.
        /// IMPORTANTE: no calcula MD5 en .NET. Devuelve la formula que la BD va a ejecutar.
        /// Equivalente exacto de FuncionComun.sqlMD5 (loan1\Comun\FuncionComun.vb:3046).
        /// </summary>
        public static string SqlMD5(string eCampo, string baseDeDatos)
        {
            if (baseDeDatos == "SQLSERVER" || baseDeDatos == "SQLSERVER14")
            {
                // SQL Server
                return "LOWER(CONVERT(NVARCHAR(32),HashBytes('MD5', " + eCampo + "),2))";
            }
            else
            {
                // MySQL
                return "MD5(" + eCampo + ")";
            }
        }

        /// <summary>
        /// Calcula HMAC-SHA256 sobre password con palabraClave, devuelve Base64.
        /// Equivalente exacto de FuncionComun.obtenerPassword (loan1\Comun\FuncionComun.vb:7564).
        /// </summary>
        public static string ObtenerPassword(string ePassword, string ePalabraClave)
        {
            var encoding = new ASCIIEncoding();
            byte[] keyByte = encoding.GetBytes(ePalabraClave);
            byte[] messageBytes = encoding.GetBytes(ePassword);

            using (var hmac = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmac.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashmessage);
            }
        }
    }
}
