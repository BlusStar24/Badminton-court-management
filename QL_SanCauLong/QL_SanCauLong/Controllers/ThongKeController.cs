using QL_SanCauLong.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QL_SanCauLong.Controllers
{
    public class ThongKeController : Controller
    {
        QuanLySanCauLongdbEntities db = new QuanLySanCauLongdbEntities();
        // GET: ThongKe
        public ActionResult Index()
        {
            var today = DateTime.Today;
            var model = new DashboardViewModel
            {
                TongLuotDatSan = db.bookings.Count(),
                DoanhThuHomNay = db.bookings
                                  .Where(b => DbFunctions.TruncateTime(b.date) == today)
                                  .Sum(b => (decimal?)b.price) ?? 0,
                SoSanDangHoatDong = db.courts.Count(c => c.status == "available")
            };

            return View(model);
        }
    }
}