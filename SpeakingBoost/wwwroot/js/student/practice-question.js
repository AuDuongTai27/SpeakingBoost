(function () {
    'use strict';

    const params = new URLSearchParams(location.search);
    const topicId = parseInt(params.get('topicId')) || 0;
    const part = parseInt(params.get('part')) || 0;

    let questions = [];
    let currentIdx = 0;
    let mediaRecorder = null;
    let audioChunks = [];
    let audioBlob = null;
    let isRecording = false;
    let timerInterval = null;
    let seconds = 0;
    let pollingInterval = null;

    window.jumpTo = function (idx) {
        currentIdx = idx;
        document.querySelectorAll('.question-list-item').forEach((el, i) => el.classList.toggle('active', i === idx));
        const q = questions[idx];
        document.getElementById('questionText').textContent = `"${q.question}"`;
        document.getElementById('qNum').textContent = `Câu hỏi ${idx + 1}`;
        document.getElementById('headerCount').textContent = `Câu ${idx + 1} / ${questions.length}`;
        window.resetRecording();
    };

    window.nextQuestion = function () {
        if (currentIdx < questions.length - 1) window.jumpTo(currentIdx + 1);
        else Toast.ok('Bạn đã hoàn thành tất cả câu hỏi!');
    };

    window.toggleRecord = async function () {
        if (!isRecording) {
            try {
                const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
                audioChunks = [];
                mediaRecorder = new MediaRecorder(stream);
                mediaRecorder.ondataavailable = e => { if (e.data.size > 0) audioChunks.push(e.data); };
                mediaRecorder.onstop = () => {
                    audioBlob = new Blob(audioChunks, { type: 'audio/webm' });
                    const url = URL.createObjectURL(audioBlob);
                    const ap = document.getElementById('audioPlayback');
                    ap.src = url; ap.style.display = 'block';
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
            mediaRecorder.stop();
            clearInterval(timerInterval);
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
        const q = questions[currentIdx];
        const fd = new FormData();
        fd.append('audio', audioBlob, 'recording.webm');
        fd.append('exerciseId', q.exerciseId);
        fd.append('part', part || 1);

        document.getElementById('submitBtn').disabled = true;
        document.getElementById('recordStatus').textContent = '⏳ Đang gửi bài...';

        const res = await Api.postForm('/api/student/practice/analyze', fd);
        if (!res.success) { Toast.err(res.message); document.getElementById('submitBtn').disabled = false; return; }

        Toast.ok('Đã nộp! Đang chờ AI chấm điểm...');
        const submissionId = res.data.submissionId;
        startPolling(submissionId);
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
                showResult(d);
            }
        }, 3000);
    }

    function showResult(d) {
        document.getElementById('recordStatus').textContent = 'Đã chấm xong!';
        const sc = scoreStyle(d.overall);
        const circle = document.getElementById('resultScoreCircle');
        circle.style.background = `linear-gradient(135deg,#6366f1,#06b6d4)`;
        circle.querySelector('.score-val').textContent = sc.text;

        const subs = [
            { label: 'Pronunciation', val: d.pronunciation, color: '#6366f1' },
            { label: 'Grammar', val: d.grammar, color: '#818cf8' },
            { label: 'Vocabulary', val: d.lexicalResource, color: '#06b6d4' },
            { label: 'Coherence', val: d.coherence, color: '#6366f1' },
        ];
        document.getElementById('resultSubScores').innerHTML = subs.map(s => `
            <div class="col-6 col-md-3 text-center">
                <div style="font-size:1.5rem;font-weight:800;color:${s.color};">${s.val != null ? s.val.toFixed(1) : '—'}</div>
                <div style="font-size:0.72rem;color:#64748b;">${s.label}</div>
            </div>`).join('');

        if (d.aiFeedback) {
            let fbText = d.aiFeedback;
            try { const fb = JSON.parse(d.aiFeedback); fbText = fb.feedback || fb.Feedback || fbText; } catch {}
            document.getElementById('feedbackText').textContent = fbText;
            document.getElementById('resultFeedback').style.display = 'block';
        }
        Toast.ok('AI đã chấm xong bài của bạn!');
    }

    initStudentPage('practice', async () => {
        const res = await Api.get(`/api/student/practice/topics/${topicId}?part=${part}`);
        if (!res.success || !res.data || res.data.length === 0) {
            Toast.err(res.message || 'Không tải được câu hỏi.');
            return;
        }
        questions = res.data;
        document.getElementById('headerTopicName').textContent = questions[0]?.title?.split(' - ')[0] || `Chủ đề #${topicId}`;
        document.getElementById('headerTitle').textContent = questions[0]?.title?.split(' - ')[0] || `Chủ đề #${topicId}`;
        document.getElementById('headerPartBadge').textContent = part > 0 ? `Part ${part}` : 'Practice';
        document.getElementById('qPartBadge').textContent = part > 0 ? `Part ${part}` : 'Practice';

        document.getElementById('questionList').innerHTML = questions.map((q, i) => `
            <div class="question-list-item${i===0?' active':''}" onclick="window.jumpTo(${i})">
                <span class="q-num">${i+1}</span>
                <span style="flex:1;min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;font-size:0.8rem;">${q.question}</span>
            </div>`).join('');

        window.jumpTo(0);
    });
})();