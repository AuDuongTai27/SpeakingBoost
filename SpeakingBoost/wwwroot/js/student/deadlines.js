(function () {
    'use strict';

    let allDeadlines = [];

    window.renderList = function () {
        const search = (document.getElementById('searchInput').value || '').toLowerCase();
        const items = search ? allDeadlines.filter(d => d.title.toLowerCase().includes(search)) : allDeadlines;
        const c = document.getElementById('deadlineList');
        if (!items.length) {
            c.innerHTML = `<div class="col-12 empty-state"><i class="bi bi-calendar-x"></i><p>Không có bài deadline nào.</p></div>`;
            return;
        }
        c.innerHTML = items.map(d => {
            const isPending = d.status === 'Pending';
            const isOverdue = d.status === 'Overdue';
            const isSubmitted = d.status === 'Submitted';
            const iconBg = isPending ? '#fef3c7' : isSubmitted ? '#dcfce7' : '#fee2e2';
            const iconColor = isPending ? '#f59e0b' : isSubmitted ? '#16a34a' : '#ef4444';
            const iconClass = isPending ? 'clock-fill' : isSubmitted ? 'check-circle-fill' : 'x-circle-fill';
            const badgeBg = isPending ? '#fef3c7' : isSubmitted ? '#dcfce7' : '#fee2e2';
            const badgeColor = isPending ? '#92400e' : isSubmitted ? '#14532d' : '#7f1d1d';
            const badgeLabel = isPending ? 'Chờ nộp' : isSubmitted ? 'Đã nộp' : 'Quá hạn';

            let actionHtml;
            if (isSubmitted) {
                const sc = scoreStyle(d.score);
                actionHtml = `<div style="font-size:0.82rem;margin-bottom:0.75rem;">
                    <i class="bi bi-star-fill text-warning me-1"></i>Điểm: <strong style="color:${sc.color};">${sc.text}</strong>
                </div>
                <a href="/student/submission-detail.html?id=${d.submissionId}" class="btn btn-outline-success rounded-pill fw-medium px-3" style="font-size:0.875rem;">
                    <i class="bi bi-eye me-1"></i>Xem kết quả
                </a>`;
            } else if (isPending) {
                actionHtml = `<div style="font-size:0.82rem;color:#64748b;margin-bottom:0.75rem;">
                    <i class="bi bi-calendar-event me-1"></i>Hạn nộp: <strong style="color:#f59e0b;">${fmtDateTime(d.deadline)}</strong>
                </div>
                <a href="/student/deadline-question.html?id=${d.classExerciseId}" class="btn btn-primary rounded-pill fw-semibold px-4" style="font-size:0.875rem;">
                    <i class="bi bi-mic-fill me-1"></i>Làm bài ngay
                </a>`;
            } else {
                actionHtml = `<div style="font-size:0.82rem;color:#64748b;margin-bottom:0.75rem;">
                    <i class="bi bi-calendar-event me-1"></i>Hạn: ${fmtDate(d.deadline)}
                </div>
                <span class="btn btn-outline-secondary rounded-pill px-3" style="font-size:0.875rem;opacity:0.6;cursor:default;">Đã quá hạn</span>`;
            }

            return `<div class="col-md-6 col-xl-4">
                <div class="deadline-card">
                    <div class="d-flex align-items-start gap-3 mb-3">
                        <div style="width:48px;height:48px;border-radius:12px;background:${iconBg};display:flex;align-items:center;justify-content:center;color:${iconColor};font-size:1.3rem;flex-shrink:0;">
                            <i class="bi bi-${iconClass}"></i>
                        </div>
                        <div class="flex-grow-1">
                            <span class="part-badge" style="background:${badgeBg};color:${badgeColor};margin-bottom:6px;display:inline-block;">${badgeLabel}</span>
                            <div style="font-weight:700;font-size:0.95rem;color:#1e293b;margin-top:2px;">${d.title}</div>
                            <div style="font-size:0.78rem;color:#64748b;">${d.className || ''}</div>
                        </div>
                    </div>
                    ${actionHtml}
                </div>
            </div>`;
        }).join('');
    };

    initStudentPage('deadlines', async () => {
        const res = await Api.get('/api/student/deadlines');
        if (!res.success) { Toast.err(res.message); return; }
        allDeadlines = res.data || [];
        document.getElementById('cntPending').textContent   = allDeadlines.filter(d => d.status === 'Pending').length;
        document.getElementById('cntSubmitted').textContent = allDeadlines.filter(d => d.status === 'Submitted').length;
        document.getElementById('cntOverdue').textContent   = allDeadlines.filter(d => d.status === 'Overdue').length;
        window.renderList();
    });
})();
