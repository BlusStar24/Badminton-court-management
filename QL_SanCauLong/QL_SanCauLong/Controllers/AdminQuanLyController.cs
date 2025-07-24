using QL_SanCauLong.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
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
        QuanLySanCauLongEntities3 db = new QuanLySanCauLongEntities3();

        // GET: Admin

        public ActionResult Index()
        {
            return View();
        }
        [Authorize]
        public ActionResult Booking()
        {
            var danhSachKhachHang = db.customers.Select(c => c.name).Distinct().ToList();
            ViewBag.DanhSachKhachHang = danhSachKhachHang;
            return View();
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
                start_time = Math.Round(b.start_time.TotalHours, 2), // Làm tròn 2 chữ số thập phân
                end_time = Math.Round(b.end_time.TotalHours, 2),   // Làm tròn 2 chữ số thập phân
                b.price,
                b.type,
                b.is_paid,
                customer_id = b.customer_id, // Thêm customer_id
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
                // 1. Xóa invoice_details liên quan trước
                var relatedDetails = db.invoice_details.Where(d => d.booking_id != null && ids.Contains(d.booking_id.Value)).ToList();
                db.invoice_details.RemoveRange(relatedDetails);
                db.SaveChanges();

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
                using (var freshDb = new QuanLySanCauLongEntities3())
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
                        message = $"SĐT này đã đăng ký tên: {samePhone.name}. Bạn muốn dùng tên nào?",
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

                var parsedBookings = model.bookings
                    .Select(b => new
                    {
                        b.court_id,
                        date = DateTime.Parse(b.date).Date,
                        start = TimeSpan.Parse(b.start_time),
                        end = TimeSpan.Parse(b.end_time),
                        type = (b.type ?? "").Trim().ToLower(),
                        b.is_paid,
                        b.payment_method,
                        b.manual_price
                    })
                    .OrderBy(x => x.date).ThenBy(x => x.court_id).ThenBy(x => x.start)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"🔍 Parsed bookings: {parsedBookings.Count}");
                foreach (var b in parsedBookings)
                {
                    System.Diagnostics.Debug.WriteLine($"🔹 Booking: court={b.court_id}, date={b.date:yyyy-MM-dd}, start={b.start}, end={b.end}, type={b.type}");
                }
              
                // Không cần gộp lại vì client đã gộp
                var bookingsToAdd = new List<booking>();
                foreach (var b in parsedBookings)
                {
                    decimal price = 0;
                    if (b.manual_price.HasValue && b.manual_price.Value > 0)
                    {
                        price = b.manual_price.Value;
                    }
                    else
                    {
                        double startHour = b.start.TotalHours;
                        double endHour = b.end.TotalHours;
                        int dow = (int)b.date.DayOfWeek;

                        var rules = db.price_rules
                            .Where(r => r.day_of_week == dow && r.type == b.type &&
                                        r.start_hour < endHour && r.end_hour > startHour)
                            .OrderBy(r => r.start_hour)
                            .ToList();

                        if (!rules.Any())
                            throw new Exception($"Không có bảng giá cho loại '{b.type}' từ {b.start} đến {b.end} ngày {b.date:dd/MM}");

                        foreach (var rule in rules)
                        {
                            double from = Math.Max((double)(rule.start_hour ?? 0), startHour);
                            double to = Math.Min((double)(rule.end_hour ?? 0), endHour);
                            if (from < to)
                            {
                                double blockHours = to - from;
                                price += (decimal)blockHours * (rule.price_per_hour ?? 0);
                            }
                        }
                    }

                    var conflict = db.bookings.Any(old =>
                        old.court_id == b.court_id &&
                        old.date == b.date &&
                        (
                            (b.start >= old.start_time && b.start < old.end_time) ||
                            (b.end > old.start_time && b.end <= old.end_time) ||
                            (b.start <= old.start_time && b.end >= old.end_time)
                        ));

                    if (conflict)
                    {
                        string khungGio = $"{b.start:hh\\:mm} - {b.end:hh\\:mm}";
                        return Json(new { success = false, message = $"Khung giờ {khungGio} tại sân {b.court_id} ngày {b.date:dd/MM/yyyy} đã bị trùng." });
                    }
                    bool daThanhToan = b.payment_method?.Trim().ToLower() != "nợ";
                    bookingsToAdd.Add(new booking
                    {
                        court_id = b.court_id,
                        customer_id = customer.id,
                        date = b.date,
                        start_time = b.start,
                        end_time = b.end,
                        type = b.type,
                        is_paid =daThanhToan,
                        payment_method = b.payment_method ?? "Tiền mặt",
                        price = price,
                        created_at = DateTime.Now,
                        is_confirmed = true, // Mặc định là đã xác nhận
                    });
                }

                var invoice = new invoice
                {
                    customer_id = customer.id,
                    total_amount = bookingsToAdd.Sum(b => b.price),
                    note = "Tạo khi đặt sân",
                    is_paid = bookingsToAdd.All(b => (bool)b.is_paid),
                    created_at = DateTime.Now,
                    payment_method = bookingsToAdd.FirstOrDefault()?.payment_method ?? "Tiền mặt"
                };

                db.invoices.Add(invoice);
                db.bookings.AddRange(bookingsToAdd);
                db.SaveChanges(); // Lúc này booking.id mới được cập nhật từ DB

                foreach (var b in bookingsToAdd)
                {
                    b.invoice_id = invoice.id;

                    var existingDetail = db.invoice_details.FirstOrDefault(d =>
                        d.invoice_id == invoice.id && d.booking_id == b.id);

                    if (existingDetail != null)
                    {
                        existingDetail.unit_price = b.price;
                        existingDetail.total_price = b.price;
                        existingDetail.is_paid = b.is_paid;
                        existingDetail.created_at = DateTime.Now;
                    }
                    else
                    {
                        db.invoice_details.Add(new invoice_details
                        {
                            invoice_id = invoice.id,
                            booking_id = b.id,
                            item_id = 1,
                            quantity = 1,
                            unit_price = b.price,
                            total_price = b.price,
                            is_paid = b.is_paid,
                            created_at = DateTime.Now
                        });
                    }
                }


                db.SaveChanges();
          

                System.Diagnostics.Debug.WriteLine($"✅ Đã lưu {bookingsToAdd.Count} booking(s)");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi khi cập nhật booking: {ex.Message}\n{ex.StackTrace}");
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
        //============================================================================================================

        [HttpGet]
        public JsonResult CheckLogin()
        {
            return Json(new { isLoggedIn = User.Identity.IsAuthenticated }, JsonRequestBehavior.AllowGet);
        }
        [Authorize]
        public ActionResult thongbaotuWEB(DateTime? from, DateTime? to)
        {
            // Không lọc chỉ "Chờ xác nhận" nữa
            var danhSachThongBao = ThongBaoModel.DanhSachThongBao.ToList();

            // Clear danh sách cũ để làm mới
            ThongBaoModel.DanhSachThongBao.Clear();

            // Lấy bookings từ DB, bao gồm cả đã xác nhận và chưa xác nhận
            var bookings = db.bookings
                .OrderByDescending(b => b.created_at)
                .ToList();

            foreach (var booking in bookings)
            {
                var customer = booking.customer;
                if (customer == null) continue;

                var chiTiet = new List<BookingInput>
        {
            new BookingInput
            {
                court = booking.court?.name ?? "Không rõ",
                date = booking.date.ToString("yyyy-MM-dd"),
                start = booking.start_time.ToString(@"hh\:mm"),
                end = booking.end_time.ToString(@"hh\:mm"),
                price = (long)booking.price
            }
        };

                var isConfirmed = booking.is_confirmed == true ? "Đã xác nhận" : "Chờ xác nhận";

                ThongBaoModel.DanhSachThongBao.Add(new ThongBaoModel
                {
                    Id = ThongBaoModel.DanhSachThongBao.Any() ? ThongBaoModel.DanhSachThongBao.Max(x => x.Id) + 1 : 1,
                    HoTen = customer.name,
                    SoDienThoai = customer.phone,
                    NgayTao = booking.created_at?.ToString("dd/MM/yyyy HH:mm") ?? "",
                    TongTien = booking.price,
                    ChiTiet = chiTiet,
                    BookingId = booking.id,
                    IsConfirmed = isConfirmed
                });
            }

            // Lấy các booking Asc từ chối từ rejected_bookings
            var rejectedBookings = db.rejected_bookings
                .OrderByDescending(r => r.created_at)
                .ToList();

            foreach (var r in rejectedBookings)
            {
                var customer = db.customers.FirstOrDefault(c => c.id == r.customer_id);
                var court = db.courts.FirstOrDefault(c => c.id == r.court_id);

                var chiTiet = new List<BookingInput>
        {
            new BookingInput
            {
                court = court?.name ?? "Không rõ",
                date = r.date.ToString("yyyy-MM-dd"),
                start = r.start_time.ToString(@"hh\:mm"),
                end = r.end_time.ToString(@"hh\:mm"),
                price = (long)r.price
            }
        };

                ThongBaoModel.DanhSachThongBao.Add(new ThongBaoModel
                {
                    Id = ThongBaoModel.DanhSachThongBao.Any() ? ThongBaoModel.DanhSachThongBao.Max(x => x.Id) + 1 : 1,
                    HoTen = customer?.name ?? "Không rõ",
                    SoDienThoai = customer?.phone ?? "",
                    NgayTao = r.created_at.ToString("dd/MM/yyyy HH:mm"),
                    TongTien = r.price,
                    ChiTiet = chiTiet,
                    BookingId = null,
                    IsConfirmed = "Đã từ chối"
                });
            }

            // Lọc theo ngày
            var ds = ThongBaoModel.DanhSachThongBao.AsQueryable();

            if (from.HasValue)
            {
                var fromDate = from.Value.Date;
                ds = ds.Where(tb => DateTime.Parse(tb.NgayTao).Date >= fromDate);
            }

            if (to.HasValue)
            {
                var toDate = to.Value.Date;
                ds = ds.Where(tb => DateTime.Parse(tb.NgayTao).Date <= toDate);
            }

            ds = ds.OrderByDescending(tb => DateTime.Parse(tb.NgayTao));

            // Cập nhật minh chứng
            foreach (var tb in ds)
            {
                if (!tb.BookingId.HasValue) continue;

                var booking = db.bookings.FirstOrDefault(b => b.id == tb.BookingId);
                if (booking != null && booking.invoice_id != null)
                {
                    var invoice = db.invoices.FirstOrDefault(i => i.id == booking.invoice_id);
                    tb.MinhChungChuyenKhoan = invoice?.payment_image ?? "";
                }
            }

            return View(ds.ToList());
        }


        [HttpPost]
        public ActionResult XacNhanBooking(int id)
        {
            var booking = db.bookings.FirstOrDefault(b => b.id == id);
            if (booking == null) return HttpNotFound();

            if (booking.invoice_id.HasValue)
            {
                var all = db.bookings.Where(b => b.invoice_id == booking.invoice_id).ToList();
                foreach (var b in all)
                {
                    b.is_confirmed = true;
                    // Cập nhật trạng thái trong RAM
                    var tb = ThongBaoModel.DanhSachThongBao.FirstOrDefault(t => t.BookingId == b.id);
                    if (tb != null)
                    {
                        tb.IsConfirmed = "Đã xác nhận";
                    }
                }
            }
            else
            {
                booking.is_confirmed = true;
                // Cập nhật trạng thái trong RAM
                var tb = ThongBaoModel.DanhSachThongBao.FirstOrDefault(t => t.BookingId == id);
                if (tb != null)
                {
                    tb.IsConfirmed = "Đã xác nhận";
                }
            }

            db.SaveChanges();

            TempData["Success"] = "Đã xác nhận đặt sân thành công.";
            return RedirectToAction("thongbaotuWEB");
        }

        private void XoaBookingTheoId(int id)
        {
            var booking = db.bookings.FirstOrDefault(b => b.id == id);
            if (booking == null) return;

            List<int> deletedBookingIds = new List<int>();

            if (booking.invoice_id.HasValue)
            {
                var invoiceId = booking.invoice_id.Value;

                var relatedBookings = db.bookings
                    .Where(b => b.invoice_id == invoiceId)
                    .ToList();

                foreach (var b in relatedBookings)
                {
                    db.rejected_bookings.Add(new rejected_bookings
                    {
                        booking_id = b.id,
                        customer_id = b.customer_id,
                        court_id = b.court_id,
                        date = b.date,
                        start_time = b.start_time,
                        end_time = b.end_time,
                        price = b.price,
                        reason = "Từ chối bởi admin",
                        created_at = DateTime.Now
                    });
                    deletedBookingIds.Add(b.id);

                    // Cập nhật trạng thái trong RAM
                    var tb = ThongBaoModel.DanhSachThongBao.FirstOrDefault(t => t.BookingId == b.id);
                    if (tb != null)
                    {
                        tb.IsConfirmed = "Đã từ chối";
                        tb.BookingId = null; // Đặt BookingId về null vì booking đã bị xóa
                    }
                }

                db.SaveChanges(); // Lưu rejected_bookings trước khi xóa

                // Dùng raw SQL để xóa
                db.Database.ExecuteSqlCommand("DELETE FROM invoice_details WHERE invoice_id = {0}", invoiceId);
                db.Database.ExecuteSqlCommand("DELETE FROM bookings WHERE invoice_id = {0}", invoiceId);
                db.Database.ExecuteSqlCommand("DELETE FROM invoices WHERE id = {0}", invoiceId);
            }
            else
            {
                db.rejected_bookings.Add(new rejected_bookings
                {
                    booking_id = booking.id,
                    customer_id = booking.customer_id,
                    court_id = booking.court_id,
                    date = booking.date,
                    start_time = booking.start_time,
                    end_time = booking.end_time,
                    price = booking.price,
                    reason = "Từ chối bởi admin",
                    created_at = DateTime.Now
                });

                db.SaveChanges();

                db.Database.ExecuteSqlCommand("DELETE FROM bookings WHERE id = {0}", booking.id);
                deletedBookingIds.Add(booking.id);

                // Cập nhật trạng thái trong RAM
                var tb = ThongBaoModel.DanhSachThongBao.FirstOrDefault(t => t.BookingId == booking.id);
                if (tb != null)
                {
                    tb.IsConfirmed = "Đã từ chối";
                    tb.BookingId = null; // Đặt BookingId về null vì booking đã bị xóa
                }
            }

            // Không cần thêm thông báo mới vào RAM vì đã cập nhật trạng thái ở trên
        }

        [HttpPost]
        public ActionResult TuChoiBooking(int id)
        {
            var booking = db.bookings.FirstOrDefault(b => b.id == id);
            if (booking == null) return HttpNotFound();

            try
            {
                XoaBookingTheoId(id); // đã lo xóa DB + RAM

                TempData["Error"] = "Đã từ chối và xóa hóa đơn.";
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Lỗi đồng bộ dữ liệu. Có thể booking đã bị xoá.";
            }

            return RedirectToAction("thongbaotuWEB");
        }





        [HttpPost]
        [ValidateAntiForgeryToken] // nếu bạn dùng AntiForgeryToken
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