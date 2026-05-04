/**
 * deadlines.js — Quản lý Deadline
 */
(function () {
    'use strict';

    let allDeadlines = [], allClasses = [], allTopics = [];
    const pg = new Paginator({ containerId: 'deadlineBody', pageSize: 10, infoId: 'pgInfo', wrapId: 'pgWrap' });

    async function loadAll() {
        const res = await Api.get('/api/admin/deadlines');
        if (!res.success) { Toast.err('Lỗi tải dữ liệu!'); return; }

        const d = res.data;
        allDeadlines = d.activeDeadlines || [];
        allClasses   = d.classes         || [];
        allTopics    = d.topics          || [];

        // Populate dropdowns
        const classOpts = allClasses.map(c => `<option value="${c.classId}">${c.className}</option>`).join('');
        document.getElementById('classFilter').innerHTML  = '<option value="">Tất cả lớp</option>' + classOpts;
        document.getElementById('bulkClassId').innerHTML  = '<option value="">-- Chọn lớp --</option>' + classOpts;

        const topicOpts = allTopics.map(t => `<option value="${t.topicId}">${t.name}</option>`).join('');
        document.getElementById('bulkTopicId').innerHTML  = '<option value="">-- Chọn chủ đề --</option>' + topicOpts;

        updateStats(allDeadlines);
        applyFilter();
    }

    function updateStats(data) {
        const now  = new Date();
        const soon = new Date(now.getTime() + 24 * 60 * 60 * 1000);
        document.getElementById('statTotal').textContent   = data.length;
        document.getElementById('statSoon').textContent    = data.filter(d => d.deadline && new Date(d.deadline) > now && new Date(d.deadline) < soon).length;
        document.getElementById('statOverdue').textContent = data.filter(d => d.isOverdue).length;
    }

    function applyFilter() {
        const cls    = document.getElementById('classFilter').value;
        const status = document.getElementById('statusFilter').value;
        const filtered = allDeadlines.filter(d => {
            const clsOk    = !cls    || String(d.classId) === cls;
            const statusOk = !status || (status === 'overdue' ? d.isOverdue : !d.isOverdue);
            return clsOk && statusOk;
        });
        pg.setData(filtered, renderRows);
    }

    function renderRows(data) {
        if (!data.length) return '<tr><td colspan="5" class="text-center py-4 text-muted">Không có deadline nào</td></tr>';
        return data.map(d => `<tr>
            <td><span class="badge bg-light text-dark">${d.className}</span></td>
            <td class="fw-500">${d.exerciseTitle}</td>
            <td style="color:${d.isOverdue ? '#dc2626' : '#374151'}">${d.deadline ? fmtDateTime(d.deadline) : '—'}</td>
            <td>${deadlineBadge(d.isOverdue)}</td>
            <td class="text-center">
                <button class="btn btn-sm btn-light rounded-pill" onclick="deleteDeadline(${d.classExerciseId})" title="Xóa deadline">
                    <i class="bi bi-trash-fill text-danger"></i>
                </button>
            </td>
        </tr>`).join('');
    }

    window.deleteDeadline = async function (id) {
        if (!await adminConfirm('Xóa deadline này?')) return;
        const res = await Api.del(`/api/admin/deadlines/${id}`);
        if (res.success) { Toast.ok('Đã xóa!'); loadAll(); }
        else Toast.err(res.message || 'Lỗi!');
    };

    window.doBulkAssign = async function () {
        const classId  = document.getElementById('bulkClassId').value;
        const deadline = document.getElementById('bulkDeadline').value;
        const topicId  = document.getElementById('bulkTopicId').value;

        if (!classId)  { Toast.err('Vui lòng chọn lớp.'); return; }
        if (!deadline) { Toast.err('Vui lòng nhập deadline.'); return; }
        if (!topicId)  { Toast.err('Vui lòng chọn chủ đề.'); return; }

        const btn = document.getElementById('btnBulk');
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Đang gán...';

        const res = await Api.post('/api/admin/deadlines/assign', {
            classId:  parseInt(classId),
            topicId:  parseInt(topicId),
            deadline: deadline,
        });

        btn.disabled = false;
        btn.innerHTML = '<i class="bi bi-send me-1"></i>Gán bài';

        if (res.success) {
            Toast.ok(res.message || 'Gán bài thành công!');
            bootstrap.Modal.getInstance(document.getElementById('modalBulk'))?.hide();
            loadAll();
        } else {
            Toast.err(res.message || 'Gán thất bại.');
        }
    };

    document.getElementById('classFilter').addEventListener('change', applyFilter);
    document.getElementById('statusFilter').addEventListener('change', applyFilter);

    initAdminPage('nav-deadlines', 'Quản lý Deadline');
    AuthGuard.onReady(() => loadAll());
})();
