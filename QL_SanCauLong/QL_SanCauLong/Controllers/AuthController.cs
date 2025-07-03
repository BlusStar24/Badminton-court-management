using QL_SanCauLong.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace QL_SanCauLong.Controllers
{
    public class AuthController : Controller
    {
        // GET: Auth
        QuanLySanCauLongdbEntities db = new QuanLySanCauLongdbEntities();

        // GET: /Auth/Login
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        // GET: /Auth/Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: /Auth/Register
        [HttpPost]
        public ActionResult Register(string HoTen, string SoDienThoai)
        {
            Request.ContentEncoding = System.Text.Encoding.UTF8;
            if (string.IsNullOrEmpty(HoTen))
            {
                ViewBag.Message = "Vui lòng nhập họ tên.";
                return View();
            }

            if (!Regex.IsMatch(SoDienThoai ?? "", @"^\d{10}$"))
            {
                ViewBag.Message = "Số điện thoại phải gồm 10 chữ số.";
                return View();
            }

            var existing = db.customers.FirstOrDefault(c => c.phone == SoDienThoai);
            if (existing != null)
            {
                ViewBag.Message = "Số điện thoại đã được đăng ký.";
                return View();
            }

            var newCus = new customers
            {
                name = HoTen, // test cố định
                phone = SoDienThoai,
                password = SoDienThoai,
                role = "customer",
                created_at = DateTime.Now
            };

            db.customers.Add(newCus);
            db.SaveChanges();
            System.Diagnostics.Debug.WriteLine("HoTen: " + HoTen);
            foreach (byte b in System.Text.Encoding.UTF8.GetBytes(HoTen))
            {
                System.Diagnostics.Debug.Write(b.ToString("X2") + " ");
            }
            return Content("<script>window.location.href='/Auth/Login';</script>", "text/html");
        }
        [HttpGet]
        public JsonResult CheckRole(string hoten, string sdt)
        {
            var user = db.customers.FirstOrDefault(c => c.name == hoten && c.phone == sdt);
            return Json(new { role = user?.role ?? "customer" }, JsonRequestBehavior.AllowGet);
        }

        // POST: /Auth/Login
        [HttpPost]
        public ActionResult Login(string HoTen, string SoDienThoai, string password)
        {
            Request.ContentEncoding = System.Text.Encoding.UTF8;

            var user = db.customers.FirstOrDefault(c => c.name == HoTen && c.phone == SoDienThoai);
            if (user == null)
            {
                ViewBag.Message = "Thông tin không tồn tại.";
                return View();
            }

            if (user.role == "admin")
            {
                if (string.IsNullOrEmpty(password) || user.password != password)
                {
                    ViewBag.Message = "Sai mật khẩu quản trị.";
                    return View();
                }

                FormsAuthentication.SetAuthCookie(user.phone, false);
                Session["UserID"] = user.id;
                Session["UserName"] = user.name;
                Session["UserPhone"] = user.phone;
                Session["UserRole"] = user.role;

                // ✅ Redirect về trang admin từ cửa sổ chính
                return Content("<script>window.opener.location.href='/ThongKe/Index'; window.close();</script>", "text/html");
            }

            // Nếu là user thường
            FormsAuthentication.SetAuthCookie(user.phone, false);
            Session["UserID"] = user.id;
            Session["UserName"] = user.name;
            Session["UserPhone"] = user.phone;
            Session["UserRole"] = user.role;

            // ✅ Trả về script để reload parent + đóng popup
            return Content("<script>window.opener.location.reload(); window.close();</script>", "text/html");
        }
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("ViewDatSan", "Booking");
        }
    }
}