using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DBTableModelGenerator.Models
{
    public class CreateTableViewModel
    {
        public string ConnectionString { get; set; }

        public string Schema { get; set; }

        public string TableName { get; set; }

        public string TableDescription { get; set; }

        public string PrimaryKeys { get; set; }

        public IEnumerable<ColumnViewModel> Columns { get; set; }

        public string Script { get; set; }

        public CreateTableViewModel()
        {
            Columns = new List<ColumnViewModel>();
            this.ConnectionString = string.Empty;
            this.TableName = string.Empty;
            this.TableDescription = string.Empty;
            this.Script = string.Empty;
            this.Schema = string.Empty;
            this.PrimaryKeys = string.Empty;
        }
    }

    public class ColumnViewModel {
        public string Name { get; set; }

        public string Type { get; set; }

        public int Length { get; set; }

        public bool IsIdentity { get; set; }

        public bool IsNotNull { get; set; }

        public string Default { get; set; }

        public string Description { get; set; }

        public string ForeignKey { get; set; }

        public ColumnViewModel()
        {
            this.Name = string.Empty;
            this.Type = string.Empty;
            this.Length = 0;
            this.IsIdentity = false;
            this.IsNotNull = false;
            this.Default = string.Empty;
            this.Description = string.Empty;
            this.ForeignKey = null;
        }
    }
}