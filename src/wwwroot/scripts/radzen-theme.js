export function setTheme(theme, appRootUrl) {
    var body = document.getElementsByTagName('body')[0];
    body.style.display = 'none';
    body.classList.add(theme);
    document.getElementById('radzen-theme').href = `${appRootUrl}_content/Radzen.Blazor/css/${theme}.css`;
    setTimeout(function () {
        body.style.display = '';
    }, 500);
}
