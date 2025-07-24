
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QL_SanCauLong.Models
{
    public class ThongBaoModel
    {
        public int Id { get; set; }
        public string HoTen { get; set; }
        public string SoDienThoai { get; set; }
        public string NgayTao { get; set; }
        public decimal TongTien { get; set; }

        public List<BookingInput> ChiTiet { get; set; }
        public string MinhChungChuyenKhoan { get; set; }
        public int? BookingId { get; set; }
        public string IsConfirmed { get; set; } // mặc định false

        // ✅ Dùng chung toàn bộ project
        public static List<ThongBaoModel> DanhSachThongBao = new List<ThongBaoModel>();
    }
}