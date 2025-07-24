using Newtonsoft.Json;
using QL_SanCauLong.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Text.RegularExpressions;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions; 
namespace QL_SanCauLong.Controllers
{
    public class BookingController : Controller
    {
        QuanLySanCauLongEntities3 db = new QuanLySanCauLongEntities3();
        // GET: Booking form (Admin thêm lịch từ Zalo, Facebook)
        [AllowAnonymous]
        public ActionResult ViewDatSan(string date)
        {
            DateTime selectedDate;
            if (!DateTime.TryParse(date, out selectedDate))
                selectedDate = DateTime.Today;

            db.Database.CommandTimeout = 60; // Tăng thời gian chờ truy vấn

            var bookings = db.bookings
                .Where(b => b.date.Year == selectedDate.Year &&
                            b.date.Month == selectedDate.Month &&
                            b.date.Day == selectedDate.Day)
                .Select(b => new
                {
                    b.date,
                    b.court_id,
                    b.start_time
                })
                .ToList()
                .Select(b => new
                {
                    date = b.date.ToString("yyyy-MM-dd"),
                    court = "Sân " + b.court_id,
                    time = b.start_time.ToString(@"hh\:mm"),
                    status = "booked"
                })
                .ToList();

            ViewBag.BookingData = bookings;
            return View();
        }

        //================================================================================================================================

        public ActionResult LichDaDat()
        {
            if (Session["UserID"] == null)
            {
                TempData["LoginMessage"] = "Bạn cần đăng nhập để xem lịch sân đã đặt.\nTài khoản và mật khẩu chính là tên và số điện thoại bạn dùng khi đặt sân.";
                return RedirectToAction("ViewDatSan", "Booking", new { loginWarn = true });
            }

            int customerId = (int)Session["UserID"];

            // Lấy các booking còn tồn tại
            var lichDaDat = db.bookings
                .Where(b => b.customer_id == customerId)
                .OrderByDescending(b => b.created_at)
                .Select(b => new
                {
                    b.id,
                    b.date,
                    b.start_time,
                    b.end_time,
                    court = b.court.name,
                    b.price,
                    b.type,
                    b.is_paid,
                    b.payment_method,
                    invoice_id = b.invoice_id,
                    b.is_confirmed
                })
                .ToList()
                .Select(b => new BookingHoaDonKhachModel
                {
                    Id = b.id,
                    Date = b.date.ToString("yyyy-MM-dd"),
                    TimeRange = $"{b.start_time:hh\\:mm} - {b.end_time:hh\\:mm}",
                    Court = b.court,
                    Price = b.price,
                    Type = b.type,
                    IsPaid = (b.is_paid ?? false) ? "Đã thanh toán" : "Chưa thanh toán",
                    PaymentMethod = (b.payment_method == "Nợ" ? "Tiền mặt" : b.payment_method),
                    InvoiceId = b.invoice_id,
                    IsConfirmed = (b.is_confirmed == true)
                        ? "✔️ Đã xác nhận"
                        : (b.is_confirmed == null
                            ? "⏳ Chờ xác nhận"
                            : "❌ Đã từ chối")
                })
                .ToList();
            var courtDict = db.courts.ToDictionary(c => c.id, c => c.name);

            // Lấy các booking đã bị từ chối
            var daTuChoi = db.rejected_bookings
                .Where(b => b.customer_id == customerId)
                .OrderByDescending(b => b.created_at)
                .ToList()
                .Select(b => new BookingHoaDonKhachModel
                {
                    Id = b.booking_id,
                    Date = b.date.ToString("yyyy-MM-dd"),
                    TimeRange = $"{b.start_time:hh\\:mm} - {b.end_time:hh\\:mm}",

                    // Fix for CS1503: Convert nullable int (int?) to int using the null-coalescing operator or explicit cast.
                    Court = courtDict.ContainsKey(b.court_id ?? 0) ? courtDict[b.court_id ?? 0] : "Không rõ",
                    Price = b.price,
                    Type = "vãng lai",
                    IsPaid = "Đã từ chối",
                    PaymentMethod = "Không",
                    InvoiceId = null,
                    IsConfirmed = "❌ Đã từ chối"
                })
                .ToList();

            lichDaDat.AddRange(daTuChoi);

            return View(lichDaDat);
        }


