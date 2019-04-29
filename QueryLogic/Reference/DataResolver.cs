using System;
using System.Data.SqlClient;
using System.Collections.Generic;
using QueryLogic.Reference.Constants;

namespace QueryLogic.Core.Reference
{
    public static class DataResolver
    {
        private static readonly Dictionary<string, Action<SqlDataReader, Row, string, int>> _map = new Dictionary <string, Action<SqlDataReader, Row, string, int>>
        {
            { DataTypes.INT16, (reader, row, columnName, index) => { row.Add(columnName, reader.GetInt16(index)); } },
            { DataTypes.INT32, (reader, row, columnName, index) => { row.Add(columnName, reader.GetInt32(index)); } },
            { DataTypes.INT64, (reader, row, columnName, index) => { row.Add(columnName, reader.GetInt64(index)); } },
            { DataTypes.GUID, (reader, row, columnName, index) => { row.Add(columnName, reader.GetGuid(index)); } },
            { DataTypes.DECIMAL, (reader, row, columnName, index) => { row.Add(columnName, reader.GetDecimal(index)); } },
            { DataTypes.DATETIME, (reader, row, columnName, index) => { row.Add(columnName, reader.GetDateTime(index)); } },
            { DataTypes.TIME, (reader, row, columnName, index) => { row.Add(columnName, reader.GetTimeSpan(index)); } },
            { DataTypes.VARCHAR, (reader, row, columnName, index) => { row.Add(columnName, reader.GetString(index)); } },
            { DataTypes.BYTE, (reader, row, columnName, index) => { row.Add(columnName, reader.GetByte(index)); } },
            { DataTypes.BYTECODE, (reader, row, columnName, index) => { row.Add(columnName, reader.GetRowBytecode(columnName)); } },
            { DataTypes.BOOL, (reader, row, columnName, index) => { row.Add(columnName, reader.GetBoolean(index)); } }
        };

        public static void Resolve(string dataType, SqlDataReader reader, Row row, string columnName, int columnIndex)
        {
            if (!_map.ContainsKey(dataType))
            {
                row.Add(columnName, null);

                return;
            }

            _map[dataType].Invoke(reader, row, columnName, columnIndex);
        }
    }
}