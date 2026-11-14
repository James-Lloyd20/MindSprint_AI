// Form có data-confirm hỏi lại trước khi gửi. Câu hỏi đọc từ thuộc tính (đã mã hóa HTML),
// không ghép chuỗi vào JavaScript, nên tên do người dùng đặt không chạy được thành mã.
// Dùng hộp Bootstrap thay confirm(): một số trình duyệt nhúng/webview tự trả "Hủy" mà không hiện hộp thoại.
(() => {
    let modal, pending;
    function dialog() {
        if (modal) return modal;
        const el = document.createElement('div');
        el.className = 'modal fade'; el.tabIndex = -1;
        el.setAttribute('aria-labelledby', 'confirm-dialog-text');
        el.innerHTML = '<div class="modal-dialog modal-dialog-centered"><div class="modal-content"><div class="modal-body"><p id="confirm-dialog-text" class="mb-0"></p></div>'
            + '<div class="modal-footer"><button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Hủy</button><button type="button" class="btn btn-primary confirm-ok">Đồng ý</button></div></div></div>';
        document.body.append(el);
        modal = { el, box: new bootstrap.Modal(el) };
        el.querySelector('.confirm-ok').addEventListener('click', () => {
            const { form, submitter } = pending || {};
            pending = null; modal.box.hide();
            if (!form) return;
            form.dataset.confirmed = '1';
            // requestSubmit giữ giá trị nút đã bấm và chạy lại các trình xử lý submit khác (vd. play.js).
            form.requestSubmit(submitter && submitter.form === form ? submitter : undefined);
        });
        el.addEventListener('hidden.bs.modal', () => { pending = null; });
        return modal;
    }
    // Ô nhập sai nằm trong mục thu gọn (<details>) thì mở mục đó ra để trình duyệt chỉ được chỗ cần sửa.
    document.addEventListener('invalid', e => { const box = e.target.closest?.('details'); if (box) box.open = true; }, true);
    document.addEventListener('submit', e => {
        // Nút có data-confirm riêng (vd. chỉ nút Từ chối trong form Duyệt/Từ chối) thắng câu của form.
        const form = e.target, message = e.submitter?.dataset?.confirm || form.dataset?.confirm;
        if (!message) return;
        if (form.dataset.confirmed) { delete form.dataset.confirmed; return; }
        e.preventDefault();
        if (!window.bootstrap?.Modal) { if (confirm(message)) { form.dataset.confirmed = '1'; form.requestSubmit(e.submitter || undefined); } return; }
        const d = dialog();
        d.el.querySelector('#confirm-dialog-text').textContent = message; // chữ thuần, không chạy HTML
        pending = { form, submitter: e.submitter };
        d.el.addEventListener('shown.bs.modal', () => d.el.querySelector('.confirm-ok').focus(), { once: true });
        d.box.show();
    });
})();

(() => {
    const form = document.getElementById('assignment-form');
    if (!form) return;
    const mode = form.querySelector('#Mode');
    function update(resetShuffle) {
        const test = mode.value === '2';
        for (const [selector, visible] of [['[data-test-field]', test], ['[data-practice-field]', !test]]) {
            form.querySelectorAll(selector).forEach(group => {
                group.classList.toggle('d-none', !visible);
                group.querySelectorAll('input, select').forEach(input => input.disabled = !visible);
            });
        }
        form.querySelector('#DurationMinutes').required = test;
        if (resetShuffle && !form.dataset.practicePreset) {
            form.querySelector('#ShuffleQuestions').checked = test;
            form.querySelector('#ShuffleOptions').checked = test;
        }
    }
    mode.addEventListener('change', () => update(true));
    update(false);
})();

// Ô nhập dài có giới hạn (data-limit): đếm ký tự và chặn lưu khi vượt.
// Không dùng maxlength vì trình duyệt cắt bớt chữ dán vào mà không báo, người dùng lưu bản thiếu mà không biết.
document.querySelectorAll('textarea[data-limit]').forEach(box => {
    const limit = Number(box.dataset.limit), out = document.getElementById(box.dataset.limitOutput);
    const vn = n => n.toLocaleString('vi-VN');
    const update = () => {
        const over = box.value.length - limit;
        box.setCustomValidity(over > 0 ? `Văn bản dài hơn giới hạn ${vn(over)} ký tự. Hãy rút bớt rồi lưu lại.` : '');
        if (!out) return;
        out.textContent = over > 0 ? `Dài hơn giới hạn ${vn(over)} ký tự. Hãy rút bớt rồi lưu lại.` : `${vn(box.value.length)} / ${vn(limit)} ký tự`;
        out.classList.toggle('text-danger', over > 0);
    };
    box.addEventListener('input', update);
    update();
});
