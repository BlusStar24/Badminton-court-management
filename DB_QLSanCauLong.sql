CREATE DATABASE QuanLySanCauLong;
GO

USE QuanLySanCauLong;
GO
	
-- B?ng khách hàng
CREATE TABLE customers (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    phone VARCHAR(20) UNIQUE NOT NULL,
    password NVARCHAR(100) NOT NULL,
    role NVARCHAR(20) DEFAULT N'customer' CHECK (role IN (N'admin', N'customer')),
    created_at DATETIME DEFAULT GETDATE()
);

-- B?ng sân
CREATE TABLE courts (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    status VARCHAR(20) DEFAULT 'available' CHECK (status IN ('available', 'maintenance')),
    created_at DATETIME DEFAULT GETDATE()
);
GO

-- B?ng nhân viên
CREATE TABLE employees (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    phone VARCHAR(20),
    position VARCHAR(50),
    created_at DATETIME DEFAULT GETDATE()
);
GO

-- B?ng ??t sân
CREATE TABLE bookings (
    id INT IDENTITY(1,1) PRIMARY KEY,
    customer_id INT NOT NULL,
    court_id INT NOT NULL,
    date DATE NOT NULL,
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    type NVARCHAR(20) NOT NULL CHECK (type IN (N'vãng lai', N'pass sân', N'cố định')),
    price DECIMAL(10,2) NOT NULL,
    created_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (customer_id) REFERENCES customers(id),
    FOREIGN KEY (court_id) REFERENCES courts(id)
);
GO
ALTER TABLE bookings ADD is_paid BIT DEFAULT 0;
ALTER TABLE bookings
ADD payment_method NVARCHAR(50) NULL;
ALTER TABLE bookings ADD invoice_id INT NULL;
ALTER TABLE bookings ADD CONSTRAINT FK_bookings_invoices FOREIGN KEY (invoice_id) REFERENCES invoices(id);
ALTER TABLE bookings
ADD is_confirmed BIT DEFAULT 0;


CREATE TABLE rejected_bookings (
    id INT IDENTITY(1,1) PRIMARY KEY,
    booking_id INT NOT NULL,
    customer_id INT NULL,
    court_id INT NULL,
    date DATE NOT NULL,
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    price DECIMAL(18, 2) NOT NULL,
    reason NVARCHAR(255) NULL,
    created_at DATETIME NOT NULL DEFAULT GETDATE()
);


--Qui định giá
CREATE TABLE price_rules (
    id INT IDENTITY(1,1) PRIMARY KEY,
    day_of_week INT,         -- 0 = CN, 1 = T2, ..., 6 = T7
    start_hour INT,          -- giờ bắt đầu
    end_hour INT,            -- giờ kết thúc
    type NVARCHAR(20),       -- 'cố định' hoặc 'vãng lai'
    price_per_hour DECIMAL(10,2)
);

CREATE TABLE mat_hang (
    id INT IDENTITY(1,1) PRIMARY KEY,
    ten_hang NVARCHAR(100) NOT NULL,             -- Ví dụ: "Nước suối"
    don_vi_chinh NVARCHAR(20) NOT NULL,     -- Ví dụ: "chai"
    don_vi_quy_doi NVARCHAR(20),            -- Ví dụ: "thùng"
    so_luong_quy_doi INT DEFAULT 1,         -- Ví dụ: 24 (1 thùng = 24 chai)
    gia_nhap DECIMAL(10,2) NOT NULL,        -- Theo đơn vị chính
    gia_ban DECIMAL(10,2) NOT NULL,         -- Theo đơn vị chính
    loai NVARCHAR(50),                      -- Ví dụ: "nước", "bánh"
    created_at DATETIME DEFAULT GETDATE()
);
ALTER TABLE mat_hang
ADD don_vi NVARCHAR(20) NULL;
ALTER TABLE mat_hang
ADD hinh_anh NVARCHAR(255) NULL;


CREATE TABLE nhap_kho (
    id INT IDENTITY(1,1) PRIMARY KEY,
    item_id INT NOT NULL,
    so_luong INT NOT NULL,
    gia_nhap DECIMAL(10,2) NOT NULL,
    created_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (item_id) REFERENCES mat_hang(id)
);
ALTER TABLE nhap_kho
ADD don_vi NVARCHAR(20) NOT NULL DEFAULT N'chưa rõ';

