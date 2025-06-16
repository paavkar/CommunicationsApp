window.localStorageHelper = {
    setItem: (key, value) => localStorage.setItem(key, value),
    getItem: (key) => localStorage.getItem(key),
    clear: () => localStorage.clear(),
};
