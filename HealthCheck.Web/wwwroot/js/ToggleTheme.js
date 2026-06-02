var toggleThemeLink = document.getElementById('sf-theme');

function toggleTheme(theme)
{
    // Altera o tema do syncfusion
    toggleThemeLink.href = `_content/Syncfusion.Blazor/styles/${theme}.css`;
}

function getWindowWidth()
{
    return window.innerWidth;
}

var resizeDotNetRef = null;

function registerResizeHandler(dotNetRef)
{
    resizeDotNetRef = dotNetRef;
    window.addEventListener('resize', onResize);
}

function onResize()
{
    if (resizeDotNetRef) {
        resizeDotNetRef.invokeMethodAsync('OnWindowResize');
    }
}

