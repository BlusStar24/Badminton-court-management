
-- File tạo toàn bộ cơ sở dữ liệu hệ thống quản lý sân cầu lông
-- Bao gồm: khách hàng, sân, đặt sân, hàng hóa, kho, hóa đơn, nhân viên, lịch làm, lương

CREATE DATABASE QuanLySanCauLong;
GO

USE QuanLySanCauLong;
GO

-- Bảng khách hàng
CREATE TABLE customers (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(100) NOT NULL,
    phone VARCHAR(20) UNIQUE NOT NULL,
    password NVARCHAR(100) NOT NULL,
    role NVARCHAR(20) DEFAULT N'customer' CHECK (role IN (N'admin', N'customer')),
    created_at DATETIME DEFAULT GETDATE()
);

-- Bảng sân
CREATE TABLE courts (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    status VARCHAR(20) DEFAULT 'available' CHECK (status IN ('available', 'maintenance')),
    created_at DATETIME DEFAULT GETDATE()
);
GO

-- Bảng nhân viên
CREATE TABLE employees (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    phone VARCHAR(20),
    position VARCHAR(50),
    created_at DATETIME DEFAULT GETDATE()
);
GO

-- Bảng đặt sân
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
    is_paid BIT DEFAULT 0,
    FOREIGN KEY (customer_id) REFERENCES customers(id),
    FOREIGN KEY (court_id) REFERENCES courts(id)
);
GO

-- Bảng quy định giá theo giờ và loại
CREATE TABLE price_rules (
    id INT IDENTITY(1,1) PRIMARY KEY,
    day_of_week INT,         -- 0 = CN, 1 = T2, ..., 6 = T7
    start_hour INT,
    end_hour INT,
    type NVARCHAR(20),       -- 'cố định' hoặc 'vãng lai'
    price_per_hour DECIMAL(10,2)
);

-- Bảng mặt hàng
CREATE TABLE mat_hang (
    id INT IDENTITY(1,1) PRIMARY KEY,
    ten NVARCHAR(100) NOT NULL,
    don_vi_chinh NVARCHAR(20) NOT NULL,
    don_vi_quy_doi NVARCHAR(20),
    so_luong_quy_doi INT DEFAULT 1,
    gia_nhap DECIMAL(10,2) NOT NULL,
    gia_ban DECIMAL(10,2) NOT NULL,
    loai NVARCHAR(50),
    created_at DATETIME DEFAULT GETDATE()
);

-- Nhập kho
CREATE TABLE nhap_kho (
    id INT IDENTITY(1,1) PRIMARY KEY,
    item_id INT NOT NULL,
    so_luong INT NOT NULL,
    gia_nhap DECIMAL(10,2) NOT NULL,
    don_vi NVARCHAR(20) NOT NULL DEFAULT N'chưa rõ',
    created_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (item_id) REFERENCES mat_hang(id)
);

-- Tồn kho
CREATE TABLE ton_kho (
    item_id INT PRIMARY KEY,
    so_luong_ton INT DEFAULT 0,
    FOREIGN KEY (item_id) REFERENCES mat_hang(id)
);

-- Bảng lịch làm
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

-- Bảng lương
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

-- Hóa đơn và chi tiết hóa đơn
CREATE TABLE invoices (
    id INT IDENTITY(1,1) PRIMARY KEY,
    customer_id INT NOT NULL,
    total_amount DECIMAL(10,2) NOT NULL,
    note NVARCHAR(255),
    created_at DATETIME DEFAULT GETDATE(),
    is_paid BIT DEFAULT 0,
    FOREIGN KEY (customer_id) REFERENCES customers(id)
);

CREATE TABLE invoice_details (
    id INT IDENTITY(1,1) PRIMARY KEY,
    invoice_id INT NOT NULL,
    item_id INT NOT NULL,
    quantity INT NOT NULL DEFAULT 1,
    unit_price DECIMAL(10,2) NOT NULL,
    total_price AS (quantity * unit_price) PERSISTED,
    created_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (invoice_id) REFERENCES invoices(id),
    FOREIGN KEY (item_id) REFERENCES mat_hang(id)
);

-- Trigger cập nhật tồn kho khi nhập
CREATE TRIGGER trg_nhap_kho_after_insert
ON nhap_kho
AFTER INSERT
AS
BEGIN
    -- Nếu tồn tại rồi thì cập nhật số lượng
    UPDATE ton_kho
    SET so_luong_ton = ISNULL(t.so_luong_ton, 0) +
        (CASE 
            WHEN i.don_vi = mh.don_vi_quy_doi THEN i.so_luong * mh.so_luong_quy_doi
            ELSE i.so_luong
        END)
    FROM ton_kho t
    JOIN inserted i ON t.item_id = i.item_id
    JOIN mat_hang mh ON i.item_id = mh.id;

    -- Nếu chưa tồn tại thì thêm mới
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
GO

-- Trigger trừ tồn kho khi bán
CREATE TRIGGER trg_ban_hang_after_insert
ON invoice_details
AFTER INSERT
AS
BEGIN
    UPDATE ton_kho
    SET so_luong_ton = ISNULL(t.so_luong_ton, 0) - i.quantity
    FROM ton_kho t
    JOIN inserted i ON t.item_id = i.item_id;
END
GO
