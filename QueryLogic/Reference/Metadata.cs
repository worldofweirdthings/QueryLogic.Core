using System.Collections.Generic;

namespace QueryLogic.Reference
{
    internal class Metadata
    {
        public string TableName { get; set; }
        public List<Column> Columns { get; set; }

        public Metadata()
        {
            Columns = new List<Column>();
        }
    }
}