CREATE TABLE ton_kho (
    item_id INT PRIMARY KEY,
    so_luong_ton INT DEFAULT 0,
    FOREIGN KEY (item_id) REFERENCES mat_hang(id)
);

SELECT * FROM mat_hang WHERE id = 1;

-- B?ng l?ch làm
CREATE TABLE work_schedule (
    id INT IDENTITY(1,1) PRIMARY KEY,
    employee_id INT NOT NULL,
    work_date DATE NOT NULL,
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    note TEXT,
    created_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (employee_id) REFERENCES employees(id)
);
GO

-- B?ng l??ng
CREATE TABLE salary (
    id INT IDENTITY(1,1) PRIMARY KEY,
    employee_id INT NOT NULL,
    month INT NOT NULL,
    year INT NOT NULL,
    total_shift INT DEFAULT 0,
    total_hours DECIMAL(5,2) DEFAULT 0.0,
    base_salary DECIMAL(10,2) NOT NULL,
    bonus DECIMAL(10,2) DEFAULT 0.0,
    total_amount AS (base_salary + bonus) PERSISTED,
    created_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (employee_id) REFERENCES employees(id)
);
GO
CREATE TABLE expenses (
    id INT PRIMARY KEY IDENTITY(1,1),
    title NVARCHAR(255),
    amount BIGINT,
    created_at DATETIME,
    note NVARCHAR(MAX),
    category NVARCHAR(100)
);

CREATE TABLE invoices (
    id INT IDENTITY(1,1) PRIMARY KEY,
    customer_id INT NOT NULL,
    total_amount DECIMAL(10,2) NOT NULL,
    note NVARCHAR(255),
    created_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (customer_id) REFERENCES customers(id)
);
GO
-- Đánh dấu hóa đơn là đã thanh toán
ALTER TABLE invoices
ADD is_paid BIT DEFAULT 0;
ALTER TABLE invoices
ADD payment_method NVARCHAR(50) NULL; -- Ví dụ: 'tiền mặt', 'chuyển khoản', 'momo'
ALTER TABLE invoices
ADD payment_image NVARCHAR(255) NULL; -- Lưu đường dẫn file


CREATE TABLE invoice_details (
    id INT IDENTITY(1,1) PRIMARY KEY,
    invoice_id INT NOT NULL,
    item_id INT NOT NULL,                 -- liên kết với mat_hang
    quantity INT NOT NULL DEFAULT 1,
    unit_price DECIMAL(10,2) NOT NULL,
    total_price AS (quantity * unit_price) PERSISTED,
    created_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (invoice_id) REFERENCES invoices(id),
    FOREIGN KEY (item_id) REFERENCES mat_hang(id)
);
ALTER TABLE invoice_details
ADD is_paid BIT DEFAULT 0;
ALTER TABLE invoice_details ADD booking_id INT NULL;
ALTER TABLE invoice_details 
ADD CONSTRAINT FK_invoice_details_booking 
FOREIGN KEY (booking_id) REFERENCES bookings(id);


--=======================================================================
--Trigger sau khi nhập kho
CREATE TRIGGER trg_nhap_kho_after_insert
ON nhap_kho
AFTER INSERT
AS
BEGIN
    UPDATE ton_kho
    SET so_luong_ton = ISNULL(so_luong_ton, 0) + i.so_luong
    FROM ton_kho t
    JOIN inserted i ON t.item_id = i.item_id;

    -- Nếu chưa có trong tồn kho thì thêm mới
    INSERT INTO ton_kho(item_id, so_luong_ton)
    SELECT i.item_id, i.so_luong
    FROM inserted i
    WHERE NOT EXISTS (
        SELECT 1 FROM ton_kho WHERE item_id = i.item_id
    );
END
--==========================================================================================
--Trigger sau khi bán hàng
ALTER TRIGGER trg_nhap_kho_after_insert
ON nhap_kho
AFTER INSERT
AS
BEGIN
    -- Tính số lượng quy đổi về đơn vị chính
    UPDATE ton_kho
    SET so_luong_ton = ISNULL(t.so_luong_ton, 0) +
        (CASE 
            WHEN i.don_vi = mh.don_vi_quy_doi THEN i.so_luong * mh.so_luong_quy_doi
            ELSE i.so_luong
        END)
    FROM ton_kho t
    JOIN inserted i ON t.item_id = i.item_id
    JOIN mat_hang mh ON i.item_id = mh.id;

    -- Thêm mới nếu chưa có
    INSERT INTO ton_kho(item_id, so_luong_ton)
    SELECT 
        i.item_id, 
        CASE 
            WHEN i.don_vi = mh.don_vi_quy_doi THEN i.so_luong * mh.so_luong_quy_doi
            ELSE i.so_luong
        END
    FROM inserted i
    JOIN mat_hang mh ON i.item_id = mh.id
    WHERE NOT EXISTS (SELECT 1 FROM ton_kho WHERE item_id = i.item_id);
