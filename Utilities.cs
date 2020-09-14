using SharpLearning.Containers.Matrices;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace AdoptionNeuralNet
{
    public static class Utilities
    {
        public static DataTable GetDataTable(string commandText, string connectionString)
        {
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(commandText, connection);
                connection.Open();
                SqlDataAdapter dataAdapter = new SqlDataAdapter(command);
                dataAdapter.Fill(dataTable);
                connection.Close();
            }

            return dataTable;
        }

        public static DataTable OneHotEncoder(DataTable table, string[] columnsToEncode)
        {
            DataTable encoded = new DataTable();
            // create a map for each column to encode
            Dictionary<string, List<object>> maps = new Dictionary<string, List<object>>();
            foreach (var column in columnsToEncode)
            {
                // create a list of all the distinct values in the column
                List<object> list = new List<object>();
                maps.Add(column, list);

                var query = (from DataRow r in table.Rows
                             select r[column]).Distinct();

                foreach (var item in query)
                {
                    // add a new column to the encoded table for each distictive value
                    list.Add(item);
                    DataColumn dataColumn = new DataColumn($"{column}_{list.Count - 1}", typeof(double));
                    encoded.Columns.Add(dataColumn);
                }
            }

            // create columns in the encoded table for all the remaining columns from the original table
            for (int n = 0; n < table.Columns.Count; n++)
            {
                if (!maps.ContainsKey(table.Columns[n].ColumnName))
                {
                    DataColumn dataColumn = new DataColumn(table.Columns[n].ColumnName, table.Columns[n].DataType);
                    encoded.Columns.Add(dataColumn);
                }
            }

            // copy over the values
            for (int row = 0; row < table.Rows.Count; row++)
            {
                DataRow dataRow = encoded.NewRow();
                for (int col = 0; col < table.Columns.Count; col++)
                {
                    string columnName = table.Columns[col].ColumnName;
                    if (maps.ContainsKey(columnName))
                    {
                        // if the column is an encoded column, find the index in the list of the value
                        int index = maps[columnName].IndexOf(table.Rows[row][col]);
                        for (int n = 0; n < maps[columnName].Count; n++)
                        {
                            // set the appropriate column to 1, initialize all the others to zero
                            dataRow[$"{columnName}_{n}"] = n == index ? 1 : 0;
                        }
                    }
                    else
                    {
                        // if not an encoded column, just copy over the values
                        dataRow[columnName] = table.Rows[row][col];
                    }
                }

                // add the set of values to the encoded table
                encoded.Rows.Add(dataRow);
            }

            return encoded;
        }

        public static (F64Matrix, double[]) ObservationsFromDataTable(DataTable table, string targetName)
        {
            var targetIndex = table.Columns[targetName].Ordinal;
            return ObservationsFromDataTable(table, targetIndex);
        }

        public static (F64Matrix, double[]) ObservationsFromDataTable(DataTable table, int targetIndex)
        {
            var targets = new double[table.Rows.Count];
            var observations = new F64Matrix(table.Rows.Count, table.Columns.Count - 1);
            for (int row = 0; row < table.Rows.Count; row++)
            {
                for (int col = 0; col < table.Columns.Count; col++)
                {
                    if (col == targetIndex)
                    {
                        targets[row] = Convert.ToDouble(table.Rows[row][col]);
                    }
                    else
                    {
                        observations[row, col] = Convert.ToDouble(table.Rows[row][col]);
                    }
                }
            }

            return (observations, targets);
        }

        public static (F64Matrix, double[]) ObservationsFromDataRows(DataRow[] rows, string targetName)
        {
            int targetIndex = rows[0].Table.Columns[targetName].Ordinal;
            return ObservationsFromDataRows(rows, targetIndex);
        }
        public static (F64Matrix, double[]) ObservationsFromDataRows(DataRow[] rows, int targetIndex)
        {
            var targets = new double[rows.Length];
            var observations = new F64Matrix(rows.Length, rows[0].Table.Columns.Count - 1);
            for (int row = 0; row < rows.Length; row++)
            {
                for (int col = 0; col < rows[0].Table.Columns.Count; col++)
                {
                    if (col == targetIndex)
                    {
                        targets[row] = Convert.ToDouble(rows[row][col]);
                    }
                    else
                    {
                        observations[row, col] = Convert.ToDouble(rows[row][col]);
                    }
                }
            }

            return (observations, targets);
        }
    }
}
