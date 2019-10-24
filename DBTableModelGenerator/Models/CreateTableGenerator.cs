using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;

namespace DBTableModelGenerator.Models
{
    public class CreateTableGenerator
    {
        private CreateTableViewModel _model = null;
        private StringBuilder _stringBuilder = null;
        public CreateTableGenerator(CreateTableViewModel model)
        {
            this._model = model;
            this._stringBuilder = new StringBuilder();
        }

        public string Generate() {
            return this._GenCreateTableScript();
        }

        public CreateTableViewModel LoadDB() {
            _model.TableDescription = this.QueryTableDescription();
            DataTable dtColumns = this.QueryColumns();
            _model.Columns = this._GetColumnViewModels(dtColumns);
            DataTable dtFKeys = this.QueryFKeys();
            _SetForeignKey(dtFKeys);
            return _model;
        }

        public void CreateTable() {
            using (SqlConnection conn = new SqlConnection(_model.ConnectionString))
            {
                SqlCommand sqlCommand = new SqlCommand(_model.Script, conn);
                sqlCommand.Parameters.AddWithValue("fktable_name", _model.TableName);
                conn.Open();
                sqlCommand.ExecuteNonQuery();
                conn.Close();
            }
        }

        private DataTable QueryColumns()
        {
            DataTable rtn = new DataTable();
            using (SqlConnection conn = new SqlConnection(_model.ConnectionString))
            {
                SqlCommand sqlCommand = new SqlCommand(@"select sc.name [name]
                    	, UPPER([types].[name]) [type]
                    	, sc.[max_length] [max_length]
                    	, sc.is_identity [is_identity]
                    	, ~sc.[is_nullable] [is_not_nullable]
                    	, [default].[definition]
                    	, sep.value [desc]
                    from sys.columns sc 
                    	LEFT JOIN sys.extended_properties sep on sc.column_id = sep.minor_id and sep.name = 'MS_Description' AND sc.object_id = sep.major_id
                    	LEFT JOIN sys.types [types] ON sc.[system_type_id] = [types].[system_type_id] AND [types].[name] != 'sysname'
                    	LEFT JOIN sys.default_constraints [default] ON [sc].column_id = [default].parent_column_id AND [sc].object_id = [default].parent_object_id
                    where sc.object_id = OBJECT_ID(@table)", conn);
                sqlCommand.Parameters.AddWithValue("table", _model.TableName);
                SqlDataAdapter da = new SqlDataAdapter(sqlCommand);
                conn.Open();
                da.Fill(rtn);
                conn.Close();
            }

            return rtn;
        }

        private string QueryTableDescription() {
            string rtn = string.Empty;
            using (SqlConnection conn = new SqlConnection(_model.ConnectionString))
            {
                SqlCommand sqlCommand = new SqlCommand(@"SELECT [sep].[value] 
                    FROM sys.extended_properties [sep]
                    	INNER JOIN sys.objects [obj] ON [sep].[major_id] = obj.[object_id] AND [sep].[name] = 'MS_Description'
                    WHERE [sep].[minor_id] = 0
                    	AND [obj].[object_id] = OBJECT_ID(@table)
                    	AND [obj].[schema_id] = SCHEMA_ID(@schema)", conn);
                sqlCommand.Parameters.AddWithValue("table", _model.TableName);
                sqlCommand.Parameters.AddWithValue("schema", _model.Schema);
                conn.Open();
                var res = sqlCommand.ExecuteScalar();
                if (res != null) {
                    rtn = res.ToString();
                }
                conn.Close();
            }

            return rtn;
        }

        private DataTable QueryFKeys() {
            DataTable rtn = new DataTable();
            using (SqlConnection conn = new SqlConnection(_model.ConnectionString))
            {
                SqlCommand sqlCommand = new SqlCommand(@"sp_fkeys", conn);
                sqlCommand.Parameters.AddWithValue("fktable_name", _model.TableName);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(sqlCommand);
                conn.Open();
                da.Fill(rtn);
                conn.Close();
            }

            return rtn;
        }

        private void _SetForeignKey(DataTable dataTable) {
            foreach (DataRow dr in dataTable.Rows) {
                var cols = dr["FKCOLUMN_NAME"].ToString();
                var col = _model.Columns.Where(x => x.Name == cols).SingleOrDefault();
                if (col != null) {
                    var pktable = dr["PKTABLE_NAME"].ToString();
                    var pkcol = dr["PKCOLUMN_NAME"].ToString();
                    col.ForeignKey = string.Format("[{0}].[{1}]", pktable, pkcol);
                }
            }
        }

        private IEnumerable<ColumnViewModel> _GetColumnViewModels(DataTable dataTable) {
            List<ColumnViewModel> rtn = new List<ColumnViewModel>();
            foreach (DataRow dr in dataTable.Rows) {
                string type = dr["type"].ToString();
                int leng = !type.Contains("CHAR") ? 0 : int.Parse(dr["max_length"].ToString());
                if (type.StartsWith("N")) {
                    leng = leng / 2;
                }

                bool isIdentity = Boolean.Parse(dr["is_identity"].ToString());
                bool isNotNull = Boolean.Parse(dr["is_not_nullable"].ToString());
                string def = dr["definition"].ToString()
                    .Replace("('", "'").Replace("')", "'")
                    .TrimStart('(').Replace("))", ")");

                if (!def.Contains("()"))
                {
                    if ((def.Contains("(") && !def.Contains(")")) || (!def.Contains("(") && def.Contains(")")))
                    {
                        def = def.Replace("(", "").Replace(")", "");
                    }
                }
                else {
                    def = def.ToUpper();
                }

                string desc = dr["desc"].ToString();
                rtn.Add(new ColumnViewModel
                {
                    Name = dr["name"].ToString(),
                    Type = type,
                    Length = leng,
                    IsIdentity = isIdentity,
                    IsNotNull = isNotNull,
                    Default = def,
                    Description = desc,
                });
            }

            return rtn;
        }

        private string _GenCreateTableScript() {
            DataTable foreignKeys = this._QueryForeignKey();

            this._Gen_DropTable(foreignKeys);
            this._Gen_CreateTable();
            _stringBuilder.AppendLine("");
            this._Gen_ReferencedForeignKeys(foreignKeys);
            this._Gen_Description();
            return _stringBuilder.ToString();
        }

        private DataTable _QueryForeignKey() {
            DataTable rtn = new DataTable();
            using (SqlConnection conn = new SqlConnection(_model.ConnectionString))
            {
                SqlCommand sqlCommand = new SqlCommand(@"sp_fkeys", conn);
                sqlCommand.Parameters.AddWithValue("pktable_name", _model.TableName);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                SqlDataAdapter da = new SqlDataAdapter(sqlCommand);
                conn.Open();
                da.Fill(rtn);
                conn.Close();
            }

            return rtn;
        }

        private void _Gen_DropTable(DataTable removedFK) {
            _stringBuilder.AppendLine("IF EXISTS(SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('" + _model.TableName + "') AND schema_id = SCHEMA_ID('" + _model.Schema + "'))");
            _stringBuilder.AppendLine("BEGIN");
            foreach (DataRow dr in removedFK.Rows) {
                _stringBuilder.AppendLine("\tALTER TABLE [" + dr["FKTABLE_OWNER"].ToString() + "].[" + dr["FKTABLE_NAME"].ToString() + "] DROP CONSTRAINT [" + dr["FK_NAME"].ToString() + "]");
            }

            _stringBuilder.AppendLine("\tDROP TABLE [" + _model.Schema + "].[" + _model.TableName + "];");
            _stringBuilder.AppendLine("END");
            _stringBuilder.AppendLine("");
        }

        private void _Gen_CreateTable() {
            _stringBuilder.AppendLine("CREATE TABLE [" + _model.Schema + "].[" + _model.TableName + "](");
            List<string> listCol = new List<string>();
            foreach (var col in _model.Columns)
            {
                var str = string.Format("\t[{0}]", col.Name);
                str += string.Format("\t{0}", col.Type.ToLower().Contains("char")
                    ? col.Type.ToUpper() + "(" + col.Length + ")"
                    : col.Type);
                str += string.Format("\t{0}", col.IsIdentity ? "IDENTITY(1, 1)" : string.Empty);
                str += string.Format("\t{0}", col.IsNotNull ? "NOT NULL" : "NULL");
                str += string.Format("\t{0}", string.IsNullOrEmpty(col.Default)
                    ? string.Empty
                    : "DEFAULT(" + col.Default + ")");

                listCol.Add(str.TrimEnd());
            }
            _stringBuilder.Append(string.Join(",\r\n", listCol));

            this._Gen_Constraint_PrimaryKey();
            this._Gen_Constraint_ForeignKey();

            _stringBuilder.AppendLine(");");
        }

        private void _Gen_Constraint_PrimaryKey() {
            var pk_col = _model.Columns.Where(x => x.IsIdentity).SingleOrDefault();
            if (pk_col != null) {
                this.PadDot();
                _stringBuilder.Append(string.Format("\tCONSTRAINT pk_{0} PRIMARY KEY([{1}])", _model.TableName, pk_col.Name));
            }
        }

        private void _Gen_Constraint_ForeignKey()
        {
            var fk_cols = _model.Columns.Where(x => !string.IsNullOrEmpty(x.ForeignKey));
            if (fk_cols.Count() > 0) {
                this.PadDot();
                List<string> fk_string = new List<string>();
                foreach (var col in fk_cols)
                {
                    var str = col.ForeignKey.Split('.');
                    str[0] = str[0].Replace("[", "").Replace("]", "");
                    str[1] = str[1].Replace("[", "").Replace("]", "");
                    fk_string.Add(string.Format("\tCONSTRAINT fk_{0}_{1}_{2} FOREIGN KEY([{3}]) REFERENCES [{4}]([{5}])"
                        , _model.TableName, str[0], col.Name, col.Name, str[0], str[1]));
                }

                _stringBuilder.Append(string.Join(",\r\n", fk_string));
            }
        }

        private void _Gen_ReferencedForeignKeys(DataTable removedFK) {
            foreach (DataRow dr in removedFK.Rows)
            {
                _stringBuilder.AppendLine("ALTER TABLE [" + dr["FKTABLE_OWNER"].ToString() + "].[" + dr["FKTABLE_NAME"].ToString() + "] ADD CONSTRAINT " + dr["FK_NAME"].ToString() + " FOREIGN KEY ([" + dr["FKCOLUMN_NAME"].ToString() + "]) REFERENCES [" + _model.Schema + "].[" + _model.TableName + "](" + dr["PKCOLUMN_NAME"].ToString() + ")");
            }
        }

        private void _Gen_Description() {
            _stringBuilder.AppendLine("EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'" + _model.TableDescription + "' ");
            _stringBuilder.AppendLine("\t, @level0type=N'SCHEMA', @level0name=N'" + _model.Schema + "'");
            _stringBuilder.AppendLine("\t, @level1type=N'TABLE', @level1name=N'" + _model.TableName + "'");
            _stringBuilder.AppendLine("");

            foreach (var col in _model.Columns)
            {
                _stringBuilder.AppendLine("EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'" + col.Description + "' ");
                _stringBuilder.AppendLine("\t, @level0type=N'SCHEMA', @level0name=N'" + _model.Schema + "'");
                _stringBuilder.AppendLine("\t, @level1type=N'TABLE', @level1name=N'" + _model.TableName + "'");
                _stringBuilder.AppendLine("\t, @level2type=N'COLUMN', @level2name=N'" + col.Name + "';");
                _stringBuilder.AppendLine("");
            }
        }

        private void PadDot() {
            if (_stringBuilder.ToString().TrimEnd().Last() != ',')
            {
                _stringBuilder.AppendLine(",");
            }
        }
    }
}