/**
 * tests.js — Quản lý Đề thi (Topics & Questions)
 */
(function () {
    'use strict';

    let allTopics = [];

    async function loadTopics() {
        const res = await Api.get('/api/admin/tests/topics');
        if (!res.success) { Toast.err('Lỗi tải dữ liệu!'); return; }
        allTopics = res.data;
        renderTopics(allTopics);
    }

    function renderTopics(topics) {
        const grid = document.getElementById('topicsGrid');
        if (!topics.length) {
            grid.innerHTML = `<div class="col-12 text-center py-5 text-muted">
                <i class="bi bi-collection" style="font-size:3rem;opacity:.3;"></i>
                <p class="mt-2">Chưa có chủ đề nào</p></div>`;
            return;
        }
        grid.innerHTML = topics.map(t => `
            <div class="col-md-6 col-xl-4">
                <div class="card-panel p-4 h-100" style="border-top:3px solid #6366f1;">
                    <div class="d-flex justify-content-between align-items-start mb-3">
                        <div>
                            <h6 class="fw-600 mb-1" style="font-size:.95rem;">${t.name}</h6>
                            <small class="text-muted">${t.description || 'Không có mô tả'}</small>
                        </div>
                        <span class="badge rounded-pill" style="background:#ede9fe;color:#6366f1;">${t.exerciseCount} câu</span>
                    </div>
                    <div class="d-flex gap-2 flex-wrap">
                        <a href="/admin/topic-detail.html?id=${t.topicId}" class="btn btn-sm btn-primary rounded-pill flex-grow-1">
                            <i class="bi bi-eye me-1"></i>Xem câu hỏi
                        </a>
                        <button class="btn btn-sm btn-light rounded-pill" onclick="openImport(${t.topicId})" title="Import Excel">
                            <i class="bi bi-file-earmark-excel text-success"></i>
                        </button>
                        <button class="btn btn-sm btn-light rounded-pill" onclick="openEditTopic(${t.topicId})" title="Sửa">
                            <i class="bi bi-pencil-fill text-primary"></i>
                        </button>
                        <button class="btn btn-sm btn-light rounded-pill" onclick="deleteTopic(${t.topicId},'${t.name.replace(/'/g, "\\'")}')" title="Xóa">
                            <i class="bi bi-trash-fill text-danger"></i>
                        </button>
                    </div>
                </div>
            </div>`).join('');
    }

    window.openCreateTopic = function () {
        document.getElementById('modalTopicTitle').textContent = 'Thêm chủ đề mới';
        document.getElementById('topicId').value    = '';
        document.getElementById('topicName').value  = '';
        document.getElementById('topicDesc').value  = '';
    };

    window.openEditTopic = function (id) {
        const t = allTopics.find(x => x.topicId === id);
        if (!t) return;
        document.getElementById('modalTopicTitle').textContent = 'Chỉnh sửa chủ đề';
        document.getElementById('topicId').value    = t.topicId;
        document.getElementById('topicName').value  = t.name;
        document.getElementById('topicDesc').value  = t.description || '';
        new bootstrap.Modal(document.getElementById('modalTopic')).show();
    };

    window.saveTopic = async function () {
        const id   = document.getElementById('topicId').value;
        const name = document.getElementById('topicName').value.trim();
        const desc = document.getElementById('topicDesc').value.trim();
        if (!name) { Toast.err('Vui lòng nhập tên chủ đề.'); return; }

        let res;
        if (id) {
            res = await Api.put(`/api/admin/tests/topics/${id}`, { name, description: desc || null });
        } else {
            res = await Api.post('/api/admin/tests/topics', { name, description: desc || null });
        }

        if (res.success) {
            Toast.ok(id ? 'Cập nhật thành công!' : 'Thêm chủ đề thành công!');
            bootstrap.Modal.getInstance(document.getElementById('modalTopic'))?.hide();
            loadTopics();
        } else Toast.err(res.message || 'Lỗi!');
    };

    window.deleteTopic = async function (id, name) {
        if (!await adminConfirm(`Xóa chủ đề "${name}" và tất cả câu hỏi trong đó?`)) return;
        const res = await Api.del(`/api/admin/tests/topics/${id}`);
        if (res.success) { Toast.ok('Đã xóa chủ đề!'); loadTopics(); }
        else Toast.err(res.message || 'Không thể xóa — có thể đã có bài nộp.');
    };

    window.openImport = function (topicId) {
        document.getElementById('importTopicId').value = topicId;
        document.getElementById('importFile').value    = '';
        new bootstrap.Modal(document.getElementById('modalImport')).show();
    };

    window.doImport = async function () {
        const topicId = document.getElementById('importTopicId').value;
        const file    = document.getElementById('importFile').files[0];
        if (!file) { Toast.err('Vui lòng chọn file Excel.'); return; }

        const btn = document.getElementById('btnImport');
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Đang import...';

        const fd = new FormData();
        fd.append('excelFile', file);
        const res = await Api.postForm(`/api/admin/tests/topics/${topicId}/import`, fd);

        btn.disabled = false;
        btn.innerHTML = '<i class="bi bi-upload me-1"></i>Import';

        if (res.success) {
            Toast.ok(res.message || 'Import thành công!');
            bootstrap.Modal.getInstance(document.getElementById('modalImport'))?.hide();
            loadTopics();
        } else Toast.err(res.message || 'Import thất bại.');
    };

    initAdminPage('nav-tests', 'Quản lý Đề thi');
    AuthGuard.onReady(() => loadTopics());
})();
