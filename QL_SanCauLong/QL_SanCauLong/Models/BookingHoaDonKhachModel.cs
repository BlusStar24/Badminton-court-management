using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QL_SanCauLong.Models
{
    public class BookingHoaDonKhachModel
    {
        public int Id { get; set; }
        public string Date { get; set; }
        public string TimeRange { get; set; }
        public string Court { get; set; }
        public decimal Price { get; set; }
        public string Type { get; set; }
        public string IsPaid { get; set; }
        public string PaymentMethod { get; set; }
        public int? InvoiceId { get; set; }
        public string IsConfirmed { get; set; }

    }
}