

 

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

        // Hàm chọn/bỏ chọn ô
        function toggleCellSelection(cell, isCtrlPressed) {
            if (isCtrlPressed) {
                if (!cell.classList.contains('selected-cell')) {
                    cell.classList.add('selected-cell');
                    selectedCells.push(cell);

                    // Gán loại đang chọn và tô màu luôn
                    cell.dataset.type = selectedType;
                    if (selectedType === "pass sân") cell.style.backgroundColor = "#f87171";
                    else if (selectedType === "cố định") cell.style.backgroundColor = "#60a5fa";
                    else if (selectedType === "giờ chẵn") cell.style.backgroundColor = "#86efac";
                    else if (selectedType === "giờ lẻ") cell.style.backgroundColor = "#fde68a";
                    else cell.style.backgroundColor = "";
                }
            } else {
                if (cell.classList.contains('selected-cell')) {
                    cell.classList.remove('selected-cell');
                    selectedCells = selectedCells.filter(c => c !== cell);
                }
            }
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

    if (!name) return alert('Vui lòng nhập tên!');
    if (selectedCells.length === 0) return alert('Bạn chưa chọn ô nào.');

    const hasConflict = selectedCells.some(cell => cell.innerHTML.trim() !== "");
    if (hasConflict) {
        alert("Trong số các ô bạn chọn có ô đã được đặt. Vui lòng bỏ chọn các ô đó.");
        return;
    }

    const bookings = selectedCells.map(cell => {
        const typeRaw = cell.dataset.type?.trim().toLowerCase();
        let actualType = typeRaw;
        if (typeRaw === "giờ chẵn" || typeRaw === "giờ lẻ") actualType = "vãng lai";
        if (cell.classList.contains("bg-red-300")) actualType = "pass sân";

        const colorMap = {
            "pass sân": "bg-red-300",
            "cố định": "bg-blue-400",
            "vãng lai": typeRaw === "giờ chẵn" ? "bg-green-400" : "bg-yellow-400"
        };

        cell.className = `border px-4 py-3 text-center custom-cell text-white ${colorMap[actualType] || ""}`;

        return {
            court_id: parseInt(cell.dataset.court),
            date: cell.dataset.date,
            start_time: formatTime(parseFloat(cell.dataset.hour)),
            end_time: formatTime(parseFloat(cell.dataset.hour) + 0.5),
            type: actualType,
            is_paid: isPaid,
            payment_method: paymentMethod || null,
            manual_price: !isNaN(manualPrice) ? manualPrice : null
        };
    });

    const payload = { name, phone, bookings };

    $.ajax({
        url: '/AdminQuanLy/UpdateBookingsWithCustomer',
        method: 'POST',
        contentType: 'application/json',
        dataType: 'json',
        data: JSON.stringify(payload),
        success: function (res) {
            if (res.success) {
                alert("✅ Cập nhật lịch thành công!");
                const onlyChangeColor = bookings.every(b => b.type === 'pass sân');
                clearSelection?.();
                if (!onlyChangeColor) renderBookingTable?.();
            } else if (res.conflict) {
                const useOld = confirm(`${res.message}\n\nOK: ${res.options[0]}\nCancel: ${res.options[1]}`);
                $.ajax({
                    url: '/AdminQuanLy/UpdateBookingsWithCustomer',
                    method: 'POST',
                    contentType: 'application/json',
                    dataType: 'json',
                    data: JSON.stringify({ ...payload, name: useOld ? res.options[0] : res.options[1] }),
                    success: r2 => {
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
        error: xhr => alert("❌ Lỗi server: " + xhr.responseText)
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

            // Cập nhật lại nội dung nút dựa vào trạng thái checkbox
            event.target.textContent = allChecked ? 'Chọn tất cả' : 'Bỏ chọn tất cả';

            applyDayFilter();
}
//===============================================================================================================================================================================
         function applyDayFilter() {
                const visibleDates = Array.from(document.querySelectorAll('.day-checkbox:checked')).map(cb => cb.value);

                document.querySelectorAll('#thead th[data-date]').forEach(th => {
                    th.style.display = visibleDates.includes(th.dataset.date) ? '' : 'none';
                });
                document.querySelectorAll('#tbody td[data-date]').forEach(td => {
                    td.style.display = visibleDates.includes(td.dataset.date) ? '' : 'none';
                });
            }
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
function renderBookingTable() {
    const numCourts = 6;
    const startHour = 5;
    const endHour = 24;

    const selectedMonth = parseInt(document.getElementById('monthSelect').value);
    const selectedYear = parseInt(document.getElementById('yearSelect').value);

    const dates = [];
    const daysInMonth = new Date(selectedYear, selectedMonth + 1, 0).getDate();
    for (let i = 1; i <= daysInMonth; i++) {
        const d = new Date(selectedYear, selectedMonth, i);
        dates.push({
            date: formatDateToYMD(d),
            label: d.toLocaleDateString('vi-VN', { weekday: 'short', day: '2-digit', month: '2-digit' })
        });
    }

    // ✅ Tạo lại dropdown lọc ngày và giờ
    populateDayHourDropdowns(dates);

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

    // ❌ Bỏ phần checkbox cũ tại đây (dayFilter)

    // Gọi API
    $.getJSON("/AdminQuanLy/GetBookings", {
        month: selectedMonth + 1,
        year: selectedYear
    }, function (data) {
        document.querySelectorAll('.custom-cell').forEach(cell => {
            cell.innerHTML = "";
            cell.className = 'border px-4 py-3 text-center custom-cell hover:bg-green-100 cursor-pointer select-none';
            delete cell.dataset.bookingId;
        });

        const grouped = {};
        data.forEach(b => {
            const key = `${b.date}_${b.court_id}`;
            if (!grouped[key]) grouped[key] = [];
            grouped[key].push(b);
        });

        for (const key in grouped) {
            const list = grouped[key].sort((a, b) => parseFloat(a.start_time) - parseFloat(b.start_time));
            let i = 0;
            while (i < list.length) {
                const b = list[i];
                const merged = {
                    ...b,
                    start_time: parseFloat(b.start_time),
                    end_time: parseFloat(b.end_time),
                    total_price: b.manual_price > 0 ? b.manual_price : b.price
                };
                const name = (b.customer_name || "").trim();
                const phone = (b.customer_phone || "").trim();

                let j = i + 1;
                while (
                    j < list.length &&
                    parseFloat(list[j].start_time) === merged.end_time &&
                    list[j].is_paid === b.is_paid &&
                    ((list[j].customer_name || "").trim() === name || (list[j].customer_phone || "").trim() === phone)
                ) {
                    merged.end_time = parseFloat(list[j].end_time);
                    merged.total_price += list[j].manual_price > 0 ? list[j].manual_price : list[j].price;
                    j++;
                }
                i = j;

                const selector = `td[data-court="${b.court_id}"][data-hour="${merged.start_time.toFixed(1)}"][data-date="${b.date}"]`;
                const cell = document.querySelector(selector);
                if (!cell) continue;

                const rowspan = (merged.end_time - merged.start_time) * 2;
                const isWhole = x => Math.abs(x - Math.round(x)) < 0.01;
                const bgColor = {
                    "cố định": "bg-blue-400",
                    "pass sân": "bg-red-300",
                    "vãng lai": isWhole(merged.start_time) && isWhole(merged.end_time) ? "bg-green-400" : "bg-yellow-400"
                }[b.type] || "bg-yellow-400";

                const textColor = b.is_paid ? "text-black" : "text-red-600";
                const priceStr = merged.total_price.toLocaleString('vi-VN', { style: 'currency', currency: 'VND' });

                cell.rowSpan = rowspan;
                cell.classList.add(bgColor, "text-white");
                cell.classList.remove("hover:bg-green-100");
                cell.innerHTML = `<div class="${textColor} font-semibold">${name}${merged.total_price > 0 ? `<br/>${priceStr}` : ''}</div>`;
                cell.dataset.bookingId = b.id;

                cell.addEventListener('click', () => {
                    document.getElementById("nameInput").value = name;
                    document.getElementById("phoneInput").value = phone;

                    $.ajax({
                        url: "/AdminQuanLy/XemChiTietBooking",
                        data: {
                            date: b.date,
                            court_id: b.court_id,
                            hour: merged.start_time
                        },
                        dataType: "html",
                        success: function (html) {
                            const detailDiv = document.getElementById("bookingDetailPopup");
                            if (detailDiv) {
                                detailDiv.innerHTML = ""; // Xóa nội dung cũ
                                detailDiv.innerHTML = html; // Gán nội dung mới
                                detailDiv.style.display = "block"; // Đảm bảo nó hiển thị lại
                            }
                        },
                        error: function () {
                            alert("Không tải được chi tiết đặt sân.");
                        }
                    });
                });

                for (let h = merged.start_time + 0.5; h < merged.end_time; h += 0.5) {
                    const removeSelector = `td[data-court="${b.court_id}"][data-hour="${h.toFixed(1)}"][data-date="${b.date}"]`;
                    document.querySelector(removeSelector)?.remove();
                }
            }
        }

        document.addEventListener("click", function (e) {
            const popup = document.getElementById("bookingDetailPopup");
            if (popup && !popup.contains(e.target) && !e.target.closest('.custom-cell')) {
                closeBookingDetail();
            }
        });

        localStorage.removeItem("pass_san_custom");
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


//=========================================================================================================================
function populateDayHourDropdowns(dates) {
    const dayDiv = $('#dayFilterDropdown').empty();
    const hourDiv = $('#hourFilterDropdown').empty();

    // Thêm dòng check tất cả
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

    dates.forEach(d => {
        const label = d.label;
        const value = d.date;
        const checkbox = $(`<div><label><input type="checkbox" class="day-checkbox mr-2" value="${value}" checked /> ${label}</label></div>`);
        dayDiv.append(checkbox);
    });

    for (let h = 5; h < 24; h++) {
        const label = `${h}h - ${h + 1}h`;
        const value = h.toFixed(1);
        const checkbox = $(`<div><label><input type="checkbox" class="hour-checkbox mr-2" value="${value}" checked /> ${label}</label></div>`);
        hourDiv.append(checkbox);
    }

    // Gắn lại sự kiện sau khi đã thêm vào DOM
    $('#checkAllDays').on('change', function () {
        const checked = this.checked;
        $('.day-checkbox').prop('checked', checked);
        filterBookingTable();
    });
    $('#checkAllHours').on('change', function () {
        const checked = this.checked;
        $('.hour-checkbox').prop('checked', checked);
        filterBookingTable();
    });
}


function filterBookingTable() {
    const selectedDays = $('.day-checkbox:checked').map((_, el) => el.value).get();
    const selectedHours = $('.hour-checkbox:checked').map((_, el) => el.value).get();

    document.querySelectorAll('#tbody tr').forEach(row => {
        const rowHour = row.dataset.hour;
        const matchHour = selectedHours.includes(rowHour);
        row.style.display = matchHour ? '' : 'none';

        row.querySelectorAll('td').forEach(cell => {
            if (cell.dataset.date) {
                cell.style.display = selectedDays.includes(cell.dataset.date) ? '' : 'none';
            }
        });
    });
}

   

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

    renderBookingTable();
    filterBookingTable();

    // Gắn sự kiện sau khi DOM sẵn sàng
    $(document).on('change', '.day-checkbox, .hour-checkbox', filterBookingTable);
    $('#toggleDayFilter').click(() => $('#dayFilterDropdown').toggle());
    $('#toggleHourFilter').click(() => $('#hourFilterDropdown').toggle());
   

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
            const wb = XLSX.utils.book_new();
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
            let rowOffset = 1; // Bắt đầu từ dòng 1 do dòng 0 là header

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

                    // Nếu ô có rowspan > 1 thì thêm merge
                    const rs = c.rowSpan || 1;
                    if (rs > 1) {
                        merges.push({
                            s: { r: rowOffset, c: cIdx },
                            e: { r: rowOffset + rs - 1, c: cIdx }
                        });
                    }

                    row.push(cellObj);
                }

                ws_data.push(row);
                rowOffset++;
            }

            const ws = XLSX.utils.aoa_to_sheet(ws_data);
            ws['!cols'] = new Array(ws_data[0].length).fill({ wch: 20 });
            ws['!rows'] = ws_data.map(() => ({ hpt: 24 }));
            ws['!merges'] = merges;

            XLSX.utils.book_append_sheet(wb, ws, "LichSan");
            XLSX.writeFile(wb, "LichSan.xlsx");
        }
 
