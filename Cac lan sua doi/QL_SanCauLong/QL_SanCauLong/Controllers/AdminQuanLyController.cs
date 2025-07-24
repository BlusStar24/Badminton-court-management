using QL_SanCauLong.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QL_SanCauLong.Controllers
{
    public class AdminQuanLyController : Controller
    {
        // GET: AdminQuanLy
        QuanLySanCauLongEntities db = new QuanLySanCauLongEntities();

        // GET: Admin
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Booking()
        {
            return View(); // sẽ gọi Views/Admin/Booking.cshtml
        }
        // Lấy danh sách sân
        public JsonResult GetBookings(int month, int year)
        {
            var rawList = db.bookings
                .Where(b => b.date.Month == month && b.date.Year == year)
                .AsNoTracking()
                .ToList();

            System.Diagnostics.Debug.WriteLine($"📦 Tổng booking trong {month}/{year}: {rawList.Count}");
            foreach (var b in rawList)
            {
                System.Diagnostics.Debug.WriteLine($"🔹 bookingId={b.id}, court={b.court_id}, date={b.date:yyyy-MM-dd}, start={b.start_time}, end={b.end_time}");
            }

            var list = rawList.Select(b => new
            {
                b.id,
                b.court_id,
                date = b.date.ToString("yyyy-MM-dd"),
                start_time = b.start_time.TotalHours,
                end_time = b.end_time.TotalHours,
                b.price,
                b.type,
                b.is_paid,
                customer_name = b.customer?.name ?? "Ẩn danh",
                customer_phone = b.customer?.phone ?? ""
            }).ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult DeleteBookings(List<int> ids)
        {
            try
            {
                if (ids == null || ids.Count == 0)
                {
                    return Json(new { success = false, message = "Không có lịch nào được chọn để xóa.", receivedIds = ids });
                }

                System.Diagnostics.Debug.WriteLine("📝 Received IDs to delete: " + string.Join(", ", ids));

                var toDelete = db.bookings.Where(b => ids.Contains(b.id)).ToList();

                if (toDelete.Count == 0)
                {
                    return Json(new { success = false, message = "Không tìm thấy lịch đặt tương ứng.", receivedIds = ids });
                }

                var deletedIds = toDelete.Select(b => b.id).ToList();

                db.bookings.RemoveRange(toDelete);
                db.SaveChanges();

                System.Diagnostics.Debug.WriteLine("✅ Deleted bookings with IDs: " + string.Join(", ", deletedIds));

                // ✅ Kiểm tra lại DB xem có còn không
                using (var freshDb = new QuanLySanCauLongEntities())
                {
                    var stillExists = freshDb.bookings
                                             .Where(b => deletedIds.Contains(b.id))
                                             .Select(b => b.id)
                                             .ToList();

                    if (stillExists.Any())
                    {
                        System.Diagnostics.Debug.WriteLine("❌ Những booking vẫn còn trong DB sau khi xóa: " + string.Join(", ", stillExists));
                        return Json(new
                        {
                            success = false,
                            message = "Một số lịch vẫn chưa bị xóa khỏi DB.",
                            remainingIds = stillExists
                        });
                    }

                    System.Diagnostics.Debug.WriteLine("✅ DB xác nhận các booking đã được xóa sạch.");
                }

                return Json(new { success = true, deletedIds = deletedIds });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("❌ Exception during deletion: " + ex.Message);
                return Json(new { success = false, message = ex.Message, stackTrace = ex.StackTrace });
            }
        }
        //===========================================================================================================================
        [HttpPost]
        public JsonResult CapNhatGiaTheoKhung(string date, int court_id, string start_time, string end_time, decimal new_price)
        {
            try
            {
                var parsedDate = DateTime.Parse(date);
                var start = TimeSpan.Parse(start_time);
                var end = TimeSpan.Parse(end_time);

                var slots = db.bookings.Where(b =>
                    b.date == parsedDate &&
                    b.court_id == court_id &&
                    b.start_time >= start &&
                    b.start_time < end
                ).ToList();

                if (!slots.Any())
                    return Json(new { success = false, message = "Không tìm thấy lịch đặt phù hợp." });

                var pricePerSlot = new_price / slots.Count;

                foreach (var b in slots)
                {
                    b.price = Math.Round(pricePerSlot, 0);
                }

                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Đã cập nhật tổng {new_price:N0}đ cho {slots.Count} ô, mỗi ô {Math.Round(pricePerSlot, 0):N0}đ"
                }, JsonRequestBehavior.AllowGet); // ⚠️ Nếu lỗi, thêm AllowGet
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("❌ Cập nhật giá lỗi: " + ex.ToString());
                return Json(new { success = false, message = ex.ToString() }, JsonRequestBehavior.AllowGet);
            }
        }
        //========================================================================================================================================
        [HttpGet]
        public ActionResult XemChiTietBooking(string date, int court_id, string hour)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[XemChiTietBooking] Input - date: {date}, court_id: {court_id}, hour: {hour}");

                if (!DateTime.TryParse(date, out var parsedDate))
                    return Content("Lỗi: Ngày không hợp lệ");

                if (!double.TryParse(hour, NumberStyles.Any, CultureInfo.InvariantCulture, out var hourDouble))
                    return Content("Lỗi: Giờ không hợp lệ: " + hour);

                var bookings = db.bookings
                    .Where(b => b.date == parsedDate && b.court_id == court_id)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"[XemChiTietBooking] Tổng bookings: {bookings.Count}, HourInput={hourDouble}");

                var matched = new List<booking>();

                foreach (var b in bookings)
                {
                    var diff = Math.Abs(b.start_time.TotalHours - hourDouble);
                    System.Diagnostics.Debug.WriteLine($"-- booking: {b.start_time.TotalHours} vs input {hourDouble} -> diff: {diff}");
                    if (diff < 0.01)
                        matched.Add(b);
                }

                System.Diagnostics.Debug.WriteLine($"[XemChiTietBooking] Sau lọc còn lại: {matched.Count} booking(s)");

                if (!matched.Any())
                    return Content("Không tìm thấy lịch đặt tại ô này.");

                var slots = matched.Select(b =>
                {
                    var start = b.start_time.ToString(@"hh\:mm");
                    var end = b.end_time.ToString(@"hh\:mm");
                    var thuVN = b.date.ToString("dddd", new CultureInfo("vi-VN"));

                    return new BookingInput
                    {
                        DayName = thuVN,
                        date = b.date.ToString("yyyy-MM-dd"),
                        court = b.court_id.ToString(),
                        start = start,
                        end = end,
                        type = b.type,
                        price = (long)b.price
                    };
                }).ToList();

                ViewBag.hoTen = matched.FirstOrDefault()?.customer?.name ?? "";
                ViewBag.BookingId = matched.FirstOrDefault()?.id ?? 0;
                return PartialView("XemChiTietBooking", slots);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "NULL";
                System.Diagnostics.Debug.WriteLine($"[XemChiTietBooking] Exception: {ex.Message}\n{ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"[XemChiTietBooking] InnerException: {inner}");

                return Content("Lỗi: " + ex.Message + "<br/>" + ex.StackTrace + "<br/>Inner: " + inner);
            }
        }

        //========================================================================================================================================
        [HttpPost]
        public JsonResult UpdateBookingsWithCustomer(BookingViewModel model)
        {
            try
            {
                var name = model.name?.Trim();
                var phone = model.phone?.Trim();
                var bookings = model.bookings;

                if (string.IsNullOrEmpty(name))
                    return Json(new { success = false, message = "Tên khách hàng không được để trống." });

                var samePhone = db.customers.FirstOrDefault(c => c.phone == phone);
                var sameName = db.customers.FirstOrDefault(c => c.name == name);

                if (samePhone != null && samePhone.name != name)
                {
                    return Json(new
                    {
                        success = false,
                        conflict = true,
                        message = $"Số điện thoại này đã được đăng ký với tên: {samePhone.name}. Bạn muốn dùng tên nào?",
                        options = new[] { samePhone.name, name }
                    });
                }

                customer customer = samePhone ?? sameName;

                if (customer == null)
                {
                    customer = new customer
                    {
                        name = name,
                        phone = phone ?? "",
                        password = phone ?? Guid.NewGuid().ToString("N"),
                        role = "customer",
                        created_at = DateTime.Now
                    };
                    db.customers.Add(customer);
                    db.SaveChanges();
                }
                else if (!string.IsNullOrEmpty(phone) && customer.phone == phone && customer.name != name)
                {
                    customer.name = name;
                    db.SaveChanges();
                }

                var grouped = bookings
                    .Select(b => new
                    {
                        b.court_id,
                        date = DateTime.Parse(b.date).Date,
                        start = TimeSpan.Parse(b.start_time),
                        end = TimeSpan.Parse(b.end_time),
                        type = (b.type ?? "").Trim().ToLower(),
                        b.is_paid,
                        b.payment_method,
                        manual_price = b.manual_price
                    })
                    .GroupBy(g => new { g.court_id, g.date, g.type, g.payment_method, g.is_paid })
                    .SelectMany(g =>
                    {
                        var list = g.OrderBy(x => x.start).ToList();
                        TimeSpan s = list[0].start;
                        TimeSpan e = list[list.Count - 1].end;
                        bool paid = g.Key.is_paid;
                        string method = g.Key.payment_method;
                        decimal? manual = list.FirstOrDefault()?.manual_price;
                        decimal total = 0;

                        if (manual.HasValue && manual.Value > 0)
                        {
                            total = manual.Value;
                        }
                        else
                        {
                            foreach (var x in list)
                            {
                                double duration = (x.end - x.start).TotalHours;
                                int dow = (int)g.Key.date.DayOfWeek;
                                var rule = db.price_rules.FirstOrDefault(r =>
                                    r.day_of_week == dow &&
                                    r.type == g.Key.type &&
                                    r.start_hour <= x.start.TotalHours &&
                                    r.end_hour > x.start.TotalHours);

                                if (rule == null)
                                    throw new Exception($"Không có bảng giá cho loại '{g.Key.type}' lúc {x.start} ngày {g.Key.date:dd/MM}");

                                total += (rule.price_per_hour ?? 0) * (decimal)duration;
                            }
                        }

                        return new[]
                        {
                    new
                    {
                        court_id = g.Key.court_id,
                        date = g.Key.date,
                        type = g.Key.type,
                        start_time = s,
                        end_time = e,
                        is_paid = paid,
                        payment_method = method,
                        price = total
                    }
                        };
                    })
                    .ToList();

                foreach (var b in grouped)
                {
                    var overlaps = db.bookings.Where(old =>
                        old.court_id == b.court_id &&
                        old.date == b.date &&
                        (
                            (b.start_time >= old.start_time && b.start_time < old.end_time) ||
                            (b.end_time > old.start_time && b.end_time <= old.end_time) ||
                            (b.start_time <= old.start_time && b.end_time >= old.end_time)
                        )).ToList();

                    if (overlaps.Any())
                    {
                        string khungGio = $"{b.start_time:hh\\:mm} - {b.end_time:hh\\:mm}";
                        return Json(new
                        {
                            success = false,
                            message = $"Khung giờ {khungGio} tại sân {b.court_id} ngày {b.date:dd/MM/yyyy} đã được đặt. Không thể cập nhật."
                        });
                    }

                    var invoice = new invoice
                    {
                        customer_id = customer.id,
                        total_amount = b.price,
                        note = "Tạo khi đặt sân",
                        is_paid = b.is_paid,
                        created_at = DateTime.Now,
                        payment_method = b.payment_method ?? "Tiền mặt"
                    };
                    db.invoices.Add(invoice);
                    db.SaveChanges();

                    var newBooking = new booking
                    {
                        court_id = b.court_id,
                        customer_id = customer.id,
                        date = b.date,
                        start_time = b.start_time,
                        end_time = b.end_time,
                        type = b.type,
                        price = b.price,
                        is_paid = b.is_paid,
                        payment_method = b.payment_method,
                        created_at = DateTime.Now,
                        invoice_id = invoice.id
                    };
                    db.bookings.Add(newBooking);

                    db.invoice_details.Add(new invoice_details
                    {
                        invoice_id = invoice.id,
                        item_id = b.court_id,
                        quantity = 1,
                        unit_price = b.price,
                        total_price = b.price,
                        is_paid = b.is_paid,
                        created_at = DateTime.Now
                    });

                    db.SaveChanges();
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        //============================================================================================================================

        //trạng thái thanh toán
        [HttpPost]
        public JsonResult MarkAsPaid(int bookingId)
        {
            var booking = db.bookings.Find(bookingId);
            if (booking != null)
            {
                booking.is_paid = true;
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Không tìm thấy booking" });
        }

        // Thống kê tổng quan
        public JsonResult Summary()
        {
            var totalBookings = db.bookings.Count();
            var todayRevenue = db.bookings
                .Where(b => b.date == DateTime.Today)
                .Sum(b => (decimal?)b.price) ?? 0;

            var activeCourts = db.courts.Count(c => c.status == "available");

            return Json(new
            {
                totalBookings,
                todayRevenue,
                activeCourts
            }, JsonRequestBehavior.AllowGet);
        }

        public class BookingViewModel
        {
            public string name { get; set; }
            public string phone { get; set; }
            public List<BookingRequest> bookings { get; set; }
        }

        public class BookingRequest
        {
            public int court_id { get; set; }
            public string date { get; set; }
            public string start_time { get; set; }
            public string end_time { get; set; }
            public string type { get; set; }
            public bool is_paid { get; set; }
            public int? booking_id { get; set; }
            public string payment_method { get; set; }
            public decimal? manual_price { get; set; } 
            public decimal? price { get; set; }
        }





        // Thêm lịch đặt sân
        [HttpPost]
        public JsonResult AddBooking(int court_id, int customer_id, string date, string start_time, string end_time, string type, decimal price)
        {
            try
            {
                var booking = new booking
                {
                    court_id = court_id,
                    customer_id = customer_id,
                    date = DateTime.Parse(date),
                    start_time = TimeSpan.Parse(start_time),
                    end_time = TimeSpan.Parse(end_time),
                    type = type,
                    price = price
                };

                db.bookings.Add(booking);
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public FileResult HienThiMinhChung(string file)
        {
            var path = Path.Combine(@"C:\Uploads\Invoices", file);
            var mime = MimeMapping.GetMimeMapping(path);
            return File(System.IO.File.ReadAllBytes(path), mime);
        }

        // Thông báo từ web
        public ActionResult thongbaotuWEB(DateTime? from, DateTime? to)
        {
            var ds = ThongBaoModel.DanhSachThongBao;

            if (from.HasValue)
                ds = ds.Where(tb => DateTime.Parse(tb.NgayTao).Date >= from.Value.Date).ToList();
            if (to.HasValue)
                ds = ds.Where(tb => DateTime.Parse(tb.NgayTao).Date <= to.Value.Date).ToList();

            // Gán thêm đường dẫn ảnh từ bảng invoices
            foreach (var tb in ds)
            {
                var customer = db.customers.FirstOrDefault(c => c.phone == tb.SoDienThoai);
                if (customer != null)
                {
                    var invoice = db.invoices
                        .Where(i => i.customer_id == customer.id)
                        .OrderByDescending(i => i.created_at)
                        .FirstOrDefault();

                    tb.MinhChungChuyenKhoan = invoice?.payment_image ?? "";
                }
            }
            return View(ds);
        }

        [HttpPost]
        public ActionResult XoaThongBao(int id)
        {
            var tb = ThongBaoModel.DanhSachThongBao.FirstOrDefault(t => t.Id == id);
            if (tb != null)
            {
                ThongBaoModel.DanhSachThongBao.Remove(tb);
            }
            return RedirectToAction("thongbaotuWEB");
        }

        public JsonResult DemThongBaoMoi()
        {
            var ds = ThongBaoModel.DanhSachThongBao;
            return Json(new { count = ds.Count }, JsonRequestBehavior.AllowGet);
        }

    }
}