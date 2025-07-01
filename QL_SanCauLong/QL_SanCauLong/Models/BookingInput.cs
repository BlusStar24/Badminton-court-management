using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanLySanCauLong.Models
{
    public class BookingInput
    {
        public string date { get; set; }
        public string DayName { get; set; }
        public string court { get; set; }
        public string start { get; set; }
        public string end { get; set; }
        public string type { get; set; }
        public long price { get; set; }
    }
}