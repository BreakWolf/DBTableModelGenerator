using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using TemplateEngine.Docx;

namespace DBTableModelGenerator.Models
{
    public class Exportor
    {
        private ExportViewModel _viewModel;

        public Exportor(ExportViewModel viewModel)
        {
            this._viewModel = viewModel;
        }

        public void Export() {
            this.setViewModel();
            var fileInfo = new FileInfo(_viewModel.TemplatePath);
            var newFileInfo = new FileInfo(fileInfo.DirectoryName + "\\" + fileInfo.Name.Split('.')[0] + "_new" + fileInfo.Extension);
            File.Delete(newFileInfo.FullName);
            File.Copy(fileInfo.FullName, newFileInfo.FullName);

            using (var outputDocument = new TemplateProcessor(newFileInfo.FullName).SetRemoveContentControls(true)) {

                ListContent listContent = new ListContent("Table list");
                foreach (var t in _viewModel.Tables) {
                    var listItemContent = new ListItemContent();
                    listContent.AddItem(listItemContent);
                    listItemContent.AddField("Table title", "[" + t.Schema + "].[" + t.TableName + "]");
                    listItemContent.AddField("Table name", "[" + t.Schema + "].[" + t.TableName + "]");
                    listItemContent.AddField("Table description", t.Description);
                    listItemContent.AddField("Table pk", t.PrimaryKeys);

                    var columnContent = new TableContent("Column row");
                    listItemContent.AddTable(columnContent);
                    foreach (var c in t.Columns) {
                        columnContent.AddRow(
                            new FieldContent("Name", c.Name),
                            new FieldContent("Type", c.Type),
                            new FieldContent("Null", c.Nullable),
                            new FieldContent("Description", c.Description));
                    }
                }
                
                Content mainContent = new Content(listContent);
                outputDocument.FillContent(mainContent);
                outputDocument.SaveChanges();
            }
        }

        private void setViewModel() {
            List<SchemaTable> tableNames = this.QueryTableNames();
            foreach (var tablename in tableNames) {
                CreateTableGenerator createTableGenerator = new CreateTableGenerator(new CreateTableViewModel
                {
                    ConnectionString = this._viewModel.ConnectionString,
                    TableName = tablename.Table,
                    Schema = tablename.Schema,
                    PrimaryKeys = string.Join(", ", QueryPrimaryKeys(tablename.Table))
                });

                _viewModel.AddTable(createTableGenerator.LoadDB());
            }
        }

        private List<SchemaTable> QueryTableNames() {
            List<SchemaTable> rtn = new List<SchemaTable>();
            using (SqlConnection conn = new SqlConnection(_viewModel.ConnectionString))
            {
                SqlCommand sqlCommand = new SqlCommand(@"SELECT SCHEMA_NAME(schema_id) [schema], [name] 
                    FROM sys.objects
                    WHERE[type] = 'U'", conn);
                conn.Open();
                SqlDataReader sqlreader = sqlCommand.ExecuteReader();
                if (sqlreader.HasRows) {
                    while (sqlreader.Read()) {
                        rtn.Add(new SchemaTable {
                            Schema = sqlreader.GetString(0),
                            Table = sqlreader.GetString(1),
                        });
                    }
                }
                sqlreader.Close();
                conn.Close();
            }

            return rtn;
        }

        private List<string> QueryPrimaryKeys(string tablename) {
            List<string> pkList = new List<string>();
            using (SqlConnection conn = new SqlConnection(_viewModel.ConnectionString)) {
                SqlCommand pkCommand = new SqlCommand(@"sp_pkeys", conn);
                pkCommand.CommandType = System.Data.CommandType.StoredProcedure;
                pkCommand.Parameters.AddWithValue("@table_name", tablename);
                conn.Open();
                SqlDataReader pkQueryReader = pkCommand.ExecuteReader();
                if (pkQueryReader.HasRows)
                {
                    while (pkQueryReader.Read())
                    {
                        pkList.Add("[" + pkQueryReader.GetString(3) + "]");
                    }
                }
                pkQueryReader.Close();
                conn.Close();
            }
            return pkList;
        }

        class SchemaTable {
            public string Schema { get; set; }
            public string Table { get; set; }
        }
    }
}