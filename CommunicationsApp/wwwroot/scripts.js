window.localStorageHelper = {
    setItem: (key, value) => localStorage.setItem(key, value),
    getItem: (key) => localStorage.getItem(key),
    clear: () => localStorage.clear(),
};

window.timeZoneHelper = {
    getTimeZone: () => {
        const offsetHours = new Date().getTimezoneOffset()/-60;
        return offsetHours;
    }
};

window.titleHelper = {
    setTitle: (title) => {
        document.title = title;
    }
};

window.chatScroll = {
    isAtBottom: true,
    checkScrollLock: function (elementId) {
        const el = document.getElementById(elementId);
        if (!el) return;

        this.isAtBottom = el.scrollHeight - el.scrollTop <= el.clientHeight + 20;
    },
    scrollToBottom: function (elementId) {
        const el = document.getElementById(elementId);
        if (el && this.isAtBottom) {
            el.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    }
};