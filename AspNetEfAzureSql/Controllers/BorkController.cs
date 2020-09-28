using AspNetEfAzureSql.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace AspNetEfAzureSql.Controllers
{
    public class BorkController : Controller
    {
        private BorkContext db = new BorkContext();

        public ActionResult Index()
        {
            return View(db.Borks.ToList());
        }

        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Bork bork = db.Borks.Find(id);
            if (bork == null)
            {
                return HttpNotFound();
            }
            return View(bork);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,IsBorked")] Bork bork)
        {
            if (ModelState.IsValid)
            {
                db.Borks.Add(bork);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(bork);
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Bork bork = db.Borks.Find(id);
            if (bork == null)
            {
                return HttpNotFound();
            }
            return View(bork);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,IsBorked")] Bork bork)
        {
            if (ModelState.IsValid)
            {
                db.Entry(bork).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(bork);
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Bork bork = db.Borks.Find(id);
            if (bork == null)
            {
                return HttpNotFound();
            }
            return View(bork);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Bork bork = db.Borks.Find(id);
            db.Borks.Remove(bork);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}