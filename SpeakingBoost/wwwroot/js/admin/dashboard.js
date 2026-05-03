/**
 * dashboard.js — Admin Dashboard
 */
(function () {
    'use strict';

    initAdminPage('nav-dashboard', 'Dashboard');

    AuthGuard.onReady(async user => {
        // Welcome
        const name = user.fullName || 'Admin';
        document.getElementById('welcomeMsg').textContent    = `Chào mừng, ${name}! 👋`;
        document.getElementById('welcomeSubMsg').textContent = 'Quản lý hệ thống luyện IELTS Speaking';

        // Load stats
        const [usersRes, classesRes, exRes] = await Promise.all([
            Api.get('/api/admin/users'),
            Api.get('/api/admin/classes'),
            Api.get('/api/admin/tests/topics'),
        ]);

        if (usersRes.success) {
            const users = usersRes.data.filter(u => u.role.toLowerCase() === 'user');
            document.querySelector('#cardUsers .stat-value').textContent  = users.length;
            document.querySelector('#cardUsers .stat-delta').textContent  = `${users.length} học sinh`;
        }
        if (classesRes.success) {
            document.querySelector('#cardClasses .stat-value').textContent = classesRes.data.length;
            document.querySelector('#cardClasses .stat-delta').textContent = `${classesRes.data.length} lớp`;
        }
        if (exRes.success) {
            const totalEx = exRes.data.reduce((s, t) => s + (t.exerciseCount || 0), 0);
            document.querySelector('#cardExercises .stat-value').textContent = totalEx;
            document.querySelector('#cardExercises .stat-delta').textContent = `${exRes.data.length} chủ đề`;
        }

        // Today submissions (optional — skip if API not available)
        try {
            const sub = await Api.get('/api/admin/dashboard');
            if (sub.success && sub.data) {
                document.querySelector('#cardSubmissions .stat-value').textContent =
                    sub.data.recentActivities?.length ?? '—';
                document.querySelector('#cardSubmissions .stat-delta').textContent = 'Hoạt động gần đây';
            }
        } catch { /* non-critical */ }
    });
})();
