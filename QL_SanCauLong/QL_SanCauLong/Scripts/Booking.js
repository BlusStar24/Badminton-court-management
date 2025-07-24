

let lastBookingTimestamp = "";



function checkBookingChanges() {
    $.getJSON("/Booking/GetLastBookingTimestamp", function (res) {
        if (!res.lastUpdated) return;

        if (res.lastUpdated !== lastBookingTimestamp) {
            console.log("🔄 Có lịch mới:", res.lastUpdated);
            lastBookingTimestamp = res.lastUpdated;
            renderBookingTable();
        } else {
            console.log("✅ Không có thay đổi.");
        }
    });
}


        // Hàm định dạng giờ
        function formatTime(hour) {
            const h = String(Math.floor(hour)).padStart(2, '0');
            const m = hour % 1 === 0.5 ? '30' : '00';
            return `${h}:${m}`;
        }

        // Hàm định dạng ngày tháng năm
        function formatDateToYMD(date) {
            const y = date.getFullYear();
            const m = String(date.getMonth() + 1).padStart(2, '0');
            const d = String(date.getDate()).padStart(2, '0');
            return `${y}-${m}-${d}`;
        }
        // Hàm hiển thị/ẩn dropdown nền tảng
function togglePlatformDropdown() {
    const dropdown = document.getElementById('platformDropdown');
    dropdown.classList.toggle('hidden');
}

        function appendPlatform(platform) {
            const nameInput = document.getElementById('nameInput');
            const baseName = nameInput.value.trim().replace(/\s+(zalo|facebook)$/i, '');
            nameInput.value = `${baseName} ${platform}`;
            document.getElementById('platformDropdown').classList.add('hidden');
        }

let isMouseDown = false;
let selectedCells = [];
let lastClickTime = 0;
let bookingsCache = [];

        // Hàm chọn/bỏ chọn ô
function toggleCellSelection(cell, isCtrlPressed) {
    console.log("🔍 Gọi toggleCellSelection:", {
        court: cell.dataset.court,
        date: cell.dataset.date,
        hour: cell.dataset.hour,
        isCtrlPressed,
        hasCustomCell: cell.classList.contains('custom-cell')
    });

    if (!cell.classList.contains('custom-cell')) {
        console.warn("❌ Ô không có class custom-cell:", cell);
        return;
    }

    // Nếu đang chọn giờ lẻ, tự động chọn cả ô giờ tiếp theo
    const hour = parseFloat(cell.dataset.hour);
    if (!cell.classList.contains("selected-cell")) {
        const nextSelector = `td[data-court="${cell.dataset.court}"][data-date="${cell.dataset.date}"][data-hour="${(hour + 0.5).toFixed(1)}"]`;
        const nextCell = document.querySelector(nextSelector);
        if (hour % 1 === 0 && nextCell && !nextCell.classList.contains('selected-cell')) {
            toggleCellSelection(nextCell, true);
        }
    }

    if (isCtrlPressed) {
        if (!cell.classList.contains('selected-cell')) {
            cell.classList.add('selected-cell');
            selectedCells.push(cell);
            cell.dataset.type = selectedType;
            if (selectedType === "pass sân") cell.style.backgroundColor = "#f87171";
            else if (selectedType === "cố định") cell.style.backgroundColor = "#60a5fa";
            else if (selectedType === "giờ chẵn") cell.style.backgroundColor = "#86efac";
            else if (selectedType === "giờ lẻ") cell.style.backgroundColor = "#fde68a";
            else cell.style.backgroundColor = "";
        }
    } else {
        if (cell.classList.contains('selected-cell')) {
            // Bỏ chọn ô
            cell.classList.remove('selected-cell');
            selectedCells = selectedCells.filter(c => c !== cell);
            cell.dataset.type = ""; // Xóa type tạm thời
            cell.style.backgroundColor = ""; // Xóa màu tạm thời

            // Kiểm tra nếu ô có lịch (dataset.bookingId)
            if (cell.dataset.bookingId) {
                // Tìm thông tin lịch trong bookingsCache
                const booking = bookingsCache.find(b =>
                    b.id == cell.dataset.bookingId &&
                    b.date === cell.dataset.date &&
                    b.court_id == cell.dataset.court &&
                    parseFloat(b.start_time).toFixed(1) === cell.dataset.hour
                );

                if (booking) {
                    // Áp dụng lại màu và nội dung dựa trên thông tin lịch
                    const isWhole = x => Math.abs(x - Math.round(x)) < 0.01;
                    const bgColor = booking.type === "cố định" ? "bg-blue-400" :
                        booking.type === "pass sân" ? "bg-red-300" :
                            (isWhole(booking.start_time) && isWhole(booking.end_time) ? "bg-green-400" : "bg-yellow-400");
                    const textColor = booking.is_paid ? "text-black" : "text-red-600";
                    const priceStr = booking.manual_price > 0 ? booking.manual_price.toLocaleString('vi-VN', { style: 'currency', currency: 'VND' }) :
                        booking.price > 0 ? booking.price.toLocaleString('vi-VN', { style: 'currency', currency: 'VND' }) : '';

                    cell.className = `border px-4 py-3 text-center custom-cell ${bgColor} text-white`;
                    cell.innerHTML = `<div class="${textColor} font-semibold">${booking.customer_name}${priceStr ? `<br/>${priceStr}` : ''}</div>`;
                    cell.dataset.bookingType = booking.type;
                    cell.dataset.isPaid = booking.is_paid;
                    cell.dataset.customerName = booking.customer_name;
                    cell.dataset.totalPrice = booking.manual_price > 0 ? booking.manual_price : booking.price;
                } else {
                    // Nếu không tìm thấy booking, đặt lại trạng thái mặc định
                    cell.className = 'border px-4 py-3 text-center custom-cell hover:bg-green-100 cursor-pointer select-none';
                    cell.innerHTML = "";
                    delete cell.dataset.bookingId;
                    delete cell.dataset.bookingType;
                    delete cell.dataset.isPaid;
                    delete cell.dataset.customerName;
                    delete cell.dataset.totalPrice;
                }
            } else {
                // Nếu ô không có lịch, đặt lại trạng thái mặc định
                cell.className = 'border px-4 py-3 text-center custom-cell hover:bg-green-100 cursor-pointer select-none';
                cell.innerHTML = "";
            }
        }
    }

    console.log("🔍 selectedCells sau khi chọn:", selectedCells.map(c => ({
        court: c.dataset.court,
        date: c.dataset.date,
        hour: c.dataset.hour,
        type: c.dataset.type
    })));
}

        // Bỏ chọn tất cả ô
        function clearSelection() {
            selectedCells.forEach(c => c.classList.remove('selected-cell'));
            selectedCells = [];
        }
        let selectedType = 'vãng lai'; // mặc định

        document.querySelectorAll('.type-option').forEach(el => {
            el.addEventListener('click', () => {
                const clickedType = el.dataset.type;

                if (selectedType === clickedType) {
                    // Nếu bấm lại đúng màu đang chọn → xóa màu các ô
                    selectedCells.forEach(cell => {
                        cell.dataset.type = "";
                        cell.style.backgroundColor = "";
                    });
                    selectedType = "";
                    return;
                }

                // Nếu chọn loại mới → tô màu
                selectedType = clickedType;
                selectedCells.forEach(cell => {
                    cell.dataset.type = selectedType;
                    if (selectedType === "pass sân") cell.style.backgroundColor = "#f87171";
                    else if (selectedType === "cố định") cell.style.backgroundColor = "#60a5fa";
                    else if (selectedType === "giờ chẵn") cell.style.backgroundColor = "#86efac";
                    else if (selectedType === "giờ lẻ") cell.style.backgroundColor = "#fde68a";
                });
            });
        });

        let selectedPaymentStatus = false; // ✅ mặc định là chưa thanh toán

        //trạng thái thanh toán
        function setPaymentStatus(status) {
            selectedPaymentStatus = status;

            const paidBtn = document.getElementById("statusPaidBtn");
            const unpaidBtn = document.getElementById("statusUnpaidBtn");

            if (status) {
                paidBtn.classList.add("bg-green-500", "text-white");
                unpaidBtn.classList.remove("bg-red-500", "text-white");
            } else {
                unpaidBtn.classList.add("bg-red-500", "text-white");
                paidBtn.classList.remove("bg-green-500", "text-white");
            }
        }

        //đánh dấu trạng thái thanh toán
        function markAsPaid(bookingId, btnElement) {
            if (!confirm("Bạn có chắc muốn đánh dấu đã thanh toán?")) return;

            $.ajax({
                url: '/AdminQuanLy/MarkAsPaid',
                method: 'POST',
                data: { bookingId: bookingId },
                    success: function (res) {
                        if (res.success) {
                            alert("✅ Đã cập nhật trạng thái thanh toán.");
                            // Làm mờ nút hoặc đổi màu chữ
                            const container = btnElement.parentElement;
                            container.classList.remove("text-red-600");
                            container.classList.add("text-black");
                            btnElement.remove(); // ẩn nút
                        } else {
                            alert("❌ Lỗi: " + res.message);
                        }
                },
                error: function (xhr) {
                    alert("❌ Lỗi server: " + xhr.responseText);
                }
            });
        }
        // Xóa ôô  đđãã  đđặặtt  llịịcchh
        function deleteSelectedBookings() {
            const cellsWithId = selectedCells.filter(c => c.dataset.bookingId);

            // Lấy unique bookingId
            const idsToDelete = [...new Set(cellsWithId.map(c => parseInt(c.dataset.bookingId)))];

            console.log("✅ IDs to delete (unique):", idsToDelete);

            if (idsToDelete.length === 0) {
                alert("❌ Không có lịch nào được chọn để xóa (hoặc đã bị xóa trước đó).");
                return;
            }

            if (!confirm("Bạn có chắc muốn xóa những lịch này không?")) return;

            $.ajax({
                url: '/AdminQuanLy/DeleteBookings',
                method: 'POST',
                contentType: 'application/json',
                dataType: 'json', // <- thêm dòng này
                data: JSON.stringify(idsToDelete),
                success: function (res) {
                    console.log("🔄 Server response:", res);
                    if (res.success === true) {
                        alert("✅ Xóa thành công!");
                        clearSelection();
                        setTimeout(() => {
                            renderBookingTable();
                        }, 300);
                    } else {
                        alert("❌ Lỗi khi xóa lịch: " + (res.message || "Không rõ lỗi"));
                        console.warn("❗ Server said:", res);
                    }
                },
                error: function (xhr) {
                    console.error("❌ AJAX error:", xhr.responseText);
                    alert("❌ Lỗi server: " + xhr.responseText);
                }
            });
        }
        //================================================================================================================================================================
        // Hàm gửi form đặt sân

