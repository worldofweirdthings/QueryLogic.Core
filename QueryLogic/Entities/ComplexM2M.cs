using System;
using System.Collections.Generic;
using System.Linq;

namespace QueryLogic
{
    /// <summary>
    /// Class representing a many-to-many relationship between complex entities
    /// </summary>
    /// <typeparam name="T">Data type of the child elements</typeparam>
    public class ComplexM2M<T>
    {
        private readonly IDictionary<string, List<T>> _map;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ComplexM2M()
        {
            _map = new Dictionary<string, List<T>>();
        } 
        
        /// <summary>
        /// Add a many-to-many relationship
        /// </summary>
        /// <param name="key">Id of the parent element</param>
        /// <param name="value">Id of a child element</param>
        public void AddM2M(Guid? key, T value)
        {
            add(key.ToString(), value);
        }

        /// <summary>
        /// Add a many-to-many relationship
        /// </summary>
        /// <param name="key">Id of the parent element</param>
        /// <param name="value">Id of a child element</param>
        public void AddM2M(int? key, T value)
        {
            add(key.ToString(), value);
        }

        /// <summary>
        /// Checks if a parent element id has been added 
        /// to the internal relationship map
        /// </summary>
        /// <param name="key">Id of the parent element</param>
        /// <returns>Boolean flag</returns>
        public bool Contains(Guid? key)
        {
            return contains(key?.ToString());
        }

        /// <summary>
        /// Checks if a parent element id has been added 
        /// to the internal relationship map
        /// </summary>
        /// <param name="key">Id of the parent element</param>
        /// <returns>Boolean flag</returns>
        public bool Contains(int? key)
        {
            return contains(key?.ToString());
        }

        /// <summary>
        /// Returns all children of the parent element
        /// </summary>
        /// <param name="key">Id of the parent element</param>
        /// <returns>Collection of child element ids</returns>
        public List<T> this[Guid? key] => get(key?.ToString());

        /// <summary>
        /// Returns all children of the parent element
        /// </summary>
        /// <param name="key">Id of the parent element</param>
        /// <returns>Collection of child element ids</returns>
        public List<T> this[int? key] => get(key?.ToString());

        /// <summary>
        /// Checks if any relationships have been mapped
        /// </summary>
        /// <returns>Boolean flag</returns>
        public bool Any()
        {
            return _map.Any();
        }

        /// <summary>
        /// Flattens all child elements into a flat collection
        /// </summary>
        /// <returns>Flattened collection of objects</returns>
        public List<T> Flatten()
        {
            return _map.Any() ? _map.Values.SelectMany(v => v).ToList() : new List<T>();
        }

        #region Helpers

        private void add(string key, T value)
        {
            if (_map.ContainsKey(key))
            {
                _map[key].Add(value);
            }
            else
            {
                _map.Add(key, new List<T> { value });
            }
        }

        private bool contains(string key)
        {
            return key != null && _map.ContainsKey(key);
        }

        private List<T> get(string key)
        {
            return key != null ? _map[key] : null;
        }

        #endregion
    }
}