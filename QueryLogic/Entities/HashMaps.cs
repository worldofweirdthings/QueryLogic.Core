using System.Collections.Generic;

namespace QueryLogic
{
    /// <summary>
    /// An aliased representation of a row
    /// </summary>
    public class RowMap : Dictionary<string, object> { }
    
    /// <summary>
    /// A collection of database row representations
    /// </summary>
    public class Rows : List<Row> { }
}