function submitBookingForm() {
    const name = document.getElementById('nameInput')?.value.trim();
    const phone = document.getElementById('phoneInput')?.value.trim();
    const isPaid = selectedPaymentStatus;
    const paymentMethod = document.getElementById('paymentMethodSelect')?.value.trim();
    const manualPrice = parseFloat(document.getElementById('manualPriceInput')?.value.trim());

    console.log("🔍 Bắt đầu submitBookingForm", { name, phone, isPaid, paymentMethod, manualPrice });

    if (!name) {
        console.log("❌ Thiếu tên khách hàng");
        return alert('Vui lòng nhập tên!');
    }

    if (selectedCells.length === 0) {
        console.log("❌ selectedCells rỗng:", selectedCells);
        return alert('Bạn chưa chọn ô nào.');
    }

    console.log("🔍 selectedCells:", selectedCells.map(c => ({
        court: c.dataset.court,
        date: c.dataset.date,
        hour: c.dataset.hour,
        type: c.dataset.type
    })));

    // Nhóm các ô theo ngày và sân
    const groupedByDateAndCourt = {};
    selectedCells.forEach(cell => {
        const key = `${cell.dataset.date}_${cell.dataset.court}`;
        if (!groupedByDateAndCourt[key]) {
            groupedByDateAndCourt[key] = [];
        }
        groupedByDateAndCourt[key].push(cell);
    });

    console.log("🔍 groupedByDateAndCourt:", Object.keys(groupedByDateAndCourt).map(key => ({
        key,
        cells: groupedByDateAndCourt[key].map(c => ({
            hour: c.dataset.hour,
            type: c.dataset.type
        }))
    })));

    const bookings = [];
    for (const key in groupedByDateAndCourt) {
        const cells = groupedByDateAndCourt[key].sort((a, b) => parseFloat(a.dataset.hour) - parseFloat(b.dataset.hour));
        const court = cells[0].dataset.court;
        const date = cells[0].dataset.date;
        const typeRaw = cells[0].dataset.type?.trim().toLowerCase();
        let actualType = typeRaw;
        if (typeRaw === "giờ chẵn" || typeRaw === "giờ lẻ") actualType = "vãng lai";
        if (cells.some(cell => cell.classList.contains("bg-red-300"))) actualType = "pass sân";

        let startHour = parseFloat(cells[0].dataset.hour);
        let endHour = parseFloat(cells[cells.length - 1].dataset.hour) + 0.5;

               // Kiểm tra xung đột
        const hasConflict = cells.some(cell => cell.innerHTML.trim() !== "" && !cell.classList.contains('selected-cell'));
        if (hasConflict) {
            console.log("❌ Có ô đã được đặt trong cells:", cells);
            alert("Trong số các ô bạn chọn có ô đã được đặt. Vui lòng bỏ chọn các ô đó.");
            return;
        }

        bookings.push({
            court_id: parseInt(court),
            date: date,
            start_time: formatTime(startHour),
            end_time: formatTime(endHour),
            type: actualType,
            is_paid: isPaid,
            payment_method: paymentMethod || null,
            manual_price: !isNaN(manualPrice) ? manualPrice : null
        });
    }

    const payload = { name, phone, bookings };
    console.log("📤 Payload gửi đi:", payload);

    $.ajax({
        url: '/AdminQuanLy/UpdateBookingsWithCustomer',
        method: 'POST',
        contentType: 'application/json',
        dataType: 'json',
        data: JSON.stringify(payload),
        success: function (res) {
            console.log("✅ Kết quả từ API UpdateBookingsWithCustomer:", res);
            if (res.success) {
                alert("✅ Cập nhật lịch thành công!");
                const onlyChangeColor = bookings.every(b => b.type === 'pass sân');
                clearSelection?.();
                if (onlyChangeColor) {
                    bookings.forEach(b => {
                        const selector = `td[data-court="${b.court_id}"][data-date="${b.date}"][data-hour="${parseFloat(b.start_time).toFixed(1)}"]`;
                        const cell = document.querySelector(selector);
                        if (cell) {
                            cell.className = "border px-4 py-3 text-center custom-cell text-white bg-red-300";
                            cell.innerHTML = "pass sân";
                        }
                    });
                } else {
                    renderBookingTable?.();
                }
            } else if (res.conflict) {
                const useOld = confirm(`${res.message}\n\nOK: ${res.options[0]}\nCancel: ${res.options[1]}`);
                $.ajax({
                    url: '/AdminQuanLy/UpdateBookingsWithCustomer',
                    method: 'POST',
                    contentType: 'application/json',
                    dataType: 'json',
                    data: JSON.stringify({ ...payload, name: useOld ? res.options[0] : res.options[1] }),
                    success: r2 => {
                        console.log("✅ Kết quả từ API UpdateBookingsWithCustomer (xử lý conflict):", r2);
                        if (r2.success) {
                            alert("✅ Đã cập nhật với tên đã chọn.");
                            clearSelection?.();
                            renderBookingTable?.();
                        } else alert("❌ Lỗi: " + (r2.message || "Không rõ lỗi"));
                    }
                });
            } else {
                alert("❌ Lỗi khi cập nhật: " + (res.message || "Không rõ lỗi"));
            }
        },
        error: xhr => {
            console.error("❌ Lỗi server từ UpdateBookingsWithCustomer:", xhr.responseText);
            alert("❌ Lỗi server: " + xhr.responseText);
        }
    });
}
        ////==========================================================================================================================================
        document.addEventListener('mousedown', e => {
            if (!e.target.classList.contains('custom-cell')) return;

            const now = new Date().getTime();
            const timeSinceLastClick = now - lastClickTime;

            // Nếu double click trong khoảng 300ms thì bỏ qua
            if (timeSinceLastClick < 300) {
                // Ngăn nháy đúp bắt đầu kéo chọn
                e.preventDefault();
                return;
            }

            lastClickTime = now;

            isMouseDown = true;
            toggleCellSelection(e.target, e.ctrlKey);
        });

        document.addEventListener('mouseover', e => {
            if (isMouseDown && e.target.classList.contains('custom-cell')) {
                toggleCellSelection(e.target, true); // giả lập giữ Ctrl khi kéo
            }
        });

        document.addEventListener('mouseup', () => {
            isMouseDown = false;
        });
        // Ngăn double click gây kéo chọn
        document.addEventListener('dblclick', e => {
            if (e.target.classList.contains('custom-cell')) {
                e.preventDefault();
                return false;
            }
        });

