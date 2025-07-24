INSERT INTO mat_hang (ten_hang, don_vi_chinh, don_vi_quy_doi, so_luong_quy_doi, gia_nhap, gia_ban, loai, don_vi)
VALUES 
(N'Nước suối', N'chai', N'thùng', 24, 3000, 5000, N'nước', N'chai'),
(N'Bánh mì', N'gói', NULL, 1, 5000, 8000, N'bánh', N'gói');
INSERT INTO nhap_kho (item_id, so_luong, gia_nhap, don_vi)
VALUES 
(3, 2, 72000, N'thùng'),  -- 2 thùng * 24 chai = 48 chai
(4, 5, 5000, N'gói');      -- 5 gói

SELECT * FROM invoice_details WHERE id = 406;
SELECT * FROM invoices WHERE id = 317;
	
	SELECT @@DATEFIRST AS FirstDayOfWeek,
       DATENAME(WEEKDAY, '2025-07-21') AS WeekdayName,
       DATEPART(WEEKDAY, '2025-07-21') AS WeekdayNumber

SELECT * FROM nhap_kho

SELECT * FROM customers
SELECT * FROM mat_hang
SELECT * FROM ton_kho
SELECT * FROM nhap_kho
SELECT * FROM bookings
SELECT * FROM invoices
SELECT * FROM invoice_details
EXEC sp_TinhNoChiTietKhachHang;
SELECT * FROM price_rules
DELETE FROM bookings WHERE id = 203;
DROP PROCEDURE sp_TinhNoChiTietKhachHang;

SELECT * FROM bookings WHERE is_paid = 0;
SELECT * FROM invoices WHERE id = 95;

DECLARE @invoiceId INT = SCOPE_IDENTITY();

-- Thêm chi tiết hóa đơn (ví dụ mua 2 chai nước suối và 1 bánh mì)
INSERT INTO invoice_details (invoice_id, item_id, quantity, unit_price)
VALUES 
(@invoiceId, 3, 2, 5000),   -- Nước suối: 2 chai * 5.000
(@invoiceId, 4, 1, 8000); 

SELECT *
FROM invoice_details ct
JOIN invoices inv ON ct.invoice_id = inv.id
JOIN customers c ON inv.customer_id = c.id
WHERE c.name = 'XuanCuong'
  AND ct.is_paid = 0
  AND ct.item_id != 0
  AND ct.item_id != 1;
