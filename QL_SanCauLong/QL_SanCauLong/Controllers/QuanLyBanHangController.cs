using QL_SanCauLong.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QL_SanCauLong.Controllers
{
    public class QuanLyBanHangController : Controller
    {
        private QuanLySanCauLongdbEntities db = new QuanLySanCauLongdbEntities();

        // GET: BanHang
        public ActionResult BanHang()
        {
            var danhSachMatHang = db.mat_hang.ToList();
            var danhSachKhachHang = db.customers.Select(c => c.name).Distinct().ToList();
            ViewBag.DanhSachKhachHang = danhSachKhachHang;
            return View(danhSachMatHang);
        }

        // GET: BanHang/TaoMatHang
        [HttpPost]
        public ActionResult TaoMatHang(string ten_hang, string don_vi_chinh, string don_vi_quy_doi,
          int? so_luong_quy_doi, decimal gia_nhap, decimal gia_ban, string loai, string don_vi, string hinh_anh_url)
        {
            if (string.IsNullOrEmpty(ten_hang) || string.IsNullOrEmpty(don_vi_chinh))
            {
                return Json(new { success = false, message = "Tên hàng và đơn vị chính không được để trống." });
            }

            var matHang = new mat_hang
            {
                ten_hang = ten_hang,
                don_vi_chinh = don_vi_chinh,
                don_vi_quy_doi = don_vi_quy_doi,
                so_luong_quy_doi = so_luong_quy_doi ?? 1,
                gia_nhap = gia_nhap,
                gia_ban = gia_ban,
                loai = loai,
                don_vi = don_vi ?? don_vi_chinh,
                hinh_anh = hinh_anh_url,
                created_at = DateTime.Now
            };

            db.mat_hang.Add(matHang);
            db.SaveChanges();

            return Json(new { success = true, message = "Thêm mặt hàng thành công!" });
        }
        public ActionResult GetSanPhamChuaThanhToan(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Tên khách hàng trống." }, JsonRequestBehavior.AllowGet);

            var customer = db.customers.FirstOrDefault(c => c.name == name);
            if (customer == null)
                return Json(new { success = true, data = new List<object>() }, JsonRequestBehavior.AllowGet);

            // Load trước danh sách invoice id của khách hàng
            var invoiceIds = db.invoices
                .Where(inv => inv.customer_id == customer.id)
                .Select(inv => inv.id)
                .ToList();

            var result = db.invoice_details
                .Where(ct => invoiceIds.Contains(ct.invoice_id) && ct.is_paid == false)
                .Select(ct => new
                {
                    item_id = ct.item_id,
                    name = ct.mat_hang.ten_hang,
                    price = ct.unit_price
                }).ToList();

            return Json(new { success = true, data = result }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetLichChuaThanhToan(string name)
        {
            var customer = db.customers.FirstOrDefault(c => c.name == name);
            if (customer == null)
            {
                return Json(new { success = false, data = new List<object>() }, JsonRequestBehavior.AllowGet);
            }

            var bookings = db.bookings
            .Where(b => b.customer_id == customer.id && b.is_paid == false)
            .AsEnumerable() 
            .Select(b => new
            {
                b.id,
                date = b.date.ToString("yyyy-MM-dd"),
                start_time = b.start_time.ToString(@"hh\:mm"),
                end_time = b.end_time.ToString(@"hh\:mm"),
                b.price,
                b.type,
                court_name = b.courts.name
            })
            .ToList();

            return Json(new { success = true, data = bookings }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult LuuHoaDon(string name, string phone, List<int> item_ids, List<bool> item_is_paid, List<int> quantities, List<int> booking_ids, List<bool> booking_is_paid, string payment_method)
        {
            // Kiểm tra dữ liệu sản phẩm nếu có
            bool validMatHang = item_ids != null && quantities != null && item_ids.Count == quantities.Count;
            bool validBooking = booking_ids != null && booking_ids.Any();

            if (!validMatHang && !validBooking)
                return Json(new { success = false, message = "Không có sản phẩm hay lịch sân nào để thanh toán." });

            try
            {
                name = string.IsNullOrWhiteSpace(name) ? "Vãng lai" : name.Trim();
                var customer = db.customers.FirstOrDefault(c => c.name == name);

                if (customer == null)
                {
                    customer = new customers
                    {
                        name = name,
                        phone = "",
                        password = Guid.NewGuid().ToString("N"),
                        role = "customer",
                        created_at = DateTime.Now
                    };
                    db.customers.Add(customer);
                    db.SaveChanges();
                }

                decimal tongTien = 0;

                // Tạo hóa đơn
                var invoice = new invoice
                {
                    customer_id = customer.id,
                    total_amount = 0,
                    note = "Bán tại quầy",
                    is_paid = true,
                    created_at = DateTime.Now,
                    payment_method = payment_method ?? "Tiền mặt"
                };
                db.invoices.Add(invoice);
                db.SaveChanges();

                // Thêm mặt hàng
                if (validMatHang)
                    for (int i = 0; i < item_ids.Count; i++)
                    {
                        int itemId = item_ids[i];
                        int quantity = quantities[i];
                        bool isPaid = item_is_paid[i];

                        var hang = db.mat_hang.FirstOrDefault(m => m.id == itemId);
                        if (hang == null) continue;

                        db.invoice_details.Add(new invoice_details
                        {
                            invoice_id = invoice.id,
                            item_id = itemId,
                            quantity = quantity,
                            unit_price = hang.gia_ban,
                            is_paid = isPaid,
                            created_at = DateTime.Now
                        });

                        if (isPaid)
                            tongTien += hang.gia_ban * quantity;
                    }

                // Cập nhật trạng thái lịch sân
                if (validBooking)
                {
                    for (int i = 0; i < booking_ids.Count; i++)
                    {
                        int id = booking_ids[i];
                        bool isPaid = booking_is_paid[i];

                        var booking = db.bookings.FirstOrDefault(b => b.id == id && b.customer_id == customer.id);
                        if (booking != null)
                        {
                            booking.is_paid = isPaid;
                            if (isPaid)
                                tongTien += booking.price;
                        }
                    }
                }

                invoice.total_amount = tongTien;
                db.SaveChanges();

                return Json(new { success = true, message = "Lưu hóa đơn thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }

        }
    }
}