//================================================================================================================================================================
// Hàm xóa lọc giờ đã chọn
        function clearHourFilter() {
            const checkboxes = document.querySelectorAll('.hour-checkbox');
            checkboxes.forEach(cb => cb.checked = false);
            filterBySelectedHours();
        }
//================================================================================================================================================================
        // Bỏ chọn tất cả ngày
function toggleAllDays() {
    const checkboxes = document.querySelectorAll('.day-checkbox');
    const allChecked = Array.from(checkboxes).every(cb => cb.checked);

    checkboxes.forEach(cb => cb.checked = !allChecked);

    // Cập nhật lại nội dung nút
    const toggleButton = document.getElementById('toggleDayFilter');
    if (toggleButton) {
        toggleButton.textContent = allChecked ? 'Chọn tất cả' : 'Bỏ chọn tất cả';
    }
    removeCellsHiddenByRowspan();
    filterBookingTableNew();
    removeCellsHiddenByRowspan();
}
//===============================================================================================================================================================================
    
//===============================================================================================================================================================================
function closeBookingDetail() {
    const detailDiv = document.getElementById("bookingDetailPopup");
    if (detailDiv) {
        detailDiv.style.display = "none";
        detailDiv.innerHTML = ""; // optional: xoá nội dung cũ
    }
}
//==============================================================================================================================================================================
let danhSachBooking = [];
let hoTenGlobal = "";
function renderChiTietBooking() {
    const detailDiv = document.getElementById("bookingDetailPopup");
    if (!detailDiv) return;
   

    let tong = 0;
    let rows = "";

    danhSachBooking.forEach(b => {
        tong += b.price;
        rows += `
        <tr>
            <td class="border p-2">${b.DayName}</td>
            <td class="border p-2">${formatDate(b.date)}</td>
            <td class="border p-2">${b.court}</td>
            <td class="border p-2">${b.start}</td>
            <td class="border p-2">${b.end}</td>
            <td class="border p-2">${b.type}</td>
            <td class="border p-2 text-right">${b.price.toLocaleString()} đ</td>
        </tr>`;
    });

    detailDiv.innerHTML = `
        <button onclick="closeBookingDetail()" class="absolute top-2 right-2 text-gray-600 hover:text-black text-lg font-bold">×</button>
        <div class="space-y-4 mt-6">
            <h2 class="text-lg font-bold text-center mb-4">Chi tiết đặt sân</h2>
            <p><strong>Khách:</strong> ${hoTenGlobal}</p>
            <table class="w-full text-sm border border-gray-300">
                <thead class="bg-gray-100 text-gray-700">
                    <tr>
                        <th class="border p-2">Thứ</th>
                        <th class="border p-2">Ngày</th>
                        <th class="border p-2">Sân</th>
                        <th class="border p-2">Bắt đầu</th>
                        <th class="border p-2">Kết thúc</th>
                        <th class="border p-2">Loại</th>
                        <th class="border p-2">Thành tiền</th>
                    </tr>
                </thead>
                <tbody>${rows}</tbody>
                <tfoot>
                    <tr class="bg-amber-50 font-semibold">
                        <td colspan="6" class="text-right border p-2">Tổng cộng:</td>
                        <td class="text-right border p-2">${tong.toLocaleString()} đ</td>
                    </tr>
                </tfoot>
            </table>
        </div>`;
    detailDiv.style.display = "block";
}
//===============================================================================================================================================================================
function formatDate(dateStr) {
    const d = new Date(dateStr);
    return d.toLocaleDateString("vi-VN");
}

//===============================================================================================================================================================================
        // tạo bảng quan trọng
        // Hàm xuất dữ liệu ra Excel
