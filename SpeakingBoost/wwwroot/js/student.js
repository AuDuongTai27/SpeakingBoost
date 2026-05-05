/**
 * student.js — Shared utilities cho Student pages (v2 - Navbar layout)
 * Api helper, Toast, Navbar render, Auth Guard integration
 */

// ─── 1. API Helper ──────────────────────────────────────────────────
const Api = {
    _h(extra = {}) {
        return { 'Content-Type': 'application/json', 'Authorization': `Bearer ${localStorage.getItem('token') || ''}`, ...extra };
    },
    async _safeJson(r) {
        if (r.status === 204) return { success: true };
        if (r.status === 401) { AuthGuard.logout(); return { success: false, message: 'Phiên đăng nhập hết hạn.' }; }
        if (r.status === 403) return { success: false, message: 'Không có quyền thực hiện.' };
        if (r.status === 404) return { success: false, message: 'Không tìm thấy dữ liệu.' };
        try { return await r.json(); } catch { return { success: false, message: `Lỗi server (HTTP ${r.status}).` }; }
    },
    async get(url) {
        const r = await fetch(url, { headers: this._h() });
        return this._safeJson(r);
    },
    async post(url, body) {
        const r = await fetch(url, { method: 'POST', headers: this._h(), body: JSON.stringify(body) });
        return this._safeJson(r);
    },
    async postForm(url, formData) {
        const r = await fetch(url, { method: 'POST', headers: { 'Authorization': `Bearer ${localStorage.getItem('token') || ''}` }, body: formData });
        return this._safeJson(r);
    }
};

// ─── 2. Toast Notification ──────────────────────────────────────────
const Toast = {
    _el: null,
    _init() {
        if (this._el) return;
        const wrap = document.createElement('div');
        wrap.style.cssText = 'position:fixed;top:1.25rem;right:1.25rem;z-index:9999;display:flex;flex-direction:column;gap:0.5rem;';
        document.body.appendChild(wrap);
        this._el = wrap;
    },
    show(msg, ok = true) {
        this._init();
        const color = ok ? '#10b981' : '#ef4444';
        const icon = ok ? 'check-circle-fill' : 'exclamation-triangle-fill';
        const t = document.createElement('div');
        t.style.cssText = `background:#fff;border-left:4px solid ${color};border-radius:10px;padding:0.85rem 1.1rem;box-shadow:0 4px 20px rgba(0,0,0,0.12);display:flex;align-items:center;gap:0.6rem;font-size:0.875rem;font-family:'Inter',sans-serif;min-width:280px;animation:toastIn .3s ease;`;
        t.innerHTML = `<i class="bi bi-${icon}" style="color:${color};font-size:1rem;flex-shrink:0;"></i><span>${msg}</span>`;
        this._el.appendChild(t);
        setTimeout(() => { t.style.opacity = '0'; t.style.transition = 'opacity 0.3s'; setTimeout(() => t.remove(), 300); }, 3500);
    },
    ok(msg) { this.show(msg, true); },
    err(msg) { this.show(msg, false); }
};

if (!document.getElementById('toastKf')) {
    const s = document.createElement('style');
    s.id = 'toastKf';
    s.textContent = '@keyframes toastIn{from{opacity:0;transform:translateX(20px)}to{opacity:1;transform:translateX(0)}}';
    document.head.appendChild(s);
}

// ─── 3. Navbar Render ───────────────────────────────────────────────
function renderStudentNavbar(activePage, user) {
    const name = user ? (user.fullName || 'Học sinh') : '...';
    const initials = name.split(' ').map(w => w[0]).join('').substring(0, 2).toUpperCase();

    const pages = [
        { id: 'dashboard', href: '/student/dashboard.html', label: 'Trang chủ' },
        { id: 'practice',  href: '/student/practice.html',  label: 'Luyện Speaking' },
        { id: 'deadlines', href: '/student/deadlines.html', label: 'Bài Deadline' },
        { id: 'history',   href: '/student/history.html',   label: 'Lịch sử' },
    ];

    const navItems = pages.map(p => {
        const active = p.id === activePage;
        return `<li class="nav-item"><a class="nav-link fw-medium rounded-pill px-3" href="${p.href}" style="${active ? 'background:#6366f1;color:#fff;' : 'color:#64748b;'}">${p.label}</a></li>`;
    }).join('');

    return `<nav class="navbar navbar-expand-lg sticky-top shadow-sm" style="background:#fff;border-bottom:1px solid #e2e8f0;z-index:200;">
        <div class="container">
            <a class="navbar-brand d-flex align-items-center gap-2 fw-bold" href="/student/dashboard.html" style="font-size:1.1rem;">
                <span style="width:36px;height:36px;border-radius:10px;background:linear-gradient(135deg,#6366f1,#06b6d4);display:flex;align-items:center;justify-content:center;color:#fff;font-size:1rem;"><i class="bi bi-mic-fill"></i></span>
                SpeakingBoost
            </a>
            <button class="navbar-toggler border-0 shadow-none" type="button" data-bs-toggle="collapse" data-bs-target="#studentNav">
                <i class="bi bi-list" style="font-size:1.5rem;color:#64748b;"></i>
            </button>
            <div class="collapse navbar-collapse" id="studentNav">
                <ul class="navbar-nav mx-auto gap-1">${navItems}</ul>
                <div class="d-flex align-items-center gap-3">
                    <div class="d-flex align-items-center gap-2">
                        <div style="width:34px;height:34px;border-radius:50%;background:linear-gradient(135deg,#6366f1,#06b6d4);display:flex;align-items:center;justify-content:center;color:#fff;font-size:0.75rem;font-weight:700;">${initials}</div>
                        <span class="fw-medium" style="font-size:0.875rem;color:#1e293b;">${name}</span>
                    </div>
                    <button onclick="AuthGuard.logout()" class="btn btn-outline-danger btn-sm rounded-pill fw-medium">Đăng xuất</button>
                </div>
            </div>
        </div>
    </nav>`;
}