END
ALTER TRIGGER trg_nhap_kho_after_insert
ON nhap_kho
AFTER INSERT
AS
BEGIN
    -- Cập nhật số lượng nếu đã tồn tại
    UPDATE ton_kho
    SET so_luong_ton = ISNULL(t.so_luong_ton, 0) +
        (CASE 
            WHEN i.don_vi = mh.don_vi_quy_doi THEN i.so_luong * mh.so_luong_quy_doi
            ELSE i.so_luong
        END)
    FROM ton_kho t
    JOIN inserted i ON t.item_id = i.item_id
    JOIN mat_hang mh ON i.item_id = mh.id;

    -- Thêm mới nếu chưa có
    INSERT INTO ton_kho(item_id, so_luong_ton)
    SELECT 
        i.item_id, 
        CASE 
            WHEN i.don_vi = mh.don_vi_quy_doi THEN i.so_luong * mh.so_luong_quy_doi
            ELSE i.so_luong
        END
    FROM inserted i
    JOIN mat_hang mh ON i.item_id = mh.id
    WHERE NOT EXISTS (
        SELECT 1 FROM ton_kho t WHERE t.item_id = i.item_id
    );
END

--=============================================================================================================
CREATE TRIGGER trg_ban_hang_after_insert
ON invoice_details
AFTER INSERT
AS
BEGIN
    UPDATE ton_kho
    SET so_luong_ton = ISNULL(so_luong_ton, 0) - i.quantity
    FROM ton_kho t
    JOIN inserted i ON t.item_id = i.item_id;
END
--============================================================================================================
CREATE OR ALTER FUNCTION fn_xem_ton_kho_chi_tiet()
RETURNS TABLE
AS
RETURN
(
    SELECT 
        mh.id AS item_id,
        mh.ten_hang,
        mh.loai,
        mh.don_vi_chinh,
        mh.don_vi_quy_doi,
        mh.so_luong_quy_doi,
        mh.gia_nhap,
        mh.gia_ban,
        tk.so_luong_ton AS tong_so_luong_ton,
        -- Tính số thùng
        CASE 
            WHEN mh.so_luong_quy_doi > 1 THEN FLOOR(tk.so_luong_ton * 1.0 / mh.so_luong_quy_doi)
            ELSE NULL
        END AS so_thung,
        -- Số lẻ còn lại
        CASE 
            WHEN mh.so_luong_quy_doi > 1 THEN tk.so_luong_ton % mh.so_luong_quy_doi
            ELSE tk.so_luong_ton
        END AS le_don_vi
    FROM ton_kho tk
    JOIN mat_hang mh ON tk.item_id = mh.id
);
--==================================================================================================
CREATE PROCEDURE sp_XoaNhapKhoHetTonQua1Thang
AS
BEGIN
    SET NOCOUNT ON;

    DELETE nk
    FROM nhap_kho nk
    WHERE nk.item_id IN (
        SELECT tk.item_id
        FROM ton_kho tk
        WHERE tk.so_luong_ton = 0
    )
    AND nk.created_at < DATEADD(MONTH, -1, GETDATE());
END

--===================================================================================================
CREATE TRIGGER trg_UpdateInvoiceIsPaid
ON invoice_details
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Lấy các invoice_id bị ảnh hưởng
    DECLARE @invoice_ids TABLE (id INT);
    INSERT INTO @invoice_ids(id)
    SELECT DISTINCT invoice_id
    FROM inserted;

    -- Duyệt từng hóa đơn để kiểm tra
    UPDATE invoices
    SET is_paid = 1
    FROM invoices i
    WHERE i.id IN (SELECT id FROM @invoice_ids)
      AND NOT EXISTS (
          SELECT 1 FROM invoice_details d
          WHERE d.invoice_id = i.id AND (d.is_paid IS NULL OR d.is_paid = 0)
      );
END;


--====================================================================================================

