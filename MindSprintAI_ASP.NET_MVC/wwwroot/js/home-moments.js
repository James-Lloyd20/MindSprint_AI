/* Adapted from taste-skill 2.5.1 (f6b7657), pinned-chapters and marquee-row.
   The authored HTML is the still version. No kit files are loaded by the site. */
(() => {
    const page = document.querySelector('.home-page');
    if (!page) return;
    const reduced = window.matchMedia('(prefers-reduced-motion: reduce)');
    const desktop = window.matchMedia('(min-width: 992px) and (min-height: 700px)');
    const supported = typeof CSS !== 'undefined' && CSS.supports('animation-timeline', 'view()');
    const chapters = page.querySelector('[data-moment="pinned-chapters"]');
    const stage = chapters.querySelector('.home-chapter-stage');
    const frame = chapters.querySelector('.home-chapter-frame');
    const steps = [...chapters.querySelectorAll('.home-steps > li')];
    const figures = steps.map(step => step.querySelector('.home-step-visual'));
    const marquee = page.querySelector('[data-moment="marquee-row"]');
    const rail = marquee.querySelector('.home-marquee-rail');
    const track = rail.querySelector('.home-marquee-track');
    const pause = marquee.querySelector('[data-marquee-pause]');
    let pinLive = false, clone = null, pending = 0, current = -1, onScreen = false;

    function setRunning() {
        page.classList.toggle('is-hidden', document.hidden);
        marquee.classList.toggle('is-running', onScreen && !document.hidden);
    }
    function fitMarquee() {
        if (!clone) return;
        const width = track.scrollWidth;
        marquee.style.setProperty('--marquee-width', `${width}px`);
        marquee.style.setProperty('--marquee-duration', `${width / 32}s`);
    }
    function draw() {
        pending = 0;
        if (document.hidden || !pinLive) return;
        const rect = chapters.getBoundingClientRect();
        const top = parseFloat(getComputedStyle(stage).top) || 0;
        const progress = Math.max(0, Math.min(1, (top - rect.top) / Math.max(1, rect.height - stage.offsetHeight)));
        // One scroll read/write per animation frame; a single opaque figure keeps all copy readable.
        chapters.style.setProperty('--chapter-progress', String(progress * (steps.length - 1)));
        const next = Math.round(progress * (steps.length - 1));
        if (next !== current) {
            current = next;
            steps.forEach((step, index) => {
                step.classList.toggle('is-current', index === current);
                step.classList.toggle('is-revealed', index <= current);
            });
            figures.forEach((figure, index) => figure.classList.toggle('is-current', index === current));
        }
    }
    function schedule() {
        if (!pending && !document.hidden && pinLive) pending = requestAnimationFrame(draw);
    }
    function sync() {
        const enabled = supported && !reduced.matches;
        const shouldPin = enabled && desktop.matches;
        if (shouldPin !== pinLive) {
            pinLive = shouldPin;
            chapters.classList.toggle('is-live', pinLive);
            figures.forEach((figure, index) => {
                steps[index].style.setProperty('--chapter-index', String(index));
                figure.style.setProperty('--chapter-index', String(index));
                (pinLive ? frame : steps[index]).append(figure);
            });
            if (pinLive) {
                window.addEventListener('scroll', schedule, { passive: true });
            } else {
                window.removeEventListener('scroll', schedule);
                if (pending) cancelAnimationFrame(pending);
                pending = 0;
                current = -1;
                steps.forEach(step => step.classList.remove('is-current', 'is-revealed'));
                figures.forEach(figure => figure.classList.remove('is-current'));
            }
        }
        // A paused marquee also becomes a complete wrapped row, so no subject stays clipped.
        const canMarquee = enabled && typeof IntersectionObserver !== 'undefined';
        const shouldRun = canMarquee && pause.getAttribute('aria-pressed') !== 'true';
        if (shouldRun && !clone) {
            clone = track.cloneNode(true);
            clone.classList.add('home-marquee-clone');
            clone.setAttribute('aria-hidden', 'true');
            rail.append(clone);
            marquee.classList.add('is-live');
        } else if (!shouldRun && clone) {
            clone.remove();
            clone = null;
            marquee.classList.remove('is-live');
        }
        pause.hidden = !canMarquee;
        fitMarquee();
        setRunning();
        schedule();
    }
    pause.addEventListener('click', () => {
        pause.setAttribute('aria-pressed', String(pause.getAttribute('aria-pressed') !== 'true'));
        sync();
    });
    if (typeof IntersectionObserver !== 'undefined') {
        new IntersectionObserver(entries => {
            onScreen = entries[entries.length - 1].isIntersecting;
            setRunning();
        }).observe(marquee);
    } // Without an observer the complete wrapped row remains as authored.
    if (typeof ResizeObserver !== 'undefined') new ResizeObserver(fitMarquee).observe(track);
    reduced.addEventListener('change', sync);
    desktop.addEventListener('change', sync);
    window.addEventListener('resize', sync, { passive: true });
    document.addEventListener('visibilitychange', () => {
        if (document.hidden && pending) { cancelAnimationFrame(pending); pending = 0; }
        setRunning();
        schedule();
    });
    sync();
    if (supported && !reduced.matches) page.querySelector('.home-hero').classList.add('hero-live');
    if (document.fonts) document.fonts.ready.then(fitMarquee);
})();
