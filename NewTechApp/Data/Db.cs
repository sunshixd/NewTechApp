using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace NewTechApp.Data
{
    public static class Db
    {
        public static string AppPath => AppDomain.CurrentDomain.BaseDirectory;
        private const string ConnString =
            "Server=DESKTOP-J7SEHSP\\SQLEXPRESS;Database=NewTechDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public static SqlConnection GetConn()
        {
            var c = new SqlConnection(ConnString);
            c.Open();
            return c;
        }

        public static DataTable Table(string sql, params SqlParameter[] ps)
        {
            using (var c = GetConn())
            using (var cmd = new SqlCommand(sql, c))
            {
                if (ps != null && ps.Length > 0) cmd.Parameters.AddRange(ps);
                using (var da = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        public static int Exec(string sql, params SqlParameter[] ps)
        {
            using (var c = GetConn())
            using (var cmd = new SqlCommand(sql, c))
            {
                if (ps != null && ps.Length > 0) cmd.Parameters.AddRange(ps);
                return cmd.ExecuteNonQuery();
            }
        }
    }
}