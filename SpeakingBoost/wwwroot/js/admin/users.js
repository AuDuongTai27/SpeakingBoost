/**
 * users.js — Quản lý Học sinh
 */
(function () {
    'use strict';

    const PAGE_SIZE = 10;
    let allUsers = [];
    const pg = new Paginator({ containerId: 'usersBody', pageSize: PAGE_SIZE, infoId: 'pgInfo', wrapId: 'pgWrap' });

    // ── Load ────────────────────────────────────────────────────────
    async function loadUsers() {
        const res = await Api.get('/api/admin/users');
        if (!res.success) { Toast.err('Lỗi tải dữ liệu!'); return; }
        allUsers = res.data;
        applyFilter();
    }

    // ── Filter ──────────────────────────────────────────────────────
    function applyFilter() {
        const q = document.getElementById('searchInput').value.toLowerCase();
        const filtered = allUsers.filter(u =>
            !q || u.fullName.toLowerCase().includes(q) || u.email.toLowerCase().includes(q)
        );
        pg.setData(filtered, renderRows);
    }

    // ── Render rows ─────────────────────────────────────────────────
    function renderRows(users) {
        if (!users.length) return '<tr><td colspan="3" class="text-center py-4 text-muted">Không có dữ liệu</td></tr>';
        return users.map(u => {
            const initials = (u.fullName || '?').split(' ').map(w => w[0]).join('').substring(0, 2).toUpperCase();
            return `<tr>
                <td><div class="d-flex align-items-center gap-2">
                    <div style="width:34px;height:34px;border-radius:50%;background:linear-gradient(135deg,#6366f1,#06b6d4);
                        display:flex;align-items:center;justify-content:center;color:#fff;font-size:.75rem;font-weight:600;flex-shrink:0;">
                        ${initials}
                    </div>
                    <span class="fw-500">${u.fullName}</span>
                </div></td>
                <td class="text-muted">${u.email}</td>
                <td class="text-center">
                    <button class="btn btn-sm btn-light rounded-pill me-1" onclick="openEditUser(${u.userId})" title="Sửa">
                        <i class="bi bi-pencil-fill text-primary"></i>
                    </button>
                    <button class="btn btn-sm btn-light rounded-pill" onclick="deleteUser(${u.userId},'${u.fullName.replace(/'/g, "\\'")}')" title="Xóa">
                        <i class="bi bi-trash-fill text-danger"></i>
                    </button>
                </td>
            </tr>`;
        }).join('');
    }

    // ── Create / Edit ───────────────────────────────────────────────
    window.openCreateUser = function () {
        document.getElementById('modalUserTitle').textContent = 'Thêm học sinh';
        document.getElementById('userId').value = '';
        document.getElementById('userFullName').value = '';
        document.getElementById('userEmail').value = '';
        document.getElementById('userPassword').value = '';
        document.getElementById('pwSection').style.display = '';
    };

    window.openEditUser = function (id) {
        const u = allUsers.find(x => x.userId === id);
        if (!u) return;
        document.getElementById('modalUserTitle').textContent = 'Chỉnh sửa học sinh';
        document.getElementById('userId').value = u.userId;
        document.getElementById('userFullName').value = u.fullName;
        document.getElementById('userEmail').value = u.email;
        document.getElementById('pwSection').style.display = 'none';
        new bootstrap.Modal(document.getElementById('modalUser')).show();
    };

    window.saveUser = async function () {
        const id = document.getElementById('userId').value;
        const fullName = document.getElementById('userFullName').value.trim();
        const email = document.getElementById('userEmail').value.trim();
        const password = document.getElementById('userPassword').value;

        if (!fullName || !email) { Toast.err('Vui lòng điền đầy đủ thông tin.'); return; }

        const btn = document.getElementById('btnSaveUser');
        btn.disabled = true;

        let res;
        if (id) {
            // Cập nhật học sinh (không gửi role)
            res = await Api.put(`/api/admin/users/${id}`, { fullName, email });
        } else {
            // Tạo mới học sinh (không gửi role, backend tự set = "user")
            if (!password) { Toast.err('Vui lòng nhập mật khẩu.'); btn.disabled = false; return; }
            res = await Api.post('/api/admin/users', { fullName, email, password });
        }

        btn.disabled = false;
        if (res.success) {
            Toast.ok(id ? 'Cập nhật thành công!' : 'Tạo học sinh thành công!');
            bootstrap.Modal.getInstance(document.getElementById('modalUser'))?.hide();
            loadUsers();
        } else {
            Toast.err(res.message || 'Lỗi! Vui lòng thử lại.');
        }
    };

    window.deleteUser = async function (id, name) {
        if (!await adminConfirm(`Xóa học sinh "${name}"? Hành động này không thể hoàn tác.`)) return;
        const res = await Api.del(`/api/admin/users/${id}`);
        if (res.success) { Toast.ok('Đã xóa học sinh!'); loadUsers(); }
        else Toast.err(res.message || 'Xóa thất bại.');
    };

    // ── Wire up filter inputs ────────────────────────────────────────
    document.getElementById('searchInput').addEventListener('input', applyFilter);

    // ── Init ────────────────────────────────────────────────────────
    initAdminPage('nav-users', 'Quản lý Học sinh');
    AuthGuard.onReady(() => loadUsers());
})();