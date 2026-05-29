(function () {
    'use strict';

    let allItems = [];
    let currentMode = 'all';

    window.switchMode = function (mode, el) {
        currentMode = mode;
        document.querySelectorAll('.filter-tab').forEach(t => t.classList.remove('active'));
        el.classList.add('active');
        window.renderList();
    };

    window.renderList = function () {
        const search = (document.getElementById('searchInput').value || '').toLowerCase();
        let items = allItems;
        if (currentMode === 'practice') items = items.filter(i => !i.classExerciseId);
        else if (currentMode === 'deadline') items = items.filter(i => !!i.classExerciseId);
        if (search) items = items.filter(i => i.exerciseTitle.toLowerCase().includes(search));

        const list = document.getElementById('historyList');
        if (!items.length) {
            list.innerHTML = `<div class="col-12 empty-state"><i class="bi bi-inbox"></i><p>Không có bài nộp nào.</p></div>`;
            return;
        }
        list.innerHTML = items.map(item => {
            const sc = scoreStyle(item.overall);
            const isDeadline = !!item.classExerciseId;
            const typeLabel = isDeadline
                ? `<span style="background:#fef3c7;color:#92400e;padding:2px 8px;border-radius:999px;font-size:0.68rem;font-weight:700;">Deadline</span>`
                : `<span style="background:#ede9fe;color:#7c3aed;padding:2px 8px;border-radius:999px;font-size:0.68rem;font-weight:700;">Practice</span>`;
            const statusColor = item.status === 'Evaluated' ? '#16a34a' : item.status === 'Processing' ? '#0284c7' : '#64748b';
            const statusLabel = item.status === 'Evaluated' ? 'Đã chấm' : item.status === 'Processing' ? 'Đang chấm' : item.status;
            return `<div class="col-lg-6 col-md-6 col-12 mb-3">
                <a href="/student/submission-detail.html?id=${item.submissionId}" class="text-decoration-none">
                    <div class="bg-white rounded-4 shadow-sm p-4 h-100 border" style="border-color:#e2e8f0;transition:all 0.2s;">
                        <div class="row align-items-center g-3">
                            <div class="col-auto">
                                <div style="width:42px;height:42px;border-radius:50%;background:#ede9fe;color:#7c3aed;font-size:0.95rem;font-weight:700;display:flex;align-items:center;justify-content:center;">
                                    ${item.attemptNumber}
                                </div>
                            </div>
                            <div class="col flex-grow-1" style="min-width:0;">
                                <div style="font-weight:600;font-size:0.9rem;color:#1e293b;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;margin-bottom:4px;">
                                    ${typeLabel} ${item.exerciseTitle}
                                </div>
                                <div style="font-size:0.78rem;color:#64748b;">
                                    <i class="bi bi-clock me-1"></i>${fmtDateTime(item.createdAt)}
                                    <span style="margin-left:8px;color:${statusColor};"><i class="bi bi-circle-fill me-1" style="font-size:0.5rem;"></i>${statusLabel}</span>
                                </div>
                            </div>
                            <div class="col-auto text-end">
                                <div style="padding:6px 16px;border-radius:999px;background:${sc.bg};color:${sc.color};font-weight:700;font-size:0.9rem;">${sc.text}</div>
                            </div>
                        </div>
                    </div>
                </a>
            </div>`;
        }).join('');
    };

    initStudentPage('history', async () => {
        const res = await Api.get('/api/student/submissions/all-history');
        if (!res.success) { Toast.err(res.message); return; }
        allItems = res.data || [];
        window.renderList();
    });
})();
