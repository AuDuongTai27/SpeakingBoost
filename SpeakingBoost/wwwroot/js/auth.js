/**
 * auth.js — Auth Guard Module (SpeakingBoost RESTful)
 * =====================================================
 * Dùng chung cho toàn bộ trang protected (admin/student).
 *
 * Cách dùng trong mỗi trang HTML:
 *   <script src="/js/auth.js"></script>
 *   <script>
 *     // Chỉ cho phép role 'student' vào trang này
 *     AuthGuard.require(['student']);
 *
 *     // Chỉ cho phép admin (teacher + superadmin) vào trang này
 *     AuthGuard.require(['teacher', 'superadmin']);
 *
 *     // Sau khi guard pass, lấy thông tin user
 *     AuthGuard.onReady(user => {
 *       document.getElementById('userName').textContent = user.fullName;
 *     });
 *   </script>
 */

const AuthGuard = (() => {
    const LOGIN_PAGE    = '/login.html';
    const TOKEN_KEY     = 'token';

    let _resolvedUser = null;
    let _readyCallbacks = [];

    // ─── Lấy token từ localStorage ───────────────────────────────────
    function getToken() {
        return localStorage.getItem(TOKEN_KEY);
    }

    // ─── Xoá toàn bộ thông tin auth ──────────────────────────────────
    function clearAuth() {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem('role');
        localStorage.removeItem('userId');
        localStorage.removeItem('fullName');
        localStorage.removeItem('email');
    }

    // ─── Đăng xuất ───────────────────────────────────────────────────
    function logout() {
        clearAuth();
        window.location.href = LOGIN_PAGE;
    }

    // ─── Gọi API /api/auth/me để xác minh token ─────────────────────
    // Trả về object user hoặc null nếu token không hợp lệ / hết hạn
    async function fetchMe(token) {
        try {
            const res = await fetch('/api/auth/me', {
                method: 'GET',
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (!res.ok) return null;
            const json = await res.json();
            return json.success ? json.data : null;
        } catch {
            return null;
        }
    }

    // ─── Hàm chính: kiểm tra quyền và chia luồng ────────────────────
    // allowedRoles: mảng role được phép, ví dụ ['student'] hoặc ['teacher','superadmin']
    async function require(allowedRoles = []) {
        const token = getToken();

        // Chưa đăng nhập → về login
        if (!token) {
            window.location.href = LOGIN_PAGE;
            return;
        }

        // Gọi API để xác minh token (server-side validation)
        const user = await fetchMe(token);

        // Token hết hạn / không hợp lệ → xoá cache và về login
        if (!user) {
            clearAuth();
            window.location.href = LOGIN_PAGE;
            return;
        }

        const role = (user.role || '').trim().toLowerCase();

        // Cập nhật localStorage với thông tin mới nhất từ server
        localStorage.setItem('role',     role);
        localStorage.setItem('userId',   user.userId);
        localStorage.setItem('fullName', user.fullName);
        localStorage.setItem('email',    user.email);

        // Kiểm tra role có được phép vào trang này không
        if (allowedRoles.length > 0 && !allowedRoles.includes(role)) {
            // Sai role → redirect đến đúng dashboard theo role
            window.location.href = user.redirectUrl;
            return;
        }

        // ✅ Guard pass — lưu user và gọi callback
        _resolvedUser = user;
        _readyCallbacks.forEach(cb => cb(user));
        _readyCallbacks = [];
    }

    // ─── Đăng ký callback sau khi guard pass ─────────────────────────
    // Nếu guard đã pass rồi thì gọi ngay
    function onReady(callback) {
        if (_resolvedUser) {
            callback(_resolvedUser);
        } else {
            _readyCallbacks.push(callback);
        }
    }

    // ─── Public API ───────────────────────────────────────────────────
    return { require, onReady, logout, getToken, clearAuth };
})();
