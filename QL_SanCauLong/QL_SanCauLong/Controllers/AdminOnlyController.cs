using QL_SanCauLong.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace QL_SanCauLong.Controllers
{
    public class AdminOnlyController : Controller
    {
        private readonly QuanLySanCauLongEntities3 db = new QuanLySanCauLongEntities3();

        // Attribute: chỉ cho user ID = 3
        public class AdminOnlyAttribute : AuthorizeAttribute
        {
            protected override bool AuthorizeCore(HttpContextBase httpContext)
            {
                var userId = httpContext.Session["UserId"];
                return userId != null && userId.ToString() == "3";
            }
        }

        // GET: /AdminOnly/CreateAdmin
        [AdminOnly]
        public ActionResult CreateAdmin()
        {
            var admins = db.customers.Where(c => c.role == "admin").ToList();
            ViewBag.Admins = admins;
            return View();
        }

        // POST: /AdminOnly/CreateAdmin
        [HttpPost]
        [AdminOnly]
        public ActionResult CreateAdmin(string name, string phone, string password)
        {
            if (db.customers.Any(c => c.phone == phone))
            {
                ModelState.AddModelError("", "Số điện thoại đã tồn tại.");
                ViewBag.Admins = db.customers.Where(c => c.role == "admin").ToList();
                return View();
            }

            var newAdmin = new customer
            {
                name = name,
                phone = phone,
                password = password,
                role = "admin"
            };
            db.customers.Add(newAdmin);
            db.SaveChanges();

            ViewBag.Success = "Đã tạo tài khoản admin thành công!";
            ViewBag.Admins = db.customers.Where(c => c.role == "admin").ToList();
            return View();
        }

        [HttpPost]
        [AdminOnly]
        public ActionResult UpdatePassword(int id, string newPassword)
        {
            var user = db.customers.Find(id);
            if (user == null || user.role != "admin")
            {
                return Json(new { success = false, message = "Không tìm thấy admin." });
            }

            user.password = newPassword;
            db.Entry(user).State = EntityState.Modified;
            db.SaveChanges();

            return Json(new { success = true });
        }

        [HttpPost]
        [AdminOnly]
        public ActionResult DeleteAdmin(int id)
        {
            var user = db.customers.Find(id);
            if (user.id == 3)
            {
                return Json(new { success = false, message = "Không thể xoá tài khoản quản trị gốc." });
            }
            else if (user == null || user.role != "admin")
            {
                return Json(new { success = false, message = "Không tìm thấy admin." });
            }

            // Không cho xóa chính mình
            var currentUserId = HttpContext.Session["UserId"]?.ToString();
            if (currentUserId == user.id.ToString())
            {
                return Json(new { success = false, message = "Không thể xóa chính tài khoản của bạn." });
            }

            db.customers.Remove(user);
            db.SaveChanges();
            return Json(new { success = true });

        }

    }
}