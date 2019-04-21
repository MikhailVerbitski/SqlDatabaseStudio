using SqlDatabaseStudio.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace SqlDatabaseStudio
{
    public class Context : IDisposable
    {
        private string UserPathOfDatabase;
        private string connectionString { get { return $@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={UserPathOfDatabase};"; } } 
        private SqlConnection connection;
        public IEnumerable<string> Tables
        {
            get
            {
                connection.Open();
                var tables = connection.GetSchema("Tables").Rows.Cast<DataRow>().Select(a => a[2].ToString());
                connection.Close();
                return tables;
            }
        }

        public Context(string path = null)
        {
            ChangeDatabase(path);
        }

        public DataTable Select(string what, string from, string param = "") => Execute($"SELECT {what} FROM {from} {param}");
        public void Insert(string table, IEnumerable<string> columns, IEnumerable<string> values) => Execute($"INSERT INTO {$"{table}({columns.Aggregate((a, b) => $"{a}, {b}")})"} VALUES({values.Select(a => $"'{a}'").Aggregate((a, b) => $"{a}, {b}")})");
        public void Update(string table, int id, IEnumerable<string> columns, IEnumerable<string> values) => Execute($"UPDATE {table} SET {Enumerable.Range(0, columns.Count()).Select(a => $"{columns.ElementAt(a)} = '{values.ElementAt(a)}'").Aggregate((a,b) => $"{a}, {b}")} WHERE Id = {id}");
        public void Delete(string table, int id) => Execute($"DELETE FROM {table} WHERE Id={id}");
        public List<string> StoredProcedures { get { return Execute("SELECT ROUTINE_NAME FROM INFORMATION_SCHEMA.ROUTINES").Rows.Cast<DataRow>().Select(a => a.ItemArray.First().ToString()).ToList(); } }
        public DataTable Execute(string request)
        {
            try
            {
                connection.Open();
                DataTable data = new DataTable();
                int rows_returned;
                using (var command = new SqlCommand(request, connection))
                using (var dataAdapter = new SqlDataAdapter(command))
                {
                    command.CommandText = request;
                    command.CommandType = CommandType.Text;
                    rows_returned = dataAdapter.Fill(data);
                }
                return data;
            }
            finally
            {
                connection.Close();
            }
        }
        public void ChangeDatabase(string path = null)
        {
            if (connection != null)
            {
                connection.Dispose();
            }
            UserPathOfDatabase = path;
            if(path != null)
            {
                connection = new SqlConnection(connectionString);
            }
        }

        public List<TableField> GetForeignKeys(string tableName)
        {
            var request = $"SELECT OBJECT_NAME(f.referenced_object_id) TableName, COL_NAME(fc.parent_object_id, fc.parent_column_id) ColName FROM sys.foreign_keys AS f INNER JOIN sys.foreign_key_columns AS fc ON f.OBJECT_ID = fc.constraint_object_id INNER JOIN sys.tables t ON t.OBJECT_ID = fc.referenced_object_id WHERE OBJECT_NAME(f.parent_object_id) = '{tableName}'";
            var data = Execute(request);
            return (data.Rows.Count > 0) ? data
                .Rows
                .Cast<DataRow>()
                .Select(a => new TableField()
                {
                    Text = a.ItemArray.First().ToString(),
                    TableFieldName = a.ItemArray.Skip(1).First().ToString(),
                    ForeignTable = Select("*", a.ItemArray.First().ToString())
                })
                .ToList() : null;
        }

        public void ExecuteStoredProcedure(string procedureName)
        {
            try
            {
                connection.Open();
                using (var command = new SqlCommand(procedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                connection.Close();
            }
        }

        public void Dispose()
        {
            connection.Dispose();
        }
    }
}
