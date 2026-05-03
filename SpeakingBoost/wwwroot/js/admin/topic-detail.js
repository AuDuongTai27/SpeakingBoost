/**
 * topic-detail.js — Chi tiết Chủ đề & Câu hỏi
 */
(function () {
    'use strict';

    const topicId = new URLSearchParams(location.search).get('id');
    if (!topicId) { location.href = '/admin/tests.html'; }

    let allExercises = [];
    const partColors = {
        part1: ['#dbeafe', '#1d4ed8'],
        part2: ['#fef3c7', '#d97706'],
        part3: ['#dcfce7', '#16a34a'],
    };

    async function loadTopic() {
        const res = await Api.get(`/api/admin/tests/topics/${topicId}`);
        if (!res.success) { Toast.err('Không tải được dữ liệu!'); return; }
        const d = res.data;
        document.getElementById('pageTitle').textContent = d.name;
        document.getElementById('pageDesc').textContent  = d.description || '';
        document.getElementById('breadName').textContent = d.name;
        allExercises = d.exercises || [];
        applyFilter();
    }

    function applyFilter() {
        const part = document.getElementById('partFilter').value.toLowerCase();
        const q    = document.getElementById('searchQ').value.toLowerCase();
        const filtered = allExercises.filter(e =>
            (!part || e.type.toLowerCase() === part) &&
            (!q    || e.title.toLowerCase().includes(q) || e.question.toLowerCase().includes(q))
        );
        renderExercises(filtered);
    }

    function renderExercises(exercises) {
        const container = document.getElementById('exercisesList');
        if (!exercises.length) {
            container.innerHTML = `<div class="text-center py-5 text-muted">
                <i class="bi bi-journal-x" style="font-size:3rem;opacity:.3;"></i>
                <p class="mt-2">Không có câu hỏi nào</p></div>`;
            return;
        }
        container.innerHTML = exercises.map((e, i) => {
            const [bg, tc] = partColors[e.type?.toLowerCase()] || ['#f1f5f9', '#64748b'];
            return `<div class="card-panel mb-3 p-4">
                <div class="d-flex justify-content-between align-items-start gap-3">
                    <div class="flex-grow-1">
                        <div class="d-flex align-items-center gap-2 mb-2">
                            <span style="background:${bg};color:${tc};padding:.15rem .6rem;border-radius:20px;font-size:.72rem;font-weight:700;">${e.type}</span>
                            <span class="text-muted" style="font-size:.78rem;">Câu ${i + 1} &middot; Tối đa ${e.maxAttempts} lần làm</span>
                        </div>
                        <h6 class="fw-600 mb-1">${e.title}</h6>
                        <p class="mb-0 text-muted" style="font-size:.875rem;">${e.question}</p>
                        ${e.sampleAnswer
                            ? `<div class="mt-2 p-2 rounded-2" style="background:#f8fafc;font-size:.8rem;color:#475569;">
                                <i class="bi bi-lightbulb text-warning me-1"></i>${e.sampleAnswer}</div>`
                            : ''}
                    </div>
                    <div class="d-flex gap-1 flex-shrink-0">
                        <button class="btn btn-sm btn-light rounded-pill" onclick="openEditExercise(${e.exerciseId})" title="Sửa">
                            <i class="bi bi-pencil-fill text-primary"></i>
                        </button>
                        <button class="btn btn-sm btn-light rounded-pill" onclick="deleteExercise(${e.exerciseId},'${e.title.replace(/'/g, "\\'")}')" title="Xóa">
                            <i class="bi bi-trash-fill text-danger"></i>
                        </button>
                    </div>
                </div>
            </div>`;
        }).join('');
    }

    // ── Exercise CRUD ────────────────────────────────────────────────
    window.openAddExercise = function () {
        document.getElementById('modalExTitle').textContent = 'Thêm câu hỏi';
        document.getElementById('exId').value              = '';
        document.getElementById('exTitle').value           = '';
        document.getElementById('exType').value            = 'Part1';
        document.getElementById('exQuestion').value        = '';
        document.getElementById('exSample').value          = '';
        document.getElementById('exMaxAttempts').value     = 3;
        new bootstrap.Modal(document.getElementById('modalExercise')).show();
    };

    window.openEditExercise = function (id) {
        const e = allExercises.find(x => x.exerciseId === id);
        if (!e) return;
        document.getElementById('modalExTitle').textContent = 'Chỉnh sửa câu hỏi';
        document.getElementById('exId').value              = e.exerciseId;
        document.getElementById('exTitle').value           = e.title;
        document.getElementById('exType').value            = e.type;
        document.getElementById('exQuestion').value        = e.question;
        document.getElementById('exSample').value          = e.sampleAnswer || '';
        document.getElementById('exMaxAttempts').value     = e.maxAttempts;
        new bootstrap.Modal(document.getElementById('modalExercise')).show();
    };

    window.saveExercise = async function () {
        const id   = document.getElementById('exId').value;
        const body = {
            title:        document.getElementById('exTitle').value.trim(),
            type:         document.getElementById('exType').value,
            question:     document.getElementById('exQuestion').value.trim(),
            sampleAnswer: document.getElementById('exSample').value.trim() || null,
            maxAttempts:  parseInt(document.getElementById('exMaxAttempts').value),
            topicId:      parseInt(topicId),
        };
        if (!body.title || !body.question) { Toast.err('Vui lòng điền tiêu đề và câu hỏi.'); return; }

        const res = id
            ? await Api.put(`/api/admin/tests/exercises/${id}`, body)
            : await Api.post(`/api/admin/tests/topics/${topicId}/exercises`, body);

        if (res.success) {
            Toast.ok(id ? 'Cập nhật thành công!' : 'Thêm câu hỏi thành công!');
            bootstrap.Modal.getInstance(document.getElementById('modalExercise'))?.hide();
            loadTopic();
        } else Toast.err(res.message || 'Lỗi!');
    };

    window.deleteExercise = async function (id, title) {
        if (!await adminConfirm(`Xóa câu hỏi "${title}"?`)) return;
        const res = await Api.del(`/api/admin/tests/exercises/${id}`);
        if (res.success) { Toast.ok('Đã xóa câu hỏi!'); loadTopic(); }
        else Toast.err(res.message || 'Xóa thất bại.');
    };

    // ── Import ───────────────────────────────────────────────────────
    window.openImport = function () {
        document.getElementById('importFile').value = '';
        new bootstrap.Modal(document.getElementById('modalImport')).show();
    };

    window.doImport = async function () {
        const file = document.getElementById('importFile').files[0];
        if (!file) { Toast.err('Vui lòng chọn file.'); return; }
        const btn = document.getElementById('btnImport');
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Đang import...';
        const fd = new FormData(); fd.append('excelFile', file);
        const res = await Api.postForm(`/api/admin/tests/topics/${topicId}/import`, fd);
        btn.disabled = false;
        btn.innerHTML = '<i class="bi bi-upload me-1"></i>Import';
        if (res.success) {
            Toast.ok(res.message || 'Import thành công!');
            bootstrap.Modal.getInstance(document.getElementById('modalImport'))?.hide();
            loadTopic();
        } else Toast.err(res.message || 'Import thất bại.');
    };

    // ── Filter wire-up ───────────────────────────────────────────────
    document.getElementById('partFilter').addEventListener('change', applyFilter);
    document.getElementById('searchQ').addEventListener('input', applyFilter);

    // ── Init ─────────────────────────────────────────────────────────
    initAdminPage('nav-tests', 'Chi tiết Chủ đề');
    AuthGuard.onReady(() => loadTopic());
})();
