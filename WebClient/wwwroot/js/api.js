const API = 'http://localhost:5006';

function token() { return localStorage.getItem('token'); }

window.Api = {
    async login(email, password) {
        const r = await fetch(`${API}/api/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });
        if (!r.ok) throw new Error('Неверный email или пароль');
        return r.json();
    },

    async courses() {
        const r = await fetch(`${API}/api/courses`, {
            headers: { 'Authorization': `Bearer ${token()}` }
        });
        return r.json();
    }
};