using QL_SanCauLong.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace QL_SanCauLong.Controllers
{
    public class QuanLyBanHangController : Controller
    {
        private QuanLySanCauLongEntities3 db = new QuanLySanCauLongEntities3();

        [Authorize]
        public ActionResult BanHang()
        {
            var danhSachMatHang = db.mat_hang.ToList();
            var danhSachKhachHang = db.customers.Select(c => c.name).Distinct().ToList();
            ViewBag.DanhSachKhachHang = danhSachKhachHang;
            return View(danhSachMatHang);
        }

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

            var result = db.invoice_details
                .Where(ct => ct.invoice.customer_id == customer.id
                          && ct.invoice.payment_method == "Nợ"
                          && ct.item_id != 0
                          && ct.item_id != 1
                          && ct.is_paid == false)
                .Select(ct => new
                {
                    id = ct.id,
                    item_id = ct.item_id,
                    name = ct.mat_hang.ten_hang ?? "Không xác định",
                    price = ct.unit_price * ct.quantity,
                    quantity = ct.quantity
                }).ToList();

            System.Diagnostics.Debug.WriteLine($"GetSanPhamChuaThanhToan for {name}: {result.Count} items returned");
            foreach (var item in result)
            {
                System.Diagnostics.Debug.WriteLine($"Item: id={item.id}, item_id={item.item_id}, name={item.name}, price={item.price}, quantity={item.quantity}");
            }

            return Json(new { success = true, data = result }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetLichChuaThanhToan(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Tên khách hàng trống." }, JsonRequestBehavior.AllowGet);

            var customer = db.customers.FirstOrDefault(c => c.name == name);
            if (customer == null)
                return Json(new { success = true, data = new List<object>() }, JsonRequestBehavior.AllowGet);

            var bookings = db.bookings
                .Where(b => b.customer_id == customer.id
                         && b.invoice_id != null
                         && b.invoice.payment_method == "Nợ"
                         && b.is_paid == false)
                .AsEnumerable()
                .Select(b => new
                {
                    b.id,
                    date = b.date.ToString("yyyy-MM-dd"),
                    start_time = b.start_time.ToString(@"hh\:mm"),
                    end_time = b.end_time.ToString(@"hh\:mm"),
                    b.price,
                    b.type,
                    court_name = b.court.name ?? "Không xác định"
                })
                .ToList();

            System.Diagnostics.Debug.WriteLine($"GetLichChuaThanhToan for {name}: {bookings.Count} bookings returned");
            foreach (var item in bookings)
            {
                System.Diagnostics.Debug.WriteLine($"Booking: id={item.id}, date={item.date}, price={item.price}, type={item.type}");
            }

            return Json(new { success = true, data = bookings }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult XoaSanPhamChuaThanhToan(int item_id, string name, int detail_id)
        {
            try
            {
                var customer = db.customers.FirstOrDefault(c => c.name == name);
                if (customer == null)
                    return Json(new { success = false, message = "Khách hàng không tồn tại." });

                var invoiceIds = db.invoices
                    .Where(inv => inv.customer_id == customer.id && inv.is_paid == false)
                    .Select(inv => inv.id)
                    .ToList();

                var details = db.invoice_details
                    .Where(ct => invoiceIds.Contains(ct.invoice_id) && ct.item_id == item_id && ct.id == detail_id && ct.is_paid == false)
                    .ToList();

                if (!details.Any())
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm chưa thanh toán." });

                db.invoice_details.RemoveRange(details);

                foreach (var invoiceId in invoiceIds)
                {
                    var stillHasDetail = db.invoice_details.Any(ct => ct.invoice_id == invoiceId);
                    if (!stillHasDetail)
                    {
                        var inv = db.invoices.Find(invoiceId);
                        if (inv != null)
                            db.invoices.Remove(inv);
                    }
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Xóa sản phẩm thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi xóa sản phẩm: {ex.Message}" });
            }
        }

        [HttpPost]
        public ActionResult XoaLichChuaThanhToan(int booking_id, string name)
        {
            try
            {
                var customer = db.customers.FirstOrDefault(c => c.name == name);
                if (customer == null)
                    return Json(new { success = false, message = "Khách hàng không tồn tại." });

                var booking = db.bookings.FirstOrDefault(b => b.id == booking_id && b.customer_id == customer.id && b.is_paid == false);
                if (booking == null)
                    return Json(new { success = false, message = "Không tìm thấy lịch chưa thanh toán." });

                var invoiceId = booking.invoice_id;
                booking.invoice_id = null;
                booking.is_paid = false;

                var detail = db.invoice_details.FirstOrDefault(d => d.booking_id == booking_id && d.invoice_id == invoiceId);
                if (detail != null)
                {
                    db.invoice_details.Remove(detail);
                }

                var stillHasDetail = db.invoice_details.Any(ct => ct.invoice_id == invoiceId);
                if (!stillHasDetail)
                {
                    var inv = db.invoices.Find(invoiceId);
                    if (inv != null)
                        db.invoices.Remove(inv);
                }

                db.SaveChanges();
                return Json(new { success = true, message = "Xóa lịch thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi xóa lịch: {ex.Message}" });
            }
        }

        [HttpPost]
        public ActionResult LuuHoaDon(string name, string phone, List<int> item_ids, List<int> item_detail_ids, List<int> quantities, List<bool> item_is_paid, List<int> booking_ids, List<bool> booking_is_paid, string payment_method)
        {
            System.Diagnostics.Debug.WriteLine($"Received: name={name}, item_ids={string.Join(", ", item_ids ?? new List<int>())}," +
                $" item_detail_ids={string.Join(", ", item_detail_ids ?? new List<int>())}, booking_ids={string.Join(", ", booking_ids ?? new List<int>())}," +
                $" payment_method={payment_method}");

            if ((item_ids == null || item_ids.Count == 0) && (booking_ids == null || booking_ids.Count == 0))
                return Json(new { success = false, message = "Không có dữ liệu để lưu." });

            bool isPaid = payment_method != "Nợ";
            item_is_paid = item_ids?.Select(_ => isPaid).ToList() ?? new List<bool>();
            booking_is_paid = booking_ids?.Select(_ => isPaid).ToList() ?? new List<bool>();

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
                        is_paid = isPaid,
                        created_at = DateTime.Now,
                        payment_method = payment_method ?? "Tiền mặt"
                    };
                    db.invoices.Add(invoice);
                    db.SaveChanges();
                }
                else
                {
                    invoice.payment_method = payment_method;
                    invoice.is_paid = isPaid;
                }

                if (item_ids != null)
                {
                    for (int i = 0; i < item_ids.Count; i++)
                    {
                        int itemId = item_ids[i];
                        int quantity = quantities[i];
                        bool isPaidStatus = item_is_paid[i];
                        int detailId = item_detail_ids[i];

                        var chiTiet = db.invoice_details.FirstOrDefault(ct => ct.id == detailId && ct.invoice_id == invoice.id);
                        if (chiTiet != null)
                        {
                            chiTiet.quantity = quantity;
                            chiTiet.is_paid = isPaidStatus;
                            chiTiet.created_at = DateTime.Now;
                            continue;
                        }

                        chiTiet = db.invoice_details.FirstOrDefault(ct => ct.invoice_id == invoice.id && ct.item_id == itemId && ct.booking_id == null);
                        if (chiTiet != null)
                        {
                            chiTiet.quantity += quantity;
                            chiTiet.is_paid = isPaidStatus;
                            chiTiet.created_at = DateTime.Now;
                            continue;
                        }

                        var hang = db.mat_hang.FirstOrDefault(m => m.id == itemId);
                        if (hang == null) continue;

                        db.invoice_details.Add(new invoice_details
                        {
                            invoice_id = invoice.id,
                            item_id = itemId,
                            quantity = quantity,
                            unit_price = hang.gia_ban,
                            is_paid = isPaidStatus,
                            created_at = DateTime.Now
                        });
                    }
                }

                if (booking_ids != null)
                {
                    for (int i = 0; i < booking_ids.Count; i++)
                    {
                        int bookingId = booking_ids[i];
                        bool isPaidStatus = booking_is_paid[i];
                        var booking = db.bookings.FirstOrDefault(b => b.id == bookingId);
                        if (booking == null) continue;
                        booking.customer_id = customer.id; 
                        int dow = (int)booking.date.DayOfWeek;
                        var rule = db.price_rules.FirstOrDefault(r =>
                            r.day_of_week == dow &&
                            r.type == booking.type &&
                            r.start_hour <= booking.start_time.Hours &&
                            r.end_hour > booking.start_time.Hours);

                        decimal bookingPrice = 0;
                        if (rule != null)
                        {
                            double duration = (booking.end_time - booking.start_time).TotalHours;
                            bookingPrice = (decimal)duration * (rule.price_per_hour ?? 0);
                            booking.price = bookingPrice;
                            booking.payment_method = payment_method;
                        }
                        else
                        {
                            continue;
                        }

                        if (booking.invoice_id == null)
                        {
                            booking.invoice_id = invoice.id;
                        }
                        else if (booking.invoice_id != invoice.id)
                        {
                            continue;
                        }

                        booking.is_paid = isPaidStatus;

                        var detail = db.invoice_details.FirstOrDefault(d =>
                            d.invoice_id == invoice.id &&
                            d.booking_id == booking.id);

                        if (detail != null)
                        {
                            detail.unit_price = booking.price;
                            detail.is_paid = isPaidStatus;
                            detail.created_at = DateTime.Now;
                        }
                        else
                        {
                            db.invoice_details.Add(new invoice_details
                            {
                                invoice_id = invoice.id,
                                item_id = 1,
                                quantity = 1,
                                unit_price = booking.price,
                                is_paid = isPaidStatus,
                                booking_id = booking.id,
                                created_at = DateTime.Now
                            });
                        }
                    }
                }

                db.SaveChanges();

                var details = db.invoice_details.Where(d => d.invoice_id == invoice.id).ToList();
                if (!details.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"Hóa đơn ID {invoice.id} không có chi tiết hóa đơn, xóa hóa đơn.");
                    db.invoices.Remove(invoice);
                }
                else
                {
                    invoice.total_amount = details.Sum(d => d.unit_price * d.quantity);
                    invoice.is_paid = isPaid;
                    System.Diagnostics.Debug.WriteLine($"Hóa đơn ID {invoice.id}: total_amount = {invoice.total_amount}, is_paid = {invoice.is_paid}");
                }

                db.SaveChanges();

                return Json(new { success = true, message = isPaid ? "Thanh toán thành công!" : "Ghi nợ thành công!" });
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                if (ex.InnerException != null) msg += " | Inner: " + ex.InnerException.Message;
                System.Diagnostics.Debug.WriteLine($"Lỗi khi lưu hóa đơn: {msg}");
                return Json(new { success = false, message = $"Lỗi khi lưu hóa đơn: {msg}" });
            }
        }
    }
}