// lọc ngày, lọc giờ
function roundToNearestHalf(num) {
    return Math.round(parseFloat(num) * 2) / 2;
}
let lastClickedBookingInfo = null;
function renderBookingTable() {
    const numCourts = 6;
    const startHour = 5;
    const endHour = 24;

    const selectedMonth = parseInt(document.getElementById('monthSelect').value);
    const selectedYear = parseInt(document.getElementById('yearSelect').value);
    const currentDate = new Date();

    // Tạo mảng dates chứa tất cả các ngày trong tháng
    const dates = [];
    const daysInMonth = new Date(selectedYear, selectedMonth + 1, 0).getDate();
    for (let i = 1; i <= daysInMonth; i++) {
        const d = new Date(selectedYear, selectedMonth, i);
        const dateStr = formatDateToYMD(d);
        if (!dates.some(existing => existing.date === dateStr)) {
            dates.push({
                date: dateStr,
                label: d.toLocaleDateString('vi-VN', { weekday: 'short', day: '2-digit', month: '2-digit' })
            });
        }
    }
    console.log("📅 Mảng dates:", dates);

    // Cập nhật dropdown với tất cả các ngày
    populateDayHourDropdowns(dates);
    removeCellsHiddenByRowspan();
    // Xây dựng bảng
    const thead = document.getElementById('thead');
    const tbody = document.getElementById('tbody');
    thead.innerHTML = '';
    tbody.innerHTML = '';

    const headRow = document.createElement('tr');
    headRow.innerHTML = `<th class="border px-2 py-1">Sân</th><th class="border px-2 py-1">Khung giờ</th>`;
    for (const d of dates) {
        const th = document.createElement('th');
        th.className = 'border px-3 py-2';
        th.textContent = d.label;
        th.dataset.date = d.date;
        headRow.appendChild(th);
    }
    thead.appendChild(headRow);

    for (let court = 1; court <= numCourts; court++) {
        const courtClass = court % 2 === 0 ? 'court-even' : 'court-odd';
        for (let hour = startHour; hour < endHour; hour += 0.5) {
            const tr = document.createElement('tr');
            tr.dataset.hour = hour.toFixed(1);

            const startLabel = Math.floor(hour) + (hour % 1 === 0.5 ? 'h30' : 'h');
            const endLabel = Math.floor(hour + 0.5) + ((hour + 0.5) % 1 === 0.5 ? 'h30' : 'h');

            tr.innerHTML = `
                <td class="border px-2 py-1 font-bold text-blue-600 w-8 ${courtClass}">${court}</td>
                <td class="border px-2 py-1 text-gray-700">${startLabel} - ${endLabel}</td>
            `;

            for (const d of dates) {
                const td = document.createElement('td');
                td.className = 'border px-4 py-3 text-center custom-cell hover:bg-green-100 cursor-pointer select-none';
                td.dataset.court = court;
                td.dataset.hour = hour.toFixed(1);
                td.dataset.date = d.date;
                td.dataset.type = selectedType;
                tr.appendChild(td);
            }

            tbody.appendChild(tr);
        }
    }

    // Gọi API để lấy dữ liệu booking
    $.getJSON("/AdminQuanLy/GetBookings", {
        month: selectedMonth + 1,
        year: selectedYear
    }, function (data) {
        console.log("📦 Dữ liệu từ API:", data);

        // Lưu dữ liệu vào bookingsCache
        bookingsCache = [];
        const seen = new Set();
        data.forEach(b => {
            let bookingDate;
            try {
                bookingDate = new Date(b.date);
                if (isNaN(bookingDate)) throw new Error(`Ngày không hợp lệ: ${b.date}`);
            } catch (e) {
                console.error(`❌ Lỗi định dạng ngày cho booking:`, b);
                return;
            }

            if (bookingDate.getFullYear() === selectedYear && bookingDate.getMonth() === selectedMonth) {
                const key = `${formatDateToYMD(bookingDate)}_${b.court_id}_${b.start_time}_${b.end_time}_${b.customer_id}`;
                if (!seen.has(key)) {
                    seen.add(key);
                    bookingsCache.push({
                        ...b,
                        date: formatDateToYMD(bookingDate),
                        start_time: parseFloat(b.start_time),
                        end_time: parseFloat(b.end_time)
                    });
                } else {
                    console.warn(`❌ Bỏ qua booking trùng lặp:`, b);
                }
            }
        });
        console.log("📋 Dữ liệu bookingsCache:", bookingsCache);

        // Reset tất cả ô
        document.querySelectorAll('.custom-cell').forEach(cell => {
            cell.innerHTML = "";
            cell.className = 'border px-4 py-3 text-center custom-cell hover:bg-green-100 cursor-pointer select-none';
            delete cell.dataset.bookingId;
        });

        const grouped = {};
        bookingsCache.forEach(b => {
            const key = `${b.date}_${b.court_id}_${b.customer_id}`;
            if (!grouped[key]) grouped[key] = [];
            grouped[key].push(b);
        });
        console.log("📋 Dữ liệu sau khi nhóm:", grouped);

        for (const key in grouped) {
            const list = grouped[key].sort((a, b) => a.start_time - b.start_time);
            let i = 0;

            while (i < list.length) {
                const b = list[i];
                console.log("🔍 Xử lý booking:", b);

                const merged = {
                    ...b,
                    start_time: roundToNearestHalf(b.start_time),
                    end_time: roundToNearestHalf(b.end_time),
                    total_price: b.manual_price > 0 ? b.manual_price : b.price
                };

                let j = i + 1;
                while (
                    j < list.length &&
                    roundToNearestHalf(list[j].start_time) <= merged.end_time &&
                    list[j].is_paid === b.is_paid &&
                    list[j].customer_id === b.customer_id &&
                    list[j].type === b.type
                ) {
                    console.log("🔗 Gộp booking:", list[j]);
                    merged.end_time = roundToNearestHalf(list[j].end_time);
                    merged.total_price += list[j].manual_price > 0 ? list[j].manual_price : list[j].price;
                    j++;
                }

                console.log("🔗 Kết quả gộp:", {
                    court_id: b.court_id,
                    date: b.date,
                    start_time: merged.start_time,
                    end_time: merged.end_time,
                    customer_id: b.customer_id,
                    total_price: merged.total_price
                });

                i = j;

                const hourStr = parseFloat(merged.start_time).toFixed(1); // ép chặt định dạng
                const selector = `td[data-court="${b.court_id}"][data-hour="${hourStr}"][data-date="${b.date}"]`;
                const cell = document.querySelector(selector);
                cell?.dataset && (cell.dataset.hour = merged.start_time.toFixed(1));

                if (!cell) {
                    console.warn(`❌ Không tìm thấy ô cho booking: court=${b.court_id}, hour=${merged.start_time.toFixed(1)}, date=${b.date}`);
                    continue;
                }

                const rowspan = Math.round((merged.end_time - merged.start_time) / 0.5);
                console.log("📏 Rowspan:", rowspan, "Start:", merged.start_time, "End:", merged.end_time);

                const isWhole = x => Math.abs(x - Math.round(x)) < 0.01;
                const bgColor = {
                    "cố định": "bg-blue-400",
                    "pass sân": "bg-red-300",
                    "vãng lai": isWhole(merged.start_time) && isWhole(merged.end_time) ? "bg-green-400" : "bg-yellow-400"
                }[b.type] || "bg-yellow-400";

                const textColor = b.is_paid ? "text-black" : "text-red-600";
                const priceStr = merged.total_price.toLocaleString('vi-VN', { style: 'currency', currency: 'VND' });

                cell.rowSpan = rowspan;
                cell.classList.remove("bg-green-400", "bg-blue-400", "bg-red-300", "bg-yellow-400");
                cell.classList.add(bgColor, "text-white");
                cell.classList.remove("hover:bg-green-100");
                cell.innerHTML = `<div class="${textColor} font-semibold">${b.customer_name}${merged.total_price > 0 ? `<br/>${priceStr}` : ''}</div>`;
                cell.dataset.bookingId = b.id;

                // Xóa các ô bị gộp do rowspan
                for (let h = merged.start_time + 0.5; h < merged.end_time; h += 0.5) {
                    const hRounded = h.toFixed(1);
                    const row = document.querySelector(`tr[data-hour="${hRounded}"]`);
                    if (row) {
                        const tdToHide = Array.from(row.children).find(td =>
                            td.dataset?.court === String(b.court_id) &&
                            td.dataset?.date === b.date
                        );
                        if (tdToHide) {
                            tdToHide.remove();
                            // Nếu dòng chỉ còn 2 ô (Sân + Giờ) thì xóa luôn dòng
                            const remainingCells = row.querySelectorAll("td").length;
                            if (remainingCells <= 2) row.remove();
                        }
                    }
                }
            }
        }

      

        document.querySelectorAll('.custom-cell[data-booking-id]').forEach(cell => {
            cell.addEventListener('click', () => {
                const courtId = cell.dataset.court;
                const date = cell.dataset.date;
                const hour = cell.dataset.hour;

                if (!courtId || !date || !hour) return;

                lastClickedBookingInfo = { date, courtId, hour };

                const btn = document.getElementById("btnShowBookingDetail");
                if (btn) btn.classList.remove("hidden"); // Hiện nút khi chọn ô
            });
        });
        filterBookingTableNew();                
        setTimeout(removeCellsHiddenByRowspan, 10); 
        localStorage.removeItem("pass_san_custom");
    }).fail(function (xhr) {
        console.error("❌ Lỗi khi gọi API GetBookings:", xhr.responseText);
        alert("❌ Lỗi khi tải dữ liệu lịch: " + xhr.responseText);
    });
}
function showBookingDetail() {
    if (!lastClickedBookingInfo) {
        alert("Vui lòng chọn ô lịch trước.");
        return;
    }

    const { date, courtId, hour } = lastClickedBookingInfo;

    $.get(`/AdminQuanLy/XemChiTietBooking?date=${date}&court_id=${courtId}&hour=${hour}`, function (html) {
        $("#bookingDetailPopup").html(html).fadeIn();
    });
}

