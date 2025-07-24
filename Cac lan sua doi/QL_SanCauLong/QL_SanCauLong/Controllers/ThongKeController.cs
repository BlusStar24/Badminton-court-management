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
        QuanLySanCauLongEntities db = new QuanLySanCauLongEntities();
        // GET: ThongKe
        public ActionResult Index()
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var hoaDons = db.invoices.Where(h => h.is_paid == true && h.created_at.HasValue);

            // Gọi stored procedure đã import sẵn
            var congNoKhachHang = db.sp_TinhNoChiTietKhachHang().ToList();

            var viewModel = new DashboardViewModel
            {
                TongLuotDatSan = db.bookings.Count(),
                SoSanDangHoatDong = db.courts.Count(c => c.status == "active"),

                DoanhThuNgay_TienMat = hoaDons.Where(h =>
                    DbFunctions.TruncateTime(h.created_at) == today &&
                    h.payment_method == "Tiền mặt").Sum(h => (decimal?)h.total_amount) ?? 0,

                DoanhThuNgay_ChuyenKhoan = hoaDons.Where(h =>
                    DbFunctions.TruncateTime(h.created_at) == today &&
                    h.payment_method == "Chuyển khoản").Sum(h => (decimal?)h.total_amount) ?? 0,

                DoanhThuThang_TienMat = hoaDons.Where(h =>
                    h.created_at >= monthStart &&
                    h.payment_method == "Tiền mặt").Sum(h => (decimal?)h.total_amount) ?? 0,

                DoanhThuThang_ChuyenKhoan = hoaDons.Where(h =>
                    h.created_at >= monthStart &&
                    h.payment_method == "Chuyển khoản").Sum(h => (decimal?)h.total_amount) ?? 0,

                CongNoKhachHang = congNoKhachHang
            };

            return View(viewModel);
        }

    }
}