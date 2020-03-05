using Microsoft.AspNetCore.Mvc;

namespace GroupDocs.Editor.MVC.Controllers
{
    public class EditorController : Controller
    {
        public ActionResult Index()
        {
            return View("Index");
        }
    }
}
