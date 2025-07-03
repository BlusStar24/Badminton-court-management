using Newtonsoft.Json;
using QL_SanCauLong.Models;
using QuanLySanCauLong.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QuanLySanCauLong.Areas.KhachHang.controllers
{
    public class BookingController : Controller
    {
        QuanLySanCauLongdbEntities db = new QuanLySanCauLongdbEntities();
        // GET: Booking form (Admin thêm lịch từ Zalo, Facebook)
        public ActionResult ViewDatSan(string date)
        {

            DateTime selectedDate;
            if (!DateTime.TryParse(date, out selectedDate))
                selectedDate = DateTime.Today;

            var bookings = db.bookings
                .Where(b => b.date == selectedDate)
                .ToList()
                .Select(b => new
                {
                    date = b.date.ToString("yyyy-MM-dd"), // thêm dòng này
                    court = "Sân " + b.court_id,
                    time = b.start_time.Hours + ":" + (b.start_time.Minutes == 0 ? "00" : b.start_time.Minutes.ToString("00")),
                    status = "booked"
                }).ToList();

            ViewBag.BookingData = bookings;
            return View();
        }
        //================================================================================================================================
        // GET: Booking form (Khách hàng đặt sân)
        public JsonResult GetTrangThai(string ngay)
        {
            DateTime selectedDate;
            if (!DateTime.TryParse(ngay, out selectedDate))
                selectedDate = DateTime.Today;

            var bookings = db.bookings
                .Where(b => b.date == selectedDate)
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

                for (double hour = startHour; hour < endHour; hour += 0.5)
                {
                    int hourInt = (int)Math.Floor(hour);

                    var rule = db.price_rules.FirstOrDefault(r =>
                        r.day_of_week == day &&
                        r.start_hour <= hourInt &&
                        r.end_hour > hourInt &&
                        r.type == loai);

                    decimal pricePerHour = rule?.price_per_hour ?? 0;
                    total += pricePerHour * 0.5m; // tính theo nửa giờ
                }

                b.price = (long)total;
            }

            return View("ThanhToan", bookings);
        }
        //================================================================================================================================
        // lưu lịch đặt sân của customer vào DB 
        [HttpPost]
        public JsonResult LuuDatSan(List<BookingInput> bookings, string HoTen, string SoDienThoai, string payment_method, HttpPostedFileBase payment_image)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
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
                        customer = new customers
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
                    List<bookings> createdBookings = new List<bookings>();

                    foreach (var b in bookings)
                    {
                        var courtId = db.courts.FirstOrDefault(c => c.name == b.court)?.id ?? 1;

                        var booking = new bookings
                        {
                            customer_id = customer.id,
                            court_id = courtId,
                            date = DateTime.Parse(b.date),
                            start_time = TimeSpan.Parse(b.start),
                            end_time = TimeSpan.Parse(b.end),
                            type = "vãng lai",
                            price = b.price,
                            created_at = DateTime.Now,
                            is_paid = (payment_method == "Chuyển khoản"),
                            payment_method = payment_method
                        };

                        bool trungGio = db.bookings.Any(x =>
                            x.court_id == courtId &&
                            x.date == booking.date &&
                            x.start_time == booking.start_time &&
                            x.end_time == booking.end_time);

                        if (!trungGio)
                        {
                            db.bookings.Add(booking);
                            db.SaveChanges();
                            tongTien += b.price;
                            createdBookings.Add(booking);
                        }
                    }

                    // Xử lý ảnh minh chứng nếu có
                    string imagePath = null;
                    if (payment_image != null && payment_image.ContentLength > 0)
                    {
                        string folder = @"C:\Uploads\Invoices";
                        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                        string fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileName(payment_image.FileName)}";
                        string fullPath = Path.Combine(folder, fileName);
                        payment_image.SaveAs(fullPath);

                        imagePath = fullPath; 
                    }

                    var invoice = new invoice
                    {
                        customer_id = customer.id,
                        total_amount = tongTien,
                        note = "Đặt sân tự động",
                        is_paid = (payment_method == "Chuyển khoản"),
                        payment_method = payment_method,
                        payment_image = imagePath,
                        created_at = DateTime.Now
                    };
                    db.invoices.Add(invoice);
                    db.SaveChanges();

                    foreach (var b in createdBookings)
                    {
                        var detail = new invoice_details
                        {
                            invoice_id = invoice.id,
                            item_id = 1,
                            quantity = 1,
                            unit_price = b.price,
                            created_at = DateTime.Now
                        };
                        db.invoice_details.Add(detail);
                    }

                    db.SaveChanges();

                    var thongBao = new ThongBaoModel
                    {
                        Id = ThongBaoModel.DanhSachThongBao.Any() ? ThongBaoModel.DanhSachThongBao.Max(t => t.Id) + 1 : 1,
                        HoTen = customer.name,
                        SoDienThoai = customer.phone,
                        NgayTao = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                        TongTien = tongTien,
                        ChiTiet = bookings
                    };
                    ThongBaoModel.DanhSachThongBao.Add(thongBao);

                    transaction.Commit();
                    return Json(new { success = true, redirectUrl = Url.Action("ViewDatSan", "Booking") });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = ex.Message });
                }
            }
        }

    }

}


