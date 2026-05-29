/**
 * dashboard.js — Admin Dashboard
 */
(function () {
    'use strict';

    initAdminPage('nav-dashboard', 'Dashboard');

    AuthGuard.onReady(async user => {
        // Welcome
        const name = user.fullName || 'Admin';
        document.getElementById('welcomeMsg').textContent = `Chào mừng, ${name}! 👋`;
        document.getElementById('welcomeSubMsg').textContent = 'Quản lý hệ thống luyện IELTS Speaking';

        try {
            // Load dashboard data
            const [usersRes, classesRes, exRes, dashboardRes] = await Promise.all([
                Api.get('/api/admin/users'),
                Api.get('/api/admin/classes'),
                Api.get('/api/admin/tests/topics'),
                Api.get('/api/admin/dashboard')
            ]);

            // Deadline API is optional for now
            // If this endpoint is not available, dashboard should still work
            let deadlinesRes = null;

            try {
                deadlinesRes = await Api.get('/api/admin/deadlines');
            } catch {
                deadlinesRes = {
                    success: false,
                    data: []
                };
            }

            // Card: Students
            if (usersRes.success && Array.isArray(usersRes.data)) {
                const users = usersRes.data.filter(u => (u.role || '').toLowerCase() === 'user');
                document.querySelector('#cardUsers .stat-value').textContent = users.length;
                document.querySelector('#cardUsers .stat-delta').textContent = 'Đang hoạt động';
            }

            // Card: Classes
            if (classesRes.success && Array.isArray(classesRes.data)) {
                document.querySelector('#cardClasses .stat-value').textContent = classesRes.data.length;
                document.querySelector('#cardClasses .stat-delta').textContent = 'Đang quản lý';
            }

            // Card: Exercises
            if (exRes.success && Array.isArray(exRes.data)) {
                const totalEx = exRes.data.reduce((sum, topic) => sum + (topic.exerciseCount || 0), 0);
                document.querySelector('#cardExercises .stat-value').textContent = totalEx;
                document.querySelector('#cardExercises .stat-delta').textContent = `${exRes.data.length} chủ đề`;
            }

            // Card: Deadlines
            if (deadlinesRes?.success && deadlinesRes.data) {
                const activeDeadlines = deadlinesRes.data.activeDeadlines || [];
                const totalDeadlines = activeDeadlines.length;

                document.querySelector('#cardDeadlines .stat-value').textContent = totalDeadlines;
                document.querySelector('#cardDeadlines .stat-delta').textContent = 'Đang áp dụng';
            } else {
                document.querySelector('#cardDeadlines .stat-value').textContent = '—';
                document.querySelector('#cardDeadlines .stat-delta').textContent = 'Chưa tải được';
            }

            // Dashboard details
            const dashboardData = dashboardRes.success ? dashboardRes.data : null;

            renderClassOverview(dashboardData?.classList || []);
            renderRecentActivities(dashboardData?.recentActivities || []);

        } catch (error) {
            console.error('Dashboard load error:', error);
            Toast.err('Không thể tải dữ liệu dashboard.');
        }
    });

    function renderClassOverview(classList) {
        const container = document.getElementById('classOverviewList');
        if (!container) return;

        if (!classList || classList.length === 0) {
            container.innerHTML = `
                <div class="empty-state">
                    <i class="bi bi-building"></i>
                    <span>Chưa có lớp học nào.</span>
                </div>
            `;
            return;
        }

        container.innerHTML = classList.map(c => `
            <div class="class-overview-item">
                <div>
                    <div class="class-name">${escapeHtml(c.className || 'Không có tên lớp')}</div>
                    <div class="class-meta">Mã lớp: ${c.classId}</div>
                </div>
                <span class="class-count">${c.studentCount || 0} học sinh</span>
            </div>
        `).join('');
    }

    function renderRecentActivities(activities) {
        const container = document.getElementById('recentActivityList');
        if (!container) return;

        if (!activities || activities.length === 0) {
            container.innerHTML = `
                <div class="empty-state">
                    <i class="bi bi-inbox"></i>
                    <span>Chưa có bài nộp nào trong 7 ngày gần đây.</span>
                    <small>Khi học sinh nộp bài mới, hoạt động sẽ hiển thị tại đây.</small>
                </div>
            `;
            return;
        }

        container.innerHTML = activities.map(a => `
            <div class="recent-activity-item">
                <div>
                    <div class="activity-title">${escapeHtml(a.studentName || 'Học sinh')} đã nộp bài</div>
                    <div class="activity-meta">
                        ${escapeHtml(a.exerciseTitle || 'Bài tập')} · ${formatDateTime(a.createdAt)}
                    </div>
                </div>
                ${a.overall != null ? `<span class="class-count">${a.overall}</span>` : ''}
            </div>
        `).join('');
    }

    function formatDateTime(value) {
        if (!value) return 'Không rõ thời gian';

        try {
            return new Date(value).toLocaleString('vi-VN', {
                day: '2-digit',
                month: '2-digit',
                year: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            });
        } catch {
            return 'Không rõ thời gian';
        }
    }

    function escapeHtml(value) {
        return String(value)
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#039;');
    }
})();