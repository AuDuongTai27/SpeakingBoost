document.addEventListener("DOMContentLoaded", () => {

                    // ===== Transcript highlight =====
                    const transcriptRaw = (window.__attemptDetail && window.__attemptDetail.transcriptRaw) || "";

                    // Feedback JSON (for Correct + Description in popover)
                    const feedbackRaw = (window.__attemptDetail && window.__attemptDetail.feedbackRaw) || {};

                    // Normalize for matching (ignore punctuation)
                    const normKey = (s) => (s || "")
                        .toString()
                        .trim()
                        .toLowerCase()
                        .replace(/[\u2019']/g, "'")
                        .replace(/[^a-z0-9\s]/gi, " ")
                        .replace(/\s+/g, " ")
                        .trim();

                    // Get first property by key name pattern
                    function getByKeyPattern(obj, patterns) {
                        if (!obj || typeof obj !== "object") return "";
                        for (const k of Object.keys(obj)) {
                            const lk = k.toLowerCase();
                            if (patterns.some(p => lk.includes(p))) {
                                const v = obj[k];
                                if (v != null && v !== "") return v;
                            }
                        }
                        return "";
                    }

                    function collectEntries(node) {
                        const entries = [];
                        const visited = new Set();

                        const walk = (n, depth = 0) => {
                            if (!n || depth > 10) return;
                            if (typeof n !== "object") return;
                            if (visited.has(n)) return;
                            visited.add(n);

                            if (Array.isArray(n)) {
                                n.forEach(x => walk(x, depth + 1));
                                return;
                            }

                            // Try to infer fields by key patterns (robust vs hard-coded keys)
                            const wrongVal = getByKeyPattern(n, ["wrong", "original", "mistake", "error", "incorrect"]);
                            const correctVal = getByKeyPattern(n, ["correct", "replacement", "suggest", "fix", "expected", "right"]);
                            const explainVal = getByKeyPattern(n, ["explain", "description", "reason", "note", "detail", "message"]);

                            const wrong = wrongVal != null ? String(wrongVal).trim() : "";
                            const correct = correctVal != null ? String(correctVal).trim() : "";
                            const explain = explainVal != null ? String(explainVal).trim() : "";

                            if (wrong && (correct || explain)) {
                                entries.push({ wrong, correct, explain });
                            }

                            for (const k of Object.keys(n)) walk(n[k], depth + 1);
                        };

                        walk(node);

                        // Deduplicate by normalized wrong
                        const map = new Map();
                        for (const e of entries) {
                            const k = normKey(e.wrong);
                            if (!k) continue;
                            if (!map.has(k)) map.set(k, { ...e });
                            else {
                                const cur = map.get(k);
                                if (!cur.correct && e.correct) cur.correct = e.correct;
                                if (!cur.explain && e.explain) cur.explain = e.explain;
                            }
                        }

                        const list = Array.from(map.values());
                        // Prefer longer wrongs first for substring matching
                        list.sort((a, b) => normKey(b.wrong).length - normKey(a.wrong).length);
                        return { map, list };
                    }

    // File: attempt-detail.js

    function pickCategoryRoot(root, key) {
        const r = root || {};

        // SỬA: Thêm ưu tiên check các key snake_case (khớp với JSON Backend)
        if (key === "fluency") {
            return r.fluency_coherence ||  // ✅ Key chính xác từ JSON
                r.FluencyCoherence ||
                r.fluencyCoherence ||
                r.fluency ||
                r.Fluency ||
                null;
        }

        if (key === "lexical") {
            return r.lexical_resource ||   // ✅ Key chính xác từ JSON
                r.LexicalResource ||
                r.lexicalResource ||
                r.lexical ||
                r.Lexical ||
                null;
        }

        if (key === "grammar") {
            return r.grammar ||
                r.Grammar ||
                null;
        }

        return null;
    }

                    function buildFeedbackIndex(root) {
                        const idx = {};
                        const all = collectEntries(root);
                        idx._all = all;

                        ["fluency", "lexical", "grammar"].forEach(k => {
                            const catRoot = pickCategoryRoot(root, k);
                            idx[k] = catRoot ? collectEntries(catRoot) : { map: new Map(), list: [] };
                        });

                        return idx;
                    }

                    function lookupFromIndex(catIndex, frag, fallbackAll) {
                        const key = normKey(frag);
                        if (!key) return null;

                        const tryIndex = (ci) => {
                            if (!ci) return null;
                            if (ci.map && ci.map.has(key)) return ci.map.get(key);

                            // substring match (ignore punctuation)
                            for (const e of (ci.list || [])) {
                                const w = normKey(e.wrong);
                                if (!w) continue;
                                if (key.includes(w) || w.includes(key)) return e;
                            }
                            return null;
                        };

                        return tryIndex(catIndex) || tryIndex(fallbackAll) || null;
                    }

                    const feedbackIndex = buildFeedbackIndex(feedbackRaw);
                    const transcriptEl = document.getElementById("transcriptText");
                    const transcriptPanel = document.querySelector(".transcript-panel");

                    let currentKey = "fluency";
                    let openPopEl = null;
                    let openSpanEl = null;

                    const norm = (s) => (s || "").toString().trim().toLowerCase().replace(/\s+/g, " ");

                    function escapeRegex(s) { return s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&"); }
                    const ICON_PLAY = `<svg class="icon-play" viewBox="0 0 16 16" aria-hidden="true"><path d="M5 3.5v9l8-4.5-8-4.5z"/></svg>`;
const ICON_SPEAKER = `<svg class="icon-speaker" viewBox="0 0 16 16" aria-hidden="true"><path d="M9.5 2.5 6.2 5H3.5A1.5 1.5 0 0 0 2 6.5v3A1.5 1.5 0 0 0 3.5 11h2.7l3.3 2.5a.8.8 0 0 0 1.3-.64V3.14a.8.8 0 0 0-1.3-.64z"/><path d="M12.2 5.3a.5.5 0 0 1 .7.1 4.5 4.5 0 0 1 0 5.2.5.5 0 0 1-.8-.6 3.5 3.5 0 0 0 0-4 .5.5 0 0 1 .1-.7z"/></svg>`;

function escapeHtml(s) {
                        return (s || "").toString()
                            .replace(/&/g, "&amp;")
                            .replace(/</g, "&lt;")
                            .replace(/>/g, "&gt;");
                    }

                    function closePopover() {
                        if (openPopEl) { openPopEl.remove(); openPopEl = null; }
                        if (openSpanEl) { openSpanEl.classList.remove("hl-active"); openSpanEl = null; }
                    }

                    function findWrongElement(paneId, frag) {
                        const pane = document.getElementById(paneId);
                        if (!pane) return null;

                        const want = norm(frag);
                        const els = [...pane.querySelectorAll("[data-wrong]")];
                        return els.find(el => norm(el.dataset.wrong) === want)
                            || els.find(el => norm(el.textContent) === want)
                            || null;
                    }

    function extractDetailFromPane(key, frag, clickedElement) { // Thêm tham số thứ 3
        // Khởi tạo object detail mặc định
        const detail = {
            key,
            title: frag,
            wrong: frag,
            correct: "",
            explain: "",
            pills: [],
            pron: null
        };

        // ============================================================
        // NHÁNH 1: PRONUNCIATION (Lấy trực tiếp từ JSON theo Index)
        // ============================================================
        if (key === "pronun") {
            if (!clickedElement || !clickedElement.dataset.index) {
                detail.explain = "Lỗi: Không xác định được vị trí từ.";
                return detail;
            }

            const idx = parseInt(clickedElement.dataset.index);

            // Tìm object từ trong mảng low_words
            let lowWord = null;
            try {
                const words = feedbackRaw.pronunciationReport.low_words || [];
                lowWord = words.find(w => w.id === idx);
            } catch (e) { }

            if (!lowWord) {
                detail.explain = "Không tìm thấy dữ liệu chi tiết trong JSON.";
                return detail;
            }

            // 1. Điền Pills (Accuracy, ErrorType)
            const acc = lowWord.accuracyScore;
            const errType = lowWord.errorType;
            if (acc) detail.pills.push(`Accuracy: ${Math.round(acc)}`);
            if (errType) detail.pills.push(errType);

            // 2. Build HTML cho Phonemes
            let phHtml = "";
            if (lowWord.phonemes && Array.isArray(lowWord.phonemes)) {
                phHtml = `<div class="d-flex flex-wrap gap-2">`;
                // Lấy ngưỡng threshold (mặc định 80 nếu ko có)
                const th = feedbackRaw.pronunciationReport.threshold || 80;

                lowWord.phonemes.forEach(p => {
                    const isBad = p.accuracyScore < th;
                    const extraClass = isBad ? "border-danger" : "";
                    const phErr = p.errorType || "";
                    phHtml += `
                    <span class="ph-chip ${extraClass}" title="${phErr}">
                        <span class="fw-semibold">${escapeHtml(p.phoneme)}</span>
                        <span class="score text-muted">${Math.round(p.accuracyScore || 0)}</span>
                    </span>
                 `;
                });
                phHtml += `</div>`;
            }

            // 3. Tính toán thời gian audio
            // Ưu tiên startSec, nếu không thì tính từ offset/ticks
            const start = lowWord.startSec || (lowWord.offset ? lowWord.offset / 10000000 : 0);
            const end = lowWord.endSec || (lowWord.duration ? start + (lowWord.duration / 10000000) : start);

            detail.pron = {
                start: start,
                end: end,
                timeText: `${start.toFixed(2)} – ${end.toFixed(2)}`,
                phonemesHtml: phHtml
            };

            return detail;
        }

        // ============================================================
        // NHÁNH 2: CÁC TAB KHÁC (Logic CŨ - Quét HTML Table)
        // ============================================================

        // Tìm phần tử hiển thị lỗi bên cột trái/tab panel
        const paneId =
            key === "fluency" ? "tab-fluency" :
                key === "lexical" ? "tab-lexical" :
                    "tab-grammar";

        const wrongEl = findWrongElement(paneId, frag);

        if (!wrongEl) {
            detail.explain = "Không tìm thấy chi tiết cho mục này.";
            return detail;
        }

        // Lấy text hiển thị chính xác từ HTML
        detail.wrong = (wrongEl.dataset.wrong || wrongEl.textContent || frag).trim();

        // -- Logic cũ: Lấy Correct/Explain từ JSON (feedbackIndex) --
        const j = lookupFromIndex(feedbackIndex[key], frag, feedbackIndex._all);
        if (j) {
            detail.correct = j.correct || detail.correct || "";
            detail.explain = j.explain || detail.explain || "";
        }

        // -- Logic cũ: Fallback lấy từ HTML Table (nếu JSON fail) --
        const row = wrongEl.closest("tr")
            || wrongEl.closest(".feedback-item")
            || wrongEl.closest("[data-row]")
            || wrongEl.parentElement;

        // Lấy từ data attributes
        const correctEl = row ? row.querySelector("[data-correct]") : null;
        const explainEl = row ? row.querySelector("[data-explain]") : null;

        if (correctEl) detail.correct = (correctEl.dataset.correct || correctEl.textContent || "").trim();
        if (explainEl) detail.explain = (explainEl.dataset.explain || explainEl.textContent || "").trim();

        // Lấy từ table columns (cột 2 là correct, cột 3 là explain)
        const tr = wrongEl.closest("tr");
        if (tr && (!detail.correct || !detail.explain)) {
            const tds = [...tr.querySelectorAll("td")].map(td => td.innerText.trim());
            if (!detail.correct && tds.length >= 2) detail.correct = tds[1] || "";
            if (!detail.explain && tds.length >= 3) detail.explain = tds.slice(2).filter(Boolean).join("\n");
        }

        return detail;
    }

                    function buildPopover(detail) {
                        const pop = document.createElement("div");
                        pop.className = "hl-popover";
                        pop.setAttribute("role", "dialog");

                        const pills = (detail.pills && detail.pills.length)
                            ? `<div class="hl-pop-pills">${detail.pills.map(p => `<span class="hl-pop-pill">${escapeHtml(p)}</span>`).join("")}</div>`
                            : "";

                        let bodyHtml = `${pills}
                            <div class="hl-pop-row"><span class="hl-wrong">Bạn nói:</span> <span class="hl-wrong">${escapeHtml(detail.wrong)}</span></div>`;

                        if (detail.key !== "pronun") {
                            const correct = (detail.correct && detail.correct.trim()) ? detail.correct : "—";
                            const explain = (detail.explain && detail.explain.trim()) ? detail.explain : "—";

                            // Always show both Sai + Đúng (Pronunciation already handled in else)
                            bodyHtml += `<div class="hl-pop-row"><span class="hl-correct">Đề xuất:</span> <span class="hl-correct">${escapeHtml(correct)}</span></div>`;

                            // Always show Description (from Explain column)
                            bodyHtml += `<div class="hl-desc-title fw-bold mt-2">Description</div><div class="hl-explain">${escapeHtml(explain)}</div>`;
                        } else {
                            const start = detail.pron?.start || "";
                            const end = detail.pron?.end || "";
                            const timeText = detail.pron?.timeText || "";

                            bodyHtml += `
                                <div class="hl-pop-pron">
                                    <div class="d-flex align-items-center gap-2">
                                        <div class="pron-actions"><button type="button"
                                                class="play-seg-btn js-play-seg"
                                                data-start="${escapeHtml(start)}"
                                                data-end="${escapeHtml(end)}"
                                                title="Nghe lại từ này" aria-label="Nghe lại từ này">${ICON_PLAY}</button>
                                        <button type="button"
                                                class="play-correct-btn js-speak-word"
                                                data-word="${escapeHtml(detail.wrong)}"
                                                title="Nghe phát âm đúng" aria-label="Nghe phát âm đúng">${ICON_SPEAKER}</button></div>
                                        <div class="fw-semibold">${escapeHtml(detail.wrong)}</div>
                                        ${timeText ? `<div class="ms-auto text-muted small">${escapeHtml(timeText)}</div>` : ``}
                                    </div>

                                    <div class="mt-2 small text-muted">Phonemes (IPA)</div>
                                    ${detail.pron?.phonemesHtml
                                        ? `<div class="ph-wrap">${detail.pron.phonemesHtml}</div>`
                                        : `<div class="text-muted">Không có phoneme chi tiết cho từ này.</div>`}
                                </div>
                            `;
                        }

                        pop.innerHTML = `
                            <div class="hl-pop-head">
                                <div class="hl-pop-title">${escapeHtml(detail.title || "")}</div>
                                <button type="button" class="hl-pop-close" aria-label="Close">×</button>
                            </div>
                            ${bodyHtml}
                        `;

                        pop.addEventListener("click", (e) => e.stopPropagation());
                        pop.querySelector(".hl-pop-close")?.addEventListener("click", (e) => {
                            e.stopPropagation();
                            closePopover();
                        });

                        return pop;
                    }

                    function positionPopover(span, pop) {
                        if (!transcriptPanel) return;

                        const panelRect = transcriptPanel.getBoundingClientRect();
                        const spanRect = span.getBoundingClientRect();

                        // Default: show below the tapped word
                        let top = (spanRect.bottom - panelRect.top) + 10;
                        let left = (spanRect.left - panelRect.left);

                        pop.style.top = "0px";
                        pop.style.left = "0px";
                        pop.style.visibility = "hidden";
                        transcriptPanel.appendChild(pop);

                        const pad = 10;
                        const maxLeft = panelRect.width - pop.offsetWidth - pad;
                        left = Math.max(pad, Math.min(left, maxLeft));

                        // If not enough space below, try above
                        const maxTop = panelRect.height - pop.offsetHeight - pad;
                        if (top > maxTop) {
                            const above = (spanRect.top - panelRect.top) - pop.offsetHeight - 12;
                            top = Math.max(pad, above);
                        }

                        pop.style.left = `${left}px`;
                        pop.style.top = `${top}px`;
                        pop.style.visibility = "visible";
                    }

                    function getWrongsFromPane(paneId) {
                        const pane = document.getElementById(paneId);
                        if (!pane) return [];
                        const arr = [...pane.querySelectorAll("[data-wrong]")]
                            .map(x => (x.dataset.wrong || "").trim())
                            .filter(Boolean);
                        return [...new Set(arr)].sort((a, b) => b.length - a.length);
                    }

                function renderTranscript(key) {
                    // 1. Xử lý tab Suggested Answer (giữ nguyên)
                    if (key === "suggest") {
                        closePopover();
                        transcriptEl.innerHTML = escapeHtml(transcriptRaw).replace(/\n/g, "<br/>");
                        return;
                    }

                    closePopover();

                    // ============================================================
                    // NHÁNH 1: PRONUNCIATION (Logic MỚI - Dùng Index)
                    // ============================================================
                    if (key === "pronun") {
                        // Lấy danh sách từ lỗi từ JSON gốc (không phụ thuộc HTML accordion)
                        let lowWords = [];
                        try {
                            if (feedbackRaw && feedbackRaw.pronunciationReport && Array.isArray(feedbackRaw.pronunciationReport.low_words)) {
                                lowWords = feedbackRaw.pronunciationReport.low_words;
                            }
                        } catch (e) { console.error("Error reading pronunciation json", e); }


                        // Tạo Map: Index -> Có lỗi không?
                        const errorMap = new Set();
                        lowWords.forEach(w => {
                            // w.id là index bạn đã thêm ở backend
                            errorMap.add(w.id);
                        });


                        // 👉 2. DEBUG MAP ID

                        // Tách transcript thành từ (Split theo khoảng trắng)
                        // Lưu ý: Backend tách thế nào thì Frontend tách thế đó để khớp index
                        const words = transcriptRaw.trim().split(/\s+/);


                        // 👉 3. DEBUG MẢNG TỪ & INDEX
                  
                        // Render lại HTML
                        // Render lại HTML
                        const htmlParts = words.map((word, idx) => {
                            const escapedWord = escapeHtml(word);

                            if (errorMap.has(idx)) {
                                // 🔴 SỬA: Viết thành 1 dòng liền mạch để tránh bị chèn <br/>
                                return `<span class="hl hl-pro hl-tap" data-index="${idx}" role="button" tabindex="0">${escapedWord}</span>`;
                            }

                            return escapedWord;
                        });

                        // Nối lại và hiển thị
                        transcriptEl.innerHTML = htmlParts.join(" ").replace(/\n/g, "<br/>");
                        return;
                    }

                    // ============================================================
                    // NHÁNH 2: CÁC TAB KHÁC (Logic CŨ - Quét DOM & Regex)
                    // ============================================================
                    const paneId =
                        key === "fluency" ? "tab-fluency" :
                            key === "lexical" ? "tab-lexical" :
                                "tab-grammar"; // Default fallback

                    const wrongs = getWrongsFromPane(paneId);
                    let text = transcriptRaw;

                    const marks = [];
                    wrongs.forEach((frag, idx) => {
                        if (!frag) return;
                        const re = new RegExp(escapeRegex(frag), "gi");
                        text = text.replace(re, (m) => {
                            const token = `@@HL${idx}_${marks.length}@@`;
                            marks.push({ token, match: m });
                            return token;
                        });
                    });

                    let html = escapeHtml(text);
                    const cls =
                        key === "fluency" ? "hl hl-flu" :
                            key === "lexical" ? "hl hl-lex" :
                                "hl hl-gra";

                    marks.forEach(m => {
                        const enc = encodeURIComponent(m.match);
                        html = html.split(m.token).join(`<span class="${cls} hl-tap" data-hl="${enc}" role="button" tabindex="0">${escapeHtml(m.match)}</span>`);
                    });

                    transcriptEl.innerHTML = html.replace(/\n/g, "<br/>");
                }

                    // Default
                    currentKey = "fluency";
                    renderTranscript(currentKey);

                    // Tab change
                    document.querySelectorAll('.tab-custom .nav-link[data-key]').forEach(btn => {
                        btn.addEventListener("shown.bs.tab", (e) => {
                            currentKey = e.target.dataset.key;
                            closePopover();
                            renderTranscript(currentKey);
                        });
                        btn.addEventListener("click", () => {
                            setTimeout(() => {
                                currentKey = btn.dataset.key;
                                closePopover();
                                renderTranscript(currentKey);
                            }, 0);
                        });
                    });

                    // Click underlined transcript => open popover (desktop + mobile)
    transcriptEl.addEventListener("click", (e) => {
    const span = e.target.closest(".hl-tap");
    if (!span) return; // Thêm dòng này để tránh lỗi

    e.stopPropagation();

    const frag = span.dataset.hl ? decodeURIComponent(span.dataset.hl) : (span.textContent || "").trim();

    if (openSpanEl && openSpanEl === span) {
        closePopover();
        return;
    }

    closePopover();
    openSpanEl = span;
    openSpanEl.classList.add("hl-active");

    // QUAN TRỌNG: Truyền thêm 'span' vào tham số thứ 3
    const detail = extractDetailFromPane(currentKey, frag, span);

    openPopEl = buildPopover(detail);
    transcriptPanel.appendChild(openPopEl);
    positionPopover(span, openPopEl);
});

                    // Reposition popover on scroll inside transcript (desktop) / resize
                    transcriptEl.addEventListener("scroll", () => {
                        if (openSpanEl && openPopEl) positionPopover(openSpanEl, openPopEl);
                    });
                    window.addEventListener("resize", () => {
                        if (openSpanEl && openPopEl) positionPopover(openSpanEl, openPopEl);
                    });

                    // Click outside closes
                    document.addEventListener("click", () => {
                        closePopover();
                    });

                    // ===== Audio: play segments (delegate so popover button works too) =====
                    const audio = document.getElementById("attemptAudio");
                    let endAt = null;

                    if (audio) {
                        audio.addEventListener("timeupdate", () => {
                            if (endAt != null && audio.currentTime >= endAt) {
                                audio.pause();
                                endAt = null;
                            }
                        });
                    }

                    function playSegment(start, end) {
                        if (!audio) return;
                        endAt = (end != null && !Number.isNaN(end) && end > start) ? end : null;
                        audio.currentTime = Math.max(0, start);
                        audio.play();
                    }

                    document.addEventListener("click", (e) => {
                        const btn = e.target.closest(".js-play-seg");
                        if (!btn) return;

                        // Capture-phase delegation so it works inside popover too
                        const start = parseFloat(btn.dataset.start);
                        const end = parseFloat(btn.dataset.end);
                        if (!Number.isNaN(start)) playSegment(start, Number.isNaN(end) ? null : end);
                    }, true);

                    // ===== Correct word: listen to the "right" pronunciation (TTS, fallback to dictionary audio) =====
                    const canTts = ("speechSynthesis" in window) && ("SpeechSynthesisUtterance" in window);
                    let ttsVoices = [];

                    function refreshVoices() {
                        try {
                            if (!canTts) return;
                            ttsVoices = window.speechSynthesis.getVoices() || [];
                        } catch { /* ignore */ }
                    }

                    if (canTts) {
                        refreshVoices();
                        // Chrome needs this event to get voices list.
                        if (typeof window.speechSynthesis.onvoiceschanged !== "undefined") {
                            window.speechSynthesis.onvoiceschanged = refreshVoices;
                        }
                    }

                    function pickEnglishVoice() {
                        if (!ttsVoices || !ttsVoices.length) refreshVoices();
                        const v = ttsVoices || [];

                        // --- PHẦN THÊM MỚI: Danh sách ưu tiên ---
                        const preferredNames = [
                            "Google US English",      // Giọng Google (Chrome) - Rất tự nhiên
                            "Samantha",               // Giọng (macOS/iOS)
                            "Daniel"                  // Giọng (macOS/iOS)
                        ];

                        // 1. Tìm xem có giọng nào trùng tên trong danh sách ưu tiên không
                        const preferred = v.find(voice => preferredNames.some(name => voice.name.includes(name)));
                        if (preferred) return preferred;
                        // ----------------------------------------

                        // 2. Nếu không có giọng xịn, dùng logic cũ (Ưu tiên en-US trước cho phổ thông)
                        const first = (pred) => v.find(pred);
                        return (
                            first(x => (x.lang || "").toLowerCase().startsWith("en-us"))
                            || first(x => (x.lang || "").toLowerCase().startsWith("en-gb"))
                            || first(x => (x.lang || "").toLowerCase().startsWith("en"))
                            || v[0]
                            || null
                        );
                    }

                    function cleanWord(raw) {
                        return (raw || "")
                            .toString()
                            .trim()
                            .replace(/[\u2019]/g, "'")
                            .replace(/[^a-zA-Z'\-\s]/g, "")
                            .replace(/\s+/g, " ")
                            .trim();
                    }

                    let dictAudio = null;
                    async function playDictionaryAudio(word) {
                        const w = encodeURIComponent((word || "").toLowerCase());
                        const url = `https://api.dictionaryapi.dev/api/v2/entries/en/${w}`;
                        const res = await fetch(url);
                        if (!res.ok) throw new Error("dictionary_api_failed");
                        const data = await res.json();

                        // Try to find the first usable audio url in phonetics
                        const phonetics = (data && data[0] && data[0].phonetics) ? data[0].phonetics : [];
                        const audioUrl = (phonetics || []).map(p => p && p.audio).find(a => a && a.toString().trim());
                        if (!audioUrl) throw new Error("no_audio");

                        if (audio && !audio.paused) {
                            audio.pause();
                            endAt = null;
                        }

                        if (!dictAudio) dictAudio = new Audio();
                        dictAudio.pause();
                        dictAudio.currentTime = 0;
                        dictAudio.src = audioUrl;
                        await dictAudio.play();
                    }

                    function speakWord(rawWord, btnEl) {
                        const word = cleanWord(rawWord);
                        if (!word) return;

                        // Pause user's audio if playing to avoid overlap
                        if (audio && !audio.paused) {
                            audio.pause();
                            endAt = null;
                        }
                        if (dictAudio) {
                            try { dictAudio.pause(); } catch { }
                        }

                        if (!canTts) throw new Error("no_tts");

                        const voice = pickEnglishVoice();

                        const utter = new SpeechSynthesisUtterance(word);               

                        if (voice) {
                            utter.voice = voice;
                            utter.lang = voice.lang || "en-GB";
                        } else {
                            utter.lang = "en-GB";
                        }
                        utter.rate = 0.95;
                        utter.pitch = 1;
                        utter.volume = 1;

                        if (btnEl) btnEl.classList.add("is-speaking");
                        utter.onend = () => { if (btnEl) btnEl.classList.remove("is-speaking"); };
                        utter.onerror = () => { if (btnEl) btnEl.classList.remove("is-speaking"); };

                        try { window.speechSynthesis.cancel(); } catch { }
                        window.speechSynthesis.speak(utter);
                    }

                    document.addEventListener("click", async (e) => {
                        const btn = e.target.closest(".js-speak-word");
                        if (!btn) return;

                        e.preventDefault();
                        e.stopPropagation();

                        const rawWord = btn.dataset.word || "";

                        try {
                            // Primary: browser TTS
                            speakWord(rawWord, btn);
                        } catch {
                            // Fallback: dictionary audio (online)
                            btn.classList.add("is-speaking");
                            try {
                                await playDictionaryAudio(cleanWord(rawWord));
                            } catch {
                                alert("Trình duyệt không hỗ trợ TTS và cũng không lấy được audio từ điển cho từ này.");
                            } finally {
                                btn.classList.remove("is-speaking");
                            }
                        }
                    }, true);

                    // ===== Scorecard audio control (play + slider) =====
                    const playBtn = document.getElementById("mobileReplayBtn");
                    const range = document.getElementById("mobileProgressRange");

                    function paintRange(r) {
                        if (!r) return;
                        const min = Number(r.min || 0);
                        const max = Number(r.max || 100);
                        const val = Number(r.value || 0);
                        const pct = ((val - min) / (max - min)) * 100;

                        r.style.background =
                            `linear-gradient(90deg,
                                rgba(255,255,255,.95) 0%,
                                rgba(255,255,255,.95) ${pct}%,
                                rgba(255,255,255,.35) ${pct}%,
                                rgba(255,255,255,.35) 100%)`;
                    }

                    if (range) {
                        range.addEventListener("input", () => paintRange(range));
                        paintRange(range);
                    }

                    if (audio && range) {
                        let isDragging = false;
                        const clamp = (n, a, b) => Math.min(b, Math.max(a, n));

                        const seekFromRange = () => {
                            if (!audio.duration || !isFinite(audio.duration)) return;
                            const pct = clamp(Number(range.value || 0) / 100, 0, 1);
                            audio.currentTime = pct * audio.duration;
                        };

                        const syncRangeFromAudio = () => {
                            if (!audio.duration || !isFinite(audio.duration) || isDragging) return;
                            const pct = clamp((audio.currentTime / audio.duration) * 100, 0, 100);
                            range.value = pct.toString();
                            paintRange(range);
                        };

                        audio.addEventListener("loadedmetadata", syncRangeFromAudio);
                        audio.addEventListener("timeupdate", syncRangeFromAudio);
                        audio.addEventListener("ended", () => {
                            if (playBtn) playBtn.classList.remove("is-playing");
                            if (range) {
                                range.value = "100";
                                paintRange(range);
                            }
                        });

                        const startDrag = () => { isDragging = true; };
                        const endDrag = () => { isDragging = false; seekFromRange(); };

                        range.addEventListener("pointerdown", startDrag);
                        range.addEventListener("pointerup", endDrag);

                        range.addEventListener("touchstart", startDrag, { passive: true });
                        range.addEventListener("touchend", endDrag, { passive: true });

                        range.addEventListener("change", seekFromRange);
                    }

                    if (audio && playBtn) {
                        playBtn.addEventListener("click", () => {
                            if (audio.paused) audio.play();
                            else audio.pause();
                        });

                        audio.addEventListener("play", () => playBtn.classList.add("is-playing"));
                        audio.addEventListener("pause", () => playBtn.classList.remove("is-playing"));
                    }

                    // ===== Transcript UX (toggle + copy) =====
                    const btnToggle = document.getElementById("btnToggleTranscript");
                    const btnCopy = document.getElementById("btnCopyTranscript");

                    if (btnToggle && transcriptPanel) {
                        btnToggle.addEventListener("click", () => {
                            transcriptPanel.classList.toggle("is-collapsed");
                            const isCollapsed = transcriptPanel.classList.contains("is-collapsed");
                            btnToggle.textContent = isCollapsed ? "Show more" : "Show less";
                            if (openSpanEl && openPopEl) positionPopover(openSpanEl, openPopEl);
                        });
                    }

                    if (btnCopy) {
                        btnCopy.addEventListener("click", async () => {
                            try {
                                await navigator.clipboard.writeText(transcriptRaw || "");
                                btnCopy.textContent = "Copied!";
                                setTimeout(() => (btnCopy.textContent = "Copy"), 900);
                            } catch (e) {
                                const ta = document.createElement("textarea");
                                ta.value = transcriptRaw || "";
                                ta.style.position = "fixed";
                                ta.style.opacity = "0";
                                document.body.appendChild(ta);
                                ta.focus();
                                ta.select();
                                document.execCommand("copy");
                                document.body.removeChild(ta);
                                btnCopy.textContent = "Copied!";
                                setTimeout(() => (btnCopy.textContent = "Copy"), 900);
                            }
                        });
                    }

                });
