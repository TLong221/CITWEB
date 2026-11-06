/**
 * Serene — mood / thống kê dùng chung (localStorage)
 */
(function () {
    "use strict";

    var STORAGE_KEY = "serene.v1";

    var MOODS = [
        { id: "great", emoji: "😊", label: "Tuyệt vời", score: 5 },
        { id: "ok", emoji: "🙂", label: "Ổn", score: 4 },
        { id: "neutral", emoji: "😐", label: "Bình thường", score: 3 },
        { id: "tired", emoji: "😔", label: "Mệt mỏi", score: 2 },
        { id: "other", emoji: "🤔", label: "Khác", score: 3 },
    ];

    var WEEKDAY = ["CN", "T2", "T3", "T4", "T5", "T6", "T7"];

    function defaultState() {
        return { moodLog: [], journal: [], sleepLog: {}, userName: "bạn" };
    }

    function load() {
        try {
            var raw = localStorage.getItem(STORAGE_KEY);
            if (!raw) return defaultState();
            var data = JSON.parse(raw);
            return {
                moodLog: Array.isArray(data.moodLog) ? data.moodLog : [],
                journal: Array.isArray(data.journal) ? data.journal : [],
                sleepLog: data.sleepLog && typeof data.sleepLog === "object" ? data.sleepLog : {},
                userName: data.userName || "bạn",
            };
        } catch (e) {
            return defaultState();
        }
    }

    function save(data) {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
    }

    function todayKey(d) {
        var x = d || new Date();
        return x.toISOString().slice(0, 10);
    }

    function uuid() {
        if (typeof crypto !== "undefined" && crypto.randomUUID) return crypto.randomUUID();
        return String(Date.now()) + Math.random().toString(16).slice(2);
    }

    function getMood(id) {
        for (var i = 0; i < MOODS.length; i++) {
            if (MOODS[i].id === id) return MOODS[i];
        }
        return null;
    }

    function logMood(moodId, note) {
        var mood = getMood(moodId);
        if (!mood) return null;
        var data = load();
        var now = Date.now();
        var entry = {
            id: uuid(),
            moodId: mood.id,
            score: mood.score,
            label: mood.label,
            emoji: mood.emoji,
            note: note || "",
            at: now,
            date: todayKey(),
        };
        data.moodLog.unshift(entry);
        if (data.moodLog.length > 300) data.moodLog = data.moodLog.slice(0, 300);

        var journalLine =
            note || "Cảm thấy " + mood.label.toLowerCase() + " — ghi nhanh từ trang tổng quan.";
        data.journal.unshift({
            id: entry.id,
            title: mood.emoji + " " + mood.label,
            preview: journalLine.length > 120 ? journalLine.slice(0, 117) + "…" : journalLine,
            moodId: mood.id,
            at: now,
            date: entry.date,
        });
        if (data.journal.length > 50) data.journal = data.journal.slice(0, 50);
        save(data);
        return entry;
    }

    function getWeekDays() {
        var days = [];
        for (var i = 6; i >= 0; i--) {
            var d = new Date();
            d.setHours(12, 0, 0, 0);
            d.setDate(d.getDate() - i);
            days.push({
                key: todayKey(d),
                label: WEEKDAY[d.getDay()],
                isToday: i === 0,
            });
        }
        return days;
    }

    function entriesForDate(data, dateKey) {
        return data.moodLog.filter(function (e) {
            return e.date === dateKey;
        });
    }

    function weekChartData() {
        var data = load();
        var days = getWeekDays();
        var bars = days.map(function (day) {
            var entries = entriesForDate(data, day.key);
            var avg = null;
            if (entries.length) {
                var sum = 0;
                for (var i = 0; i < entries.length; i++) sum += entries[i].score;
                avg = sum / entries.length;
            }
            return {
                key: day.key,
                label: day.label,
                isToday: day.isToday,
                avg: avg,
                count: entries.length,
            };
        });

        var maxAvg = 0;
        for (var j = 0; j < bars.length; j++) {
            if (bars[j].avg !== null && bars[j].avg > maxAvg) maxAvg = bars[j].avg;
        }

        var hasAny = bars.some(function (b) {
            return b.avg !== null;
        });

        return { bars: bars, maxAvg: maxAvg, hasAny: hasAny };
    }

    function dominantMoodLastDays(days) {
        var data = load();
        var since = new Date();
        since.setDate(since.getDate() - (days - 1));
        since.setHours(0, 0, 0, 0);
        var counts = {};
        for (var i = 0; i < data.moodLog.length; i++) {
            var e = data.moodLog[i];
            if (e.at >= since.getTime()) counts[e.moodId] = (counts[e.moodId] || 0) + 1;
        }
        var best = null;
        var bestN = 0;
        for (var m = 0; m < MOODS.length; m++) {
            var n = counts[MOODS[m].id] || 0;
            if (n > bestN) {
                bestN = n;
                best = MOODS[m];
            }
        }
        var total = 0;
        for (var k in counts) {
            if (Object.prototype.hasOwnProperty.call(counts, k)) total += counts[k];
        }
        return { mood: best, count: bestN, total: total, pct: total ? Math.round((bestN / total) * 100) : 0 };
    }

    function moodStability() {
        var chart = weekChartData();
        var avgs = [];
        for (var i = 0; i < chart.bars.length; i++) {
            if (chart.bars[i].avg !== null) avgs.push(chart.bars[i].avg);
        }
        if (avgs.length < 2) return { label: "Chưa đủ dữ liệu", delta: null };
        var mean = avgs.reduce(function (a, b) {
            return a + b;
        }, 0) / avgs.length;
        var variance =
            avgs.reduce(function (s, v) {
                return s + (v - mean) * (v - mean);
            }, 0) / avgs.length;
        var std = Math.sqrt(variance);
        if (std < 0.35) return { label: "Ổn định", delta: -12 };
        if (std < 0.7) return { label: "Dao động vừa", delta: 5 };
        return { label: "Biến động nhiều", delta: 18 };
    }

    function weekTrendPercent() {
        var data = load();
        var mid = new Date();
        mid.setDate(mid.getDate() - 6);
        mid.setHours(0, 0, 0, 0);
        var lastWeekStart = new Date();
        lastWeekStart.setDate(lastWeekStart.getDate() - 13);
        lastWeekStart.setHours(0, 0, 0, 0);

        function avgScores(from, to) {
            var list = data.moodLog.filter(function (e) {
                return e.at >= from && e.at < to;
            });
            if (!list.length) return null;
            var s = 0;
            for (var i = 0; i < list.length; i++) s += list[i].score;
            return s / list.length;
        }

        var cur = avgScores(mid.getTime(), Date.now() + 1);
        var prev = avgScores(lastWeekStart.getTime(), mid.getTime());
        if (cur === null || prev === null || prev === 0) return null;
        return Math.round(((cur - prev) / prev) * 100);
    }

    function formatRelativeTime(ts) {
        var diff = Date.now() - ts;
        var min = Math.floor(diff / 60000);
        if (min < 1) return "Vừa xong";
        if (min < 60) return min + " phút trước";
        var hr = Math.floor(min / 60);
        if (hr < 24) return hr + " giờ trước";
        var d = new Date(ts);
        return "Hôm nay, " + d.toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" });
    }

    function barHeightPct(avg, maxAvg) {
        if (avg === null) return 8;
        var base = maxAvg > 0 ? (avg / maxAvg) * 100 : 0;
        return Math.max(12, Math.min(100, Math.round(base)));
    }

    function barColorClass(index, isToday) {
        if (isToday) return "primary";
        var classes = ["primary", "secondary", "tertiary"];
        return classes[index % classes.length];
    }

    function escapeHtml(s) {
        return String(s)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;");
    }

    function renderChartBars(container, options) {
        if (!container) return;
        options = options || {};
        var chart = weekChartData();
        container.innerHTML = "";

        if (!chart.hasAny) {
            container.innerHTML =
                '<p class="chart-empty">' +
                escapeHtml(options.emptyMessage || "Chưa có dữ liệu. Hãy ghi mood để xem biểu đồ.") +
                "</p>";
            return;
        }

        for (var i = 0; i < chart.bars.length; i++) {
            var bar = chart.bars[i];
            var wrap = document.createElement("div");
            wrap.className = options.barWrapperClass || "chart-bar-wrapper";
            if (bar.isToday) wrap.classList.add("is-today");

            var barEl = document.createElement("div");
            var h = barHeightPct(bar.avg, chart.maxAvg || 5);
            barEl.className =
                (options.barClass || "chart-bar") + " " + barColorClass(i, bar.isToday) + (bar.avg === null ? " is-empty" : "");
            barEl.style.height = h + "%";
            var tip =
                bar.count === 0
                    ? bar.label + ": chưa ghi nhận"
                    : bar.label + ": " + bar.count + " lần • TB " + bar.avg.toFixed(1) + "/5";
            barEl.title = tip;
            barEl.setAttribute("aria-label", tip);

            var lab = document.createElement("span");
            lab.className = options.labelClass || "chart-label";
            lab.textContent = bar.label;

            wrap.appendChild(barEl);
            wrap.appendChild(lab);
            container.appendChild(wrap);
        }
    }

    function renderTrangchuChart() {
        var root = document.getElementById("home-mood-chart");
        if (!root) return;
        root.innerHTML = "";
        var chart = weekChartData();
        for (var i = 0; i < chart.bars.length; i++) {
            var bar = chart.bars[i];
            var el = document.createElement("div");
            el.className = "bar" + (bar.isToday ? " today" : "") + (bar.avg === null && !bar.isToday ? " is-empty" : "");
            el.style.height = barHeightPct(bar.avg, chart.maxAvg || 5) + "%";
            el.title = bar.count ? bar.label + " — " + bar.count + " lần" : bar.label;
            root.appendChild(el);
        }

        var trendEl = document.getElementById("home-trend-text");
        if (trendEl) {
            var pct = weekTrendPercent();
            if (pct === null) trendEl.textContent = "Ghi mood hàng ngày để xem xu hướng.";
            else if (pct >= 0) trendEl.textContent = "+" + pct + "% so với tuần trước";
            else trendEl.textContent = pct + "% so với tuần trước";
        }

        var descEl = document.getElementById("home-trend-desc");
        if (descEl) {
            var dom = dominantMoodLastDays(7);
            if (dom.mood && dom.total >= 2) {
                descEl.textContent = "Tuần này bạn thường cảm thấy " + dom.mood.label.toLowerCase() + " nhất.";
            } else {
                descEl.textContent = "Chọn cảm xúc hôm nay để bắt đầu theo dõi.";
            }
        }
    }

    function renderJournalList() {
        var list = document.getElementById("journal-entries-list");
        if (!list) return;
        var data = load();
        list.innerHTML = "";
        if (!data.journal.length) {
            list.innerHTML =
                '<p class="journal-empty muted">Chưa có nhật ký. Ghi mood ở trang Tổng quan để tạo mục đầu tiên.</p>';
            return;
        }
        for (var i = 0; i < Math.min(5, data.journal.length); i++) {
            var j = data.journal[i];
            var mood = getMood(j.moodId);
            var card = document.createElement("div");
            card.className = "entry-card";
            card.innerHTML =
                '<div class="entry-icon mint">' +
                (mood ? mood.emoji : "📝") +
                "</div>" +
                '<div class="entry-content">' +
                '<div class="entry-header">' +
                "<h4>" +
                escapeHtml(j.title) +
                "</h4>" +
                "<span>" +
                escapeHtml(formatRelativeTime(j.at)) +
                "</span>" +
                "</div>" +
                "<p>" +
                escapeHtml(j.preview) +
                "</p>" +
                "</div>" +
                '<i data-lucide="chevron-right" class="chevron"></i>';
            list.appendChild(card);
        }
        if (window.lucide) lucide.createIcons();
    }

    function renderQuickMoodPicker() {
        var root = document.getElementById("stats-quick-mood");
        if (!root) return;
        root.innerHTML = "";
        for (var i = 0; i < MOODS.length; i++) {
            (function (m) {
                var btn = document.createElement("button");
                btn.type = "button";
                btn.className = "quick-mood-btn";
                btn.innerHTML = '<span class="emoji">' + m.emoji + "</span><span>" + m.label + "</span>";
                btn.addEventListener("click", function () {
                    logMood(m.id);
                    renderThongkePage();
                });
                root.appendChild(btn);
            })(MOODS[i]);
        }
    }

    function renderThongkePage() {
        renderChartBars(document.getElementById("stats-chart-bars"), {
            emptyMessage: "Chưa có dữ liệu tuần này. Ghi mood ở Trang chủ hoặc chọn cảm xúc bên dưới.",
        });

        var dom = dominantMoodLastDays(7);
        var domVal = document.getElementById("stat-dominant-mood");
        var domDesc = document.getElementById("stat-dominant-desc");
        var domIcon = document.getElementById("stat-dominant-icon");
        if (domVal) domVal.textContent = dom.mood ? dom.mood.label : "—";
        if (domIcon) domIcon.textContent = dom.mood ? dom.mood.emoji : "—";
        if (domDesc) {
            domDesc.textContent = dom.total
                ? "Chiếm " + dom.pct + "% trong " + dom.total + " lần ghi tuần này."
                : "Ghi nhận cảm xúc để thấy xu hướng.";
        }

        var stab = moodStability();
        var stabVal = document.getElementById("stat-stability");
        var stabDesc = document.getElementById("stat-stability-desc");
        if (stabVal) stabVal.textContent = stab.label;
        if (stabDesc) {
            stabDesc.textContent =
                stab.delta === null
                    ? "Cần thêm vài ngày ghi nhận."
                    : stab.delta < 0
                        ? "Ít biến động hơn " + Math.abs(stab.delta) + "% so với tuần trước."
                        : "Biến động nhiều hơn " + stab.delta + "% so với tuần trước.";
        }

        var status = document.getElementById("stats-page-status");
        if (status) {
            var chart = weekChartData();
            var logged = 0;
            for (var i = 0; i < chart.bars.length; i++) {
                if (chart.bars[i].count > 0) logged++;
            }
            status.textContent =
                logged === 0
                    ? "Tuần này chưa có bản ghi — mỗi lần chọn mood sẽ cập nhật biểu đồ ngay."
                    : "Đã ghi nhận " + logged + "/7 ngày — dữ liệu từ nhật ký cảm xúc của bạn.";
        }

        renderJournalList();
        renderQuickMoodPicker();
        if (window.lucide) lucide.createIcons();
    }

    function bindTrangchuMoods() {
        var root = document.getElementById("mood-options-root");
        if (!root) return;
        root.innerHTML = "";
        for (var i = 0; i < MOODS.length; i++) {
            (function (m) {
                var btn = document.createElement("button");
                btn.type = "button";
                btn.className = "mood-btn";
                btn.dataset.moodId = m.id;
                btn.innerHTML = '<span class="emoji">' + m.emoji + '</span><span class="label">' + m.label + "</span>";
                btn.addEventListener("click", function () {
                    root.querySelectorAll(".mood-btn").forEach(function (b) {
                        b.classList.toggle("active", b === btn);
                    });
                    logMood(m.id);
                    renderTrangchuChart();
                    showMoodToast(m);
                });
                root.appendChild(btn);
            })(MOODS[i]);
        }

        var data = load();
        var today = entriesForDate(data, todayKey());
        if (today.length) {
            var last = today[0];
            var active = root.querySelector('[data-mood-id="' + last.moodId + '"]');
            if (active) active.classList.add("active");
        }
    }

    function showMoodToast(mood) {
        var el = document.getElementById("mood-toast");
        if (!el) return;
        el.textContent = "Đã lưu: " + mood.label + " — cảm ơn bạn đã chia sẻ.";
        el.hidden = false;
        clearTimeout(showMoodToast._t);
        showMoodToast._t = setTimeout(function () {
            el.hidden = true;
        }, 2800);
    }

    function initSleepPage() {
        var data = load();
        var week = getWeekDays();
        var baseHours = [7.2, 6.5, 7.8, 7.0, 7.75, 6.2, 7.45];
        for (var i = 0; i < week.length; i++) {
            var day = week[i];
            if (!data.sleepLog[day.key]) {
                var entries = entriesForDate(data, day.key);
                var bonus = 0.5;
                if (entries.length) {
                    var s = 0;
                    for (var j = 0; j < entries.length; j++) s += entries[j].score;
                    bonus = s / entries.length / 5;
                }
                data.sleepLog[day.key] = {
                    hours: Math.round((baseHours[i] + bonus * 0.4) * 10) / 10,
                    quality: Math.min(98, Math.round(70 + bonus * 25 + (i % 3) * 3)),
                };
            }
        }
        save(data);

        var today = data.sleepLog[todayKey()];
        var qEl = document.getElementById("sleep-quality-pct");
        var rEl = document.getElementById("sleep-quality-rating");
        var subEl = document.getElementById("sleep-quality-sub");
        var ring = document.querySelector(".progress-ring .meter");
        if (today && qEl) {
            qEl.textContent = today.quality + "%";
            if (rEl) rEl.textContent = today.quality >= 85 ? "Rất tốt" : today.quality >= 70 ? "Khá tốt" : "Cần nghỉ thêm";
            if (ring) ring.style.strokeDashoffset = String(502 - (502 * today.quality) / 100);
        }
        if (subEl && today) {
            subEl.textContent = "Đêm qua chất lượng ~" + today.quality + "% — dựa trên nhịp nghỉ và mood gần đây.";
        }

        var chart = document.getElementById("sleep-week-chart");
        if (!chart) return;
        chart.innerHTML = "";
        var maxH = 10;
        for (var k = 0; k < week.length; k++) {
            var d = week[k];
            var s = data.sleepLog[d.key];
            var h = s ? s.hours : 0;
            var pct = Math.round((h / maxH) * 100);
            var g = document.createElement("div");
            g.className = "bar-group" + (d.isToday ? " active" : "");
            var bar = document.createElement("div");
            bar.className = "bar";
            bar.style.height = pct + "%";
            bar.title = d.label + ": " + h + " giờ";
            var lab = document.createElement("span");
            lab.textContent = d.label;
            g.appendChild(bar);
            g.appendChild(lab);
            chart.appendChild(g);
        }
    }

    function initBreathingPage() {
        var textEl = document.getElementById("breath-text");
        var dotIn = document.getElementById("dot-in");
        var dotOut = document.getElementById("dot-out");
        if (!textEl || !dotIn || !dotOut) return;

        function updateInstruction() {
            var inhale = textEl.textContent.indexOf("Hít") >= 0;
            textEl.textContent = inhale ? "Thở ra..." : "Hít vào...";
            dotIn.style.opacity = inhale ? "0.25" : "1";
            dotOut.style.opacity = inhale ? "1" : "0.25";
        }

        setInterval(updateInstruction, 6000);
    }

    function initUserMenu() {
        var menu = document.querySelector("[data-user-menu]");
        if (!menu) return;
        var trigger = menu.querySelector("[data-user-menu-trigger]");
        var panel = menu.querySelector("[data-user-menu-panel]");
        if (!trigger || !panel) return;

        function setOpen(open) {
            trigger.setAttribute("aria-expanded", open ? "true" : "false");
            panel.hidden = !open;
            if (open && window.lucide) lucide.createIcons();
        }

        function isOpen() {
            return trigger.getAttribute("aria-expanded") === "true";
        }

        trigger.addEventListener("click", function (e) {
            e.preventDefault();
            setOpen(!isOpen());
        });

        document.addEventListener("click", function (e) {
            if (!isOpen()) return;
            if (menu.contains(e.target)) return;
            setOpen(false);
        });

        document.addEventListener("keydown", function (e) {
            if (!isOpen()) return;
            if (e.key === "Escape") setOpen(false);
        });
    }

    function init() {
        if (document.getElementById("mood-options-root")) {
            bindTrangchuMoods();
            renderTrangchuChart();
        }
        if (document.getElementById("stats-chart-bars")) {
            renderThongkePage();
        }
        if (document.getElementById("sleep-week-chart")) {
            initSleepPage();
        }
        initBreathingPage();
        initUserMenu();
    }

    window.Serene = {
        MOODS: MOODS,
        load: load,
        logMood: logMood,
        renderTrangchuChart: renderTrangchuChart,
        renderThongkePage: renderThongkePage,
        init: init,
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
