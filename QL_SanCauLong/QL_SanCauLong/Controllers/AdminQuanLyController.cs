using QL_SanCauLong.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QL_SanCauLong.Controllers
{
    public class AdminQuanLyController : Controller
    {
        // GET: AdminQuanLy
        QuanLySanCauLongdbEntities db = new QuanLySanCauLongdbEntities();

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
                customer_name = b.customers?.name ?? "Ẩn danh",
                customer_phone = b.customers?.phone ?? ""
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
                using (var freshDb = new QuanLySanCauLongdbEntities())
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

                customers customer = samePhone ?? sameName;

                if (customer == null)
                {
                    customer = new customers
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

                // Gộp giờ liên tiếp và cộng giá
                var grouped = bookings
                    .Select(b => new
                    {
                        b.court_id,
                        date = DateTime.Parse(b.date).Date,
                        start = TimeSpan.Parse(b.start_time),
                        end = TimeSpan.Parse(b.end_time),
                        b.type,
                        b.is_paid
                    })
                    .GroupBy(g => new { g.court_id, g.date, g.type })
                    .SelectMany(g =>
                    {
                        var list = g.OrderBy(x => x.start).ToList();
                        var merged = new List<(TimeSpan start, TimeSpan end, bool is_paid, decimal price)>();

                        TimeSpan s = list[0].start;
                        TimeSpan e = list[0].end;
                        bool paid = list[0].is_paid;
                        decimal totalPrice = 0;

                        for (int i = 0; i < list.Count; i++)
                        {
                            var current = list[i];
                            double duration = (current.end - current.start).TotalHours;
                            int dow = (int)g.Key.date.DayOfWeek;

                            var rule = db.price_rules.FirstOrDefault(r =>
                                r.day_of_week == dow &&
                                r.type == g.Key.type &&
                                r.start_hour <= current.start.TotalHours &&
                                r.end_hour > current.start.TotalHours);

                            if (rule == null)
                                throw new Exception($"Không có bảng giá cho loại '{g.Key.type}' lúc {current.start} ngày {g.Key.date:dd/MM}");

                            decimal price = (rule.price_per_hour ?? 0) * (decimal)duration;

                            if (i == 0 || current.start == e)
                            {
                                e = current.end;
                                totalPrice += price;
                            }
                            else
                            {
                                merged.Add((s, e, paid, totalPrice));
                                s = current.start;
                                e = current.end;
                                paid = current.is_paid;
                                totalPrice = price;
                            }
                        }

                        merged.Add((s, e, paid, totalPrice));

                        return merged.Select(t => new
                        {
                            g.Key.court_id,
                            g.Key.date,
                            g.Key.type,
                            start_time = t.start,
                            end_time = t.end,
                            is_paid = t.is_paid,
                            price = t.price
                        });
                    }).ToList();

                var courtIds = grouped.Select(g => g.court_id).Distinct().ToList();
                var allOld = db.bookings
                    .Where(b => courtIds.Contains(b.court_id))
                    .ToList(); // thực hiện ToList() để có thể dùng .Date trong LINQ thường

                foreach (var b in grouped)
                {
                    var overlaps = allOld.Where(old =>
                        old.court_id == b.court_id &&
                        old.date.Date == b.date &&
                        (
                            (b.start_time >= old.start_time && b.start_time < old.end_time) ||
                            (b.end_time > old.start_time && b.end_time <= old.end_time) ||
                            (b.start_time <= old.start_time && b.end_time >= old.end_time)
                        )).ToList();

                    foreach (var old in overlaps)
                    {
                        if (old.customer_id == customer.id)
                        {
                            if (b.start_time == old.start_time && b.end_time == old.end_time)
                                goto SkipBooking;
                            else
                                db.bookings.Remove(old);
                        }
                        else
                        {
                            db.bookings.Remove(old);
                        }
                    }

                    db.bookings.Add(new bookings
                    {
                        court_id = b.court_id,
                        customer_id = customer.id,
                        date = b.date,
                        start_time = b.start_time,
                        end_time = b.end_time,
                        type = b.type,
                        price = b.price,
                        is_paid = b.is_paid,
                        created_at = DateTime.Now
                    });

                SkipBooking:;
                }

                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

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

        // Add the missing property 'booking_id' to the BookingRequest class to resolve the error.  
        public class BookingRequest
        {
            public int court_id { get; set; }
            public string date { get; set; }
            public string start_time { get; set; }
            public string end_time { get; set; }
            public string type { get; set; }
            public bool is_paid { get; set; }

            // Add this property to fix the error.  
            public int? booking_id { get; set; }
        }

        // Thêm lịch đặt sân
        [HttpPost]
        public JsonResult AddBooking(int court_id, int customer_id, string date, string start_time, string end_time, string type, decimal price)
        {
            try
            {
                var booking = new bookings
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
    }
}