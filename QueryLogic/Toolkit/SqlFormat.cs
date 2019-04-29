using System.Collections.Generic;

namespace QueryLogic
{
    /// <summary>
    /// Selects the correct format for conditional clause elements.
    /// </summary>
    public static class SqlFormat
    {
        private static readonly Dictionary<string, string> _map = new Dictionary<string, string>
        {
            { "int", "{0}" },
            { "smallint", "{0}" },
            { "tinyint", "{0}" },
            { "uniqueidentifier", "'{0}'" },
            { "varchar", "'{0}'" },
            { "nvarchar", "N'{0}'" },
            { "decimal", "{0}" },
            { "datetime", "'{0}'" },
            { "bit", "{0}" },
            { "System.Int32", "{0}" },
            { "System.Int64", "{0}" },
            { "System.Byte", "{0}" },
            { "System.Guid", "'{0}'" },
            { "System.String", "'{0}'" },
            { "System.Decimal", "{0}" },
            { "System.DateTime", "'{0}'" },
            { "System.TimeSpan", "'{0}'" },
            { "System.Boolean", "{0}" }
        };

        /// <summary>
        /// Resolves the SQL formatting for the object type.
        /// </summary>
        /// <param name="searchTermType">The value of the object in the conditional clause</param>
        /// <returns>The SQL formatting for the object type</returns>
        public static string Format(string searchTermType)
        {
            return _map[searchTermType];
        }
    }
}