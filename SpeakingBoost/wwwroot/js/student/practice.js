(function () {
    'use strict';

    let allTopics = [];
    let currentPart = 0;

    window.loadTopics = async function (part) {
        currentPart = part;
        document.getElementById('topicsContainer').innerHTML = `<div class="col-12 text-center py-5 text-muted"><div class="spinner-border spinner-border-sm me-2"></div>Đang tải...</div>`;
        const res = await Api.get(`/api/student/practice/topics?part=${part}`);
        if (!res.success) {
            document.getElementById('topicsContainer').innerHTML = `<div class="col-12 text-center py-5 text-muted"><i class="bi bi-exclamation-circle" style="font-size:2.5rem;opacity:0.4;display:block;margin-bottom:0.75rem;"></i><p>${res.message}</p></div>`;
            return;
        }
        allTopics = res.data || [];
        window.renderTopics();
    };

    window.renderTopics = function () {
        const search = (document.getElementById('searchInput').value || '').toLowerCase();
        const filtered = allTopics.filter(t => !search || t.title.toLowerCase().includes(search));
        const c = document.getElementById('topicsContainer');
        if (!filtered.length) {
            c.innerHTML = `<div class="empty-state"><i class="bi bi-inbox"></i><p>Không có chủ đề nào.</p></div>`;
            return;
        }
        c.innerHTML = filtered.map((t, i) => {
            const st = topicStyle(i);
            const partLabel = currentPart > 0 ? `Part ${currentPart}` : t.forecastLabel || 'Bộ đề';
            const partColors = { '1': ['#ede9fe','#7c3aed'], '2': ['#dcfce7','#16a34a'], '3': ['#e0f2fe','#0284c7'] };
            const [badgeBg, badgeColor] = partColors[String(currentPart)] || ['#f1f5f9','#64748b'];
            return `<div class="col-sm-6 col-md-4 col-xl-3">
                <a href="/student/practice-question.html?topicId=${t.id}&part=${currentPart}" class="topic-card d-block text-decoration-none">
                    <div style="width:48px;height:48px;border-radius:12px;display:flex;align-items:center;justify-content:center;margin-bottom:12px;font-size:1.3rem;background:${st.bg};color:${st.color};">
                        <i class="bi bi-${st.icon}"></i>
                    </div>
                    <span class="part-badge" style="background:${badgeBg};color:${badgeColor};">${partLabel}</span>
                    <div class="topic-card-title mt-1">${t.title}</div>
                    <div class="topic-card-meta"><i class="bi bi-question-circle me-1"></i>${t.questionCount} câu hỏi</div>
                    <div class="action-arrow mt-3"><i class="bi bi-arrow-right"></i> Bắt đầu</div>
                </a>
            </div>`;
        }).join('');
    };

    document.querySelectorAll('.btn-part').forEach(btn => {
        btn.addEventListener('click', function () {
            document.querySelectorAll('.btn-part').forEach(b => b.classList.remove('active'));
            this.classList.add('active');
            window.loadTopics(parseInt(this.dataset.part));
        });
    });

    document.getElementById('searchInput').addEventListener('input', window.renderTopics);

    initStudentPage('practice', () => window.loadTopics(0));
})();
