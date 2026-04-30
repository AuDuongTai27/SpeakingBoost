/**
 * student.js — Shared utilities cho Student pages
 * Cung cấp: API helper, Toast, Sidebar render, và Auth Guard integration
 */

// ─── 1. API Helper (tự đính kèm Bearer token) ──────────────────────
const Api = {
    _h(extra = {}) {
        return { 'Content-Type': 'application/json', 'Authorization': `Bearer ${localStorage.getItem('token') || ''}`, ...extra };
    },
    async _safeJson(r) {
        if (r.status === 204) return { success: true };
        if (r.status === 401) {
            AuthGuard.logout();
            return { success: false, message: 'Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.' };
        }
        if (r.status === 403) return { success: false, message: 'Bạn không có quyền thực hiện hành động này.' };
        if (r.status === 404) return { success: false, message: 'Không tìm thấy dữ liệu (404).' };
        try {
            return await r.json();
        } catch {
            return { success: false, message: `Lỗi server (HTTP ${r.status}). Vui lòng thử lại.` };
        }
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
        wrap.id = 'toastWrap';
        wrap.style.cssText = 'position:fixed;top:1.25rem;right:1.25rem;z-index:9999;display:flex;flex-direction:column;gap:0.5rem;';
        document.body.appendChild(wrap);
        this._el = wrap;
    },
    show(msg, ok = true) {
        this._init();
        const t = document.createElement('div');
        const color = ok ? '#10b981' : '#ef4444';
        const icon  = ok ? 'check-circle-fill' : 'exclamation-triangle-fill';
        t.style.cssText = `background:#fff;border-left:4px solid ${color};border-radius:10px;padding:0.85rem 1.1rem;
            box-shadow:0 4px 20px rgba(0,0,0,0.12);display:flex;align-items:center;gap:0.6rem;
            font-size:0.875rem;font-family:'Inter',sans-serif;min-width:280px;animation:slideIn .3s ease;`;
        t.innerHTML = `<i class="bi bi-${icon}" style="color:${color};font-size:1rem;flex-shrink:0;"></i><span>${msg}</span>`;
        this._el.appendChild(t);
        setTimeout(() => { t.style.opacity = '0'; t.style.transition = 'opacity 0.3s'; setTimeout(() => t.remove(), 300); }, 3500);
    },
    ok(msg)  { this.show(msg, true);  },
    err(msg) { this.show(msg, false); }
};

if (!document.getElementById('toastKeyframes')) {
    const ks = document.createElement('style');
    ks.id = 'toastKeyframes';
    ks.textContent = '@keyframes slideIn{from{opacity:0;transform:translateX(20px)}to{opacity:1;transform:translateX(0)}}';
    document.head.appendChild(ks);
}

// ─── 3. Sidebar HTML ───────────────────────────────────────────────
function renderStudentSidebar(activeId = '') {
    const links = [
        { id: 'nav-dashboard', href: '/student/dashboard.html',  icon: 'house-fill',         label: 'Trang chủ',      section: 'Tổng quan' },
        { id: 'nav-practice',  href: '/student/practice.html',   icon: 'mic',                label: 'Luyện Speaking', section: 'Luyện tập' },
        { id: 'nav-deadline',  href: '/student/deadlines.html',  icon: 'calendar-check',     label: 'Bài deadline',   section: null },
        { id: 'nav-history',   href: '/student/history.html',    icon: 'clock-history',      label: 'Lịch sử nộp bài',section: null },
        { id: 'nav-vocab',     href: '/student/vocabulary.html', icon: 'book',               label: 'Từ vựng',        section: 'Kết quả' },
    ];

    let html = `<nav id="sidebar">
        <div class="sidebar-brand">
            <div class="brand-icon"><i class="bi bi-mic-fill"></i></div>
            <div><span>SpeakingBoost</span><small>Học sinh</small></div>
        </div>`;

    let lastSection = '';
    for (const l of links) {
        if (l.section && l.section !== lastSection) {
            html += `<div class="nav-section">${l.section}</div>`;
            lastSection = l.section;
        }
        const active = l.id === activeId ? ' active' : '';
        html += `<a href="${l.href}" class="nav-item-link${active}" id="${l.id}">
            <i class="bi bi-${l.icon}"></i> ${l.label}
        </a>`;
    }

    html += `<div class="sidebar-footer">
            <div class="user-card">
                <div class="user-avatar" id="sidebarAvatar">S</div>
                <div class="user-info">
                    <div class="name" id="sidebarName">...</div>
                    <span class="role-badge">Học sinh</span>
                </div>
                <button class="btn-logout" onclick="AuthGuard.logout()" title="Đăng xuất">
                    <i class="bi bi-box-arrow-right"></i>
                </button>
            </div>
        </div>
    </nav>`;
    return html;
}

