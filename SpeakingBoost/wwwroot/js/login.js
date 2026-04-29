document.addEventListener("DOMContentLoaded", function () {
    const loginForm = document.getElementById("login_form");

    loginForm.addEventListener("submit", async function (e) {
        e.preventDefault(); // Chặn load lại trang

        const email = document.getElementById("username").value;
        const password = document.getElementById("password").value;
        const btnLogin = document.getElementById("btnLogin");

        // Hiệu ứng loading cho nút bấm
        const originalText = btnLogin.innerHTML;
        btnLogin.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Đang xử lý...';
        btnLogin.disabled = true;

        try {
            // Gọi API Backend (Nhà bếp)
            const response = await fetch("/api/login", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    Username: email,
                    Password: password
                })
            });

            const data = await response.json();

            if (response.ok) {
                // Đăng nhập thành công (HTTP 200)
                localStorage.setItem("userToken", data.token); // Lưu JWT vào trình duyệt

                Swal.fire({
                    icon: 'success',
                    title: 'Thành công!',
                    text: data.message,
                    timer: 1500,
                    showConfirmButton: false
                }).then(() => {
                    // Chuyển hướng theo role nhận từ API
                    window.location.href = data.redirectUrl;
                });
            } else {
                // Sai tài khoản/mật khẩu (HTTP 401) hoặc lỗi dữ liệu (HTTP 400)
                Swal.fire({
                    icon: 'error',
                    title: 'Thất bại',
                    text: data.message || "Đã có lỗi xảy ra."
                });
            }
        } catch (error) {
            // Lỗi kết nối (VD: Chưa bật server API)
            Swal.fire({
                icon: 'warning',
                title: 'Kết nối thất bại',
                text: 'Không thể kết nối tới Server API. Hãy kiểm tra Visual Studio.'
            });
            console.error("Fetch error:", error);
        } finally {
            btnLogin.innerHTML = originalText;
            btnLogin.disabled = false;
        }
    });
});