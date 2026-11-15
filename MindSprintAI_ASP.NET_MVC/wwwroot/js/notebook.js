// Sổ tay AI: đổi sổ tay khi chọn; nói để nhập câu hỏi và nghe câu trả lời (như voice.js của nhóm, dùng Web Speech của trình duyệt).
// Trình duyệt không hỗ trợ thì nút giữ ẩn; mọi thao tác chính vẫn là form thường.
(() => {
    document.querySelectorAll('[data-autosubmit]').forEach(el => el.addEventListener('change', () => el.form.requestSubmit()));

    const Recognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    const mic = document.querySelector('[data-voice-input]');
    const box = document.getElementById('nb-question');
    if (Recognition && mic && box) {
        mic.hidden = false;
        let rec = null;
        mic.addEventListener('click', () => {
            if (rec) { rec.stop(); return; }
            rec = new Recognition();
            rec.lang = 'vi-VN';
            rec.interimResults = true;
            const before = box.value ? box.value.trimEnd() + ' ' : '';
            rec.onresult = e => { box.value = before + [...e.results].map(r => r[0].transcript).join(''); };
            rec.onend = () => { rec = null; mic.setAttribute('aria-pressed', 'false'); mic.querySelector('span').textContent = 'Nhập bằng giọng nói'; box.focus(); };
            rec.onerror = () => rec?.stop();
            mic.setAttribute('aria-pressed', 'true');
            mic.querySelector('span').textContent = 'Đang nghe… bấm để dừng';
            rec.start();
        });
    }

    const speak = document.querySelector('[data-speak]');
    if (speak && 'speechSynthesis' in window) {
        speak.hidden = false;
        speak.addEventListener('click', () => {
            if (speechSynthesis.speaking) { speechSynthesis.cancel(); return; }
            const text = document.querySelector(speak.dataset.speak)?.textContent ?? '';
            const say = new SpeechSynthesisUtterance(text);
            say.lang = 'vi-VN';
            const voice = speechSynthesis.getVoices().find(v => v.lang?.toLowerCase().startsWith('vi'));
            if (voice) say.voice = voice;
            speechSynthesis.speak(say);
        });
    }
})();
