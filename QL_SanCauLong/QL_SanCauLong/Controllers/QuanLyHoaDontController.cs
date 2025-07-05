using QL_SanCauLong.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QL_SanCauLong.Controllers
{
    public class QuanLyHoaDontController : Controller
    {
        QuanLySanCauLongdbEntities db = new QuanLySanCauLongdbEntities();
        // GET: QuanLyHoaDont
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult DanhSachHoaDon(bool? daThanhToan = null, string ngay = null, string thang = null)
        {
            var ds = db.invoices.AsQueryable();

            if (daThanhToan.HasValue)
                ds = ds.Where(hd => hd.is_paid == daThanhToan.Value);

            if (!string.IsNullOrEmpty(ngay) && DateTime.TryParse(ngay, out var d))
            {
                int y = d.Year, m = d.Month, day = d.Day;
                ds = ds.Where(hd => hd.created_at.HasValue &&
                                    hd.created_at.Value.Year == y &&
                                    hd.created_at.Value.Month == m &&
                                    hd.created_at.Value.Day == day);
            }

            if (!string.IsNullOrEmpty(thang) && DateTime.TryParse(thang + "-01", out var mt))
            {
                int y = mt.Year, m = mt.Month;
                ds = ds.Where(hd => hd.created_at.HasValue &&
                                    hd.created_at.Value.Year == y &&
                                    hd.created_at.Value.Month == m);
            }

            var result = ds.Select(hd => new
            {
                hd.id,
                hd.customer_id,
                tenKhach = hd.customer.name,
                hd.total_amount,
                hd.note,
                created_at = hd.created_at,
                hd.is_paid,
                hd.payment_method,
                hd.payment_image
            }).ToList()
            .Select(hd => new
            {
                hd.id,
                hd.customer_id,
                hd.tenKhach,
                hd.total_amount,
                hd.note,
                created_at = hd.created_at?.ToString("HH:mm:ss d/M/yyyy"),
                hd.is_paid,
                hd.payment_method,
                hd.payment_image
            });

            return Json(new { success = true, data = result }, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public ActionResult ChiTietHoaDon(int id)
        {
            var hoaDon = db.invoices
             .Where(h => h.id == id)
             .AsEnumerable() // chuyển sang LINQ to Objects để dùng ToString
             .Select(h => new
             {
                 h.id,
                 h.customer_id,
                 tenKhach = h.customer.name,
                 h.total_amount,
                 h.note,
                 created_at = h.created_at.HasValue
                     ? h.created_at.Value.ToString("HH:mm:ss d/M/yyyy")
                     : null,
                 h.is_paid,
                 h.payment_method,
                 h.payment_image
             }).FirstOrDefault();

            if (hoaDon == null)
                return Json(new { success = false, message = "Không tìm thấy hóa đơn." }, JsonRequestBehavior.AllowGet);

            var chiTiet = db.invoice_details
                .Where(ct => ct.invoice_id == id)
                .AsEnumerable()
                .Select(ct => new
                {
                    ct.id,
                    ct.item_id,
                    tenHang = ct.mat_hang.ten_hang,
                    ct.quantity,
                    ct.unit_price,
                    ct.total_price,
                    created_at = ct.created_at.HasValue
                        ? ct.created_at.Value.ToString("HH:mm:ss d/M/yyyy")
                        : null,
                    ct.is_paid
                }).ToList();

            return Json(new
            {
                success = true,
                hoaDon,
                chiTiet
            }, JsonRequestBehavior.AllowGet);
        }

        public class ChiTietTrangThaiDTO
        {
            public int id { get; set; }
            public bool is_paid { get; set; }
        }
        [HttpPost]
        public ActionResult SuaHoaDon(int id, string note, bool is_paid, string payment_method, string payment_image, List<ChiTietTrangThaiDTO> chiTietTrangThai)
        {
            var hd = db.invoices.FirstOrDefault(h => h.id == id);
            if (hd == null)
                return Json(new { success = false, message = "Không tìm thấy hóa đơn." });

            hd.note = note;
            hd.is_paid = is_paid;
            hd.payment_method = payment_method;
            hd.payment_image = payment_image;

            // cập nhật trạng thái chi tiết
            if (chiTietTrangThai != null)
            {
                foreach (var ct in chiTietTrangThai)
                {
                    var chiTiet = db.invoice_details.FirstOrDefault(x => x.id == ct.id);
                    if (chiTiet != null)
                        chiTiet.is_paid = ct.is_paid;
                }
            }

            db.SaveChanges();
            return Json(new { success = true, message = "Cập nhật hóa đơn thành công." });
        }

        [HttpPost]
        public ActionResult XoaHoaDon(int id)
        {
            var hd = db.invoices.FirstOrDefault(h => h.id == id);
            if (hd == null)
                return Json(new { success = false, message = "Không tìm thấy hóa đơn." });

            var chiTiet = db.invoice_details.Where(ct => ct.invoice_id == id).ToList();
            db.invoice_details.RemoveRange(chiTiet);
            db.invoices.Remove(hd);
            db.SaveChanges();

            return Json(new { success = true, message = "Đã xóa hóa đơn và chi tiết liên quan." });
        }

    }
}