using System.Data.SqlClient;
using System.Threading.Tasks;

namespace QueryLogic
{
    /// <summary>
    /// Specifies methods for retrieving database rows and data sets
    /// </summary>
    public interface IQueryBuilder
    {
        /// <summary>
        /// Executes a stored procedure which returns data from a single select
        /// </summary>
        /// <param name="command">Database command</param>
        /// <returns>Row maps with retrieved data</returns>
        /// <exception cref="QueryException">SQL execution error</exception>
        Rows QueryData(SqlCommand command);

        /// <summary>
        /// Asynchronously executes a stored procedure which returns data from a single select
        /// </summary>
        /// <param name="command">Database command</param>
        /// <returns>Row maps with retrieved data</returns>
        /// <exception cref="QueryException">SQL execution error</exception> 
        Task<Rows> QueryDataAsync(SqlCommand command);

        /// <summary>
        /// Executes a stored procedure which returns data from a multi-select.
        /// </summary>
        /// <param name="command">SQL command</param>
        /// <returns>Dictionary of row maps with retrieved data</returns>
        /// <exception cref="QueryException">SQL execution error</exception>
        DataMap QueryDataSet(SqlCommand command);

        /// <summary>
        /// Asynchronously executes a stored procedure which returns data from a multi-select
        /// </summary>
        /// <param name="command">SQL command</param>
        /// <returns>Dictionary of row maps with retrieved data</returns>
        /// <exception cref="QueryException">SQL execution error</exception>
        Task<DataMap> QueryDataSetAsync(SqlCommand command);

        /// <summary>
        /// Executes a non-query stored procedure.
        /// </summary>
        /// <param name="command">SQL command</param>
        /// <returns>Row map with populated out parameters and/or number of affected rows</returns>
        /// <exception cref="QueryException">SQL execution error</exception>
        Row ModifyData(SqlCommand command);

        /// <summary>
        /// Asynchronously executes a non-query stored procedure
        /// </summary>
        /// <param name="command">SQL command</param>
        /// <returns>Row map with populated out parameters and/or number of affected rows</returns>
        /// <exception cref="QueryException">SQL execution error</exception>
        Task<Row> ModifyDataAsync(SqlCommand command);
    }
}