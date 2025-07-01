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
    type NVARCHAR(20) NOT NULL CHECK (type IN (N'giá chẳn', N'giá lẻ', N'pass sân', N'cố định')),
    price DECIMAL(10,2) NOT NULL,
    created_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (customer_id) REFERENCES customers(id),
    FOREIGN KEY (court_id) REFERENCES courts(id)
);
GO
ALTER TABLE bookings ADD is_paid BIT DEFAULT 0;
EXEC sp_help 'bookings';

--Qui định giá
CREATE TABLE price_rules (
    id INT IDENTITY(1,1) PRIMARY KEY,
    day_of_week INT,         -- 0 = CN, 1 = T2, ..., 6 = T7
    start_hour INT,          -- giờ bắt đầu
    end_hour INT,            -- giờ kết thúc
    type NVARCHAR(20),       -- 'cố định' hoặc 'vãng lai'
    price_per_hour DECIMAL(10,2)
);


-- B?ng bán hàng
CREATE TABLE sales (
    id INT IDENTITY(1,1) PRIMARY KEY,
    customer_id INT,
    item_name VARCHAR(100) NOT NULL,
    quantity INT NOT NULL,
    unit_price DECIMAL(10,2) NOT NULL,
    total_price AS (quantity * unit_price) PERSISTED,
    created_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (customer_id) REFERENCES customers(id)
);
GO

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

CREATE TABLE invoice_details (
    id INT IDENTITY(1,1) PRIMARY KEY,
    invoice_id INT NOT NULL,
    item_name NVARCHAR(100) NOT NULL,      -- vd: "Sân số 1", "Vợt cầu lông", "Nước suối"
    quantity INT NOT NULL DEFAULT 1,
    unit_price DECIMAL(10,2) NOT NULL,
    total_price AS (quantity * unit_price) PERSISTED,
    created_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (invoice_id) REFERENCES invoices(id)
);
GO

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
(1, 1, '2025-06-28', '07:00:00', '08:30:00', N'gi? l?', 75000),
(2, 2, '2025-06-28', '07:00:00', '08:00:00', N'gi? ch?n', 98000),
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
