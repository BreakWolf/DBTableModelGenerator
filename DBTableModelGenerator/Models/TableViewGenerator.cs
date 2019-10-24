using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;

namespace DBTableModelGenerator.Models
{
    public class TableViewGenerator
    {
        private TableViewModel _model;
        private DataTable _dataTable;
        private string[] _innerBaseModelProperties = new string[] { "Id", "CreateUser", "CreateTime", "ModifyUser", "ModifyTime" };
        private string[] _NoshowInViewModelProperties = new string[] { "CreateUser", "CreateTime", "ModifyUser", "ModifyTime" };

        public TableViewGenerator(TableViewModel model)
        {
            this._model = model;
            this._dataTable = null;
        }

        public string GenerateDBModel() {
            this.GetDataTable();
            return this.Output_DBModel();
        }

        public string GenerateViewModel() {
            this.GetDataTable();
            return this.Output_ViewModel();
        }

        public string GenerateSQL_Insert()
        {
            this.GetDataTable();
            return this.Output_SQL_INSERT();
        }

        public string GenerateSQL_Update()
        {
            this.GetDataTable();
            return this.Output_SQL_UPDATE();
        }

        public string GenerateSQL_Delete()
        {
            this.GetDataTable();
            return this.Output_SQL_DELETE();
        }


        private void GetDataTable() {
            if (this._dataTable == null) {
                this._dataTable = this.Query();
            }
        }

        private DataTable Query() {
            DataTable rtn = new DataTable();
            using (SqlConnection conn = new SqlConnection(_model.ConnectionString)) {
                SqlCommand sqlCommand = new SqlCommand(@"    select sc.name [name]
                		, sc.[system_type_id] [system_type_id]
                		, sc.[max_length] [max_length]
                		, sc.[is_nullable] [is_nullable]
                		, sep.value [desc]
                    from sys.columns sc 
                    left join sys.extended_properties sep on sc.column_id = sep.minor_id
                                                         and sep.name = 'MS_Description'
                    where sc.object_id = OBJECT_ID(@table)", conn);
                sqlCommand.Parameters.AddWithValue("table", _model.TableName);
                SqlDataAdapter da = new SqlDataAdapter(sqlCommand);
                conn.Open();
                da.Fill(rtn);
                conn.Close();
            }

            return rtn;
        }

        private string Output_DBModel() {
            StringBuilder builder = new StringBuilder();
            builder.Append("public class " + _model.TableName + " : BaseModel {\r\n");
            foreach (DataRow dr in this._dataTable.Rows) {
                if (_innerBaseModelProperties.Contains(dr["name"].ToString())) { continue; }

                var type_id = int.Parse(dr["system_type_id"].ToString());
                var maxlength = int.Parse(dr["max_length"].ToString());
                var type = this.GetTypeName(type_id);
                var is_nullable = bool.Parse(dr["is_nullable"].ToString());
                var desc = dr["desc"].ToString();

                if (!string.IsNullOrEmpty(desc)) {
                    builder.Append("\t/// <summary>\r\n");
                    builder.Append("\t/// " + desc + "\r\n");
                    builder.Append("\t/// </summary>\r\n");
                }

                if (!is_nullable) {
                    builder.Append("\t[Required]\r\n");
                }
                if (type == "string") {
                    builder.Append("\t[StringLength(" + (this.IsUnicodeString(type_id) ? (maxlength / 2) : maxlength).ToString() + ")]\r\n");
                }

                builder.Append(string.Format("\tpublic {0}{1} {2} {{ get; set; }}\r\n\r\n"
                    , type
                    , is_nullable && type != "string" ? "?" : ""
                    , dr["name"].ToString()));
            }
            builder.Append("}");
            return builder.ToString();
        }
        private string Output_ViewModel()
        {
            StringBuilder builder = new StringBuilder();
            List<string> properties = new List<string>();
            builder.Append("public class " + _model.TableName + "ViewModel {\r\n");
            foreach (DataRow dr in this._dataTable.Rows)
            {
                var name = dr["name"].ToString();
                if (_NoshowInViewModelProperties.Contains(name)) { continue; }
                var type_id = int.Parse(dr["system_type_id"].ToString());
                var maxlength = int.Parse(dr["max_length"].ToString());
                var type = this.GetTypeName(type_id);
                var is_nullable = bool.Parse(dr["is_nullable"].ToString());
                var desc = dr["desc"].ToString();

                if (!string.IsNullOrEmpty(desc))
                {
                    builder.Append("\t/// <summary>\r\n");
                    builder.Append("\t/// " + desc + "\r\n");
                    builder.Append("\t/// </summary>\r\n");
                }

                properties.Add(name);
                builder.Append(string.Format("\tpublic {0}{1} {2} {{ get; set; }}\r\n\r\n"
                    , type
                    , is_nullable && type != "string" ? "?" : ""
                    , name));
            }
            builder.Append(this.GetToModelString(properties));
            builder.Append("}");

            return builder.ToString();
        }
        private string Output_SQL_INSERT() {
            StringBuilder builder = new StringBuilder();
            List<string> properties = new List<string>();
            builder.Append("INSERT INTO [" + _model.Schema + "].[" + _model.TableName + "] (");
            foreach (DataRow dr in this._dataTable.Rows)
            {
                var name = dr["name"].ToString();
                if (name == "Id") { continue; }
                properties.Add(name);
            }

            builder.Append(string.Join(" ,", properties));
            builder.Append(") \r\nVALUES(@");
            builder.Append(string.Join(", @", properties));
            builder.Append(");\r\n");
            builder.Append("SELECT [Id] FROM [" + _model.TableName + "] WHERE [CreateTime] = @CreateTime");
            return builder.ToString();
        }
        private string Output_SQL_UPDATE()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("UPDATE [" + _model.Schema + "].[" + _model.TableName + "] SET \r\n");
            foreach (DataRow dr in this._dataTable.Rows)
            {
                var name = dr["name"].ToString();
                if (name == "Id") { continue; }
                builder.Append("\t[" + name + "] = @" +name + ", \r\n");
            }
            builder.Remove(builder.Length - 4, 4);
            builder.Append("\r\nWHERE Id = @Id");
            return builder.ToString();
        }

        private string Output_SQL_DELETE()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("DELETE [" + _model.Schema + "].[" + _model.TableName + "] WHERE [Id] = @Id");
            return builder.ToString();
        }


        private string GetTypeName(int type_id) {
            switch (type_id) {
                case 48:
                case 52:
                case 56:
                    return "int";
                case 127:
                    return "long";
                case 59:
                case 60:
                case 106:
                case 108:
                    return "decimal";
                case 62:
                    return "double";
                case 40:
                case 42:
                case 61:
                case 189:
                    return "DateTime";
                case 104:
                    return "bool";
                case 35:
                case 99:
                case 231:
                case 167:
                case 175:
                case 239:
                default:
                    return "string";
            }
        }

        private string GetToModelString(IEnumerable<string> props) {
            StringBuilder rtn = new StringBuilder();
            rtn.Append("\tpublic " + _model.TableName + " ToModel() {\r\n");
            rtn.Append("\t\treturn new " + _model.TableName + "\r\n\t\t{\r\n");
            foreach (var item in props) {
                rtn.Append("\t\t\t" + item + " = this." + item + ",\r\n");
            }
            rtn.Append("\t\t};\r\n");
            rtn.Append("\t}\r\n");
            return rtn.ToString();
        }

        private bool IsUnicodeString(int type_id) {
            return type_id == 99
                || type_id == 108
                || type_id == 231
                || type_id == 239;
        }

    }
}