//=========================================================================================================================================================================
//Cập nhật giá theo khung giờ đã chọn
function updateManualPrice() {
    const manualPrice = parseFloat(document.getElementById('manualPriceInput')?.value.trim());

    if (isNaN(manualPrice)) {
        alert("Vui lòng nhập giá hợp lệ.");
        return;
    }

    if (selectedCells.length === 0) {
        alert("Vui lòng chọn ô muốn cập nhật giá.");
        return;
    }

    const sorted = [...selectedCells].sort((a, b) => parseFloat(a.dataset.hour) - parseFloat(b.dataset.hour));
    const date = sorted[0].dataset.date;
    const court_id = parseInt(sorted[0].dataset.court);
    const start_time = formatTime(parseFloat(sorted[0].dataset.hour));
    const end_time = formatTime(parseFloat(sorted[sorted.length - 1].dataset.hour) + 0.5);

    console.log("⚙️ Gửi cập nhật giá:", { date, court_id, start_time, end_time, new_price: manualPrice });

    $.ajax({
        url: '/AdminQuanLy/CapNhatGiaTheoKhung',
        method: 'POST',
        dataType: 'json',
        data: { date, court_id, start_time, end_time, new_price: manualPrice },
        success: function (res) {
            console.log("📦 Phản hồi từ server:", res);

            if (res.success) {
                alert("✅ " + res.message);
                renderBookingTable?.();
                clearSelection?.();
            } else {
                console.error("❌ Server không trả về thành công:", res);
                alert("❌ " + (res.message || "Lỗi không xác định"));
            }
        },
        error: function (xhr) {
            console.error("❌ Lỗi server:", xhr.responseText);
            alert("❌ Lỗi server: " + xhr.responseText);
        }
    });
}


