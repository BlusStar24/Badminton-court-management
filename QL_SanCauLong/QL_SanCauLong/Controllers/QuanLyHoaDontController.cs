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
        QuanLySanCauLongEntities3 db = new QuanLySanCauLongEntities3();

        // GET: QuanLyHoaDont
        [Authorize]
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult DanhSachHoaDon(bool? daThanhToan = null, string ngay = null, string thang = null)
        {
            var ds = db.invoices.AsQueryable();

            if (daThanhToan.HasValue)
                ds = ds.Where(hd => hd.is_paid == daThanhToan.Value);

            if (!string.IsNullOrEmpty(ngay) && DateTime.TryParseExact(ngay, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var d))
                ds = ds.Where(hd => hd.created_at.HasValue &&
                                    hd.created_at.Value.Year == d.Year &&
                                    hd.created_at.Value.Month == d.Month &&
                                    hd.created_at.Value.Day == d.Day);

            if (!string.IsNullOrEmpty(thang) && DateTime.TryParse(thang + "-01", out var mt))
                ds = ds.Where(hd => hd.created_at.HasValue &&
                                    hd.created_at.Value.Year == mt.Year &&
                                    hd.created_at.Value.Month == mt.Month);

            // 👉 Thêm dòng này để sắp xếp theo id giảm dần
            ds = ds.OrderByDescending(hd => hd.id);

            // SELECT chỉ các trường cần, không dùng ToString() trong SQL
            var rawData = ds.Select(hd => new
            {
                hd.id,
                hd.customer_id,
                tenKhach = hd.customer.name,
                hd.total_amount,
                hd.note,
                hd.created_at,
                hd.is_paid,
                hd.payment_method,
                hd.payment_image
            }).ToList(); // Lúc này đã filter xong mới load lên

            // Sau đó format ngày ở LINQ to Object
            var result = rawData.Select(hd => new
            {
                hd.id,
                hd.customer_id,
                hd.tenKhach,
                hd.total_amount,
                hd.note,
                created_at = hd.created_at.HasValue
                    ? hd.created_at.Value.ToString("yyyy-MM-ddTHH:mm:ss")
                    : null,
                hd.is_paid,
                hd.payment_method,
                hd.payment_image
            });

            return Json(new { success = true, data = result }, JsonRequestBehavior.AllowGet);
        }
        [Authorize]
        [HttpGet]
        public ActionResult ChiTietHoaDon(int id)
        {
            try
            {
                // Bước 1: Truy vấn dữ liệu gốc từ DB
                var hoaDonRaw = (from h in db.invoices
                                 where h.id == id
                                 select new
                                 {
                                     h.id,
                                     h.customer_id,
                                     customer_name = h.customer.name,
                                     h.total_amount,
                                     h.note,
                                     h.created_at,
                                     h.is_paid,
                                     h.payment_method,
                                     h.payment_image
                                 }).FirstOrDefault();

                if (hoaDonRaw == null)
                    return Json(new { success = false, message = "Không tìm thấy hóa đơn." }, JsonRequestBehavior.AllowGet);

                // Bước 2: Format lại khi đã là LINQ to Objects
                var hoaDon = new
                {
                    hoaDonRaw.id,
                    hoaDonRaw.customer_id,
                    tenKhach = string.IsNullOrEmpty(hoaDonRaw.customer_name) ? "(Không có khách)" : hoaDonRaw.customer_name,
                    hoaDonRaw.total_amount,
                    hoaDonRaw.note,
                    created_at = hoaDonRaw.created_at?.ToString("HH:mm:ss d/M/yyyy"),
                    hoaDonRaw.is_paid,
                    hoaDonRaw.payment_method,
                    hoaDonRaw.payment_image
                };

                // Chi tiết hóa đơn
                var chiTietRaw = (from ct in db.invoice_details
                                  join mh in db.mat_hang on ct.item_id equals mh.id into temp
                                  from mh in temp.DefaultIfEmpty()
                                  where ct.invoice_id == id
                                  select new
                                  {
                                      ct.id,
                                      ct.item_id,
                                      matHangTen = mh.ten_hang,
                                      ct.quantity,
                                      ct.unit_price,
                                      ct.total_price,
                                      ct.created_at,
                                      ct.is_paid
                                  }).ToList();

                var chiTiet = chiTietRaw.Select(ct => new
                {
                    ct.id,
                    ct.item_id,
                    tenHang = string.IsNullOrEmpty(ct.matHangTen) ? "(Đã xóa mặt hàng)" : ct.matHangTen,
                    ct.quantity,
                    ct.unit_price,
                    ct.total_price,
                    created_at = ct.created_at?.ToString("HH:mm:ss d/M/yyyy"),
                    ct.is_paid
                }).ToList();

                return Json(new
                {
                    success = true,
                    hoaDon,
                    chiTiet
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Lỗi xử lý: " + ex.Message,
                    inner = ex.InnerException?.Message,
                    stack = ex.StackTrace
                }, JsonRequestBehavior.AllowGet);
            }
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