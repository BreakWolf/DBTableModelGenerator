using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DBTableModelGenerator.Models
{
    public class ExportViewModel
    {
        public string ConnectionString { get; set; }

        public string TemplatePath { get; set; }

        public IList<TableView> Tables { get; set; }

        public ExportViewModel()
        {
            this.Tables = new List<TableView>();
        }

        public void AddTable(CreateTableViewModel tableViewModel) {
            this.Tables.Add(new TableView(tableViewModel));
        }
    }

    public class TableView {
        public string Schema { get; set; }

        public string TableName { get; set; }

        public string PrimaryKeys { get; set; }

        public string Description { get; set; }

        public IEnumerable<ColumnView> Columns { get; set; }

        public TableView()
        {
            this.Columns = new List<ColumnView>();
        }

        public TableView(CreateTableViewModel tableViewModel)
        {
            this.Schema = tableViewModel.Schema;
            this.TableName = tableViewModel.TableName;
            this.Description = tableViewModel.TableDescription;
            this.PrimaryKeys = tableViewModel.PrimaryKeys;
            var columns = new List<ColumnView>();
            foreach (var col in tableViewModel.Columns) {
                columns.Add(new ColumnView(col));
            }

            this.Columns = columns;
        }

        public TableView(TableViewModel tableViewModel)
        {
            this.Schema = tableViewModel.Schema;
            this.TableName = tableViewModel.TableName;
            this.Columns = new List<ColumnView>();
        }
    }

    public class ColumnView {
        public string Name { get; set; }

        public string Type { get; set; }

        public string Nullable { get; set; }

        public string Default { get; set; }

        public string Description { get; set; }

        public ColumnView(ColumnViewModel columnViewModel)
        {
            this.Name = columnViewModel.Name;
            this.Type = string.Format("{0}{1}"
                , columnViewModel.Type
                , columnViewModel.Type.Contains("CHAR") ? ("(" + columnViewModel.Length + ")") : string.Empty);
            this.Nullable = columnViewModel.IsNotNull ? string.Empty : "Y";
            this.Default = columnViewModel.Default;
            this.Description = columnViewModel.Description;
        }
    }
}