        //================================================================================================================================
        // GET: Booking form (Khách hàng đặt sân)
        public JsonResult GetTrangThai(string ngay)
        {
            DateTime selectedDate;
            if (!DateTime.TryParse(ngay, out selectedDate))
                selectedDate = DateTime.Today;

            db.Database.CommandTimeout = 60;

            var bookings = db.bookings
                .Where(b => b.date.Year == selectedDate.Year &&
                            b.date.Month == selectedDate.Month &&
                            b.date.Day == selectedDate.Day)
                .Select(b => new
                {
                    b.court_id,
                    b.start_time,
                    b.end_time
                })
                .ToList();

            var result = new List<object>();

            foreach (var b in bookings)
            {
                var start = b.start_time;
                var end = b.end_time;
                while (start < end)
                {
                    result.Add(new
                    {
                        court = "Sân " + b.court_id,
                        time = start.ToString(@"hh\:mm"),
                        status = "booked"
                    });
                    start = start.Add(TimeSpan.FromMinutes(30));
                }
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        //================================================================================================================================
        // GET: Booking form (Hiện thị thông tin cho khách hàng đặt sân)
        [HttpPost]
        public ActionResult ThanhToan(string bookingsJson)
        {
            if (string.IsNullOrEmpty(bookingsJson))
                return RedirectToAction("DatSan");

            var bookings = JsonConvert.DeserializeObject<List<BookingInput>>(bookingsJson);

            foreach (var b in bookings)
            {
                // Sửa lỗi nếu giờ là "24:00"
                if (b.start == "24:00") b.start = "23:59";
                if (b.end == "24:00") b.end = "23:59";

                TimeSpan start = TimeSpan.Parse(b.start);
                TimeSpan end = TimeSpan.Parse(b.end);
                DateTime date = DateTime.Parse(b.date);
                int day = (int)date.DayOfWeek;
                string loai = "vãng lai";

                // Gán thứ tiếng Việt
                string thuVN = date.ToString("dddd", new System.Globalization.CultureInfo("vi-VN"));
                b.DayName = thuVN;

                decimal total = 0;
                double startHour = start.TotalHours;
                double endHour = end.TotalHours;

                var rules = db.price_rules
                  .Where(r => r.day_of_week == day && r.type == loai)
                  .ToList();

                for (double hour = startHour; hour < endHour; hour += 0.5)
                {
                    int hourInt = (int)Math.Floor(hour);

                    var rule = rules.FirstOrDefault(r =>
                        r.start_hour <= hourInt &&
                        r.end_hour > hourInt);

                    decimal pricePerHour = rule?.price_per_hour ?? 0;
                    total += pricePerHour * 0.5m;
                }

                b.price = (long)total;
            }

            return View("ThanhToan", bookings);
        }

        //===============================================================================================================================
        public static class CloudinaryHelper
        {
            private static readonly Cloudinary _cloudinary;

            static CloudinaryHelper()
            {
                Account account = new Account(
                    "dbwdohabb",                // cloud name
                    "633735185578915",               // API key
                    "7tP4ecnvlixKUb_0GZj0BtRAh44"    // API secret
                );
                _cloudinary = new Cloudinary(account);
            }

            public static string UploadImage(HttpPostedFileBase file)
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, file.InputStream),
                    Folder = "phuthinh",
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false
                };

                var uploadResult = _cloudinary.Upload(uploadParams);
                return uploadResult.SecureUrl?.ToString();
            }
        }
        //================================================================================================================================
        [HttpPost]
        [ValidateAntiForgeryToken] // Yêu cầu CSRF token
        public JsonResult LuuDatSan(List<BookingInput> bookings, string HoTen, string SoDienThoai, string payment_method, HttpPostedFileBase payment_image)
        {
            try
            {
                if (bookings == null || !bookings.Any())
                {
                    return Json(new { success = false, message = "Không có thông tin đặt sân được cung cấp." });
                }

                // Kiểm tra định dạng số điện thoại
                if (!Regex.IsMatch(SoDienThoai, @"^[0-9]{10,11}$"))
                {
                    return Json(new { success = false, message = "Số điện thoại không hợp lệ." });
                }


                // Tiếp tục logic hiện tại của bạn
                using (var transaction = db.Database.BeginTransaction())
                {
                    var samePhone = db.customers.FirstOrDefault(c => c.phone == SoDienThoai);
                    if (samePhone != null && samePhone.name != HoTen)
                    {
                        return Json(new
                        {
                            success = false,
                            requireConfirm = true,
                            message = $"Số điện thoại này đã được đăng ký với tên: {samePhone.name}. Bạn có muốn tiếp tục dùng tên này không?"
                        });
                    }

                    var sameName = db.customers.FirstOrDefault(c => c.name == HoTen);
                    if (sameName != null && sameName.phone != SoDienThoai)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Tên khách hàng '{HoTen}' đã tồn tại với số điện thoại khác. Vui lòng dùng tên khác."
                        });
                    }

                    var customer = db.customers.FirstOrDefault(c => c.phone == SoDienThoai);
                    if (customer == null)
                    {
                        customer = new customer
                        {
                            name = HoTen,
                            phone = SoDienThoai,
                            password = SoDienThoai,
                            role = "customer",
                            created_at = DateTime.Now
                        };
                        db.customers.Add(customer);
                        db.SaveChanges();
                    }


