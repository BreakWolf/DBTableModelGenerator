using DBTableModelGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DBTableModelGenerator.Controllers
{
    public class CreateTableController : Controller
    {
        // GET: CreateTable
        public ActionResult Index(CreateTableViewModel model)
        {
            model = this.SetDefaultViewModel(model);
            return View(model);
        }

        public ActionResult LoadDB(CreateTableViewModel model) {
            if (string.IsNullOrEmpty(model.TableName)) {
                return this.RedirectToAction("Index");
            }

            CreateTableGenerator generator = new CreateTableGenerator(model);
            model = generator.LoadDB();
            return this.View("Index", model);
        }

        public ActionResult CreateTable(CreateTableViewModel model) {
            if (string.IsNullOrEmpty(model.ConnectionString)) {
                return this.RedirectToAction("Index");
            }
            CreateTableGenerator generator = new CreateTableGenerator(model);
            generator.CreateTable();

            model = generator.LoadDB();
            return this.View("Index", model);
        }

        [HttpPost]
        public JsonResult GenScript(CreateTableViewModel model) {
            CreateTableGenerator generator = new CreateTableGenerator(model);
            return new JsonResult { Data = generator.Generate() };
        }


        private CreateTableViewModel SetDefaultViewModel(CreateTableViewModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.ConnectionString))
            {
                model = new CreateTableViewModel
                {
                    ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnectionString"].ToString()
                };
                model.Schema = "dbo";

                var columns = (List<ColumnViewModel>)model.Columns;
                columns.Add(new ColumnViewModel { Name = "Id", Type = "INT", IsIdentity = true, IsNotNull = true, Description="Identity" });
                columns.Add(new ColumnViewModel { Name = "CreateUser", Type = "NVARCHAR", Length = 50, IsIdentity = false, IsNotNull = true, Default = "'SqlScript'", Description = "CreateUser" });
                columns.Add(new ColumnViewModel { Name = "ModifyUser", Type = "NVARCHAR", Length = 50, IsIdentity = false, IsNotNull = false, Default = string.Empty, Description = "ModifyUser" });
                columns.Add(new ColumnViewModel { Name = "CreateTime", Type = "DATETIME", IsIdentity = false, IsNotNull = true, Default = "GETDATE()", Description = "CreateTime" });
                columns.Add(new ColumnViewModel { Name = "ModifyTime", Type = "DATETIME", IsIdentity = false, IsNotNull = false, Default = string.Empty, Description = "ModifyTime" });
            }

            return model;
        }
    }
}