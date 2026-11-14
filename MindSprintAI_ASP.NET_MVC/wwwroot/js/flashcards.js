// (D2c, 73A) chung một đường lưu cho nút, phím, vuốt; lưu thành công mới chuyển thẻ.
(() => {
    const root = document.getElementById('cards');
    if (!root) return;
    const cards = [...root.querySelectorAll('.study-card')];
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
    let index = Number(root.dataset.start), busy = false;
    function show(focus = false) {
        cards.forEach((card, i) => card.classList.toggle('d-none', i !== index));
        document.getElementById('cards-ended').classList.toggle('d-none', index < cards.length);
        document.getElementById('card-position').textContent = index < cards.length ? `Thẻ ${index + 1}/${cards.length}` : '';
        if (focus) {
            const target = cards[index] ?? document.getElementById('cards-ended');
            target.tabIndex = 0;
            target.focus({ preventScroll: true });
        }
    }
    async function rate(card, rating) {
        if (busy || card.querySelector('.card-back').classList.contains('d-none')) return;
        busy = true;
        card.setAttribute('aria-busy', 'true');
        card.querySelectorAll('button').forEach(b => b.disabled = true);
        const form = new FormData();
        form.set('studySetId', root.dataset.studySet);
        form.set('assignmentId', root.dataset.assignment);
        form.set('cardId', card.dataset.id);
        form.set('rating', rating);
        form.set('mode', root.dataset.mode);
        form.set('__RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]').value);
        try {
            const response = await fetch(root.dataset.rateUrl, { method: 'POST', body: form, headers: { Accept: 'application/json' } });
            const result = await response.json();
            if (!response.ok) throw new Error(result.error || 'Không lưu được đánh giá.');
            document.getElementById('card-error').textContent = '';
            document.getElementById('cards-studied').textContent = `${result.studied}/${result.required}`;
            const status = document.getElementById('cards-status');
            if (status && result.status) status.textContent = result.status;
            if (result.dueCount != null) {
                // (D5b) số mới do service trả về sau khi lưu đánh giá.
                document.getElementById('srs-due-summary').textContent = `Hôm nay có ${result.dueCount} thẻ đến hạn ôn`;
                document.getElementById('due-cards-link').textContent = `Ôn thẻ đến hạn hôm nay (${result.dueCount} thẻ)`;
                document.getElementById('next-due-date').textContent = result.dueCount === 0 && result.nextDueDate
                    ? `Lần đến hạn gần nhất: ${result.nextDueDate} (giờ Việt Nam).` : '';
            }
            index++;
            show(true);
        } catch (error) { document.getElementById('card-error').textContent = error.message; }
        finally {
            busy = false;
            card.removeAttribute('aria-busy');
            card.querySelectorAll('button').forEach(b => b.disabled = false);
        }
    }
    cards.forEach(card => {
        card.querySelector('.flip').addEventListener('click', () => card.querySelector('.card-back').classList.toggle('d-none'));
        card.querySelectorAll('.rate').forEach(button => button.addEventListener('click', () => rate(card, button.dataset.rating)));
        card.addEventListener('keydown', event => {
            if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') return;
            event.preventDefault();
            if (!event.repeat) rate(card, event.key === 'ArrowRight' ? '1' : '2');
        });
        let drag = null;
        const reset = () => {
            drag = null;
            card.style.transform = '';
            card.classList.remove('border-success', 'border-danger');
            feedback.textContent = '';
        };
        const feedback = document.createElement('p');
        feedback.className = 'small fw-bold';
        feedback.setAttribute('aria-hidden', 'true');
        card.append(feedback);
        card.addEventListener('pointerdown', event => {
            if (!event.isPrimary || event.button !== 0 || busy || event.target.closest('button, a')
                || card.querySelector('.card-back').classList.contains('d-none')) return;
            drag = { id: event.pointerId, x: event.clientX, y: event.clientY };
        });
        card.addEventListener('pointermove', event => {
            if (!drag || drag.id !== event.pointerId) return;
            const dx = event.clientX - drag.x, dy = event.clientY - drag.y;
            if (Math.abs(dy) > Math.abs(dx) && Math.abs(dy) > 12) { reset(); return; }
            if (Math.abs(dx) < 12) return;
            card.setPointerCapture(event.pointerId);
            const x = Math.max(-100, Math.min(100, dx));
            // DESIGN.md: thẻ nghiêng nhẹ theo hướng vuốt; bỏ nghiêng khi máy bật giảm chuyển động.
            card.style.transform = `translateX(${x}px)${reduceMotion.matches ? '' : ` rotate(${x / 25}deg)`}`;
            card.classList.toggle('border-success', dx > 0);
            card.classList.toggle('border-danger', dx < 0);
            feedback.textContent = dx > 0 ? 'Nhớ →' : '← Chưa nhớ';
        });
        card.addEventListener('pointerup', event => {
            if (!drag || drag.id !== event.pointerId) return;
            const dx = event.clientX - drag.x, dy = event.clientY - drag.y;
            const accepted = Math.abs(dx) >= Math.min(90, card.offsetWidth * 0.22) && Math.abs(dx) > Math.abs(dy) * 1.5;
            reset();
            if (card.hasPointerCapture(event.pointerId)) card.releasePointerCapture(event.pointerId);
            if (accepted) rate(card, dx > 0 ? '1' : '2');
        });
        card.addEventListener('pointercancel', reset);
        card.addEventListener('lostpointercapture', reset);
    });
    show();
})();
