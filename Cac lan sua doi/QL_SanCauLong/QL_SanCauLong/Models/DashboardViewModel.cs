using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QL_SanCauLong.Models
{
    public class DashboardViewModel
    {
        public int TongLuotDatSan { get; set; }
        public int SoSanDangHoatDong { get; set; }

        public decimal DoanhThuNgay_TienMat { get; set; }
        public decimal DoanhThuNgay_ChuyenKhoan { get; set; }

        public decimal DoanhThuThang_TienMat { get; set; }
        public decimal DoanhThuThang_ChuyenKhoan { get; set; }
        public decimal DoanhThuHomNay => DoanhThuNgay_TienMat + DoanhThuNgay_ChuyenKhoan;
        public List<sp_TinhNoChiTietKhachHang_Result> CongNoKhachHang { get; set; }

    }

}