// ─── 4. Topbar HTML ──────────────────────────────────────────────────
function renderStudentTopbar(title = '') {
    return `<header id="topbar">
        <button class="topbar-toggle" id="sidebarToggle" onclick="toggleSidebar()" aria-label="Toggle sidebar">
            <i class="bi bi-list"></i>
        </button>
        <span class="topbar-title">${title}</span>
        <div class="topbar-right">
            <div class="topbar-user">
                <div class="topbar-avatar" id="topbarAvatar">S</div>
                <span id="topbarName">...</span>
            </div>
        </div>
    </header>`;
}

// ─── 5. initStudentPage ───────────────────────────────────────────────
function initStudentPage(activeNavId, topbarTitle) {
    document.getElementById('sidebarPlaceholder').innerHTML = renderStudentSidebar(activeNavId);
    document.getElementById('topbarPlaceholder').innerHTML  = renderStudentTopbar(topbarTitle);

    AuthGuard.require(['student']);
    AuthGuard.onReady(user => {
        const name    = user.fullName || 'Học sinh';
        const initials = name.split(' ').map(w => w[0]).join('').substring(0, 2).toUpperCase();

        document.getElementById('sidebarName').textContent   = name;
        document.getElementById('sidebarAvatar').textContent = initials;
        document.getElementById('topbarName').textContent    = name;
        document.getElementById('topbarAvatar').textContent  = initials;

        const loader = document.getElementById('pageLoader');
        if (loader) { loader.style.opacity = '0'; setTimeout(() => loader.remove(), 400); }
    });
}

// ─── 6. Sidebar toggle ──────────────────────────────────────────────
let _sidebarOpen = window.innerWidth >= 768;
function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    const topbar  = document.getElementById('topbar');
    const main    = document.getElementById('main');
    if (window.innerWidth >= 768) {
        _sidebarOpen = !_sidebarOpen;
        sidebar.classList.toggle('collapsed', !_sidebarOpen);
        topbar.classList.toggle('sidebar-collapsed', !_sidebarOpen);
        main.classList.toggle('sidebar-collapsed', !_sidebarOpen);
    } else {
        sidebar.classList.toggle('open');
    }
}

// ─── 7. Format helpers ──────────────────────────────────────────────
function fmtDate(iso) {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('vi-VN', { day:'2-digit', month:'2-digit', year:'numeric' });
}
function fmtDateTime(iso) {
    if (!iso) return '—';
    return new Date(iso).toLocaleString('vi-VN', { day:'2-digit', month:'2-digit', year:'numeric', hour:'2-digit', minute:'2-digit' });
}
function statusBadge(status) {
    const map = {
        'Submitted': ['#dcfce7','#16a34a','Đã nộp'],
        'Pending':   ['#fef3c7','#d97706','Chờ nộp'],
        'Overdue':   ['#fee2e2','#dc2626','Quá hạn'],
        'Processing': ['#e0f2fe','#0284c7','Đang chấm điểm'],
        'Evaluated': ['#dcfce7','#16a34a','Đã chấm'],
        'Error':     ['#fee2e2','#dc2626','Lỗi xử lý']
    };
    const [bg, color, label] = map[status] || ['#f1f5f9','#64748b', status];
    return `<span class="badge" style="background:${bg};color:${color};padding:0.35rem 0.6rem;font-weight:600;font-size:0.75rem;">${label}</span>`;
}
