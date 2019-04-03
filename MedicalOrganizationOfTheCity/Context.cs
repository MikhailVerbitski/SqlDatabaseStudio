using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace MedicalOrganizationOfTheCity
{
    public class Context : IDisposable
    {
        private string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=D:\CLOUD\PROJECTS\C#\MEDICALORGANIZATIONOFTHECITY\MEDICALORGANIZATIONOFTHECITY\DB.MDF;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        private SqlConnection connection;
        public IEnumerable<string> Tables { get { return connection.GetSchema("Tables").Rows.Cast<DataRow>().Select(a => a[2].ToString()); } }
        public Context()
        {
            connection = new SqlConnection(connectionString);
            connection.Open();
        }
        public DataTable Select(string what, string from, string param = "")
        {
            var request = $"SELECT {what} FROM {from} {param}";
            using (var command = new SqlCommand(request, connection))
            {
                return Select(command);
            }
        }
        public DataTable Select(SqlCommand command)
        {
            using (var reader = command.ExecuteReader())
            {
                var dataTable = new DataTable();
                dataTable.Load(reader);
                return dataTable;
            }
        }
        public object Insert(string where, string valuesString)
        {
            var request = $"INSERT INTO {where} VALUES({valuesString})";
            using (var command = new SqlCommand(request, connection))
            {
                return Insert(command);
            }
        }
        public object Insert(SqlCommand command)
        {
            var response = command.ExecuteNonQuery();
            return response;
        }
        public void Delete(string table, int id)
        {
            var request = $"DELETE FROM {table} WHERE Id={id}";
            using (var command = new SqlCommand(request, connection))
            {
                Delete(command);
            }
        }
        public void Delete(SqlCommand command)
        {
            command.ExecuteNonQuery();
        }

        public void Dispose()
        {
            connection.Close();
            connection.Dispose();
        }
    }
}
