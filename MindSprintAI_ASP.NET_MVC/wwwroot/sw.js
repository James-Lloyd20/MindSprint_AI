// Cài MindSprint AI lên máy như ứng dụng (PWA, như sw.js của nhóm). An toàn hơn bản nhóm: chỉ lưu đệm tệp tĩnh công khai
// (css, js, lib, img, font) và trang báo mất mạng; KHÔNG lưu trang HTML đã đăng nhập hay dữ liệu cá nhân.
const CACHE = 'mindsprint-static-v1';
const OFFLINE = '/offline.html';

self.addEventListener('install', event => {
    event.waitUntil(caches.open(CACHE).then(cache => cache.addAll([OFFLINE, '/img/app/icon-192.jpg', '/img/app/icon-512.jpg'])).then(() => self.skipWaiting()));
});

self.addEventListener('activate', event => {
    event.waitUntil(caches.keys().then(keys => Promise.all(keys.filter(k => k !== CACHE).map(k => caches.delete(k)))).then(() => self.clients.claim()));
});

self.addEventListener('fetch', event => {
    const request = event.request;
    if (request.method !== 'GET') return;
    const url = new URL(request.url);
    if (url.origin !== self.location.origin) return;
    if (request.mode === 'navigate') {
        // Trang: luôn lấy từ mạng; mất mạng thì hiện trang báo, không trả bản cũ của người khác.
        event.respondWith(fetch(request).catch(() => caches.match(OFFLINE)));
        return;
    }
    if (/^\/(css|js|lib|img)\//.test(url.pathname)) {
        event.respondWith(caches.match(request).then(hit => hit || fetch(request).then(response => {
            if (response.ok) { const copy = response.clone(); caches.open(CACHE).then(cache => cache.put(request, copy)); }
            return response;
        })));
    }
});
