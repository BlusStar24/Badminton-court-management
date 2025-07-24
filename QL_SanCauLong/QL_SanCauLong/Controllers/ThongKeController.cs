using QL_SanCauLong.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace QL_SanCauLong.Controllers
{
    public class ThongKeController : Controller
    {
        QuanLySanCauLongEntities3 db = new QuanLySanCauLongEntities3();

        [Authorize]
        public ActionResult Index(DateTime? ngayLoc = null, int? thangLoc = null, int? namLoc = null, int? sanPhamId = null)
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var ngayLocThucTe = ngayLoc ?? today;
            var thangThucTe = thangLoc ?? today.Month;
            var namThucTe = namLoc ?? today.Year;
            var thangBatDau = new DateTime(namThucTe, thangThucTe, 1);
            var thangKetThuc = thangBatDau.AddMonths(1);

            var hoaDons = db.invoices.Where(h => h.is_paid == true && h.created_at.HasValue);

            ViewBag.DanhSachMatHang = db.mat_hang.ToList();
            ViewBag.SanPhamId = sanPhamId;

            var matHangNgay = db.invoice_details
                .Include("mat_hang")
                .Where(d => d.is_paid == true && DbFunctions.TruncateTime(d.created_at) == ngayLocThucTe.Date)
                .Where(d => d.mat_hang != null)
                .Where(d => sanPhamId == null || d.mat_hang.id == sanPhamId)
                .GroupBy(g => new
                {
                    g.mat_hang.ten_hang,
                    g.mat_hang.hinh_anh,
                    g.mat_hang.id,
                    g.unit_price
                })
                .Select(g => new MatHangThongKe
                {
                    TenHang = g.Key.ten_hang + (g.Key.id == 1 ? " (" + g.Key.unit_price + "đ)" : ""),
                    HinhAnh = g.Key.hinh_anh,
                    SoLuong = g.Sum(x => x.quantity),
                    TongTien = g.Sum(x => x.quantity * x.unit_price)
                }).ToList();

            var matHangThang = db.invoice_details
                .Include("mat_hang")
                .Where(d => d.is_paid == true && d.created_at >= thangBatDau && d.created_at < thangKetThuc)
                .Where(d => d.mat_hang != null)
                .Where(d => sanPhamId == null || d.mat_hang.id == sanPhamId)
                .GroupBy(g => new
                {
                    g.mat_hang.ten_hang,
                    g.mat_hang.hinh_anh,
                    g.mat_hang.id,
                    g.unit_price
                })
                .Select(g => new MatHangThongKe
                {
                    TenHang = g.Key.ten_hang + (g.Key.id == 1 ? " (" + g.Key.unit_price + "đ)" : ""),
                    HinhAnh = g.Key.hinh_anh,
                    SoLuong = g.Sum(x => x.quantity),
                    TongTien = g.Sum(x => x.quantity * x.unit_price)
                }).ToList();

            // ✅ Gọi stored procedure đúng cách
            var congNoKhachHang = db.Database
                .SqlQuery<CongNoKhachHang>("EXEC sp_TinhNoChiTietKhachHang")
                .ToList();


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

                MatHangBanTrongNgay = matHangNgay,
                MatHangBanTrongThang = matHangThang,

                CongNoKhachHang = congNoKhachHang,
                TongCongNoTatCaKhach = congNoKhachHang.Sum(x => (decimal?)x.tong_no ?? 0)
            };

            return View(viewModel);
        }
    }
}
