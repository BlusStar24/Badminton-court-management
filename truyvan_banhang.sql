INSERT INTO mat_hang (ten_hang, don_vi_chinh, don_vi_quy_doi, so_luong_quy_doi, gia_nhap, gia_ban, loai, don_vi)
VALUES 
(N'Nước suối', N'chai', N'thùng', 24, 3000, 5000, N'nước', N'chai'),
(N'Bánh mì', N'gói', NULL, 1, 5000, 8000, N'bánh', N'gói');
INSERT INTO nhap_kho (item_id, so_luong, gia_nhap, don_vi)
VALUES 
(3, 2, 72000, N'thùng'),  -- 2 thùng * 24 chai = 48 chai
(4, 5, 5000, N'gói');      -- 5 gói

INSERT INTO invoices (customer_id, total_amount, note, is_paid)
VALUES (1, 10000, N'Mua hàng tại quầy', 1);

SELECT * FROM fn_xem_ton_kho_chi_tiet();
SELECT * FROM mat_hang
SELECT * FROM nhap_kho

SELECT * FROM customers
SELECT * FROM bookings
SELECT * FROM invoices
SELECT * FROM invoice_details
DELETE FROM bookings WHERE id = 203;



DECLARE @invoiceId INT = SCOPE_IDENTITY();

-- Thêm chi tiết hóa đơn (ví dụ mua 2 chai nước suối và 1 bánh mì)
INSERT INTO invoice_details (invoice_id, item_id, quantity, unit_price)
VALUES 
(@invoiceId, 3, 2, 5000),   -- Nước suối: 2 chai * 5.000
(@invoiceId, 4, 1, 8000); 