(function () {
    'use strict';

    initStudentPage('dashboard', async () => {
        // Load dashboard stats
        const dash = await Api.get('/api/student/dashboard');
        if (dash.success && dash.data) {
            const d = dash.data;
            document.getElementById('statSubmitted').textContent = d.completedExercisesCount ?? 0;
            document.getElementById('statAvg').textContent = d.averageScore != null ? d.averageScore.toFixed(1) : '—';
            document.getElementById('statPending').textContent = d.pendingAssignmentsCount ?? 0;
        }

        // Load recent submissions (top 3)
        const hist = await Api.get('/api/student/submissions/all-history');
        const container = document.getElementById('recentSubmissions');
        if (!hist.success || !hist.data || hist.data.length === 0) {
            container.innerHTML = `<div class="col-12 text-center py-5 text-muted"><i class="bi bi-inbox" style="font-size:2.5rem;opacity:0.4;display:block;margin-bottom:0.75rem;"></i><p>Chưa có bài nộp nào.</p></div>`;
            return;
        }
        const recent = hist.data.slice(0, 3);
        const colors = ['#ede9fe:#7c3aed', '#dcfce7:#16a34a', '#e0f2fe:#0284c7'];
        container.innerHTML = recent.map((s, i) => {
            const sc = scoreStyle(s.overall);
            const [bgC, clrC] = (colors[i] || '#f1f5f9:#64748b').split(':');
            const isDeadline = !!s.classExerciseId;
            return `<div class="col-md-6 col-lg-4">
                <div class="bg-white rounded-4 shadow-sm p-3" style="border:1px solid #e2e8f0;">
                    <div class="d-flex align-items-center gap-3 mb-3">
                        <div style="width:36px;height:36px;border-radius:50%;background:${bgC};color:${clrC};font-size:0.8rem;font-weight:700;display:flex;align-items:center;justify-content:center;flex-shrink:0;">#${i+1}</div>
                        <div class="flex-grow-1" style="min-width:0;">
                            <div style="font-weight:600;font-size:0.875rem;color:#1e293b;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">${s.exerciseTitle}</div>
                            <div style="font-size:0.72rem;color:#64748b;">${fmtDateTime(s.createdAt)} · ${isDeadline ? 'Deadline' : 'Practice'}</div>
                        </div>
                        <span style="padding:4px 12px;border-radius:999px;background:${sc.bg};color:${sc.color};font-weight:700;font-size:0.8rem;">${sc.text}</span>
                    </div>
                    <a href="/student/submission-detail.html?id=${s.submissionId}" class="btn btn-sm btn-outline-primary rounded-pill w-100" style="font-size:0.78rem;">Xem chi tiết <i class="bi bi-chevron-right ms-1"></i></a>
                </div>
            </div>`;
        }).join('');
    });
})();
