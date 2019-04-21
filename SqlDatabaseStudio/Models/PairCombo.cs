using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SqlDatabaseStudio.Models
{
    public class TableField
    {
        public string Text { get; set; }
        public string TableFieldName { get; set; }
        public string FieldSelected { get; set; }
        public DataTable ForeignTable { get; set; }
        public string Selected { get; set; }

        private int indexField;

        public IEnumerable<string> ComboList
        {
            get
            {
                indexField = (FieldSelected == null) ? 0 : ForeignTable.Columns.Cast<DataColumn>().TakeWhile(x => !x.ToString().Contains(FieldSelected)).Count();
                var items = ForeignTable.Rows.Cast<DataRow>().Select(b => b.ItemArray[indexField].ToString());
                return items;
            }
        }
        public IEnumerable<string> FieldsList
        {
            get
            {
                var fields = ForeignTable.Columns.Cast<DataColumn>().Select(a => a.ToString());
                if (FieldSelected == null)
                {
                    FieldSelected = fields.First();
                }
                return fields;
            }
        }
        public string GetId() => ForeignTable.Rows.Cast<DataRow>().First(a => a.ItemArray[indexField].ToString() == Selected).ItemArray.First().ToString();
    }
}
