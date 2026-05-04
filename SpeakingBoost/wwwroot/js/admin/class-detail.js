/**
 * class-detail.js — Chi tiết Lớp học
 * - Tab Học sinh: thêm / xóa khỏi lớp
 * - Tab Bài tập: xem bài tập đã gán (read-only, không còn giao từ đây)
 * - Pagination trong mỗi tab
 */
(function () {
    'use strict';

    const classId = new URLSearchParams(location.search).get('id');
    if (!classId) { location.href = '/admin/classes.html'; }

    let detail = null, allUsers = [];
    let activeTab = 'students';

    const pgStudents  = new Paginator({ containerId: 'studentsBody',  pageSize: 10, infoId: 'pgStudentInfo',  wrapId: 'pgStudentWrap' });
    const pgExercises = new Paginator({ containerId: 'exercisesBody', pageSize: 10, infoId: 'pgExerciseInfo', wrapId: 'pgExerciseWrap' });

    // ── Tabs ────────────────────────────────────────────────────────
    window.showTab = function (tab) {
        activeTab = tab;
        document.getElementById('tabStudents').style.display  = tab === 'students'  ? '' : 'none';
        document.getElementById('tabExercises').style.display = tab === 'exercises' ? '' : 'none';
        document.querySelectorAll('#detailTabs .nav-link').forEach((b, i) =>
            b.classList.toggle('active', (i === 0 && tab === 'students') || (i === 1 && tab === 'exercises'))
        );
    };

    // ── Load ────────────────────────────────────────────────────────
    async function loadDetail() {
        const [detailRes, usersRes] = await Promise.all([
            Api.get(`/api/admin/classes/${classId}/details`),
            Api.get('/api/admin/users'),
        ]);

        if (!detailRes.success) { Toast.err('Không tải được dữ liệu!'); return; }
        detail = detailRes.data;

        // Header
        document.getElementById('pageTitle').textContent      = detail.className;
        document.getElementById('pageSubtitle').textContent   = `Sĩ số: ${detail.students.length} học sinh`;
        document.getElementById('breadCrumbName').textContent = detail.className;
        document.getElementById('studentCount').textContent   = detail.students.length;
        document.getElementById('exerciseCount').textContent  = detail.assignedExercises.length;

        // Render tabs
        pgStudents.setData(detail.students, renderStudentRows);
        pgExercises.setData(detail.assignedExercises, renderExerciseRows);

        // Student add-dropdown (students NOT in this class)
        if (usersRes.success) {
            const inClass = new Set(detail.students.map(s => s.studentId));
            allUsers = usersRes.data.filter(u => u.role.toLowerCase() === 'user' && !inClass.has(u.userId));
            document.getElementById('addStudentSelect').innerHTML =
                '<option value="">-- Chọn học sinh để thêm --</option>' +
                allUsers.map(s => `<option value="${s.userId}">${s.fullName} (${s.email})</option>`).join('');
        }
    }

    // ── Render: students ────────────────────────────────────────────
    function renderStudentRows(students) {
        if (!students.length) return '<tr><td colspan="4" class="text-center py-4 text-muted">Chưa có học sinh trong lớp</td></tr>';
        return students.map(s => `
            <tr>
                <td class="fw-500">${s.fullName}</td>
                <td class="text-muted">${s.email}</td>
                <td><span class="badge bg-light text-dark">${s.submissionCount} bài</span></td>
                <td class="text-center">
                    <button class="btn btn-sm btn-light rounded-pill"
                            onclick="removeStudent(${s.studentClassId},'${s.fullName.replace(/'/g, "\\'")}')"
                            title="Xóa khỏi lớp">
                        <i class="bi bi-person-x-fill text-danger"></i>
                    </button>
                </td>
            </tr>`).join('');
    }

    // ── Render: exercises (read-only) ────────────────────────────────
    function renderExerciseRows(exercises) {
        if (!exercises.length) return '<tr><td colspan="4" class="text-center py-4 text-muted">Chưa có bài tập nào được gán</td></tr>';
        return exercises.map(e => {
            const overdue = e.deadline && new Date(e.deadline) < new Date();
            return `<tr>
                <td class="fw-500">${e.title}</td>
                <td><span class="badge bg-light text-secondary">${e.type}</span></td>
                <td style="color:${overdue ? '#dc2626' : '#374151'}">
                    ${e.deadline ? fmtDateTime(e.deadline) : '<span class="text-muted">Không giới hạn</span>'}
                </td>
                <td>${deadlineBadge(overdue)}</td>
            </tr>`;
        }).join('');
    }

    // ── Add student ──────────────────────────────────────────────────
    window.addStudent = async function () {
        const sid = document.getElementById('addStudentSelect').value;
        if (!sid) { Toast.err('Vui lòng chọn học sinh.'); return; }
        const res = await Api.post(`/api/admin/classes/${classId}/students`, { studentId: parseInt(sid) });
        if (res.success) { Toast.ok('Đã thêm học sinh!'); loadDetail(); }
        else Toast.err(res.message || 'Lỗi!');
    };

    window.removeStudent = async function (scId, name) {
        if (!await adminConfirm(`Xóa "${name}" khỏi lớp này?`)) return;
        const res = await Api.del(`/api/admin/classes/${classId}/students/${scId}`);
        if (res.success) { Toast.ok('Đã xóa khỏi lớp!'); loadDetail(); }
        else Toast.err(res.message || 'Lỗi!');
    };

    // ── Init ────────────────────────────────────────────────────────
    initAdminPage('nav-classes', 'Chi tiết Lớp học');
    AuthGuard.onReady(() => loadDetail());
})();
