(function () {
    const KEY = 'pl.auth';

    function utf8ToBase64(str) {
        const bytes = new TextEncoder().encode(str);
        let binary = '';
        for (const b of bytes) binary += String.fromCharCode(b);
        return btoa(binary);
    }

    function getCreds() {
        const raw = sessionStorage.getItem(KEY);
        return raw ? JSON.parse(raw) : null;
    }

    function setCreds(email, password) {
        sessionStorage.setItem(KEY, JSON.stringify({ email, password }));
    }

    function clearCreds() {
        sessionStorage.removeItem(KEY);
    }

    function authHeader() {
        const c = getCreds();
        if (!c) return {};
        return { Authorization: 'Basic ' + utf8ToBase64(`${c.email}:${c.password}`) };
    }

    function buildBasicHeader(email, password) {
        return 'Basic ' + utf8ToBase64(`${email}:${password}`);
    }

    async function authFetch(path, options = {}) {
        const headers = { ...(options.headers || {}), ...authHeader() };
        const res = await fetch(path, { ...options, headers });
        if (res.status === 401) {
            clearCreds();
            const onAuthPage = location.pathname.endsWith('/signin.html')
                            || location.pathname.endsWith('/signup.html');
            if (!onAuthPage) location.href = '/signin.html';
        }
        return res;
    }

    function requireAuth() {
        const onAuthPage = location.pathname.endsWith('/signin.html')
                        || location.pathname.endsWith('/signup.html');
        if (!getCreds() && !onAuthPage) {
            location.href = '/signin.html';
        }
    }

    window.PLAuth = {
        getCreds,
        setCreds,
        clearCreds,
        buildBasicHeader,
        authFetch,
        requireAuth
    };
})();
