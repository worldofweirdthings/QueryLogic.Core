using System;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using QueryLogic.Core.Reference;
using REF = QueryLogic.Reference;

namespace QueryLogic
{
    /// <summary>
    /// Provides methods for retrieving database rows and data sets
    /// </summary>
    public class QueryBuilder : IQueryBuilder
    {
        /// <summary>
        /// Executes a stored procedure which returns data from a single select
        /// </summary>
        /// <param name="command">Database command</param>
        /// <returns>Row maps with retrieved data</returns>
        /// <exception cref="QueryException">SQL execution error</exception> 
        public Rows QueryData(SqlCommand command)
        {
            var rows = new Rows();

            try
            {
                using (var connection = command.Connection)
                {
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        var metadata = setResultSchema(reader);

                        while (reader.Read()) getRowData(reader, metadata, rows);
                    }
                }
            }
            catch (Exception ex)
            {
                throw formatError(ex, command);
            }

            return rows;
        }

        /// <summary>
        /// Asynchronously executes a stored procedure which returns data from a single select
        /// </summary>
        /// <param name="command">Database command</param>
        /// <returns>Row maps with retrieved data</returns>
        /// <exception cref="QueryException">SQL execution error</exception> 
        public async Task<Rows> QueryDataAsync(SqlCommand command)
        {
            var rows = new Rows();

            try
            {
                await command.Connection.OpenAsync(CancellationToken.None);

                using (var reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection))
                {
                    var metadata = setResultSchema(reader);
                        
                    while (await reader.ReadAsync()) getRowData(reader, metadata, rows);
                }
            }
            catch (Exception ex)
            {
                throw formatError(ex, command);
            }

            return rows;
        }

        /// <summary>
        /// Executes a stored procedure which returns data from a multi-select
        /// </summary>
        /// <param name="command">SQL command</param>
        /// <returns>Dictionary of row maps with retrieved data</returns>
        /// <exception cref="QueryException">SQL execution error</exception>
        public DataMap QueryDataSet(SqlCommand command)
        {
            var dataMap = new DataMap();
            
            try
            {
                using (var connection = command.Connection)
                {
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        addRowData(reader, dataMap);
                        
                        while (reader.NextResult()) addRowData(reader, dataMap);
                    }
                }
            }
            catch (Exception ex)
            {
                throw formatError(ex, command);
            }

            return dataMap;
        }

        /// <summary>
        /// Asynchronously executes a stored procedure which returns data from a multi-select
        /// </summary>
        /// <param name="command">SQL command</param>
        /// <returns>Dictionary of row maps with retrieved data</returns>
        /// <exception cref="QueryException">SQL execution error</exception>
        public async Task<DataMap> QueryDataSetAsync(SqlCommand command)
        {
            var dataMap = new DataMap();

            try
            {
                await command.Connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection))
                {
                    addRowDataAsync(reader, dataMap);
                    
                    while (await reader.NextResultAsync()) addRowData(reader, dataMap);
                }
            }
            catch (Exception ex)
            {
                throw formatError(ex, command);
            }

            return dataMap;
        }

        /// <summary>
        /// Executes a non-query stored procedure
        /// </summary>
        /// <param name="command">SQL command</param>
        /// <returns>Row map with populated out parameters and/or number of affected rows</returns>
        /// <exception cref="QueryException">SQL execution error</exception>
        public Row ModifyData(SqlCommand command)
        {
            var row = new Row();
            var outputParamNames = getOutParameterNames(command);
            
            try
            {
                using (var connection = command.Connection)
                {
                    connection.Open();
                
                    row.Add("rows_affected", command.ExecuteNonQuery());
                
                    if (outputParamNames.Any()) setOutputParameterValues(row, command, outputParamNames);    
                }
            }
            catch (Exception ex)
            {
                throw formatError(ex, command);
            }

            return row;
        }

        /// <summary>
        /// Asynchronously executes a non-query stored procedure
        /// </summary>
        /// <param name="command">SQL command</param>
        /// <returns>Row map with populated out parameters and/or number of affected rows</returns>
        /// <exception cref="QueryException">SQL execution error</exception>
        public async Task<Row> ModifyDataAsync(SqlCommand command)
        {
            var row = new Row();
            var outputParamNames = getOutParameterNames(command);

            try
            {   
                await command.Connection.OpenAsync();
                
                row.Add("rows_affected", await command.ExecuteNonQueryAsync());
                
                if (outputParamNames.Any()) setOutputParameterValues(row, command, outputParamNames);

                command.Connection.Close();
            }
            catch (Exception ex)
            {
                throw formatError(ex, command);
            }

            return row;
        }

        #region Helper Methods

        private static void addRowData(SqlDataReader reader, DataMap dataMap)
        {
            var metadata = setResultSchema(reader);
            var rows = new Rows();

            while (reader.Read()) getRowData(reader, metadata, rows);

            dataMap.Add(rows);
        }

        private static async void addRowDataAsync(SqlDataReader reader, DataMap dataMap)
        {
            while (await reader.NextResultAsync())
            {
                var metadata = setResultSchema(reader);
                var rows = new Rows();

                while (await reader.ReadAsync()) getRowData(reader, metadata, rows);

                dataMap.Add(rows);
            }
        }
        
        private static REF.Metadata setResultSchema(DbDataReader dataReader)
        {
            var metadata = new REF.Metadata();
            var schema = dataReader.GetColumnSchema();

            if (!schema.IsValid()) return metadata;
            
            foreach (var row in schema) metadata.Columns.Add(new REF.Column(row.ColumnName, row.DataType.FullName, row.ColumnOrdinal.GetValueOrDefault(0)));
            
            return metadata;
        }

        private static void getRowData(SqlDataReader dataReader, REF.Metadata metadata, ICollection<Row> Rows)
        {
            var row = new Row();

            foreach (var column in metadata.Columns)
            {
                if (dataReader.IsDBNull(column.ColumnIndex))
                {
                    row.Add(column.ColumnName, null);
                }
                else
                {
                    DataResolver.Resolve(column.ColumnDataType, dataReader, row, column.ColumnName, column.ColumnIndex);
                }
            }

            Rows.Add(row);
        }

        private static List<string> getOutParameterNames(IDbCommand command)
        {
            var parameterNames = new List<string>();

            if (command is SqlCommand)
            {
                parameterNames.AddRange(getSqlParameters(command).Where(x => x.Direction == ParameterDirection.Output).Select(x => x.ParameterName));
            }
            else
            {
                throw new QueryException("Provided command type is not supported.");
            }
            
            return parameterNames;
        }

        private static void setOutputParameterValues(Row row, IDbCommand command, IEnumerable<string> parameterNames)
        {
            if (command is SqlCommand)
            {
                foreach (var paramName in parameterNames)
                {
                    var parameter = command.Parameters[paramName] as SqlParameter;

                    row.Add(paramName.Replace("@", ""), parameter?.Value);
                }
            }
            else
            {
                throw new QueryException("Provided command type is not supported.");
            }
        }

        private static IEnumerable<SqlParameter> getSqlParameters(IDbCommand command)
        {
            return command.Parameters.Cast<SqlParameter>();
        }

        private static QueryException formatError(Exception ex, IDbCommand command)
        {
            return new QueryException($"SQL execution error\ncommand : {command.CommandText} :\nerror : {ex.Message}", ex);
        }

        #endregion
    }
}