// ─── 4. Init Page (Auth guard + navbar) ────────────────────────────
function initStudentPage(activePage, onReady) {
    AuthGuard.require(['user']);
    AuthGuard.onReady(user => {
        const ph = document.getElementById('navbarPlaceholder');
        if (ph) ph.innerHTML = renderStudentNavbar(activePage, user);
        const loader = document.getElementById('pageLoader');
        if (loader) { loader.style.opacity = '0'; setTimeout(() => loader.remove(), 400); }
        if (onReady) onReady(user);
    });
}

// ─── 5. Format Helpers ──────────────────────────────────────────────
function fmtDate(iso) {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
}
function fmtDateTime(iso) {
    if (!iso) return '—';
    return new Date(iso).toLocaleString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

// ─── 6. Status Badge ────────────────────────────────────────────────
function statusBadge(status) {
    const map = {
        'Submitted':  ['#dcfce7','#16a34a','Đã nộp'],
        'Pending':    ['#fef3c7','#d97706','Chờ nộp'],
        'Overdue':    ['#fee2e2','#dc2626','Quá hạn'],
        'Processing': ['#e0f2fe','#0284c7','Đang chấm'],
        'Evaluated':  ['#dcfce7','#16a34a','Đã chấm'],
        'Error':      ['#fee2e2','#dc2626','Lỗi xử lý'],
        'Failed':     ['#fee2e2','#dc2626','Thất bại'],
    };
    const [bg, color, label] = map[status] || ['#f1f5f9','#64748b', status];
    return `<span style="background:${bg};color:${color};padding:0.3rem 0.7rem;font-weight:600;font-size:0.72rem;border-radius:9999px;">${label}</span>`;
}

// ─── 7. Score Color Helper ───────────────────────────────────────────
function scoreStyle(score) {
    if (score == null) return { bg: '#f1f5f9', color: '#94a3b8', text: 'Chưa chấm' };
    if (score >= 7) return { bg: '#dcfce7', color: '#16a34a', text: score.toFixed(1) };
    if (score >= 5) return { bg: '#fef3c7', color: '#d97706', text: score.toFixed(1) };
    return { bg: '#fee2e2', color: '#dc2626', text: score.toFixed(1) };
}

// ─── 8. Part Badge ───────────────────────────────────────────────────
function partBadgeHtml(part) {
    const map = { '1': ['#ede9fe','#7c3aed'], '2': ['#dcfce7','#16a34a'], '3': ['#e0f2fe','#0284c7'] };
    const [bg, color] = map[String(part)] || ['#f1f5f9','#64748b'];
    return `<span style="background:${bg};color:${color};padding:2px 10px;border-radius:9999px;font-size:0.68rem;font-weight:700;">Part ${part}</span>`;
}

// ─── 9. Topic Style (icon+color cycle for practice grid) ────────────
function topicStyle(idx) {
    const styles = [
        { icon: 'palette-fill',        bg: 'linear-gradient(135deg,#ede9fe,#e0f2fe)', color: '#6366f1' },
        { icon: 'briefcase-fill',      bg: 'linear-gradient(135deg,#dcfce7,#bbf7d0)', color: '#16a34a' },
        { icon: 'laptop',              bg: 'linear-gradient(135deg,#e0f2fe,#bae6fd)', color: '#0284c7' },
        { icon: 'airplane',            bg: 'linear-gradient(135deg,#fef3c7,#fde68a)', color: '#d97706' },
        { icon: 'people-fill',         bg: 'linear-gradient(135deg,#fce7f3,#fbcfe8)', color: '#db2777' },
        { icon: 'cup-hot-fill',        bg: 'linear-gradient(135deg,#fee2e2,#fecaca)', color: '#dc2626' },
        { icon: 'house-fill',          bg: 'linear-gradient(135deg,#dcfce7,#bbf7d0)', color: '#16a34a' },
        { icon: 'compass',             bg: 'linear-gradient(135deg,#e0f2fe,#bae6fd)', color: '#0284c7' },
        { icon: 'book-fill',           bg: 'linear-gradient(135deg,#fef3c7,#fde68a)', color: '#d97706' },
        { icon: 'cloud-sun-fill',      bg: 'linear-gradient(135deg,#ede9fe,#e0f2fe)', color: '#6366f1' },
        { icon: 'graduation-cap-fill', bg: 'linear-gradient(135deg,#dcfce7,#bbf7d0)', color: '#16a34a' },
        { icon: 'tree-fill',           bg: 'linear-gradient(135deg,#bbf7d0,#86efac)', color: '#16a34a' },
    ];
    return styles[idx % styles.length];
}

// ─── 10. Shared Footer HTML ─────────────────────────────────────────
function studentFooterHtml() {
    return `<footer style="background:#1e293b;color:#94a3b8;padding:28px 0;">
        <div class="container">
            <div class="d-flex flex-wrap justify-content-between align-items-center gap-2" style="font-size:0.78rem;">
                <span>© 2026 SpeakingBoost. All rights reserved.</span>
                <span>Made with <i class="bi bi-heart-fill text-danger"></i> for IELTS Learners</span>
            </div>
        </div>
    </footer>`;
}
