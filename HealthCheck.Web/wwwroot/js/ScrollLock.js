window.toggleBodyScroll = (lock) => {
    if (lock) {
        document.body.classList.add("dialog-scroll-lock");
    } else {
        document.body.classList.remove("dialog-scroll-lock");
    }
};
