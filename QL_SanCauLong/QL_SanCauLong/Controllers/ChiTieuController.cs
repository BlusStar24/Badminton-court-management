using QL_SanCauLong.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QL_SanCauLong.Controllers
{
    public class ChiTieuController : Controller
    {
        QuanLySanCauLongEntities3 db = new QuanLySanCauLongEntities3();
        [Authorize]
        public ActionResult Index(string ngay = null, string thang = null)
        {
            DateTime? filterDate = null;
            DateTime? filterMonth = null;

            if (DateTime.TryParse(ngay, out var d))
                filterDate = d;

            if (DateTime.TryParse(thang + "-01", out var m))
                filterMonth = m;

            var ds = db.expenses.AsQueryable();

            if (filterDate.HasValue)
            {
                var dateOnly = filterDate.Value.Date;
                ds = ds.Where(c => DbFunctions.TruncateTime(c.created_at) == dateOnly);
            }
            else if (filterMonth.HasValue)
            {
                int month = filterMonth.Value.Month;
                int year = filterMonth.Value.Year;
                ds = ds.Where(c => c.created_at.HasValue &&
                                   c.created_at.Value.Month == month &&
                                   c.created_at.Value.Year == year);
            }

            var danhSach = ds.OrderByDescending(c => c.created_at).ToList();

            ViewBag.Ngay = ngay;
            ViewBag.Thang = thang;
            ViewBag.DanhSach = danhSach;

            return View(new expens());
        }



        [HttpPost]
        public ActionResult ThemChiTieu(expens model)
        {
            if (ModelState.IsValid)
            {
                model.created_at = DateTime.Now;
                db.expenses.Add(model);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            // Nếu lỗi, vẫn cần load danh sách để hiển thị
            var danhSach = db.expenses.OrderByDescending(c => c.created_at).ToList();
            ViewBag.DanhSach = danhSach;
            return View("Index", model);
        }

        [HttpPost]
        public ActionResult XoaChiTieu(int id)
        {
            var ct = db.expenses.Find(id);
            if (ct == null)
                return Json(new { success = false, message = "Không tìm thấy chi tiêu." });

            db.expenses.Remove(ct);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult SuaChiTieu(expens model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

            var ct = db.expenses.Find(model.id);
            if (ct == null)
                return Json(new { success = false, message = "Không tìm thấy bản ghi." });

            ct.title = model.title;
            ct.amount = model.amount;
            ct.category = model.category;
            ct.note = model.note;
            db.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}