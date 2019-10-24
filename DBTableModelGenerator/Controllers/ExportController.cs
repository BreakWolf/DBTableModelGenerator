using DBTableModelGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DBTableModelGenerator.Controllers
{
    public class ExportController : Controller
    {
        // GET: Export
        public ActionResult Index()
        {
            return View(this.SetDefaultViewModel(null));
        }

        public ActionResult Export(ExportViewModel exportViewModel)
        {
            Exportor exportor = new Exportor(exportViewModel);
            exportor.Export();
            return this.RedirectToAction("Index", "Export");
        }

        private ExportViewModel SetDefaultViewModel(ExportViewModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.ConnectionString))
            {
                model = new ExportViewModel
                {
                    ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnectionString"].ToString(),
                    TemplatePath = System.Configuration.ConfigurationManager.AppSettings["DefaultTemplatePath"].ToString(),
                };
            }

            return model;
        }
    }
}