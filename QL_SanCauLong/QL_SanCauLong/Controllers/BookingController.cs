using Newtonsoft.Json;
using QL_SanCauLong.Models;
using QuanLySanCauLong.Models;
using System;
using System.Collections.Generic;
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


        //// GET: API lấy lịch đặt sân theo ngày
        //public JsonResult LayLichDat(string date)
        //{
        //    DateTime selectedDate = DateTime.Parse(date);
        //    var bookings = db.bookings.Where(b => b.date == selectedDate)
        //        .Select(b => new
        //        {
        //            id = b.id,
        //            customer_id = b.customer_id,
        //            court_id = b.court_id,
        //            start_time = b.start_time.ToString(),
        //            end_time = b.end_time.ToString(),
        //            type = b.type,
        //            price = b.price
        //        }).ToList();

        //    return Json(bookings, JsonRequestBehavior.AllowGet);
        //}
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
        // GET: Booking form (Hiện thị thông tin cho khách hàng đặt sân)
        [HttpPost]
        public ActionResult ThanhToan(string bookingsJson)
        {
            if (string.IsNullOrEmpty(bookingsJson))
                return RedirectToAction("DatSan");

            var bookings = JsonConvert.DeserializeObject<List<BookingInput>>(bookingsJson);

            foreach (var b in bookings)
            {
                TimeSpan start = TimeSpan.Parse(b.start);
                TimeSpan end = TimeSpan.Parse(b.end);
                DateTime date = DateTime.Parse(b.date);
                int day = (int)date.DayOfWeek;
                string loai = "vãng lai";

                // Gán thứ tiếng Việt trực tiếp không dùng hàm
                string thuVN = "";
                switch (date.DayOfWeek)
                {
                    case DayOfWeek.Monday: thuVN = "Thứ hai"; break;
                    case DayOfWeek.Tuesday: thuVN = "Thứ ba"; break;
                    case DayOfWeek.Wednesday: thuVN = "Thứ tư"; break;
                    case DayOfWeek.Thursday: thuVN = "Thứ năm"; break;
                    case DayOfWeek.Friday: thuVN = "Thứ sáu"; break;
                    case DayOfWeek.Saturday: thuVN = "Thứ bảy"; break;
                    case DayOfWeek.Sunday: thuVN = "Chủ nhật"; break;
                }

                // Gán vào model nếu BookingInput có thuộc tính DayName
                b.DayName = thuVN;

                decimal total = 0;

                for (int hour = start.Hours; hour < end.Hours; hour++)
                {
                    var rule = db.price_rules.FirstOrDefault(r =>
                        r.day_of_week == day &&
                        r.start_hour <= hour &&
                        r.end_hour > hour &&
                        r.type == loai);

                    decimal pricePerHour = rule?.price_per_hour ?? 0;
                    total += pricePerHour;
                }

                b.price = (long)total;
            }

            return View("ThanhToan", bookings);
        }

        [HttpPost]
        public JsonResult LuuDatSan(List<BookingInput> bookings, string HoTen, string SoDienThoai)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Tìm hoặc tạo khách hàng
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
                            created_at = DateTime.Now
                        };

                        // Tránh trùng khung giờ
                        if (!db.bookings.Any(x =>
                            x.court_id == courtId &&
                            x.date == booking.date &&
                            x.start_time == booking.start_time &&
                            x.end_time == booking.end_time))
                        {
                            db.bookings.Add(booking);
                            db.SaveChanges();
                            tongTien += b.price;
                            createdBookings.Add(booking);
                        }
                    }

                    // Tạo hóa đơn
                    var invoice = new invoice
                    {
                        customer_id = customer.id,
                        total_amount = tongTien,
                        note = "Đặt sân tự động",
                        is_paid = false,
                        created_at = DateTime.Now
                    };
                    db.invoices.Add(invoice);
                    db.SaveChanges();

                    // Tạo chi tiết hóa đơn
                    foreach (var b in createdBookings)
                    {
                        var courtName = db.courts.FirstOrDefault(c => c.id == b.court_id)?.name ?? "Sân";
                        string itemName = $"{courtName} ({b.date:dd/MM} {b.start_time}-{b.end_time})";

                        var detail = new invoice_details
                        {
                            invoice_id = invoice.id,
                            item_name = itemName,
                            quantity = 1,
                            unit_price = b.price,
                            created_at = DateTime.Now
                        };
                        db.invoice_details.Add(detail);
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, message = "Đặt sân và tạo hóa đơn thành công" });
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