//================================================================================================================================================================================================
function populateDayHourDropdowns(dates) {
    const dayDiv = $('#dayFilterDropdown').empty();
    const hourDiv = $('#hourFilterDropdown').empty();
    const currentDate = new Date();
    console.log("📅 Dropdown dates:", dates);

    dayDiv.append(`
        <div class="mb-2 font-semibold">
            <label><input type="checkbox" id="checkAllDays" class="mr-2" checked /> Chọn tất cả ngày</label>
        </div>
    `);
    hourDiv.append(`
        <div class="mb-2 font-semibold">
            <label><input type="checkbox" id="checkAllHours" class="mr-2" checked /> Chọn tất cả giờ</label>
        </div>
    `);

    // Tạo checkbox cho các ngày
    dates.forEach(d => {
        const label = d.label;
        const value = d.date;
        const dateObj = new Date(value);
        const isFutureOrToday = dateObj >= currentDate.setHours(0, 0, 0, 0);
        const checkbox = $(`<div><label><input type="checkbox" class="day-checkbox mr-2" value="${value}" ${isFutureOrToday ? 'checked' : ''} /> ${label}</label></div>`);
        dayDiv.append(checkbox);
    });

    // Tạo checkbox cho các giờ
    for (let h = 5; h < 24; h += 1) {
        const start = formatTime(h);
        const end = formatTime(h + 1);
        const value1 = h.toFixed(1);     // e.g., 6.0
        const value2 = (h + 0.5).toFixed(1); // e.g., 6.5

        const checkbox = $(`
        <div>
            <label>
                <input type="checkbox" class="hour-checkbox mr-2" data-combo="${value1},${value2}" checked />
                ${start} - ${end}
            </label>
        </div>
    `);
        hourDiv.append(checkbox);
    }

    $('#checkAllDays').on('change', function () {
        const checked = this.checked;
        $('.day-checkbox').prop('checked', checked);
      
        filterBookingTableNew();
        removeCellsHiddenByRowspan();
    });
    $('#checkAllHours').on('change', function () {
        const checked = this.checked;
        $('.hour-checkbox').prop('checked', checked);
       
        filterBookingTableNew();
        removeCellsHiddenByRowspan();
    });

    $('.day-checkbox, .hour-checkbox').on('change', function () {
        filterBookingTableNew();
        setTimeout(removeCellsHiddenByRowspan, 10); // thêm delay
    });


    filterBookingTableNew();
    removeCellsHiddenByRowspan();
}
//============================================================================================================================================================================================================
//xóa các ô bị đẩy
function removeCellsHiddenByRowspan() {
    document.querySelectorAll('.custom-cell[rowspan]').forEach(cell => {
        const startHour = parseFloat(cell.dataset.hour);
        const rowspan = parseInt(cell.getAttribute("rowspan"));
        const endHour = startHour + rowspan * 0.5;
        const court = cell.dataset.court;
        const date = cell.dataset.date;

        console.log(`🔍 Xử lý ô rowspan: court=${court}, date=${date}, startHour=${startHour}, endHour=${endHour}, rowspan=${rowspan}`);

        for (let h = startHour + 0.5; h < endHour; h += 0.5) {
            const hRounded = h.toFixed(1);
            const rows = document.querySelectorAll(`tr[data-hour="${hRounded}"]`); // Tìm tất cả các hàng khớp
            rows.forEach(row => {
                const tdToHide = Array.from(row.children).find(td =>
                    td.dataset?.court === court &&
                    td.dataset?.date === date &&
                    td !== cell &&
                    !td.hasAttribute('rowspan')
                );
                if (tdToHide) {
                    console.log(`🗑️ Xóa ô thừa: court=${tdToHide.dataset.court}, hour=${tdToHide.dataset.hour}, date=${tdToHide.dataset.date}`);
                    tdToHide.remove();
                } else {
                    console.log(`⚠️ Không tìm thấy ô thừa để xóa: court=${court}, hour=${hRounded}, date=${date}`);
                }
                // Kiểm tra và xóa hàng nếu chỉ còn 2 ô (Sân + Giờ)
                const remainingCells = row.querySelectorAll("td").length;
                if (remainingCells <= 2) {
                    console.log(`🗑️ Xóa hàng trống: hour=${hRounded}`);
                    row.remove();
                }
            });
        }
    });
}
function filterBookingTableNew() {
    const selectedDays = $('.day-checkbox:checked').map((_, el) => el.value).get();
    let selectedHours = [];
    $('.hour-checkbox:checked').each((_, el) => {
        const combo = el.dataset.combo;
        if (combo) {
            selectedHours.push(...combo.split(',').map(parseFloat));
        } else if (el.value) {
            selectedHours.push(parseFloat(el.value));
        }
    });


    console.log("🔍 selectedDays:", selectedDays);
    console.log("🔍 selectedHours:", selectedHours);

    // Lọc các cột theo ngày
    document.querySelectorAll('#thead th[data-date]').forEach(th => {
        th.style.display = selectedDays.includes(th.dataset.date) ? '' : 'none';
    });

    // Reset bảng về trạng thái ban đầu
    const tbody = document.getElementById('tbody');
    tbody.innerHTML = '';

    const numCourts = 6;
    const startHour = 5;
    const endHour = 24;
    const selectedMonth = parseInt(document.getElementById('monthSelect').value);
    const selectedYear = parseInt(document.getElementById('yearSelect').value);
    const dates = [];
    const daysInMonth = new Date(selectedYear, selectedMonth + 1, 0).getDate();

    // Chỉ thêm các ngày được chọn vào mảng dates
    for (let i = 1; i <= daysInMonth; i++) {
        const d = new Date(selectedYear, selectedMonth, i);
        const dateStr = formatDateToYMD(d);
        if (selectedDays.includes(dateStr)) {
            dates.push({ date: dateStr });
        }
    }
    console.log("📅 dates:", dates);

    // Tạo lại các hàng và ô
    for (let court = 1; court <= numCourts; court++) {
        for (let hour = startHour; hour < endHour; hour += 0.5) {
            const hRounded = hour.toFixed(1);
            if (!selectedHours.includes(parseFloat(hRounded))) continue; // Chỉ tạo hàng nếu giờ được chọn
            const row = document.createElement('tr');
            row.dataset.hour = hRounded;
            const startLabel = Math.floor(hour) + (hour % 1 === 0.5 ? 'h30' : 'h');
            const endLabel = Math.floor(hour + 0.5) + ((hour + 0.5) % 1 === 0.5 ? 'h30' : 'h');
            row.innerHTML = `
                <td class="border px-2 py-1 font-bold text-blue-600 w-8 ${court % 2 === 0 ? 'court-even' : 'court-odd'}">${court}</td>
                <td class="border px-2 py-1 text-gray-700">${startLabel} - ${endLabel}</td>
            `;
            for (const d of dates) {
                const td = document.createElement('td');
                td.className = 'border px-4 py-3 text-center custom-cell hover:bg-green-100 cursor-pointer select-none';
                td.dataset.court = court;
                td.dataset.hour = hRounded;
                td.dataset.date = d.date;
                row.appendChild(td);
            }
            tbody.appendChild(row);
        }
    }

    // Áp dụng dữ liệu lịch từ bookingsCache
    const grouped = {};
    bookingsCache.forEach(b => {
        const key = `${b.date}_${b.court_id}_${b.customer_id}`;
        if (!grouped[key]) grouped[key] = [];
        grouped[key].push(b);
    });

    for (const key in grouped) {
        const list = grouped[key].sort((a, b) => a.start_time - b.start_time);
        let i = 0;

        while (i < list.length) {
            const b = list[i];
            const merged = {
                ...b,
                start_time: roundToNearestHalf(b.start_time),
                end_time: roundToNearestHalf(b.end_time),
                total_price: b.manual_price > 0 ? b.manual_price : b.price
            };

            let j = i + 1;
            while (
                j < list.length &&
                roundToNearestHalf(list[j].start_time) <= merged.end_time &&
                list[j].is_paid === b.is_paid &&
                list[j].customer_id === b.customer_id &&
                list[j].type === b.type
            ) {
                merged.end_time = roundToNearestHalf(list[j].end_time);
                merged.total_price += list[j].manual_price > 0 ? list[j].manual_price : list[j].price;
                j++;
            }

            i = j;

            // Chỉ hiển thị nếu ngày thuộc selectedDays
            if (!selectedDays.includes(b.date)) {
                console.log(`⏩ Bỏ qua booking vì ngày không được chọn: ${b.date}`);
                continue;
            }

            // Tìm giờ hiển thị đầu tiên trong selectedHours
            let startDisplayHour = roundToNearestHalf(merged.start_time);
            if (!selectedHours.includes(startDisplayHour)) {
                // Nếu start_hour không có trong filter → tìm giờ tiếp theo
                for (let h = merged.start_time + 0.5; h < merged.end_time; h += 0.5) {
                    const hRounded = roundToNearestHalf(h);
                    if (selectedHours.includes(hRounded)) {
                        startDisplayHour = hRounded;
                        break;
                    }
                }
            }

            if (!startDisplayHour) {
                // 🔥 Nếu không còn khung giờ phù hợp → xóa ô còn tồn tại do render trước
                const selector = `td[data-court="${b.court_id}"][data-date="${b.date}"]`;
                const oldCell = document.querySelector(selector);
                if (oldCell) oldCell.remove();
                continue;
            }

            // Tính rowspan dựa trên các giờ được chọn
            let visibleRowspan = 0;
            const visibleHours = [];
            for (let h = merged.start_time; h < merged.end_time; h += 0.5) {
                const hRounded = roundToNearestHalf(h);
                if (selectedHours.includes(hRounded)) {
                    visibleRowspan++;
                    visibleHours.push(hRounded);
                }
            }
            if (visibleRowspan === 0) {
                console.log(`⏩ Bỏ qua booking vì không có giờ nào được chọn: ${b.date}, ${merged.start_time}-${merged.end_time}`);
                continue;
            }

            const isWhole = x => Math.abs(x - Math.round(x)) < 0.01;
            const bgColor = {
                "cố định": "bg-blue-400",
                "pass sân": "bg-red-300",
                "vãng lai": isWhole(merged.start_time) && isWhole(merged.end_time) ? "bg-green-400" : "bg-yellow-400"
            }[b.type] || "bg-yellow-400";
            const textColor = b.is_paid ? "text-black" : "text-red-600";
            const priceStr = merged.total_price.toLocaleString('vi-VN', { style: 'currency', currency: 'VND' });

            const selector = `td[data-court="${b.court_id}"][data-hour="${startDisplayHour.toFixed(1)}"][data-date="${b.date}"]`;
            const cell = document.querySelector(selector);
            cell.dataset.hour = startDisplayHour.toFixed(1); // BỔ SUNG DÒNG NÀY

            if (!cell) {
                console.warn(`❌ Không tìm thấy ô cho booking: court=${b.court_id}, hour=${startDisplayHour.toFixed(1)}, date=${b.date}`);
                continue;
            }

            cell.rowSpan = visibleRowspan;
            cell.classList.remove("bg-green-400", "bg-blue-400", "bg-red-300", "bg-yellow-400");
            cell.classList.add(bgColor, "text-white");
            cell.classList.remove("hover:bg-green-100");
            cell.innerHTML = `<div class="${textColor} font-semibold">${b.customer_name}${merged.total_price > 0 ? `<br/>${priceStr}` : ''}</div>`;
            cell.dataset.bookingId = b.id;
            cell.dataset.customerName = b.customer_name;
            cell.dataset.totalPrice = merged.total_price;
            cell.dataset.isPaid = b.is_paid;
            cell.dataset.bookingType = b.type;
            cell.dataset.startHour = merged.start_time.toFixed(1);
            cell.dataset.endHour = merged.end_time.toFixed(1);

            // Xóa các ô bị gộp do rowspan
            for (const h of visibleHours.slice(1)) {
                const row = document.querySelector(`tr[data-hour="${h.toFixed(1)}"]`);
                if (row) {
                    const tdToHide = Array.from(row.children).find(td =>
                        td.dataset?.court === String(b.court_id) &&
                        td.dataset?.date === b.date
                    );
                    if (tdToHide) {
                        tdToHide.remove();
                        const remainingCells = row.querySelectorAll("td").length;
                        if (remainingCells <= 2) row.remove();
                    }
                }
            }
        }
    }

    // Ẩn hàng nếu không còn ô dữ liệu hiển thị
    document.querySelectorAll('#tbody tr').forEach(row => {
        const visibleCells = Array.from(row.querySelectorAll('td')).filter(td => td.style.display !== 'none');
        if (visibleCells.length <= 2) {
            row.style.display = 'none';
        }
    });

    // Gắn lại sự kiện click cho các ô có booking
    document.querySelectorAll('.custom-cell[data-booking-id]').forEach(cell => {
        cell.addEventListener('click', () => {
            const courtId = cell.dataset.court;
            const date = cell.dataset.date;
            const hour = cell.dataset.hour;

            if (!courtId || !date || !hour) return;
            lastClickedBookingInfo = { date, courtId, hour };
            document.getElementById("btnShowBookingDetail")?.classList.remove("hidden");

        });
    });

    console.log("✅ filterBookingTableNew hoàn tất");
}
function filterBookingTable() {
    const selectedDays = $('.day-checkbox:checked').map((_, el) => el.value).get();
    const selectedHours = $('.hour-checkbox:checked').map((_, el) => parseFloat(el.value)).get();

    // Lọc các cột theo ngày
    document.querySelectorAll('#thead th[data-date]').forEach(th => {
        th.style.display = selectedDays.includes(th.dataset.date) ? '' : 'none';
    });
    document.querySelectorAll('#tbody td[data-date]').forEach(td => {
        td.style.display = selectedDays.includes(td.dataset.date) ? '' : 'none';
    });

    // Lọc từng hàng theo giờ
    document.querySelectorAll('#tbody tr').forEach(row => {
        const rowHour = parseFloat(row.dataset.hour);
        const matchHour = selectedHours.includes(rowHour);
        row.style.display = matchHour ? '' : 'none';
    });

    // Xử lý các ô có rowspan
    document.querySelectorAll('.custom-cell[rowspan]').forEach(cell => {
        const startHour = parseFloat(cell.dataset.hour);
        const rowspan = parseInt(cell.getAttribute("rowspan"));
        const endHour = startHour + rowspan * 0.5;
        const court = cell.dataset.court;
        const date = cell.dataset.date;

        // Lấy thông tin từ ô có rowspan
        const bookingId = cell.dataset.bookingId;
        const customerName = cell.querySelector('div')?.innerText.split('<br>')[0] || '';
        const price = cell.dataset.totalPrice || 0;
        const isPaid = cell.dataset.isPaid || cell.querySelector('div')?.classList.contains('text-black');
        const bookingType = cell.dataset.bookingType || 'vãng lai';
        const bgColor = {
            "cố định": "bg-blue-400",
            "pass sân": "bg-red-300",
            "vãng lai": parseFloat(cell.dataset.hour) % 1 === 0 ? "bg-green-400" : "bg-yellow-400"
        }[bookingType] || "bg-yellow-400";
        const textColor = isPaid ? "text-black" : "text-red-600";
        const priceStr = parseFloat(price) > 0 ? parseFloat(price).toLocaleString('vi-VN', { style: 'currency', currency: 'VND' }) : '';

        // Chỉ hiển thị ô nếu giờ bắt đầu nằm trong selectedHours
        const isVisible = selectedHours.includes(startHour);
        cell.style.display = isVisible ? '' : 'none';

        // Xử lý các ô bị gộp bởi rowspan
        for (let h = startHour + 0.5; h < endHour; h += 0.5) {
            const hRounded = h.toFixed(1);
            const row = document.querySelector(`tr[data-hour="${hRounded}"]`);
            if (row) {
                const tdToShow = Array.from(row.children).find(td =>
                    td.dataset?.court === court &&
                    td.dataset?.date === date
                );
                if (tdToShow) {
                    if (selectedHours.includes(parseFloat(hRounded))) {
                        // Hiển thị ô và khôi phục thông tin lịch
                        tdToShow.style.display = '';
                        tdToShow.className = `border px-4 py-3 text-center custom-cell ${bgColor} text-white`;
                        tdToShow.innerHTML = `<div class="${textColor} font-semibold">${customerName}${priceStr ? `<br/>${priceStr}` : ''}</div>`;
                        tdToShow.dataset.bookingId = bookingId;
                        tdToShow.dataset.customerName = customerName;
                        tdToShow.dataset.totalPrice = price;
                        tdToShow.dataset.isPaid = isPaid;
                        tdToShow.dataset.bookingType = bookingType;
                    } else {
                        // Ẩn ô nếu giờ không được chọn
                        tdToShow.style.display = 'none';
                    }
                }
            }
        }

        // Ẩn hàng nếu không còn ô dữ liệu hiển thị
        document.querySelectorAll('#tbody tr').forEach(row => {
            const visibleCells = Array.from(row.querySelectorAll('td')).filter(td => td.style.display !== 'none');
            if (visibleCells.length <= 2) {
                row.style.display = 'none';
            }
        });
    });
}
document.addEventListener("click", function (e) {
    console.log("Click event triggered on:", e.target);
    const dayFilterDropdown = document.getElementById("dayFilterDropdown");
    const hourFilterDropdown = document.getElementById("hourFilterDropdown");
    const platformDropdown = document.getElementById("platformDropdown");

    console.log("dayFilterDropdown exists:", !!dayFilterDropdown, "is hidden:", dayFilterDropdown?.classList.contains("hidden"));
    console.log("hourFilterDropdown exists:", !!hourFilterDropdown, "is hidden:", hourFilterDropdown?.classList.contains("hidden"));
    console.log("platformDropdown exists:", !!platformDropdown, "is hidden:", platformDropdown?.classList.contains("hidden"));

    const isClickInsideDay = e.target.closest("#dayFilterDropdown") || e.target.closest("#toggleDayFilter");
    const isClickInsideHour = e.target.closest("#hourFilterDropdown") || e.target.closest("#toggleHourFilter");
    const isClickInsidePlatform = e.target.closest("#platformDropdown") || e.target.closest("#nameInput");

    console.log("isClickInsideDay:", !!isClickInsideDay);
    console.log("isClickInsideHour:", !!isClickInsideHour);
    console.log("isClickInsidePlatform:", !!isClickInsidePlatform);

    if (!isClickInsideDay && dayFilterDropdown && !dayFilterDropdown.classList.contains("hidden")) {
        console.log("Hiding dayFilterDropdown");
        dayFilterDropdown.classList.add("hidden");
    }

    if (!isClickInsideHour && hourFilterDropdown && !hourFilterDropdown.classList.contains("hidden")) {
        console.log("Hiding hourFilterDropdown");
        hourFilterDropdown.classList.add("hidden");
    }

    if (!isClickInsidePlatform && platformDropdown && !platformDropdown.classList.contains("hidden")) {
        console.log("Hiding platformDropdown");
        platformDropdown.classList.add("hidden");
    }
});

        //=========================================================================================================================================================================
        // Lọc giờ
        //=========================================================================================================================================================================