                    Session["UserID"] = customer.id;
                    Session["UserName"] = customer.name;
                    Session["UserPhone"] = customer.phone;

                    decimal tongTien = 0;
                    List<booking> createdBookings = new List<booking>();

                    foreach (var b in bookings)
                    {
                        if (!DateTime.TryParse(b.date, out var date) ||
                            !TimeSpan.TryParse(b.start, out var startTime) ||
                            !TimeSpan.TryParse(b.end, out var endTime))
                        {
                            return Json(new { success = false, message = "Định dạng ngày hoặc giờ không hợp lệ." });
                        }

                        var court = db.courts.FirstOrDefault(c => c.name == b.court);
                        var courtId = court?.id ?? 1;
                        var booking = new booking
                        {
                            customer_id = customer.id,
                            court_id = courtId,
                            date = date,
                            start_time = startTime,
                            end_time = endTime,
                            type = "vãng lai",
                            price = b.price,
                            created_at = DateTime.Now,
                            is_paid = (payment_method != "Tiền mặt"),
                            payment_method = (payment_method == "Tiền mặt" ? "Nợ" : payment_method),
                            is_confirmed = null
                        };

                        bool trungGio = db.bookings.Any(x =>
                            x.court_id == courtId &&
                            x.date == booking.date &&
                            ((x.start_time <= booking.start_time && x.end_time > booking.start_time) ||
                             (x.start_time < booking.end_time && x.end_time >= booking.end_time) ||
                             (x.start_time >= booking.start_time && x.end_time <= booking.end_time)));

                        if (!trungGio)
                        {
                            db.bookings.Add(booking);
                            db.SaveChanges();
                            tongTien += b.price;
                            createdBookings.Add(booking);
                        }
                    }

                    string imagePath = null;
                    if (payment_image != null && payment_image.ContentLength > 0 && payment_method == "Chuyển khoản")
                    {
                        if (payment_image.ContentLength > 32 * 1024 * 1024)
                        {
                            return Json(new { success = false, message = "Kích thước ảnh vượt quá 32MB. Vui lòng chọn ảnh nhỏ hơn." });
                        }

                        try
                        {
                            imagePath = CloudinaryHelper.UploadImage(payment_image);
                            System.Diagnostics.Debug.WriteLine("✅ Uploaded to Cloudinary: " + imagePath);
                        }
                        catch (Exception ex)
                        {
                            return Json(new { success = false, message = "Lỗi upload Cloudinary: " + ex.Message });
                        }
                    }


                    var invoice = new invoice
                    {
                        customer_id = customer.id,
                        total_amount = tongTien,
                        note = "Đặt sân tự động",
                        is_paid = (payment_method != "Tiền mặt"),
                        payment_method = (payment_method == "Tiền mặt" ? "Nợ" : payment_method),
                        payment_image = imagePath,
                        created_at = DateTime.Now
                    };
                    db.invoices.Add(invoice);
                    db.SaveChanges();
                    // GÁN LẠI invoice_id CHO MỖI BOOKING
                    foreach (var b in createdBookings)
                    {
                        b.invoice_id = invoice.id;
                        db.Entry(b).State = EntityState.Modified;
                    }
                    db.SaveChanges(); // lưu lại booking sau khi gán invoice_id
                    foreach (var b in createdBookings)
                    {
                        var detail = new invoice_details
                        {
                            invoice_id = invoice.id,
                            item_id = 1,
                            quantity = 1,
                            unit_price = b.price,
                            created_at = DateTime.Now,
                            is_paid = invoice.is_paid,
                            booking_id = b.id
                        };
                        db.invoice_details.Add(detail);
                    }

                    db.SaveChanges();
                    var lastBooking = createdBookings.OrderByDescending(b => b.created_at).FirstOrDefault();
                    var thongBao = new ThongBaoModel
                    {
                        Id = ThongBaoModel.DanhSachThongBao.Any() ? ThongBaoModel.DanhSachThongBao.Max(t => t.Id) + 1 : 1,
                        HoTen = customer.name,
                        SoDienThoai = customer.phone,
                        NgayTao = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                        TongTien = tongTien,
                        ChiTiet = bookings,
                        BookingId = lastBooking?.id
                    };
                    ThongBaoModel.DanhSachThongBao.Add(thongBao);

                    transaction.Commit();
                    return Json(new { success = true, redirectUrl = Url.Action("ViewDatSan", "Booking") });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi tổng quát: " + ex.ToString());
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
        //================================================================================================================================
        [HttpGet]
        public JsonResult GetLastBookingTimestamp()
        {
            var last = db.bookings.OrderByDescending(b => b.created_at).Select(b => b.created_at).FirstOrDefault();
            return Json(new { lastUpdated = last?.ToString("o") }, JsonRequestBehavior.AllowGet); // dạng ISO 8601
        }


    }

}


