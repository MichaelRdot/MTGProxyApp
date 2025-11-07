// Key used in localStorage
const THEME_KEY = "theme"; // "light" | "dark" | "system"

// Compute the effective theme the app should use right now
function effectiveTheme(stored) {
    if (stored === "light" || stored === "dark") return stored;
    // system => use prefers-color-scheme
    return window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches
        ? "dark"
        : "light";
}

// Apply theme by setting data-theme on <html>
function applyTheme(theme) {
    const eff = effectiveTheme(theme);
    document.documentElement.setAttribute("data-theme", eff); // "light" | "dark"
}

// Initialize on page load
(function initTheme() {
    const saved = localStorage.getItem(THEME_KEY) || "system";
    applyTheme(saved);

    // If the OS theme changes while user is on "system", update live
    try {
        const mq = window.matchMedia("(prefers-color-scheme: dark)");
        mq.addEventListener?.("change", () => {
            const current = localStorage.getItem(THEME_KEY) || "system";
            if (current === "system") applyTheme("system");
        });
    } catch {
    }
})();

// Expose minimal API for Blazor
window.themeManager = {
    set: (value /* "light" | "dark" | "system" */) => {
        localStorage.setItem(THEME_KEY, value);
        applyTheme(value);
    },
    get: () => localStorage.getItem(THEME_KEY) || "system",
    effective: () => document.documentElement.getAttribute("data-theme") || "light"
};