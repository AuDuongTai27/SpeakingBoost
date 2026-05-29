(function () {
    'use strict';

    window.criteriaBar = function (label, val, color) {
        const pct = val != null ? ((val / 9) * 100).toFixed(1) : 0;
        const display = val != null ? val.toFixed(1) : '—';
        return `<div class="criteria-bar">
            <div class="criteria-label"><span>${label}</span><span style="font-weight:700;color:${color};">${display}</span></div>
            <div class="criteria-bg"><div class="criteria-fill" style="width:${pct}%;background:${color};"></div></div>
        </div>`;
    };

    initStudentPage('history', async () => {
        const params = new URLSearchParams(location.search);
        const id = params.get('id');
        if (!id) { Toast.err('Không tìm thấy ID bài nộp.'); return; }

        const res = await Api.get(`/api/student/submissions/${id}`);
        if (!res.success || !res.data) { Toast.err(res.message || 'Không tải được bài nộp.'); return; }
        const d = res.data;

        document.getElementById('pageTitle').textContent = d.exerciseTitle || 'Chi tiết bài nộp';

        const isDeadline = !!d.classExerciseId;
        const typeLabel = isDeadline
            ? `<span style="background:#fef3c7;color:#92400e;padding:5px 14px;border-radius:9999px;font-size:0.82rem;font-weight:700;">Deadline</span>`
            : `<span style="background:#ede9fe;color:#7c3aed;padding:5px 14px;border-radius:9999px;font-size:0.82rem;font-weight:700;">Practice</span>`;
        const partLabel = d.type ? `<span style="background:#e0f2fe;color:#0284c7;padding:5px 14px;border-radius:9999px;font-size:0.82rem;font-weight:700;">${d.type.toUpperCase()}</span>` : '';
        const statusLabel = statusBadge(d.status);

        document.getElementById('infoBadges').innerHTML = `
            ${typeLabel}${partLabel}${statusLabel}
            <span style="font-size:0.82rem;color:#64748b;display:flex;align-items:center;gap:4px;">
                <i class="bi bi-clock"></i>${fmtDateTime(d.createdAt)} · Lần nộp #${d.attemptNumber}
            </span>`;

        const sc = scoreStyle(d.overallScore);
        const backHref = isDeadline ? '/student/deadlines.html' : '/student/practice.html';
        const backLabel = isDeadline ? 'Quay lại Deadline' : 'Luyện lại';

        // Try parse AI feedback JSON
        let feedbackText = d.aiFeedback || 'Chưa có phản hồi.';
        try {
            if (typeof d.feedbackJson === 'object' && d.feedbackJson !== null) {
                const fb = d.feedbackJson;
                let html = "";
                
                const renderIssues = (title, category) => {
                    if (category && category.issues && category.issues.length > 0) {
                        html += `<div class="mb-3"><h6 class="fw-bold" style="color:#6366f1;">${title}</h6>`;
                        category.issues.forEach(issue => {
                            html += `<div class="p-2 mb-2 rounded" style="background:#f8fafc; border-left:3px solid #f59e0b;">
                                        <div><span class="text-danger text-decoration-line-through">${issue.wrong}</span> <i class="bi bi-arrow-right mx-1"></i> <span class="text-success fw-semibold">${issue.right}</span></div>
                                        <div class="small text-muted mt-1">${issue.description}</div>
                                     </div>`;
                        });
                        html += `</div>`;
                    }
                };

                renderIssues("Fluency & Coherence", fb.fluency_coherence || fb.fluencyCoherence);
                renderIssues("Lexical Resource", fb.lexical_resource || fb.lexicalResource);
                renderIssues("Grammar", fb.grammar);

                const suggest = fb.suggestion_answer || fb.suggestionAnswer;
                if (suggest) {
                    html += `<div class="mb-3"><h6 class="fw-bold" style="color:#10b981;">Câu trả lời gợi ý</h6>
                             <div class="p-3 rounded" style="background:#ecfdf5; border:1px solid #d1fae5; font-size:0.9rem;">${suggest.replace(/\\n/g, '<br>').replace(/\n/g, '<br>')}</div></div>`;
                }

                if (html) {
                    feedbackText = html;
                } else if (fb.feedback || fb.Feedback) {
                    feedbackText = fb.feedback || fb.Feedback;
                }
            }
        } catch (e) {
            console.error("Lỗi parse AI feedback", e);
        }

        document.getElementById('detailContent').innerHTML = `
            <!-- LEFT: Score -->
            <div class="col-lg-5">
                <div class="card-base text-center p-4 mb-3" style="background:linear-gradient(135deg,#6366f1,#818cf8);border:none;">
                    <div style="width:100px;height:100px;border-radius:50%;background:rgba(255,255,255,0.2);display:flex;flex-direction:column;align-items:center;justify-content:center;margin:0 auto 1rem;backdrop-filter:blur(4px);">
                        <div style="font-size:2.8rem;font-weight:900;color:#fff;line-height:1;">${sc.text}</div>
                        <div style="font-size:0.65rem;font-weight:600;color:rgba(255,255,255,0.75);">Band Overall</div>
                    </div>
                    <h3 style="font-size:1rem;font-weight:700;color:#fff;margin-bottom:2px;">${d.exerciseTitle}</h3>
                    <p style="font-size:0.82rem;color:rgba(255,255,255,0.75);margin:0;">Đánh giá bởi AI · ${d.type || ''}</p>
                </div>
                <div class="card-base p-4">
                    <h5 style="font-weight:700;font-size:0.95rem;margin-bottom:1.25rem;color:#1e293b;">
                        <i class="bi bi-bar-chart-fill me-2" style="color:#6366f1;"></i>Chi tiết điểm số
                    </h5>
                    ${window.criteriaBar('Pronunciation', d.pronunciation, '#6366f1')}
                    ${window.criteriaBar('Grammar', d.grammar, '#818cf8')}
                    ${window.criteriaBar('Lexical Resource', d.lexicalResource, '#06b6d4')}
                    ${window.criteriaBar('Coherence & Fluency', d.coherence, '#6366f1')}
                </div>
                ${d.audioPath ? `<div class="card-base p-4 mt-3">
                    <div style="font-size:0.72rem;font-weight:700;color:#6366f1;text-transform:uppercase;margin-bottom:8px;"><i class="bi bi-volume-up me-1"></i>Audio đã nộp</div>
                    <audio controls style="width:100%;border-radius:8px;" src="${d.audioPath}"></audio>
                </div>` : ''}
            </div>

            <!-- RIGHT: Question + Transcript + Feedback -->
            <div class="col-lg-7">
                <div class="card-base p-4 mb-3" style="border-left:4px solid #6366f1;">
                    <div style="font-size:0.72rem;font-weight:700;color:#6366f1;text-transform:uppercase;letter-spacing:0.06em;margin-bottom:8px;">Câu hỏi</div>
                    <h2 style="font-size:1.05rem;font-weight:600;color:#1e293b;line-height:1.6;margin:0;">"${d.question || ''}"</h2>
                </div>

                ${d.transcript ? `<div class="transcript-box mb-3">
                    <div style="font-size:0.72rem;font-weight:700;color:#6366f1;text-transform:uppercase;margin-bottom:10px;"><i class="bi bi-card-text me-1"></i>Transcript (Bản ghi)</div>
                    <p style="line-height:1.8;margin:0;color:#1e293b;font-size:0.9rem;">${d.transcript}</p>
                </div>` : ''}

                <div class="feedback-box mb-3">
                    <div style="font-size:0.72rem;font-weight:700;color:#6366f1;text-transform:uppercase;margin-bottom:10px;"><i class="bi bi-robot me-1"></i>AI Feedback</div>
                    <div style="line-height:1.8;margin:0;color:#1e293b;font-size:0.9rem;">${feedbackText}</div>
                </div>

                ${d.sampleAnswer ? `<div class="card-base p-4" style="border:1px dashed #cbd5e1;">
                    <div style="font-size:0.72rem;font-weight:700;color:#64748b;text-transform:uppercase;margin-bottom:10px;"><i class="bi bi-lightbulb me-1"></i>Sample Answer</div>
                    <p style="line-height:1.8;margin:0;color:#475569;font-size:0.875rem;font-style:italic;">"${d.sampleAnswer}"</p>
                </div>` : ''}

                <div class="d-flex gap-2 flex-wrap mt-3">
                    <a href="${backHref}" class="btn btn-primary rounded-pill fw-semibold"><i class="bi bi-arrow-repeat me-2"></i>${backLabel}</a>
                    <a href="/student/history.html" class="btn btn-outline-secondary rounded-pill"><i class="bi bi-arrow-left me-1"></i>Quay lại lịch sử</a>
                </div>
            </div>`;
    });
})();
