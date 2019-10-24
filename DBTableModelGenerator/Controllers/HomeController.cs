using DBTableModelGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DBTableModelGenerator.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index(TableViewModel model)
        {
            model = this.SetDefaultConnectionString(model);
            return View(model);
        }

        [HttpPost]
        public ActionResult Query(TableViewModel model) {
            model = this.SetDefaultConnectionString(model);
            try
            {
                var gen = new TableViewGenerator(model);
                model.DBModel = gen.GenerateDBModel();
                model.ViewModel = gen.GenerateViewModel();
                model.SQL_INSERT = gen.GenerateSQL_Insert();
                model.SQL_UPDATE = gen.GenerateSQL_Update();
                model.SQL_DELETE = gen.GenerateSQL_Delete();
            }
            catch (Exception ex) {
                model.DBModel = ex.Message;
            }

            return View("Index", model);
        }

        private TableViewModel SetDefaultConnectionString(TableViewModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.ConnectionString))
            {
                model = new TableViewModel
                {
                    ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnectionString"].ToString()
                };
            }

            model.Schema = "dbo";
            return model;
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}