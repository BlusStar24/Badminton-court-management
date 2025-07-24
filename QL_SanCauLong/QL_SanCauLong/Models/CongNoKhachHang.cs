using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QL_SanCauLong.Models
{
    public class CongNoKhachHang
    {
        public int customer_id { get; set; }
        public string customer_name { get; set; }
        public decimal no_booking { get; set; }
        public decimal no_hoadon { get; set; }
        public decimal tong_no { get; set; }
    }

}