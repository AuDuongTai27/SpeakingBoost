(function () {
    'use strict';

    const params = new URLSearchParams(location.search);
    const classExerciseId = parseInt(params.get('id')) || 0;

    let exerciseId = 0;
    let exercisePart = 1;
    let mediaRecorder = null;
    let audioChunks = [];
    let audioBlob = null;
    let isRecording = false;
    let timerInterval = null;
    let seconds = 0;
    let pollingInterval = null;

    window.toggleRecord = async function () {
        if (!isRecording) {
            try {
                const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
                audioChunks = [];
                mediaRecorder = new MediaRecorder(stream);
                mediaRecorder.ondataavailable = e => { if (e.data.size > 0) audioChunks.push(e.data); };
                mediaRecorder.onstop = () => {
                    audioBlob = new Blob(audioChunks, { type: 'audio/webm' });
                    const ap = document.getElementById('audioPlayback');
                    ap.src = URL.createObjectURL(audioBlob); ap.style.display = 'block';
                    document.getElementById('submitBtn').disabled = false;
                    stream.getTracks().forEach(t => t.stop());
                };
                mediaRecorder.start();
                isRecording = true;
                const btn = document.getElementById('recordBtn');
                btn.classList.add('recording'); btn.innerHTML = '<i class="bi bi-stop-fill"></i>';
                document.getElementById('recordStatus').textContent = '🔴 Đang ghi âm... Nhấn dừng để kết thúc';
                seconds = 0;
                timerInterval = setInterval(() => {
                    seconds++;
                    const m = Math.floor(seconds/60).toString().padStart(2,'0');
                    const s = (seconds%60).toString().padStart(2,'0');
                    document.getElementById('recordTimer').textContent = `${m}:${s}`;
                }, 1000);
            } catch(e) { Toast.err('Không thể truy cập microphone: ' + e.message); }
        } else {
            isRecording = false;
            mediaRecorder.stop(); clearInterval(timerInterval);
            const btn = document.getElementById('recordBtn');
            btn.classList.remove('recording'); btn.innerHTML = '<i class="bi bi-mic-fill"></i>';
            document.getElementById('recordStatus').textContent = 'Đã ghi xong. Nhấn nộp bài hoặc ghi lại.';
        }
    };

    window.resetRecording = function () {
        if (isRecording && mediaRecorder) mediaRecorder.stop();
        isRecording = false; clearInterval(timerInterval); clearInterval(pollingInterval);
        seconds = 0;
        document.getElementById('recordTimer').textContent = '00:00';
        document.getElementById('recordStatus').textContent = 'Nhấn nút để bắt đầu ghi âm';
        const btn = document.getElementById('recordBtn');
        btn.classList.remove('recording'); btn.innerHTML = '<i class="bi bi-mic-fill"></i>';
        document.getElementById('audioPlayback').style.display = 'none';
        document.getElementById('audioPlayback').src = '';
        document.getElementById('resultPanel').style.display = 'none';
        document.getElementById('submitBtn').disabled = true;
        audioBlob = null;
    };

    window.submitAnswer = async function () {
        if (!audioBlob) { Toast.err('Vui lòng ghi âm trước khi nộp bài.'); return; }
        const fd = new FormData();
        fd.append('audio', audioBlob, 'recording.webm');
        fd.append('exerciseId', exerciseId);
        fd.append('classExerciseId', classExerciseId);
        fd.append('part', exercisePart);

        document.getElementById('submitBtn').disabled = true;
        document.getElementById('recordStatus').textContent = '⏳ Đang gửi bài...';

        const res = await Api.postForm('/api/student/deadlines/analyze', fd);
        if (!res.success) { Toast.err(res.message); document.getElementById('submitBtn').disabled = false; return; }

        Toast.ok('Đã nộp! Đang chờ AI chấm điểm...');
        startPolling(res.data.submissionId);
    };

    function startPolling(submissionId) {
        document.getElementById('resultPanel').style.display = 'block';
        document.getElementById('resultScoreCircle').querySelector('.score-val').textContent = '...';
        document.getElementById('recordStatus').textContent = '⏳ AI đang chấm điểm...';

        pollingInterval = setInterval(async () => {
            const res = await Api.get(`/api/student/submissions/${submissionId}/status`);
            if (!res.success) return;
            const d = res.data;
            if (d.status === 'Evaluated' || d.status === 'Error' || d.status === 'Failed') {
                clearInterval(pollingInterval);
                const sc = scoreStyle(d.overall);
                document.getElementById('resultScoreCircle').querySelector('.score-val').textContent = sc.text;
                document.getElementById('resultSubScores').innerHTML = [
                    { label: 'Pronunciation', val: d.pronunciation, color: '#f59e0b' },
                    { label: 'Grammar', val: d.grammar, color: '#f97316' },
                    { label: 'Vocabulary', val: d.lexicalResource, color: '#f59e0b' },
                    { label: 'Coherence', val: d.coherence, color: '#f97316' },
                ].map(s => `<div class="col-6 col-md-3 text-center">
                    <div style="font-size:1.5rem;font-weight:800;color:${s.color};">${s.val != null ? s.val.toFixed(1) : '—'}</div>
                    <div style="font-size:0.72rem;color:#64748b;">${s.label}</div>
                </div>`).join('');
                if (d.aiFeedback) {
                    let fb = d.aiFeedback;
                    try { const obj = JSON.parse(fb); fb = obj.feedback || obj.Feedback || fb; } catch {}
                    document.getElementById('feedbackText').textContent = fb;
                    document.getElementById('resultFeedback').style.display = 'block';
                }
                document.getElementById('recordStatus').textContent = 'Đã chấm xong!';
                document.getElementById('infoStatus').textContent = 'Đã nộp';
                document.getElementById('infoStatus').style.background = '#dcfce7';
                document.getElementById('infoStatus').style.color = '#14532d';
                Toast.ok('AI đã chấm xong bài deadline!');
            }
        }, 3000);
    }

    function calcTimeLeft(deadline) {
        if (!deadline) return '—';
        const diff = new Date(deadline) - new Date();
        if (diff <= 0) return 'Đã hết hạn';
        const days = Math.floor(diff / 86400000);
        const hours = Math.floor((diff % 86400000) / 3600000);
        if (days > 0) return `${days} ngày ${hours} giờ`;
        return `${hours} giờ`;
    }

    initStudentPage('deadlines', async () => {
        if (!classExerciseId) { Toast.err('Không tìm thấy ID bài deadline.'); return; }
        const res = await Api.get(`/api/student/deadlines/${classExerciseId}`);
        if (!res.success || !res.data) { Toast.err(res.message || 'Không tải được bài deadline.'); return; }
        const d = res.data;

        exerciseId = d.exerciseId;
        exercisePart = d.part || 1;

        document.getElementById('headerTitle').textContent = d.title;
        document.getElementById('headerExerciseTitle').textContent = d.title;
        document.getElementById('headerDeadline').innerHTML = d.deadline ? `<i class="bi bi-calendar-event me-1"></i>Hạn: ${fmtDateTime(d.deadline)}` : '';
        document.getElementById('qPartBadge').textContent = `Part ${d.part || 1}`;
        document.getElementById('questionText').textContent = `"${d.question}"`;
        document.getElementById('infoClass').textContent = d.className || '—';
        document.getElementById('timeLeft').textContent = calcTimeLeft(d.deadline);

        if (d.status === 'Submitted') {
            document.getElementById('infoStatus').textContent = 'Đã nộp';
            document.getElementById('submitBtn').disabled = true;
            document.getElementById('recordBtn').disabled = true;
            document.getElementById('recordStatus').textContent = 'Bạn đã nộp bài deadline này rồi.';
        }
    });
})();