window.onload = () => {
    const monthSelect = document.getElementById('monthSelect');
    const yearSelect = document.getElementById('yearSelect');
    const currentDate = new Date();

    for (let m = 0; m < 12; m++) {
        const opt = document.createElement('option');
        opt.value = m;
        opt.textContent = new Date(0, m).toLocaleString('vi-VN', { month: 'long' });
        if (m === currentDate.getMonth()) opt.selected = true;
        monthSelect.appendChild(opt);
    }

    for (let y = currentDate.getFullYear() - 1; y <= currentDate.getFullYear() + 1; y++) {
        const opt = document.createElement('option');
        opt.value = y;
        opt.textContent = y;
        if (y === currentDate.getFullYear()) opt.selected = true;
        yearSelect.appendChild(opt);
    }

    document.getElementById('btnCapNhatGia').addEventListener('click', updateManualPrice);

    // Gắn sự kiện click cho toggleDayFilter
    $('#toggleDayFilter').off('click').click(function (e) {
        e.stopPropagation();
        $('#dayFilterDropdown').toggleClass('hidden');
    });

    // Gắn sự kiện click cho toggleHourFilter
    $('#toggleHourFilter').off('click').click(function (e) {
        e.stopPropagation();
        $('#hourFilterDropdown').toggleClass('hidden');
    });

    // Gắn sự kiện click cho platformDropdown và nameInput
    $('#platformDropdown').off('click').click(function (e) {
        e.stopPropagation();
    });

    $('#nameInput').off('click').click(function (e) {
        e.stopPropagation();
        $('#platformDropdown').toggleClass('hidden');
    });

    // Gắn sự kiện change cho checkbox
    $(document).off('change', '.day-checkbox, .hour-checkbox').on('change', '.day-checkbox, .hour-checkbox', () => {
        filterBookingTableNew();
        setTimeout(removeCellsHiddenByRowspan, 10);
    });

    renderBookingTable();
    checkBookingChanges();
    setInterval(checkBookingChanges, 10000);
};

        function clearSelection() {
            console.log("Bỏ chọn", selectedCells);
            selectedCells.forEach(c => {
                c.classList.remove('selected-cell');
                c.dataset.type = "";
                c.style.backgroundColor = "";
            });
            selectedCells = [];
        }

 ////========================================================================================================================

