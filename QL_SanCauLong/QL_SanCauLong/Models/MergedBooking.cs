using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QL_SanCauLong.Models
{
    public class MergedBooking
    {
        public DateTime Date { get; set; }
        public int CourtId { get; set; }
        public string Type { get; set; }
        public string PaymentMethod { get; set; }
        public bool IsPaid { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public List<decimal?> ManualPrices { get; set; }
    }

}