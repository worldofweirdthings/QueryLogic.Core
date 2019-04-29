using System;

namespace QueryLogic
{
    /// <summary>
    /// Represents an error which occurred during the execution of a SQL command
    /// </summary>
    public class QueryException : Exception
    {
        /// <summary>
        /// Creates new instance of the exception
        /// </summary>
        /// <param name="message">Error message</param>
        public QueryException(string message) : base(message) { }

        /// <summary>
        /// Creates new instance of the exception
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Exception captured and encapsulated for review</param>
        public QueryException(string message, Exception innerException) : base(message, innerException) { }
    }
}