function exportToExcel() {
    const table = document.getElementById("bookingTable");
    const wb = { SheetNames: [], Sheets: {} };
    const ws_data = [];
    const merges = [];

    // Header
    const headerRow = Array.from(table.querySelectorAll("thead th")).map(th => ({
        v: th.innerText.trim(),
        s: {
            font: { bold: true, sz: 12 },
            alignment: { horizontal: "center", vertical: "center", wrapText: true },
            fill: { fgColor: { rgb: "D9E1F2" } },
            border: {
                top: { style: "thin", color: { rgb: "000000" } },
                bottom: { style: "thin", color: { rgb: "000000" } },
                left: { style: "thin", color: { rgb: "000000" } },
                right: { style: "thin", color: { rgb: "000000" } }
            }
        }
    }));
    ws_data.push(headerRow);

    const rows = table.querySelectorAll("tbody tr");
    let rowOffset = 1;

    for (let rIdx = 0; rIdx < rows.length; rIdx++) {
        const r = rows[rIdx];
        const cells = r.querySelectorAll("td");
        const row = [];

        for (let cIdx = 0; cIdx < cells.length; cIdx++) {
            const c = cells[cIdx];
            let content = "";
            let fontColor = "000000";
            let bg = "FFFFFF";

            if (c.querySelector("div")) {
                const div = c.querySelector("div");
                content = div.innerText.replace(/\n/g, " - ");
                if (div.classList.contains("text-red-600")) fontColor = "FF0000";
                else if (div.classList.contains("text-black")) fontColor = "000000";
            } else {
                content = c.innerText.trim();
            }

            if (c.classList.contains("bg-green-400")) bg = "92D050";
            else if (c.classList.contains("bg-yellow-400")) bg = "FFFF00";
            else if (c.classList.contains("bg-red-300")) bg = "FF9999";
            else if (c.classList.contains("bg-blue-400")) bg = "00B0F0";
            else if (cIdx === 0 || cIdx === 1) {
                const courtNum = parseInt(cells[0]?.innerText.trim() || "0");
                bg = courtNum % 2 === 0 ? "E0F7FA" : "F9F9F9";
            }

            const cellObj = {
                v: content,
                s: {
                    fill: { fgColor: { rgb: bg } },
                    font: { name: "Arial", sz: 11, color: { rgb: fontColor } },
                    alignment: { horizontal: "center", vertical: "center", wrapText: true },
                    border: {
                        top: { style: "thin", color: { rgb: "000000" } },
                        bottom: { style: "thin", color: { rgb: "000000" } },
                        left: { style: "thin", color: { rgb: "000000" } },
                        right: { style: "thin", color: { rgb: "000000" } }
                    }
                }
            };

            const rs = c.rowSpan || 1;
            if (rs > 1) {
                merges.push({ s: { r: rowOffset, c: cIdx }, e: { r: rowOffset + rs - 1, c: cIdx } });
            }

            row.push(cellObj);
        }

        ws_data.push(row);
        rowOffset++;
    }

    const ws = {};
    for (let R = 0; R < ws_data.length; ++R) {
        for (let C = 0; C < ws_data[R].length; ++C) {
            const cell_address = XLSX.utils.encode_cell({ r: R, c: C });
            ws[cell_address] = ws_data[R][C];
        }
    }

    ws['!ref'] = XLSX.utils.encode_range({ s: { r: 0, c: 0 }, e: { r: ws_data.length - 1, c: ws_data[0].length - 1 } });
    ws['!cols'] = new Array(ws_data[0].length).fill({ wch: 20 });
    ws['!rows'] = ws_data.map(() => ({ hpt: 24 }));
    ws['!merges'] = merges;

    wb.SheetNames.push("LichSan");
    wb.Sheets["LichSan"] = ws;

    const wbout = XLSX.write(wb, { bookType: "xlsx", type: "binary" });
    function s2ab(s) {
        const buf = new ArrayBuffer(s.length);
        const view = new Uint8Array(buf);
        for (let i = 0; i < s.length; i++) view[i] = s.charCodeAt(i) & 0xFF;
        return buf;
    }
    saveAs(new Blob([s2ab(wbout)], { type: "application/octet-stream" }), "LichSan.xlsx");
}
