namespace QueryLogic.Reference
{
    internal class Column
    {
        public Column(string columnName, string columnDataType, int columnIndex)
        {
            ColumnName = columnName;
            ColumnDataType = columnDataType;
            ColumnIndex = columnIndex;
        }

        public string ColumnName { get; set; }
        public string ColumnDataType { get; set; }
        public int ColumnIndex { get; set; }
    }
}