using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QL_SanCauLong.Models
{
    public class MatHangThongKe
    {
        public string TenHang { get; set; }
        public int SoLuong { get; set; }
        public decimal TongTien { get; set; }
        public string HinhAnh { get; set; }
    }

    public class DashboardViewModel
    {
        public int TongLuotDatSan { get; set; }
        public int SoSanDangHoatDong { get; set; }

        public decimal DoanhThuNgay_TienMat { get; set; }
        public decimal DoanhThuNgay_ChuyenKhoan { get; set; }

        public decimal DoanhThuThang_TienMat { get; set; }
        public decimal DoanhThuThang_ChuyenKhoan { get; set; }
        public decimal TongCongNoTatCaKhach { get; set; }
        public decimal DoanhThuHomNay => DoanhThuNgay_TienMat + DoanhThuNgay_ChuyenKhoan;

        public List<CongNoKhachHang> CongNoKhachHang { get; set; }

        public List<MatHangThongKe> MatHangBanTrongNgay { get; set; } = new List<MatHangThongKe>();
        public List<MatHangThongKe> MatHangBanTrongThang { get; set; } = new List<MatHangThongKe>();
    }

}