CREATE TRIGGER trg_delete_booking_cascade
ON bookings
AFTER DELETE
AS
BEGIN
    -- Xóa chi tiết hóa đơn liên quan đến booking đã xóa
    DELETE d
    FROM invoice_details d
    JOIN deleted b ON d.invoice_id = b.invoice_id
    WHERE d.item_id = 1 AND d.total_price = b.price;

    -- Xóa hóa đơn nếu không còn chi tiết nào
    DELETE i
    FROM invoices i
    WHERE NOT EXISTS (
        SELECT 1 FROM invoice_details d WHERE d.invoice_id = i.id
    );
END
go
--=====================================================================================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_TinhNoChiTietKhachHang')
    DROP PROCEDURE dbo.sp_TinhNoChiTietKhachHang;
GO


CREATE PROCEDURE sp_TinhNoChiTietKhachHang
AS
BEGIN
    SET NOCOUNT ON;

    WITH BookingNo AS (
        SELECT 
            b.id AS booking_id,
            b.customer_id,
            c.name AS customer_name,
            b.price AS gia_booking
        FROM bookings b
        JOIN customers c ON c.id = b.customer_id
        WHERE b.is_paid = 0 AND b.payment_method = N'Nợ'
    ),

    CongNoBooking AS (
        SELECT 
            customer_id,
            MAX(customer_name) AS customer_name,
            SUM(gia_booking) AS no_booking
        FROM BookingNo
        GROUP BY customer_id
    ),

    CongNoHoaDon AS (
        SELECT 
            i.customer_id,
            MAX(c.name) AS customer_name,
            SUM(d.total_price) AS no_hoadon
        FROM invoices i
        JOIN customers c ON c.id = i.customer_id
        JOIN invoice_details d ON d.invoice_id = i.id
        WHERE i.is_paid = 0 AND d.item_id <> 1
        GROUP BY i.customer_id
    )

    SELECT 
        COALESCE(cb.customer_id, ch.customer_id) AS customer_id,
        COALESCE(cb.customer_name, ch.customer_name) AS customer_name,
        ISNULL(cb.no_booking, 0) AS no_booking,
        ISNULL(ch.no_hoadon, 0) AS no_hoadon,
        ISNULL(cb.no_booking, 0) + ISNULL(ch.no_hoadon, 0) AS tong_no
    FROM CongNoBooking cb
    FULL OUTER JOIN CongNoHoaDon ch ON cb.customer_id = ch.customer_id
    WHERE ISNULL(cb.no_booking, 0) + ISNULL(ch.no_hoadon, 0) > 0
    ORDER BY tong_no DESC;
END


EXEC sp_TinhNoChiTietKhachHang;

--=====================================================================================================
-- Thêm khách hàng
INSERT INTO customers (name, phone, role) VALUES 
(N'Nguy?n V?n A', '0901234567', N'customer'),
(N'Nguy?n Th? C', '0901255558', N'customer'),
(N'Admin Phú Th?nh', '0987654321', N'admin');
INSERT INTO customers (name, phone, role) VALUES 
(N'Nguyễn Văn H', '0901234123', N'customer');
SELECT name, phone, role FROM customers WHERE phone = '0901234123'

-- Thêm sân
INSERT INTO courts (name, status) VALUES 
(N'Sân 1', 'available'),
(N'Sân 2', 'available'),
(N'Sân 3', 'available'),
(N'Sân 4', 'available'),
(N'Sân 5', 'available'),
(N'Sân 6', 'available');
-- Thêm nhân viên
INSERT INTO employees (name, phone, position) VALUES
(N'Tr?n Th? B?o', '0912345678', N'Admin'),
(N'Lê V?n Chung', '0923456789', N'Gi? xe');

-- Thêm l??t ??t sân
INSERT INTO bookings (customer_id, court_id, date, start_time, end_time, type, price) VALUES
(1, 1, '2025-06-28', '07:00:00', '08:30:00', N'vãng lai', 75000),
(2, 2, '2025-06-28', '07:00:00', '08:00:00', N'cố định', 98000),
(1, 3, '2025-06-28', '08:00:00', '09:00:00', N'pass sân', 50000);

-- Thêm giao d?ch bán hàng
INSERT INTO sales (customer_id, item_name, quantity, unit_price) VALUES
(1, N'N??c su?i', 2, 10000),
(1, N'V? c?u lông', 1, 25000);

