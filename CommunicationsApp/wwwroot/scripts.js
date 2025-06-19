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
