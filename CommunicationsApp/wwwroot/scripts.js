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
        const threshold = 100;
        this.isAtBottom = (el.scrollHeight - el.scrollTop - el.clientHeight) <= threshold;
    },
    initializeScrollWatcher: function (elementId) {
        setTimeout(() => {
            const el = document.getElementById(elementId);
            if (!el) return;
            //console.log("Initializing scroll watcher for:", elementId);
            window.chatScroll.checkScrollLock(elementId);
            el.addEventListener('scroll', () => {
                window.chatScroll.checkScrollLock(elementId);
                //console.log("Scrolling:", el.scrollTop, el.clientHeight, el.scrollHeight, window.chatScroll.isAtBottom);
            });
        }, 50);
    },
    shouldScroll: function (scrollContainerId) {
        window.chatScroll.checkScrollLock(scrollContainerId);
        //console.log("Should scroll:", window.chatScroll.isAtBottom);
        return window.chatScroll.isAtBottom;
    },
    scrollToBottom: function (endElementId) {
        const el = document.getElementById(endElementId);
        if (el) {
            el.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    }
};