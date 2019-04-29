using System.Collections.Generic;

namespace QueryLogic
{
    /// <summary>
    /// Representation of a database row
    /// </summary>
    public class Row
    {
        private readonly Dictionary<string, object> _row;
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public Row()
        {
            _row = new Dictionary<string, object>();
        }

        /// <summary>
        /// Overloaded constructor
        /// </summary>
        /// <param name="rowMap">Initial data row</param>
        public Row(RowMap rowMap)
        {
            _row = rowMap;
        }

        /// <summary>
        /// Adds row cell as a key/value pair
        /// </summary>
        /// <param name="key">Name of database column</param>
        /// <param name="data">Value of data row cell</param>
        public void Add(string key, object data)
        {
            _row.Add(_row.ContainsKey(key.ToLower()) ? $"{key.ToLower()}_01" : key, data);
        }
        
        /// <summary>
        /// Number of elements in row
        /// </summary>
        public int Count => _row.Count;

        /// <summary>
        /// Returns available cell data for the 
        /// provided column name
        /// </summary>
        /// <param name="key">Column name</param>
        public object this[string key]
        {
            get
            {
                if (!_row.ContainsKey(key.ToLower())) throw new KeyNotFoundException("The specified column key was not found");
                
                return _row[key.ToLower()];
            }
        }
        
        /// <summary>
        /// Exports the contents of the row's internal mapper
        /// as an IDictionary for ORM and other functionality
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, object> GetMap()
        {
            return _row;
        }
    }
}