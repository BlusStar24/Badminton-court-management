using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QL_SanCauLong.Models
{
    public class InvoiceViewModel
    {
        public InvoiceDto Invoice { get; set; }
        public List<InvoiceDetailDto> ChiTiet { get; set; }
    }

    public class InvoiceDto
    {
        public int id { get; set; }
        public string tenKhach { get; set; }
        public DateTime created_at { get; set; }
        public string note { get; set; }
        public string payment_method { get; set; }
        public bool is_paid { get; set; }
        public decimal total_amount { get; set; }
    }

    public class InvoiceDetailDto
    {
        public int item_id { get; set; }
        public string tenHang { get; set; }
        public int quantity { get; set; }
        public decimal unit_price { get; set; }
        public decimal total_price { get; set; }
        public bool is_paid { get; set; }
    }

}