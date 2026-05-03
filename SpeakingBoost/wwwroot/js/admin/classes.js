/**
 * classes.js — Quản lý Lớp học
 * - Hiển thị danh sách lớp với inline rename
 * - Có nút xóa lớp học
 * - Pagination
 */
(function () {
    'use strict';

    let allClasses = [];
    const pg = new Paginator({ containerId: 'classesBody', pageSize: 8, infoId: 'pgInfo', wrapId: 'pgWrap' });

    // ── Load ────────────────────────────────────────────────────────
    async function loadAll() {
        const res = await Api.get('/api/admin/classes');
        if (!res.success) { Toast.err('Lỗi tải dữ liệu!'); return; }
        allClasses = res.data;
        updateStats(allClasses);
        applyFilter();
    }

    function updateStats(classes) {
        document.getElementById('statClasses').textContent = classes.length;
        document.getElementById('statStudents').textContent = classes.reduce((s, c) => s + (c.studentCount || 0), 0);
    }

    // ── Filter ──────────────────────────────────────────────────────
    function applyFilter() {
        const q = document.getElementById('searchInput').value.toLowerCase();
        const filtered = allClasses.filter(c => c.className.toLowerCase().includes(q));
        pg.setData(filtered, renderRows);
    }

    // ── Render rows ─────────────────────────────────────────────────
    function renderRows(data) {
        if (!data.length) return '<tr><td colspan="3" class="text-center py-4 text-muted">Không có lớp học nào</td></tr>';
        return data.map(c => `
            <tr>
                <td>
                    <span class="class-name-view fw-500" id="nameView_${c.classId}" title="Nhấp đúp để đổi tên"
                          ondblclick="startRename(${c.classId})">${c.className}</span>
                    <input class="form-control form-control-sm class-name-input" 
                           style="max-width:220px; display:none;" 
                           id="nameInput_${c.classId}"
                           value="${c.className}"
                           onblur="commitRename(${c.classId})"
                           onkeydown="if(event.key==='Enter')commitRename(${c.classId});if(event.key==='Escape')cancelRename(${c.classId})">
                </td>
                <td><span class="badge bg-light text-dark">${c.studentCount} học sinh</span></td>
                <td class="text-center">
                    <a href="/admin/class-detail.html?id=${c.classId}" class="btn btn-sm btn-light rounded-pill me-1" title="Chi tiết">
                        <i class="bi bi-eye-fill text-info"></i>
                    </a>
                    <button class="btn btn-sm btn-light rounded-pill" onclick="deleteClass(${c.classId},'${c.className.replace(/'/g, "\\'")}')" title="Xóa">
                        <i class="bi bi-trash-fill text-danger"></i>
                    </button>
                </td>
            </tr>`).join('');
    }

    // ── Inline rename ────────────────────────────────────────────────
    window.startRename = function (id) {
        const spanEl = document.getElementById(`nameView_${id}`);
        const inputEl = document.getElementById(`nameInput_${id}`);
        if (!spanEl || !inputEl) return;
        inputEl.value = spanEl.textContent.trim();
        spanEl.style.display = 'none';
        inputEl.style.display = 'inline-block';
        inputEl.focus();
        inputEl.select();
    };

    window.cancelRename = function (id) {
        const spanEl = document.getElementById(`nameView_${id}`);
        const inputEl = document.getElementById(`nameInput_${id}`);
        if (!spanEl || !inputEl) return;
        spanEl.style.display = '';
        inputEl.style.display = 'none';
    };

    window.commitRename = async function (id) {
        const spanEl = document.getElementById(`nameView_${id}`);
        const inputEl = document.getElementById(`nameInput_${id}`);
        if (!spanEl || !inputEl) return;

        const newName = inputEl.value.trim();
        if (!newName || newName === spanEl.textContent.trim()) {
            cancelRename(id);
            return;
        }

        inputEl.classList.add('class-name-saving');
        const res = await Api.put(`/api/admin/classes/${id}`, { className: newName });
        inputEl.classList.remove('class-name-saving');

        if (res.success) {
            spanEl.textContent = newName;
            const cls = allClasses.find(c => c.classId === id);
            if (cls) cls.className = newName;
            Toast.ok('Đã đổi tên lớp!');
        } else {
            Toast.err(res.message || 'Đổi tên thất bại.');
        }
        cancelRename(id);
    };

    // ── Delete class ────────────────────────────────────────────────
    window.deleteClass = async function (id, className) {
        if (!await adminConfirm(`Xóa lớp học "${className}"? Tất cả học sinh trong lớp sẽ bị xóa khỏi lớp này. Hành động này không thể hoàn tác.`)) return;

        const res = await Api.del(`/api/admin/classes/${id}`);
        if (res.success) {
            Toast.ok('Đã xóa lớp học!');
            loadAll();
        } else {
            Toast.err(res.message || 'Xóa thất bại.');
        }
    };

    // ── Create ───────────────────────────────────────────────────────
    window.openCreate = function () {
        document.getElementById('modalClassTitle').textContent = 'Tạo lớp mới';
        document.getElementById('classId').value = '';
        document.getElementById('className').value = '';
    };

    window.saveClass = async function () {
        const id = document.getElementById('classId').value;
        const className = document.getElementById('className').value.trim();
        if (!className) { Toast.err('Vui lòng nhập tên lớp.'); return; }

        const res = id
            ? await Api.put(`/api/admin/classes/${id}`, { className })
            : await Api.post('/api/admin/classes', { className });

        if (res.success) {
            Toast.ok(id ? 'Cập nhật thành công!' : 'Tạo lớp thành công!');
            bootstrap.Modal.getInstance(document.getElementById('modalClass'))?.hide();
            loadAll();
        } else Toast.err(res.message || 'Lỗi!');
    };

    // ── Wire up ──────────────────────────────────────────────────────
    document.getElementById('searchInput').addEventListener('input', applyFilter);

    initAdminPage('nav-classes', 'Quản lý Lớp học');
    AuthGuard.onReady(() => loadAll());
})();