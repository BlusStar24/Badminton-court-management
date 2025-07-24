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
        private QuanLySanCauLongEntities db = new QuanLySanCauLongEntities();

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
                .Where(ct => invoiceIds.Contains(ct.invoice_id) && ct.is_paid == false && ct.item_id != 1)
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
                court_name = b.court.name
            })
            .ToList();

            return Json(new { success = true, data = bookings }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult LuuHoaDon(string name, string phone, List<int> item_ids, List<bool> item_is_paid, List<int> quantities, List<int> booking_ids, List<bool> booking_is_paid, string payment_method)
        {
            bool hasMatHang = item_ids != null && quantities != null && item_ids.Count == quantities.Count && item_ids.Count > 0;
            bool hasBooking = booking_ids != null && booking_ids.Count > 0;

            if (!hasMatHang && !hasBooking)
                return Json(new { success = false, message = "Không có dữ liệu để lưu." });

            if (hasMatHang && (item_is_paid == null || item_is_paid.Count != item_ids.Count))
            {
                item_is_paid = new List<bool>();
                for (int i = 0; i < item_ids.Count; i++) item_is_paid.Add(false);
            }

            try
            {
                name = string.IsNullOrWhiteSpace(name) ? "Vãng lai" : name.Trim();
                var customer = db.customers.FirstOrDefault(c => c.name == name);
                if (customer == null)
                {
                    customer = new customer
                    {
                        name = name,
                        phone = phone ?? "",
                        password = Guid.NewGuid().ToString("N"),
                        role = "customer",
                        created_at = DateTime.Now
                    };
                    db.customers.Add(customer);
                    db.SaveChanges();
                }

                var invoice = db.invoices.FirstOrDefault(i => i.customer_id == customer.id && i.is_paid == false);
                if (invoice == null)
                {
                    invoice = new invoice
                    {
                        customer_id = customer.id,
                        total_amount = 0,
                        note = "Bán tại quầy",
                        is_paid = false,
                        created_at = DateTime.Now,
                        payment_method = payment_method ?? "Tiền mặt"
                    };
                    db.invoices.Add(invoice);
                    db.SaveChanges();
                }

                // 1. Mặt hàng
                if (hasMatHang)
                {
                    for (int i = 0; i < item_ids.Count; i++)
                    {
                        int itemId = item_ids[i];
                        int quantity = quantities[i];
                        bool isPaid = item_is_paid[i];

                        var hang = db.mat_hang.FirstOrDefault(m => m.id == itemId);
                        if (hang == null) continue;

                        var chiTiet = db.invoice_details.FirstOrDefault(ct => ct.invoice_id == invoice.id && ct.item_id == itemId);
                        if (chiTiet != null)
                        {
                            chiTiet.quantity = quantity;
                            chiTiet.unit_price = hang.gia_ban;
                            chiTiet.total_price = quantity * hang.gia_ban;
                            chiTiet.is_paid = isPaid;
                            chiTiet.created_at = DateTime.Now;
                        }
                        else
                        {
                            db.invoice_details.Add(new invoice_details
                            {
                                invoice_id = invoice.id,
                                item_id = itemId,
                                quantity = quantity,
                                unit_price = hang.gia_ban,
                                total_price = quantity * hang.gia_ban,
                                is_paid = isPaid,
                                created_at = DateTime.Now
                            });
                        }
                    }
                    db.SaveChanges();
                }

                // 2. Booking
                if (hasBooking)
                {
                    for (int i = 0; i < booking_ids.Count; i++)
                    {
                        int id = booking_ids[i];
                        bool isPaid = booking_is_paid[i];

                        var booking = db.bookings.FirstOrDefault(b => b.id == id && b.customer_id == customer.id);
                        if (booking == null || booking.invoice_id == null) continue;

                        // Tính lại giá theo price_rules
                        int dow = (int)booking.date.DayOfWeek;
                        var rule = db.price_rules.FirstOrDefault(r =>
                            r.day_of_week == dow &&
                            r.type == booking.type &&
                            r.start_hour <= booking.start_time.Hours &&
                            r.end_hour > booking.start_time.Hours);

                        if (rule != null)
                        {
                            double duration = (booking.end_time - booking.start_time).TotalHours;
                            booking.price = (decimal)duration * (rule.price_per_hour ?? 0);
                        }

                        // Gán lại invoice_id nếu null
                        if (booking.invoice_id == null)
                            booking.invoice_id = invoice.id;

                        // Cuối cùng mới gán is_paid
                        booking.is_paid = isPaid;

                        if (rule != null)
                        {
                            double duration = (booking.end_time - booking.start_time).TotalHours;
                            booking.price = (decimal)duration * (rule.price_per_hour ?? 0);
                        }


                        var detail = db.invoice_details
                         .Where(d => d.invoice_id == booking.invoice_id && d.item_id == 1)
                         .OrderByDescending(d => d.created_at)
                         .FirstOrDefault();


                        if (detail != null)
                        {
                            detail.unit_price = booking.price;
                            detail.total_price = booking.price;
                            detail.is_paid = isPaid;
                        }
                        else
                        {
                            db.invoice_details.Add(new invoice_details
                            {
                                invoice_id = booking.invoice_id.Value,
                                item_id = 1,
                                quantity = 1,
                                unit_price = booking.price,
                                total_price = booking.price,
                                is_paid = isPaid,
                                created_at = DateTime.Now
                            });
                        }
                    }
                    db.SaveChanges();
                }

                // 3. Lấy danh sách hóa đơn liên quan
                List<int> relatedInvoiceIds = new List<int>();

                if (booking_ids != null && booking_ids.Count > 0)
                {
                    relatedInvoiceIds = booking_ids
                        .Select(id => db.bookings.Where(b => b.id == id).Select(b => b.invoice_id).FirstOrDefault())
                        .Where(id => id != null)
                        .Select(id => id.Value)
                        .Distinct()
                        .ToList();

                    if (!relatedInvoiceIds.Contains(invoice.id)) relatedInvoiceIds.Add(invoice.id);
                }
                else
                {
                    relatedInvoiceIds.Add(invoice.id);
                }

                foreach (var invId in relatedInvoiceIds)
                {
                    var inv = db.invoices.FirstOrDefault(i => i.id == invId);
                    if (inv == null) continue;

                    var allDetails = db.invoice_details.Where(d => d.invoice_id == inv.id).ToList();
                    var allBookings = db.bookings.Where(b => b.invoice_id == inv.id).ToList();

                    // ✅ Tính đúng tổng hóa đơn (bao gồm cả chưa thanh toán)
                    inv.total_amount = allDetails.Sum(d => d.total_price ?? (d.quantity * d.unit_price));

                    bool allItemsPaid = allDetails.All(d => d.is_paid == true);
                    bool allBookingsPaid = allBookings.All(b => b.is_paid == true);
                    inv.is_paid = allItemsPaid && allBookingsPaid;

                    if (allItemsPaid)
                        foreach (var d in allDetails.Where(d => d.is_paid != true)) d.is_paid = true;

                    if (allBookingsPaid)
                        foreach (var b in allBookings.Where(b => b.is_paid != true)) b.is_paid = true;
                }


                db.SaveChanges();
                return Json(new { success = true, message = "Cập nhật hóa đơn thành công!" });
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex.InnerException != null) msg += " | Inner: " + ex.InnerException.Message;
                if (ex.InnerException?.InnerException != null) msg += " | SQL: " + ex.InnerException.InnerException.Message;

                return Json(new { success = false, message = msg });
            }
        }
    }
}