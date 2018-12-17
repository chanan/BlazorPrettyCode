window.blazorPrettyCode = {
    getAndHide: function (id) {
        var elem = document.getElementById(id);
        const text = elem.innerHTML;
        elem.style.display = 'none';
        return text;
    }
};