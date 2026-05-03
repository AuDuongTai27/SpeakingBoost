/**
 * profile.js — Admin Profile page
 */
(function () {
    'use strict';

    initAdminPage('', 'Hồ sơ cá nhân');

    AuthGuard.onReady(async user => {
        // Pre-fill from JWT
        document.getElementById('profileName').textContent  = user.fullName || '—';
        document.getElementById('profileEmail').textContent = user.email    || '—';
        document.getElementById('profileRole').textContent  = 'Admin';

        const initials = (user.fullName || 'A').split(' ').map(w => w[0]).join('').substring(0, 2).toUpperCase();
        document.getElementById('profileAvatar').textContent = initials;

        // Also fetch from API for fresh data
        try {
            const res = await Api.get('/api/profile');
            if (res.success && res.data) {
                document.getElementById('editFullName').value = res.data.fullName || '';
            }
        } catch { /* non-critical */ }
    });

    window.saveProfile = async function () {
        const fullName = document.getElementById('editFullName').value.trim();
        const password = document.getElementById('editPassword').value.trim();

        if (!fullName) { Toast.err('Vui lòng nhập họ tên.'); return; }

        const btn = document.getElementById('btnSaveProfile');
        btn.disabled = true;

        const res = await Api.put('/api/profile', { fullName, password: password || undefined });

        btn.disabled = false;
        if (res.success) {
            Toast.ok('Cập nhật thông tin thành công!');
            document.getElementById('profileName').textContent = fullName;
            document.getElementById('editPassword').value = '';
        } else {
            Toast.err(res.message || 'Lỗi! Vui lòng thử lại.');
        }
    };
})();
