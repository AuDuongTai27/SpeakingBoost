(function () {
    'use strict';

    initStudentPage('profile');

    AuthGuard.onReady(async () => {
        loadProfile();
    });

    async function loadProfile() {
        const res = await Api.get('/api/profile');
        if (!res.success) { Toast.err(res.message || 'Lỗi tải hồ sơ'); return; }

        const d = res.data;
        const initials = (d.fullName || 'User').split(' ').map(w => w[0]).join('').substring(0, 2).toUpperCase();

        document.getElementById('profileAvatar').textContent = initials;
        document.getElementById('profileName').textContent = d.fullName;
        document.getElementById('profileEmail').textContent = d.email;
        document.getElementById('fullName').value = d.fullName;

        const roleLabel = d.role === 'student' ? 'Học sinh' : d.role;
        document.getElementById('statRole').textContent = roleLabel;
        document.getElementById('statJoined').textContent = fmtDate(d.createdAt);
    }

    window.updateProfile = async function (e) {
        e.preventDefault();
        const btn = document.getElementById('btnSave');
        const original = btn.innerHTML;
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Đang lưu…';

        const payload = {
            fullName: document.getElementById('fullName').value.trim(),
            password: document.getElementById('password').value || null
        };

        const res = await Api.put('/api/profile', payload);
        btn.disabled = false;
        btn.innerHTML = original;

        if (res.success) {
            Toast.ok('Cập nhật thành công!');
            document.getElementById('password').value = '';

            // Reflect changes in hero
            const initials = payload.fullName.split(' ').map(w => w[0]).join('').substring(0, 2).toUpperCase();
            document.getElementById('profileAvatar').textContent = initials;
            document.getElementById('profileName').textContent = payload.fullName;

            // Update navbar
            const nb = document.getElementById('navbarName');
            const na = document.getElementById('navbarAvatar');
            if (nb) nb.textContent = payload.fullName;
            if (na) na.textContent = initials;
        } else {
            Toast.err(res.message || 'Lỗi cập nhật hồ sơ');
        }
    };
})();
