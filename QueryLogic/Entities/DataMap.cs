using System.Collections.Generic;

namespace QueryLogic
{
    /// <summary>
    /// A multi-table collection of database row representations
    /// </summary>
    public class DataMap : List<Rows>
    {
        private int _current;

        /// <summary>
        /// Default constructor
        /// </summary>
        public DataMap()
        {
            _current = 0;
        }

        /// <summary>
        /// Returns the first result set in the data map
        /// and does not advance the tracking of which
        /// result set will be returned in the next call
        /// </summary>
        /// <returns>Selected data</returns>
        public Rows First()
        {
            return this[0];
        }

        /// <summary>
        /// Returns the next result set in the data map and
        /// advances the current collection of selected data
        /// forward so the next calls returns the next result
        /// set in the result collection
        /// </summary>
        /// <returns>Selected data</returns>
        public Rows Next()
        {
            if (_current < Count) _current = _current + 1;

            return getCurrent();
        }

        /// <summary>
        /// Returns the last result set in the data map and
        /// rolls back the current collection of selected data
        /// backward so the next calls returns the next result
        /// set in the result collection
        /// </summary>
        /// <returns>Selected data</returns>
        public Rows Previous()
        {
            if (_current > 0) _current = _current - 1;

            return getCurrent();
        }

        /// <summary>
        /// Returns the first result set in the data map
        /// and does not change the tracking of which
        /// result set will be returned in the next call
        /// </summary>
        /// <returns>Selected data</returns>
        public Rows Last()
        {
            return this[Count - 1];
        }

        private Rows getCurrent()
        {
            return this[_current];
        }
    }
}