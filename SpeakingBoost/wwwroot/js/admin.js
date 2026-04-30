/**
 * admin.js — Shared utilities cho Admin pages
 * Cung cấp: API helper, Toast, Confirm dialog, Sidebar render
 */

// ─── 1. API Helper (tự đính kèm Bearer token) ──────────────────────
const Api = {
    _h(extra = {}) {
        return { 'Content-Type': 'application/json', 'Authorization': `Bearer ${localStorage.getItem('token') || ''}`, ...extra };
    },
    // Xử lý response an toàn: tránh crash khi server trả HTML thay vì JSON
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
    async put(url, body) {
        const r = await fetch(url, { method: 'PUT', headers: this._h(), body: JSON.stringify(body) });
        return this._safeJson(r);
    },
    async patch(url, body) {
        const r = await fetch(url, { method: 'PATCH', headers: this._h(), body: JSON.stringify(body) });
        return this._safeJson(r);
    },
    async del(url) {
        const r = await fetch(url, { method: 'DELETE', headers: this._h() });
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

// Inject keyframe
const ks = document.createElement('style');
ks.textContent = '@keyframes slideIn{from{opacity:0;transform:translateX(20px)}to{opacity:1;transform:translateX(0)}}';
document.head.appendChild(ks);

// ─── 3. Confirm Dialog ──────────────────────────────────────────────
function adminConfirm(msg) {
    return new Promise(resolve => {
        const id = 'adminConfirmModal';
        let el = document.getElementById(id);
        if (!el) {
            el = document.createElement('div');
            el.id = id;
            el.className = 'modal fade';
            el.innerHTML = `<div class="modal-dialog modal-dialog-centered modal-sm">
                <div class="modal-content border-0 shadow" style="border-radius:16px;">
                    <div class="modal-body p-4 text-center">
                        <div style="width:52px;height:52px;background:#fef3c7;border-radius:50%;display:flex;align-items:center;justify-content:center;margin:0 auto 1rem;">
                            <i class="bi bi-exclamation-triangle-fill" style="color:#d97706;font-size:1.4rem;"></i>
                        </div>
                        <p class="mb-0" id="confirmMsg" style="font-size:0.9rem;color:#374151;"></p>
                    </div>
                    <div class="modal-footer border-0 pt-0 justify-content-center gap-2">
                        <button class="btn btn-sm btn-light px-4 rounded-pill" id="confirmNo">Hủy</button>
                        <button class="btn btn-sm btn-danger px-4 rounded-pill" id="confirmYes">Xác nhận</button>
                    </div>
                </div>
            </div>`;
            document.body.appendChild(el);
        }
        document.getElementById('confirmMsg').textContent = msg;
        const modal = new bootstrap.Modal(el);
        modal.show();
        const yes = document.getElementById('confirmYes');
        const no  = document.getElementById('confirmNo');
        const done = (v) => { modal.hide(); resolve(v); yes.onclick = null; no.onclick = null; };
        yes.onclick = () => done(true);
        no.onclick  = () => done(false);
    });
}

// ─── 4. Sidebar HTML (chèn vào trang) ──────────────────────────────
function renderAdminSidebar(activeId = '') {
    const links = [
        { id: 'nav-dashboard', href: '/admin/dashboard.html',  icon: 'speedometer2',        label: 'Dashboard',      section: 'Tổng quan' },
        { id: 'nav-users',     href: '/admin/users.html',      icon: 'people-fill',         label: 'Người dùng',     section: 'Quản lý' },
        { id: 'nav-classes',   href: '/admin/classes.html',    icon: 'building',            label: 'Lớp học',        section: null },
        { id: 'nav-tests',     href: '/admin/tests.html',      icon: 'journal-text',        label: 'Đề thi',         section: null },
        { id: 'nav-deadlines', href: '/admin/deadlines.html',  icon: 'clock-history',       label: 'Deadline',       section: null },
        { id: 'nav-vocab',     href: '/admin/vocabulary.html', icon: 'book',                label: 'Từ vựng',        section: 'Nội dung' },
    ];

    let html = `<nav id="sidebar">
        <div class="sidebar-brand" style="cursor:pointer;" onclick="window.location.href='/admin/profile.html'">
            <div class="brand-icon"><i class="bi bi-mic-fill"></i></div>
            <div><span>SpeakingBoost</span><small>Admin Portal</small></div>
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
            <div class="user-card" style="cursor:pointer;" onclick="window.location.href='/admin/profile.html'">
                <div class="user-avatar" id="sidebarAvatar">A</div>
                <div class="user-info">
                    <div class="name" id="sidebarName">...</div>
                    <span class="role-badge" id="sidebarRole">Admin</span>
                </div>
                <button class="btn-logout" onclick="event.stopPropagation(); AuthGuard.logout()" title="Đăng xuất">
                    <i class="bi bi-box-arrow-right"></i>
                </button>
            </div>
        </div>
    </nav>`;
    return html;
}

// ─── 5. Topbar HTML ──────────────────────────────────────────────────
function renderAdminTopbar(title = '') {
    return `<header id="topbar">
        <button class="topbar-toggle" onclick="toggleSidebar()" aria-label="Toggle sidebar">
            <i class="bi bi-list"></i>
        </button>
        <span class="topbar-title">${title}</span>
        <div class="topbar-right">
            <div class="topbar-user" style="cursor:pointer;" onclick="window.location.href='/admin/profile.html'">
                <div class="topbar-avatar" id="topbarAvatar">A</div>
                <span id="topbarName">...</span>
            </div>
        </div>
    </header>`;
}

// ─── 6. initAdminPage — gọi sau khi DOM ready ───────────────────────
function initAdminPage(activeNavId, topbarTitle) {
    document.getElementById('sidebarPlaceholder').innerHTML = renderAdminSidebar(activeNavId);
    document.getElementById('topbarPlaceholder').innerHTML  = renderAdminTopbar(topbarTitle);

    AuthGuard.require(['teacher', 'superadmin']);
    AuthGuard.onReady(user => {
        const name    = user.fullName || 'Admin';
        const role    = (user.role || '').trim().toLowerCase();
        const initials = name.split(' ').map(w => w[0]).join('').substring(0, 2).toUpperCase();
        const roleLabel = role === 'superadmin' ? 'Super Admin' : 'Giáo viên';

        document.getElementById('sidebarName').textContent   = name;
        document.getElementById('sidebarRole').textContent   = roleLabel;
        document.getElementById('sidebarAvatar').textContent = initials;
        document.getElementById('topbarName').textContent    = name;
        document.getElementById('topbarAvatar').textContent  = initials;

        const loader = document.getElementById('pageLoader');
        if (loader) { loader.style.opacity = '0'; setTimeout(() => loader.remove(), 400); }
    });
}

// ─── 7. Sidebar toggle ──────────────────────────────────────────────
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

// ─── 8. Format helpers ──────────────────────────────────────────────
function fmtDate(iso) {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('vi-VN', { day:'2-digit', month:'2-digit', year:'numeric' });
}
function fmtDateTime(iso) {
    if (!iso) return '—';
    return new Date(iso).toLocaleString('vi-VN', { day:'2-digit', month:'2-digit', year:'numeric', hour:'2-digit', minute:'2-digit' });
}
function roleBadge(role) {
    const map = {
        student:    ['#dcfce7','#16a34a','Học sinh'],
        teacher:    ['#e0f2fe','#0284c7','Giáo viên'],
        superadmin: ['#ede9fe','#7c3aed','Super Admin'],
    };
    const [bg, color, label] = map[(role||'').toLowerCase()] || ['#f1f5f9','#64748b', role];
    return `<span style="background:${bg};color:${color};padding:0.2rem 0.6rem;border-radius:20px;font-size:0.72rem;font-weight:600;">${label}</span>`;
}
function statusBadge(status) {
    const map = {
        'Submitted': ['#dcfce7','#16a34a','Đã nộp'],
        'Pending':   ['#fef3c7','#d97706','Chờ nộp'],
        'Overdue':   ['#fee2e2','#dc2626','Quá hạn'],
    };
    const [bg, color, label] = map[status] || ['#f1f5f9','#64748b', status];
    return `<span style="background:${bg};color:${color};padding:0.2rem 0.6rem;border-radius:20px;font-size:0.72rem;font-weight:600;">${label}</span>`;
}
