var toggleThemeLink = document.getElementById('sf-theme');

function toggleTheme(theme)
{
    // Altera o tema do syncfusion
    toggleThemeLink.href = `_content/Syncfusion.Blazor/styles/${theme}.css`;
}
