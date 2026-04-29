document.addEventListener("DOMContentLoaded", () => {
    const questions = document.querySelectorAll('.question-item');

    const questionText = document.getElementById('questionText');
    const headerPartText = document.getElementById('headerPart');
    const historyBtn = document.getElementById("historyBtn");

    const recordBtn = document.getElementById("recordBtn");
    const maxAttemptBtn = document.getElementById("maxAttemptBtn");
    const recordHelperText = document.querySelector("#recordArea p");
    const playbackArea = document.getElementById("playbackArea");
    const audioPlayback = document.getElementById("audioPlayback");
    const redoBtn = document.getElementById("redoBtn");

    const timerContainer = document.getElementById("timerContainer");
    const timerBar = document.getElementById("timerBar");
    const timeRemaining = document.getElementById("timeRemaining");

    // ✅ Mobile picker list element (khai báo sớm)
    const exercisePickerList = document.getElementById("exercisePickerList");

    // ===== Mobile check =====
    function isMobile() {
        return window.matchMedia && window.matchMedia("(max-width: 991.98px)").matches;
    }

    // ✅ Auto shrink font để câu hỏi fit trong box (mobile), quá min thì bật scroll
    function fitQuestionText() {
        if (!questionText) return;

        if (!isMobile()) {
            questionText.style.removeProperty("font-size");
            questionText.style.overflowY = "auto";
            return;
        }

        // reset trước khi đo
        questionText.style.overflowY = "hidden";
        questionText.style.fontSize = "16px";

        const minPx = 12;
        let cur = parseFloat(getComputedStyle(questionText).fontSize) || 16;

        for (let i = 0; i < 30; i++) {
            if (questionText.scrollHeight <= questionText.clientHeight + 1) break;
            cur -= 1;
            if (cur <= minPx) { cur = minPx; break; }
            questionText.style.fontSize = cur + "px";
        }

        if (questionText.scrollHeight > questionText.clientHeight + 1) {
            questionText.style.overflowY = "auto";
        }
    }

    // ===== marquee helper =====
    function setupMarqueeForItem(item) {
        const titleWrap = item.querySelector('.q-title');
        const titleInner = item.querySelector('.q-title-inner');
        if (!titleWrap || !titleInner) return;

        const measure = () => {
            const distance = Math.max(0, titleInner.scrollWidth - titleWrap.clientWidth);
            item._marqueeDistance = distance;
            item.style.setProperty('--marquee-distance', `${distance + 16}px`);
            const duration = Math.min(14, Math.max(4, (distance + 16) / 45));
            item.style.setProperty('--marquee-duration', `${duration}s`);
        };

        measure();
        window.addEventListener('resize', measure);

        item.addEventListener('mouseenter', () => {
            if ((item._marqueeDistance ?? 0) > 0) item.classList.add('marquee');
        });

        item.addEventListener('mouseleave', () => item.classList.remove('marquee'));
    }

    questions.forEach(q => setupMarqueeForItem(q));

    // ===== data from server (JSON in <script id="practiceData">) =====
    const dataEl = document.getElementById("practiceData");
    const data = dataEl ? JSON.parse(dataEl.textContent) : {};

    const questionList = data.questionList ?? [];
    const parts = data.parts ?? [];
    const exerciseIds = data.exerciseIds ?? [];
    const maxAttempts = data.maxAttempts ?? [];
    const attemptUsed = data.attemptUsed ?? [];
    const exerciseTitlesList = data.exerciseTitlesList ?? [];
    const fallbackPart = data.fallbackPart ?? 1;

    let currentQuestion = 0;
    let part = (parts.length > 0 ? parts[0] : fallbackPart);

    // ===== history (desktop) =====
    if (historyBtn) {
        historyBtn.addEventListener("click", () => {
            const exerciseId = exerciseIds[currentQuestion];
            window.location.href = `/Student/SpeakingReview/History?exerciseId=${exerciseId}`;
        });
    }

    // ===== render Part2 =====
    function renderPart2(raw) {
        const lines = (raw ?? "")
            .split(/\r?\n/)
            .map(x => x.trim())
            .filter(x => x.length > 0);

        questionText.innerHTML = "";
        if (lines.length === 0) return;

        const head = document.createElement("div");
        head.className = "p2-head";
        head.textContent = lines[0];
        questionText.appendChild(head);

        if (lines.length > 1) {
            const ul = document.createElement("ul");
            ul.className = "p2-bullets";
            for (let i = 1; i < lines.length; i++) {
                const li = document.createElement("li");
                li.textContent = lines[i].replace(/^[-•\u2022]\s*/, "");
                ul.appendChild(li);
            }
            questionText.appendChild(ul);
        }
    }

    function getDuration(p) {
        if (p === 1) return 30000;
        if (p === 2) return 120000;
        return 45000;
    }

    let maxDuration = getDuration(part);

    function applyAttemptUI(index) {
        const used = (attemptUsed && attemptUsed.length > index) ? attemptUsed[index] : 0;
        const max = (maxAttempts && maxAttempts.length > index) ? maxAttempts[index] : 0;
        const locked = (max > 0 && used >= max);

        if (locked) {
            recordBtn.style.display = "none";
            if (maxAttemptBtn) maxAttemptBtn.style.display = "inline-flex";

            audioPlayback.src = "";
            playbackArea.style.display = "none";
            document.querySelector(".submit-btn")?.remove();
            if (recordHelperText) recordHelperText.style.display = "none";
        } else {
            if (maxAttemptBtn) maxAttemptBtn.style.display = "none";
            if (playbackArea.style.display === "none") recordBtn.style.display = "inline-block";
            if (recordHelperText) recordHelperText.style.display = "block";
        }

        return locked;
    }

    function renderQuestion(index) {
        if (!questionList || questionList.length === 0) return;

        const realPart = (parts && parts.length > index) ? parts[index] : part;

        if (headerPartText) headerPartText.textContent = realPart || "";

        if (realPart === 2) {
            questionText.classList.add("part2");
            renderPart2(questionList[index]);
        } else {
            questionText.classList.remove("part2");
            questionText.textContent = `Câu hỏi ${index + 1} (Part ${realPart}): ${questionList[index]}`;
        }

        part = realPart;
        maxDuration = getDuration(part);

        applyAttemptUI(index);
        setTimeout(fitQuestionText, 0);
    }

    function resetRecordingUI() {
        // dọn playback cũ + tránh leak objectURL
        try {
            audioPlayback.pause();
            audioPlayback.currentTime = 0;
        } catch (_) { }

        if (typeof currentAudioUrl !== "undefined" && currentAudioUrl) {
            URL.revokeObjectURL(currentAudioUrl);
            currentAudioUrl = null;
        }

        audioPlayback.src = "";
        playbackArea.style.display = "none";
        document.querySelector(".submit-btn")?.remove();
        if (recordHelperText) recordHelperText.style.display = "block";
        applyAttemptUI(currentQuestion);
    }

    function selectQuestion(index) {
        questions.forEach(item => item.classList.remove('active'));
        if (questions[index]) questions[index].classList.add('active');

        currentQuestion = index;
        renderQuestion(index);
        resetRecordingUI();
        updateExercisePickerActive();
    }

    // ===== Timer =====
    let timerInterval;

    function formatTime(ms) {
        const totalSeconds = Math.ceil(ms / 1000);
        const minutes = Math.floor(totalSeconds / 60);
        const seconds = totalSeconds % 60;
        return `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
    }

    function startTimer(duration) {
        timerContainer.style.display = "block";
        let remaining = duration;
        timerBar.style.width = "100%";
        timeRemaining.textContent = formatTime(remaining);
        const start = Date.now();

        timerInterval = setInterval(() => {
            const elapsed = Date.now() - start;
            remaining = Math.max(0, duration - elapsed);
            const percent = (remaining / duration) * 100;
            timerBar.style.width = percent + "%";

            if (percent < 30) timerBar.style.background = "#ff3b3b";
            else if (percent < 60) timerBar.style.background = "#ffb300";
            else timerBar.style.background = "#0a58ca";

            timeRemaining.textContent = formatTime(remaining);
            if (remaining <= 0) stopTimer();
        }, 1000);
    }

    function stopTimer() {
        clearInterval(timerInterval);
        timerContainer.style.display = "none";
    }

    // ===== Recording (WebAudio: lọc + khuếch đại trước khi upload) =====
    let mediaRecorder = null;
    let audioChunks = [];
    let isRecording = false;
    let isStarting = false;
    let recordingTimeout = null;

    // giữ stream mic gốc để stop() khi xong (tắt icon mic)
    let currentStream = null;

    // objectURL để revoke tránh leak
    let currentAudioUrl = null;

    // ===== WebAudio nodes =====
    let audioCtx = null;
    let waSource = null, waHP = null, waLP = null, waGain = null, waComp = null, waDest = null;

    function cleanupStream() {
        try { currentStream?.getTracks()?.forEach(t => t.stop()); } catch (_) { }
        currentStream = null;
    }

    function cleanupAudioUrl() {
        if (currentAudioUrl) {
            try { URL.revokeObjectURL(currentAudioUrl); } catch (_) { }
            currentAudioUrl = null;
        }
    }

    function cleanupWebAudio() {
        try { waSource?.disconnect(); } catch (_) { }
        try { waHP?.disconnect(); } catch (_) { }
        try { waLP?.disconnect(); } catch (_) { }
        try { waGain?.disconnect(); } catch (_) { }
        try { waComp?.disconnect(); } catch (_) { }
        waSource = waHP = waLP = waGain = waComp = waDest = null;

        if (audioCtx) {
            try { audioCtx.close(); } catch (_) { }
            audioCtx = null;
        }
    }

    async function buildProcessedStream(rawStream, opts = {}) {
        const {
            gain = 1.8,        // 1.0 = bình thường; 1.5~2.5 tuỳ mic
            hpHz = 80,         // cắt ù thấp
            lpHz = 12000,      // cắt hiss cao
            threshold = -24,   // compressor để tránh vỡ tiếng
            ratio = 12
        } = opts;

        audioCtx = new (window.AudioContext || window.webkitAudioContext)();
        if (audioCtx.state === "suspended") {
            try { await audioCtx.resume(); } catch (_) { }
        }

        waSource = audioCtx.createMediaStreamSource(rawStream);

        waHP = audioCtx.createBiquadFilter();
        waHP.type = "highpass";
        waHP.frequency.value = hpHz;

        waLP = audioCtx.createBiquadFilter();
        waLP.type = "lowpass";
        waLP.frequency.value = lpHz;

        waGain = audioCtx.createGain();
        waGain.gain.value = gain;

        waComp = audioCtx.createDynamicsCompressor();
        waComp.threshold.value = threshold;
        waComp.knee.value = 30;
        waComp.ratio.value = ratio;
        waComp.attack.value = 0.003;
        waComp.release.value = 0.25;

        waDest = audioCtx.createMediaStreamDestination();

        waSource
            .connect(waHP)
            .connect(waLP)
            .connect(waGain)
            .connect(waComp)
            .connect(waDest);

        return waDest.stream;
    }

    function pickMimeType() {
        if (!window.MediaRecorder || !MediaRecorder.isTypeSupported) return null;
        const candidates = [
            "audio/webm;codecs=opus",
            "audio/webm",
            "audio/ogg;codecs=opus"
        ];
        return candidates.find(t => MediaRecorder.isTypeSupported(t)) || null;
    }

    function setPlaybackBlob(blob) {
        try {
            audioPlayback.pause();
            audioPlayback.currentTime = 0;
        } catch (_) { }

        cleanupAudioUrl();
        currentAudioUrl = URL.createObjectURL(blob);
        audioPlayback.src = currentAudioUrl;
    }

    async function startRecording() {
        if (isRecording || isStarting) return;

        if (!navigator.mediaDevices?.getUserMedia) {
            alert("❌ Trình duyệt không hỗ trợ thu âm (getUserMedia).");
            return;
        }
        if (!window.MediaRecorder) {
            alert("❌ Trình duyệt không hỗ trợ MediaRecorder. Hãy dùng Chrome/Edge.");
            return;
        }

        isStarting = true;
        recordBtn.disabled = true;

        try {
            // dọn state cũ
            clearTimeout(recordingTimeout);
            recordingTimeout = null;
            cleanupWebAudio();
            cleanupStream();

            // xin mic (bật NS/AEC/AGC mức cơ bản của browser)
            const rawStream = await navigator.mediaDevices.getUserMedia({
                audio: {
                    echoCancellation: { ideal: true },
                    noiseSuppression: { ideal: true },
                    autoGainControl: { ideal: true }
                }
            });
            currentStream = rawStream;

            // tạo stream đã qua lọc + gain + compressor (fallback nếu fail)
            let recordStream = rawStream;
            try {
                recordStream = await buildProcessedStream(rawStream, {
                    gain: 1.8,
                    hpHz: 80,
                    lpHz: 12000,
                    threshold: -24,
                    ratio: 12
                });
            } catch (e) {
                console.warn("WebAudio processing failed, fallback to raw stream:", e);
                cleanupWebAudio();
                recordStream = rawStream;
            }

            const mimeType = pickMimeType(); // chọn loại tốt nhất có thể
            mediaRecorder = new MediaRecorder(recordStream, mimeType ? { mimeType } : undefined);

            audioChunks = [];
            isRecording = true;
            isStarting = false;
            recordBtn.disabled = false;
            recordBtn.classList.add("recording");

            maxDuration = getDuration(part);
            startTimer(maxDuration);

            mediaRecorder.ondataavailable = (e) => {
                if (e.data && e.data.size > 0) audioChunks.push(e.data);
            };

            mediaRecorder.onerror = (e) => {
                console.error("MediaRecorder error:", e);
                alert("❌ Lỗi thu âm: " + (e?.error?.message || e?.message || "unknown"));
            };

            mediaRecorder.onstop = () => {
                isRecording = false;
                recordBtn.classList.remove("recording");
                stopTimer();

                clearTimeout(recordingTimeout);
                recordingTimeout = null;

                // dọn tài nguyên xử lý + tắt mic
                cleanupWebAudio();
                cleanupStream();

                const finalType = mediaRecorder?.mimeType || mimeType || "audio/webm";
                const audioBlob = new Blob(audioChunks, { type: finalType });
                audioChunks = [];

                setPlaybackBlob(audioBlob);
                playbackArea.style.display = "flex";
                recordBtn.style.display = "none";
                createSubmitButton(audioBlob);
            };

            mediaRecorder.start();

            recordingTimeout = setTimeout(() => {
                if (mediaRecorder?.state === "recording") mediaRecorder.stop();
            }, maxDuration);

        } catch (err) {
            console.error(err);
            alert("❌ Không thể truy cập micro: " + (err?.message || err));

            isRecording = false;
            isStarting = false;
            recordBtn.disabled = false;
            recordBtn.classList.remove("recording");

            clearTimeout(recordingTimeout);
            recordingTimeout = null;

            cleanupWebAudio();
            cleanupStream();
            stopTimer();
        }
    }

    function stopRecording() {
        clearTimeout(recordingTimeout);
        recordingTimeout = null;

        if (mediaRecorder?.state === "recording") {
            mediaRecorder.stop();
        } else {
            cleanupWebAudio();
            cleanupStream();
            stopTimer();
            isRecording = false;
            isStarting = false;
            recordBtn.disabled = false;
            recordBtn.classList.remove("recording");
        }
    }

    recordBtn.addEventListener("click", async () => {
        if (applyAttemptUI(currentQuestion)) return;
        if (recordHelperText) recordHelperText.style.display = "none";

        if (!isRecording && !isStarting) await startRecording();
        else stopRecording();
    });

    redoBtn.addEventListener("click", resetRecordingUI);

    // Nếu user chuyển tab khi đang thu -> stop để khỏi kẹt mic
    document.addEventListener("visibilitychange", () => {
        if (document.hidden && isRecording) stopRecording();
    });


    // ===== Desktop list click =====
    questions.forEach((q, index) => q.addEventListener('click', () => selectQuestion(index)));

    // ===== Submit =====
    // Tìm đến hàm createSubmitButton và thay thế bằng đoạn này
    function createSubmitButton(audioBlob) {
        document.querySelector(".submit-btn")?.remove();

        const submitBtn = document.createElement("button");
        submitBtn.className = "btn btn-success submit-btn";
        submitBtn.style.marginTop = "20px";
        submitBtn.innerHTML = `Nộp bài`;

        submitBtn.onclick = async () => {
            // 1. Disable ngay khi bấm để tránh spam click
            submitBtn.disabled = true;
            submitBtn.innerHTML = `<span class="spinner-border spinner-border-sm me-2"></span>Đang gửi...`;

            const formData = new FormData();
            formData.append("audio", audioBlob, "student_record.webm");
            formData.append("questionIndex", currentQuestion);
            formData.append("part", part);
            formData.append("question", questionList[currentQuestion]);
            formData.append("exerciseId", exerciseIds[currentQuestion]);

            try {
                const res = await fetch("/Student/PracticeSpeaking/Analyze", { method: "POST", body: formData });
                if (!res.ok) throw new Error("Server error");

                await res.json();

                // Xử lý logic số lần thử (giữ nguyên logic cũ của bạn)
                attemptUsed[currentQuestion] = (attemptUsed[currentQuestion] || 0) + 1;
                const usedNow = attemptUsed[currentQuestion];
                const maxNow = (maxAttempts && maxAttempts.length > currentQuestion) ? (maxAttempts[currentQuestion] || 0) : 0;
                if (maxNow > 0 && usedNow >= maxNow) applyAttemptUI(currentQuestion);

                // ===== PHẦN SỬA ĐỔI QUAN TRỌNG Ở ĐÂY =====

                // KHÔNG chạy dòng này: submitBtn.disabled = false;
                // Vẫn giữ disabled = true để người dùng không bấm được nữa

                submitBtn.classList.remove("btn-success");
                submitBtn.classList.add("btn-primary");

                // Thay đổi nội dung
                submitBtn.innerHTML = `<i class="fas fa-check-circle me-2"></i> Bài đã được gửi xem trạng thái ở lịch sử`;

                // Gỡ bỏ sự kiện click để đảm bảo an toàn tuyệt đối
                submitBtn.onclick = null;

                // ==========================================

            } catch (err) {
                // Chỉ enable lại nút khi GẶP LỖI để người dùng thử lại
                submitBtn.disabled = false;
                submitBtn.classList.remove("btn-success");
                submitBtn.classList.add("btn-danger");
                submitBtn.innerHTML = `❌ Thử lại`;
                alert("⚠️ Lỗi khi gửi dữ liệu: " + err.message);
            }
        };

        document.querySelector(".question-box").appendChild(submitBtn);
    }

    // =========================
    // MOBILE FAB + OFFCANVAS
    // =========================
    const fabMenu = document.getElementById("mobileFabMenu");
    const fabMainBtn = document.getElementById("fabMainBtn");
    const fabExerciseBtn = document.getElementById("fabExerciseBtn");
    const fabHistoryBtn = document.getElementById("fabHistoryBtn");
    const offcanvasEl = document.getElementById("exerciseOffcanvas");
    const hasBootstrap = (typeof bootstrap !== "undefined");
    const exerciseOffcanvas = (hasBootstrap && offcanvasEl)
        ? bootstrap.Offcanvas.getOrCreateInstance(offcanvasEl)
        : null;

    function setFabOpen(open) {
        if (!fabMenu || !fabMainBtn) return;
        if (open) {
            fabMenu.classList.add("open");
            fabMainBtn.classList.remove("spin");
            void fabMainBtn.offsetWidth;
            fabMainBtn.classList.add("spin");
        } else {
            fabMenu.classList.remove("open");
        }
    }

    function buildExercisePicker() {
        if (!exercisePickerList) return;

        exercisePickerList.innerHTML = "";
        const n = (questionList?.length || 0);

        for (let i = 0; i < n; i++) {
            const btn = document.createElement("button");
            btn.type = "button";
            btn.className = "exercise-picker-item";
            btn.dataset.index = String(i);

            const title = (exerciseTitlesList && exerciseTitlesList.length > i) ? (exerciseTitlesList[i] || "") : "";
            const p = (parts && parts.length > i) ? parts[i] : part;

            btn.innerHTML = `
                        <span class="ep-left">Q${i + 1}</span>
                        <span class="ep-title">${title}</span>
                        <span class="ep-badge">Part ${p}</span>
                    `;

            btn.addEventListener("click", () => {
                selectQuestion(i);
                if (exerciseOffcanvas) exerciseOffcanvas.hide();
            });

            exercisePickerList.appendChild(btn);
        }

        updateExercisePickerActive();
    }

    function updateExercisePickerActive() {
        if (!exercisePickerList) return;
        const items = exercisePickerList.querySelectorAll(".exercise-picker-item");
        items.forEach(el => {
            const idx = Number(el.dataset.index);
            el.classList.toggle("active", idx === currentQuestion);
        });
    }

    if (fabMainBtn) {
        fabMainBtn.addEventListener("click", (e) => {
            e.preventDefault();
            e.stopPropagation();
            setFabOpen(!fabMenu?.classList.contains("open"));
        });
    }

    document.addEventListener("click", (e) => {
        if (!isMobile()) return;
        if (!fabMenu?.classList.contains("open")) return;
        if (fabMenu.contains(e.target)) return;
        setFabOpen(false);
    });

    if (fabHistoryBtn) {
        fabHistoryBtn.addEventListener("click", (e) => {
            e.preventDefault();
            setFabOpen(false);
            const exerciseId = exerciseIds[currentQuestion];
            window.location.href = `/Student/SpeakingReview/History?exerciseId=${exerciseId}`;
        });
    }

    if (fabExerciseBtn) {
        fabExerciseBtn.addEventListener("click", (e) => {
            e.preventDefault();
            setFabOpen(false);
            buildExercisePicker();
            if (exerciseOffcanvas) exerciseOffcanvas.show();
            else if (offcanvasEl) {
                offcanvasEl.classList.add("show");
                offcanvasEl.style.visibility = "visible";
            }
        });
    }

    // init
    if (questions.length > 0) selectQuestion(0);
    setTimeout(fitQuestionText, 0);

    window.addEventListener("resize", () => {
        setTimeout(fitQuestionText, 0);
        if (!isMobile()) setFabOpen(false);
    });
});
