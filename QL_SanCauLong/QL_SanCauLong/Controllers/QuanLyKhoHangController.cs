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
        private QuanLySanCauLongdbEntities db = new QuanLySanCauLongdbEntities();

        // GET: QuanLyKhoHang
        public ActionResult Index(int? idDangSua)
        {
            var list = (from mh in db.mat_hang
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
                            gia_nhap = mh.gia_nhap,
                            gia_ban = mh.gia_ban,
                            so_luong_ton = tk.so_luong_ton
                        }).ToList();

            ViewBag.IdDangSua = idDangSua;
            return View(list);
        }
        // Cap Nhat: QuanLyKhoHang/CapNhat
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
            }
            return RedirectToAction("Index");
        }

        // Delete: QuanLyKhoHang/Xoa
        [HttpPost]
        public ActionResult Xoa(int id)
        {
            var matHang = db.mat_hang.Find(id);
            if (matHang != null)
            {
                db.mat_hang.Remove(matHang);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        // Nhập kho: QuanLyKhoHang/NhapKho
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
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["LoiNhapKho"] = "❌ Lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // Xem tồn kho chi tiết
        public ActionResult XemTonKho()
        {
            var result = db.fn_xem_ton_kho_chi_tiet().ToList();
            return View(result);
        }

    }
}