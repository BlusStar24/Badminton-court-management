
using QL_SanCauLong.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QL_SanCauLong.Controllers
{
    public class QuanLyKhoHangController : Controller
    {
        private QuanLySanCauLongEntities3 db = new QuanLySanCauLongEntities3();

        public class IndexViewModel
        {
            public List<MatHangViewModel> MatHangList { get; set; }
            public List<NhapKhoViewModel> NhapKhoList { get; set; }
            public List<fn_xem_ton_kho_chi_tiet_Result> TonKhoList { get; set; }
        }

        // GET: QuanLyKhoHang
        [Authorize]
        [AdminOnlyController.AdminOnly]
        public ActionResult Index(int? idDangSuaMatHang, int? idDangSuaNhapKho)
        {
            var model = new IndexViewModel
            {
                MatHangList = (from mh in db.mat_hang
                               join tk in db.ton_kho on mh.id equals tk.item_id into gj
                               from tk in gj.DefaultIfEmpty()
                               select new MatHangViewModel
                               {
                                   id = mh.id,
                                   ten_hang = mh.ten_hang,
                                   loai = mh.loai,
                                   don_vi_chinh = mh.don_vi_chinh,
                                   don_vi_quy_doi = mh.don_vi_quy_doi,
                                   so_luong_quy_doi = mh.so_luong_quy_doi,
                                   gia_nhap = (decimal)mh.gia_nhap,
                                   gia_ban = mh.gia_ban,
                                   so_luong_ton = (decimal)(tk != null ? tk.so_luong_ton : 0)
                               }).ToList(),

                NhapKhoList = (from nk in db.nhap_kho
                               join mh in db.mat_hang on nk.item_id equals mh.id
                               select new NhapKhoViewModel
                               {
                                   id = nk.id,
                                   item_id = nk.item_id,
                                   ten_hang = mh.ten_hang,
                                   so_luong = nk.so_luong,
                                   gia_nhap = nk.gia_nhap,
                                   don_vi = nk.don_vi,
                                   created_at = (DateTime)nk.created_at
                               }).OrderByDescending(x => x.created_at).ToList(),

                TonKhoList = db.fn_xem_ton_kho_chi_tiet().ToList()
            };

            ViewBag.IdDangSuaMatHang = idDangSuaMatHang;
            ViewBag.IdDangSuaNhapKho = idDangSuaNhapKho;
            return View(model);
        }

        // Thêm mặt hàng mới
        [HttpPost]
        public ActionResult ThemMatHang(mat_hang mh)
        {
            try
            {
                db.mat_hang.Add(mh);
                db.SaveChanges();
                TempData["ThongBao"] = "✅ Thêm mặt hàng thành công!";
            }
            catch (Exception ex)
            {
                TempData["LoiNhapKho"] = "❌ Lỗi khi thêm mặt hàng: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // Cập nhật mặt hàng
        [HttpPost]
        public ActionResult CapNhat(mat_hang mh)
        {
            var entity = db.mat_hang.Find(mh.id);
            if (entity != null)
            {
                entity.ten_hang = mh.ten_hang;
                entity.loai = mh.loai;
                entity.don_vi_chinh = mh.don_vi_chinh;
                entity.don_vi_quy_doi = mh.don_vi_quy_doi;
                entity.so_luong_quy_doi = mh.so_luong_quy_doi;
                entity.gia_nhap = mh.gia_nhap;
                entity.gia_ban = mh.gia_ban;

                db.SaveChanges();
                TempData["ThongBao"] = "✅ Cập nhật mặt hàng thành công!";
            }
            else
            {
                TempData["LoiNhapKho"] = "❌ Không tìm thấy mặt hàng để cập nhật.";
            }
            return RedirectToAction("Index");
        }

        // Xóa mặt hàng
        [HttpPost]
        public ActionResult Xoa(int id)
        {
            var matHang = db.mat_hang.Find(id);
            if (matHang != null)
            {
                db.mat_hang.Remove(matHang);
                db.SaveChanges();
                TempData["ThongBao"] = "🗑️ Đã xóa mặt hàng thành công!";
            }
            else
            {
                TempData["LoiNhapKho"] = "❌ Không tìm thấy mặt hàng để xóa.";
            }
            return RedirectToAction("Index");
        }

        // Nhập kho
        [HttpPost]
        public ActionResult NhapKho(int item_id, int so_luong, decimal gia_nhap, string don_vi)
        {
            try
            {
                var nhap = new nhap_kho
                {
                    item_id = item_id,
                    so_luong = so_luong,
                    gia_nhap = gia_nhap,
                    don_vi = don_vi,
                    created_at = DateTime.Now
                };
                db.nhap_kho.Add(nhap);
                db.SaveChanges();
                TempData["ThongBao"] = "✅ Nhập kho thành công!";
            }
            catch (Exception ex)
            {
                TempData["LoiNhapKho"] = "❌ Lỗi khi nhập kho: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // Cập nhật nhập kho
        [HttpPost]
        public ActionResult CapNhatNhapKho(NhapKhoViewModel model)
        {
            var entity = db.nhap_kho.Find(model.id);
            if (entity != null)
            {
                entity.so_luong = model.so_luong;
                entity.gia_nhap = model.gia_nhap;
                entity.don_vi = model.don_vi;
                db.SaveChanges();
                TempData["ThongBao"] = "✅ Cập nhật nhập kho thành công!";
            }
            else
            {
                TempData["LoiNhapKho"] = "❌ Không tìm thấy bản ghi nhập kho để cập nhật.";
            }
            return RedirectToAction("Index");
        }

        // Xóa nhập kho
        [HttpPost]
        public ActionResult XoaNhapKho(int id)
        {
            var entity = db.nhap_kho.Find(id);
            if (entity != null)
            {
                db.nhap_kho.Remove(entity);
                db.SaveChanges();
                TempData["ThongBao"] = "🗑️ Đã xóa nhập kho thành công!";
            }
            else
            {
                TempData["LoiNhapKho"] = "❌ Không tìm thấy bản ghi nhập kho để xóa.";
            }
            return RedirectToAction("Index");
        }

        // Xóa nhập kho hết tồn quá 1 tháng
        [HttpPost]
        public ActionResult XoaNhapKhoHetTon()
        {
            try
            {
                db.Database.ExecuteSqlCommand("EXEC sp_XoaNhapKhoHetTonQua1Thang");
                TempData["ThongBao"] = "🧹 Đã dọn dẹp kho hết hàng quá 1 tháng!";
            }
            catch (Exception ex)
            {
                TempData["LoiNhapKho"] = "❌ Lỗi khi xóa nhập kho hết tồn: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // Xem tồn kho chi tiết
        public ActionResult XemTonKho()
        {
            var result = db.fn_xem_ton_kho_chi_tiet().ToList();
            return View(result);
        }

        public class MatHangViewModel
        {
            public int id { get; set; }
            public string ten_hang { get; set; }
            public string loai { get; set; }
            public string don_vi_chinh { get; set; }
            public string don_vi_quy_doi { get; set; }
            public int? so_luong_quy_doi { get; set; }
            public decimal gia_nhap { get; set; }
            public decimal gia_ban { get; set; }
            public decimal so_luong_ton { get; set; }
        }

        public class NhapKhoViewModel
        {
            public int id { get; set; }
            public string ten_hang { get; set; }
            public int item_id { get; set; }
            public int so_luong { get; set; }
            public decimal gia_nhap { get; set; }
            public string don_vi { get; set; }
            public DateTime created_at { get; set; }
        }
    }
}
