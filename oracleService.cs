using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace OracleDbConnection
{
    public class OracleService
    {
        private readonly string _connectionString;

        public OracleService(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Oracle database-тэй холбогдож, BI_EMPLOYEE хүснэгтүүдийн мэдээллийг авах
        /// </summary>
        public List<string> GetBiEmployeeTablesInfo()
        {
            List<string> textEntries = new List<string>();

            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    conn.Open();
                    Console.WriteLine("✅ Oracle холболт амжилттай!");

                    List<string> tables = GetTableNames(conn);
                    Dictionary<string, List<string>> tableColumns = GetTableColumns(conn, tables);

                    // "TABLE.COLUMN" жагсаалт үүсгэх хэсэг
                    foreach (var kvp in tableColumns)
                    {
                        string table = kvp.Key;
                        foreach (string col in kvp.Value)
                        {
                            // col нь "COLUMNNAME (DATATYPE)" хэлбэртэй тул баганын нэрийг салгах
                            var colName = col.Split(' ')[0];
                            textEntries.Add($"{table}.{colName}");
                        }
                    }

                    // Заавал хэвлэх бол болгоно
                    Console.WriteLine("\n📌 Хүснэгтүүдийн багана мэдээлэл:");
                    foreach (var entry in textEntries)
                    {
                        Console.WriteLine($"   - {entry}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Oracle холболт амжилтгүй: {ex.Message}");
                throw;
            }

            // Эцэст нь жагсаалтыг буцаана
            return textEntries;
        }


        /// <summary>
        /// BI_EMPLOYEE-ээр эхэлдэг хүснэгтүүдийн нэрийг авах
        /// </summary>
        private List<string> GetTableNames(OracleConnection connection)
        {
            List<string> tables = new List<string>();

            string query = @"
                SELECT table_name
                FROM user_tables
                WHERE table_name LIKE 'BI_EMPLOYEE%' or table_name LIKE 'BI_HREMPLOYEE' 
                ORDER BY table_name";

            using (OracleCommand cmd = new OracleCommand(query, connection))
            {
                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tables.Add(reader.GetString(0));
                    }
                }
            }

            return tables;
        }

        /// <summary>
        /// Хүснэгт тус бүрийн баганын мэдээлэл авах
        /// </summary>
        private Dictionary<string, List<string>> GetTableColumns(OracleConnection connection, List<string> tables)
        {
            Dictionary<string, List<string>> tableColumns = new Dictionary<string, List<string>>();

            foreach (string table in tables)
            {
                List<string> columns = new List<string>();

                string query = @"
                    SELECT column_name, data_type
                    FROM user_tab_columns
                    WHERE table_name = :tableName
                    ORDER BY column_id";

                using (OracleCommand cmd = new OracleCommand(query, connection))
                {
                    cmd.Parameters.Add(new OracleParameter("tableName", table));

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string columnName = reader.GetString(0);
                            string dataType = reader.GetString(1);
                            columns.Add($"{columnName} ({dataType})");
                        }
                    }
                }

                tableColumns[table] = columns;
            }

            return tableColumns;
        }

        /// <summary>
        /// Тодорхой хүснэгтийн өгөгдлийг авах
        /// </summary>
        public List<Dictionary<string, object>> GetTableData(string tableName, int limit = 10)
        {
            List<Dictionary<string, object>> results = new List<Dictionary<string, object>>();

            try
            {
                using (OracleConnection conn = new OracleConnection(_connectionString))
                {
                    conn.Open();

                    string query = $"SELECT * FROM {tableName} WHERE ROWNUM <= {limit}";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Dictionary<string, object> row = new Dictionary<string, object>();

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string columnName = reader.GetName(i);
                                    object value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                    row[columnName] = value;
                                }

                                results.Add(row);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {tableName} хүснэгтийн өгөгдөл авахад алдаа: {ex.Message}");
            }

            return results;
        }
    }
}