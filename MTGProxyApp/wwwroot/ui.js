window.ui = {
    setBodyClass: function (cls, on) {
        document.body.classList.toggle(cls, !!on);
    },
    addBodyClass: function (cls) {
        document.body.classList.add(cls);
    },
    removeBodyClass: function (cls) {
        document.body.classList.remove(cls);
    }
};