const puppeteer = require('puppeteer');
(async () => {
  const browser = await puppeteer.launch({ headless: 'new', args: ['--no-sandbox'] });
  const page = await browser.newPage();
  await page.setViewport({ width: 1366, height: 768 });
  await page.goto('http://2.24.105.55:5000/', { waitUntil: 'networkidle2', timeout: 60000 });
  await new Promise(r => setTimeout(r, 4000));
  const inputs = await page.$$('input');
  await inputs[0].type('admin@gmail.com');
  await inputs[1].type('123456');
  await page.evaluate(() => {
    document.querySelectorAll('button').forEach(b => {
      if (b.textContent.toUpperCase().includes('ENTRAR'))
        ['mousedown','mouseup','click'].forEach(e => b.dispatchEvent(new MouseEvent(e, {bubbles:true})));
    });
  });
  await new Promise(r => setTimeout(r, 8000));
  await page.setViewport({ width: 375, height: 812 });
  await new Promise(r => setTimeout(r, 2000));
  
  // Open drawer by clicking hamburger
  await page.evaluate(() => {
    const btn = document.querySelector('.mud-appbar .mud-icon-button');
    if (btn) btn.dispatchEvent(new MouseEvent('click', { bubbles: true }));
  });
  await new Promise(r => setTimeout(r, 1500));
  
  const info = await page.evaluate(() => {
    const drawer = document.querySelector('.mud-drawer');
    if (!drawer) return {error:'no-drawer'};
    const sheets = Array.from(document.styleSheets);
    let matchingRule = null;
    for (const sheet of sheets) {
      try {
        for (const rule of sheet.cssRules || []) {
          if (rule.selectorText && rule.selectorText.includes('mud-drawer-responsive') && rule.selectorText.includes('open')) {
            matchingRule = rule.cssText;
          }
        }
      } catch(e) {}
    }
    return {
      inlineStyle: drawer.getAttribute('style') || '(none)',
      computedPosition: window.getComputedStyle(drawer).position,
      computedTop: window.getComputedStyle(drawer).top,
      classes: drawer.className,
      matchingRule: matchingRule || 'NOT FOUND IN STYLESHEETS'
    };
  });
  console.log(JSON.stringify(info, null, 2));
  await browser.close();
})().catch(e => console.log('ERR:' + e.message));
