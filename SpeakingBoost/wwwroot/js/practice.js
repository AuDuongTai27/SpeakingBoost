// =========================================================
// ✅ Mobile 100vh fix (inline)
// - Fix tình trạng "mất khúc hiển thị" do thanh địa chỉ/trình duyệt trên mobile
// - Yêu cầu CSS dùng var(--app-vh): min-height: calc(var(--app-vh) * 100);
// =========================================================
(function () {
    function setAppVh() {
        var vh = window.innerHeight * 0.01;
        document.documentElement.style.setProperty('--app-vh', vh + 'px');
    }

    setAppVh();
    window.addEventListener('resize', setAppVh);
    window.addEventListener('orientationchange', setAppVh);

    // iOS Safari / Chrome iOS: visualViewport thường chuẩn hơn
    if (window.visualViewport) {
        window.visualViewport.addEventListener('resize', setAppVh);
        window.visualViewport.addEventListener('scroll', setAppVh);
    }
})();


document.addEventListener("DOMContentLoaded", function () {
    // 🩹 Giữ nguyên radio tick khi chuyển trang (cả Index và Question)
    const radios = document.querySelectorAll('#practiceSubmenu input[name="practiceType"]');
    const lastPart = sessionStorage.getItem("lastPart");
    if (lastPart && radios.length) {
        let val = "all";
        if (lastPart === "1") val = "part1";
        else if (lastPart === "2") val = "part2";
        else if (lastPart === "3") val = "part3";

        const matchRadio = document.querySelector(`#practiceSubmenu input[value="${val}"]`);
        if (matchRadio) matchRadio.checked = true;
    }

    const filterRadios = document.querySelectorAll('#practiceSubmenu input[name="practiceType"]');
    const grid = document.querySelector('.topic-grid');
    const topicCards = document.querySelectorAll('.topic-grid .topic-card');
    let isNavigating = false;

    // 🧠 Hàm lọc có hiệu ứng
    function filterCards(filter) {
        if (!topicCards.length) return;

        // Reset hiển thị để tránh topic bị mất
        topicCards.forEach(card => {
            card.style.removeProperty("display");
            card.style.opacity = "0";
            card.style.transform = "translateY(15px)";
        });

        setTimeout(() => {
            let delay = 0;
            topicCards.forEach(card => {
                const cardPart = card.getAttribute('data-part')?.trim();
                const match =
                    filter === 'all' ||
                    (filter === 'part1' && cardPart === '1') ||
                    (filter === 'part2' && cardPart === '2') ||
                    (filter === 'part3' && cardPart === '3');

                if (match) {
                    card.style.display = "block";
                    card.style.transition =
                        `opacity 0.35s cubic-bezier(0.22, 0.61, 0.36, 1),
                         transform 0.35s cubic-bezier(0.22, 0.61, 0.36, 1) ${delay}s`;
                    requestAnimationFrame(() => {
                        card.style.opacity = "1";
                        card.style.transform = "translateY(0)";
                    });
                    delay += 0.06;
                } else {
                    card.style.display = "none";
                }
            });
        }, 250);
    }

    // 🧭 Khi đổi part trong sidebar
    filterRadios.forEach(radio => {
        radio.addEventListener("change", function () {
            if (isNavigating) return;
            isNavigating = true;

            const value = this.value;
            let url = '/Student/PracticeSpeaking/Index';
            if (value === 'part1') url += '?part=1';
            else if (value === 'part2') url += '?part=2';
            else if (value === 'part3') url += '?part=3';

            // 💾 Lưu part vừa chọn để Index lọc lại & giữ tick khi sang Question
            const q = url.includes('?part=') ? url.split('=')[1] : null;
            if (q) sessionStorage.setItem("lastPart", q);
            else sessionStorage.removeItem("lastPart");

            // Fade out nhẹ trước khi điều hướng
            if (grid) {
                grid.style.transition = "opacity 0.4s ease";
                grid.style.opacity = "0";
            }

            setTimeout(() => {
                window.location.href = url;
            }, 250);
        });
    });

    // ✅ Khi đang ở Index
    if (grid && topicCards.length) {
        const params = new URLSearchParams(window.location.search);
        let partQuery = params.get('part');

        // Nếu không có query → lấy từ sessionStorage (ví dụ từ question view chuyển qua)
        if (!partQuery && sessionStorage.getItem('lastPart')) {
            partQuery = sessionStorage.getItem('lastPart');
        }

        // Chọn radio tương ứng
        let initFilter = 'all';
        if (partQuery === '1') initFilter = 'part1';
        else if (partQuery === '2') initFilter = 'part2';
        else if (partQuery === '3') initFilter = 'part3';

        const targetRadio = document.querySelector(`#practiceSubmenu input[value="${initFilter}"]`);
        if (targetRadio) targetRadio.checked = true;

        // Hiển thị topic đúng part
        filterCards(initFilter);
    }
});