-- Thêm l?ch làm vi?c
INSERT INTO work_schedule (employee_id, work_date, start_time, end_time, note) VALUES
(1, '2025-06-28', '07:00:00', '12:00:00', N'Tr?c sáng'),
(2, '2025-06-28', '12:00:00', '18:00:00', N'Tr?c chi?u');

-- Thêm d? li?u l??ng
INSERT INTO salary (employee_id, month, year, total_shift, total_hours, base_salary, bonus) VALUES
(1, 6, 2025, 20, 120.0, 5000000, 500000),
(2, 6, 2025, 18, 108.0, 4500000, 400000);

-- Dữ liệu cho type = 'vãng lai'
INSERT INTO price_rules (day_of_week, start_hour, end_hour, type, price_per_hour) VALUES
(0, 5, 17,	N'vãng lai', 49000),
(0, 17, 18, N'vãng lai', 90000),
(0, 18, 24, N'vãng lai', 110000),
			
(1, 5, 17,	N'vãng lai', 49000),
(1, 17, 18, N'vãng lai', 90000),
(1, 18, 24, N'vãng lai', 110000),
			
(2, 5, 17,	N'vãng lai', 49000),
(2, 17, 18, N'vãng lai', 90000),
(2, 18, 24, N'vãng lai', 110000),
			
(3, 5, 17,	N'vãng lai', 49000),
(3, 17, 18, N'vãng lai', 90000),
(3, 18, 24, N'vãng lai', 110000),
			
(4, 5, 17,	N'vãng lai', 49000),
(4, 17, 18, N'vãng lai', 90000),
(4, 18, 24, N'vãng lai', 110000),
			
(5, 5, 17,	N'vãng lai', 49000),
(5, 17, 18, N'vãng lai', 90000),
(5, 18, 24, N'vãng lai', 110000),
			
(6, 5, 17,	N'vãng lai', 89000),
(6, 17, 18, N'vãng lai', 89000),
(6, 18, 24, N'vãng lai', 89000);

-- Dữ liệu cho type = 'cố định'
INSERT INTO price_rules (day_of_week, start_hour, end_hour, type, price_per_hour) VALUES
(0, 5, 17,	N'cố định', 89000),
(0, 17, 18, N'cố định', 89000),
(0, 18, 24, N'cố định', 89000),

(1, 5, 17,	N'cố định', 49000),
(1, 17, 18, N'cố định', 80000),
(1, 18, 24, N'cố định', 100000),

(2, 5, 17,	N'cố định', 49000),
(2, 17, 18, N'cố định', 80000),
(2, 18, 24, N'cố định', 100000),

(3, 5, 17,	N'cố định', 49000),
(3, 17, 18, N'cố định', 80000),
(3, 18, 24, N'cố định', 100000),

(4, 5, 17,	N'cố định', 49000),
(4, 17, 18, N'cố định', 80000),
(4, 18, 24, N'cố định', 100000),

(5, 5, 17,	N'cố định', 49000),
(5, 17, 18, N'cố định', 80000),
(5, 18, 24, N'cố định', 100000),

(6, 5, 17,	N'cố định', 89000),
(6, 17, 18, N'cố định', 89000),
(6, 18, 24, N'cố định', 89000);

-- Dữ liệu cho type = 'vãng lai'
INSERT INTO price_rules (day_of_week, start_hour, end_hour, type, price_per_hour) VALUES
(0, 5, 17,	N'pass sân', 49000),
(0, 17, 18, N'pass sân', 90000),
(0, 18, 24, N'pass sân', 110000),
			 
(1, 5, 17,	N'pass sân', 49000),
(1, 17, 18, N'pass sân', 90000),
(1, 18, 24, N'pass sân', 110000),
			
(2, 5, 17,	N'pass sân', 49000),
(2, 17, 18, N'pass sân', 90000),
(2, 18, 24, N'pass sân', 110000),
		
(3, 5, 17,	N'pass sân', 49000),
(3, 17, 18, N'pass sân', 90000),
(3, 18, 24, N'pass sân', 110000),
			 
(4, 5, 17,	N'pass sân', 49000),
(4, 17, 18, N'pass sân', 90000),
(4, 18, 24, N'pass sân', 110000),
			  
(5, 5, 17,	N'pass sân', 49000),
(5, 17, 18, N'pass sân', 90000),
(5, 18, 24, N'pass sân', 110000),
			 
(6, 5, 17,	N'pass sân', 89000),
(6, 17, 18, N'pass sân', 89000),
(6, 18, 24, N'pass sân', 89000);
