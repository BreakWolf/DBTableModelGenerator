using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DBTableModelGenerator.Models
{
    public class TableViewModel
    {
        public string ConnectionString { get; set; }

        public string Schema { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string TableName { get; set; }

        public string DBModel { get; set; }

        public string ViewModel { get; set; }

        public string SQL_INSERT { get; set; }

        public string SQL_UPDATE { get; set; }

        public string SQL_DELETE { get; set; }
    }
}