const API_BASE = 'http://localhost:5006';

window.Api = {
    async login(email, password) {
        const res = await fetch(`${API_BASE}/api/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });
        if (!res.ok) throw new Error('Login failed');
        return res.json();
    },

    async getCourses(token) {
        const res = await fetch(`${API_BASE}/api/courses`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        return res.json();
    }
};