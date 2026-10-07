// Pembantu tema: menyimpan & menerapkan tema terang/gelap
window.themeHelper = {
    set: function (theme) {
        document.documentElement.setAttribute('data-theme', theme);
        try { localStorage.setItem('theme', theme); } catch (e) { }
    },
    get: function () {
        try { return localStorage.getItem('theme') || 'light'; } catch (e) { return 'light'; }
    }
};

// Terapkan tema terakhir begitu halaman dibuka
window.themeHelper.set(window.themeHelper.get());