using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace SqlDatabaseStudio
{
    public class Context : IDisposable
    {
        private string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|DB.mdf;Integrated Security=True;Connect Timeout=30;";
        private SqlConnection connection;
        public IEnumerable<string> Tables { get { return connection.GetSchema("Tables").Rows.Cast<DataRow>().Select(a => a[2].ToString()); } }
        public Context()
        {
            connection = new SqlConnection(connectionString);
            connection.Open();
        }
        public DataTable Select(string what, string from, string param = "") => Execute($"SELECT {what} FROM {from} {param}");
        public void Insert(string where, string valuesString) => Execute($"INSERT INTO {where} VALUES({valuesString})");
        public void Delete(string table, int id) => Execute($"DELETE FROM {table} WHERE Id={id}");
        public DataTable Execute(string request)
        {
            DataTable data = new DataTable();
            int rows_returned;
            using (var command = new SqlCommand(request, connection))
            using (var dataAdapter = new SqlDataAdapter(command))
            {
                command.CommandText = request;
                command.CommandType = CommandType.Text;
                rows_returned = dataAdapter.Fill(data);
            }
            return (data.Rows.Count == 0) ? null : data;
        }

        public void Dispose()
        {
            connection.Close();
            connection.Dispose();
        }
    }
}
