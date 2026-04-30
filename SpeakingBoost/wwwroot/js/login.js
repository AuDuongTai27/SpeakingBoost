$(document).on('submit', '#login_form', function (e) {
    e.preventDefault();

    const $form = $(this);
    const $btn = $form.find("button[type=submit]");
    const originalText = $btn.html();

    // Disable nút và hiện overlay
    $btn.attr("disabled", true).html('<span>Đang đăng nhập...</span>');
    $("#loading-overlay").fadeIn(200);

    const payload = {
        username: $('#username').val(),
        password: $('#password').val(),
        __RequestVerificationToken: $('input[name=__RequestVerificationToken]').val()
    };

    $.ajax({
        type: "POST",
        url: "/Login/LoginToSystem",
        data: payload,
        dataType: 'json',
        success: function (res) {
            if (res.status === 'success') {
                setTimeout(() => {
                    window.location.href = res.redirect; 
                }, 2000);
            } else {
                $("#loading-overlay").fadeOut(200);
                $btn.attr("disabled", false).html(originalText);
                Swal.fire({
                    icon: "error",
                    title: "Lỗi đăng nhập",
                    text: res.message || "Sai tên đăng nhập hoặc mật khẩu"
                });
            }
        },
        error: function () {
            $("#loading-overlay").fadeOut(200);
            $btn.attr("disabled", false).html(originalText);
            Swal.fire({
                icon: "error",
                title: "Lỗi kết nối",
                text: "Không thể kết nối tới máy chủ"
            });
        }
    });
});
