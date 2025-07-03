using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QL_SanCauLong.Models
{
    // Models/ViewModel/KhoViewModel.cs
    public class MatHangViewModel
    {
        public int id { get; set; }
        public string ten_hang { get; set; }
        public string loai { get; set; }
        public string don_vi_chinh { get; set; }
        public string don_vi_quy_doi { get; set; }
        public int? so_luong_quy_doi { get; set; }
        public decimal? gia_nhap { get; set; }
        public decimal? gia_ban { get; set; }
        public int? so_luong_ton { get; set; }
    }

}