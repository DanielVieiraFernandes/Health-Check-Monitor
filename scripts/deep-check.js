const puppeteer = require('puppeteer');
(async () => {
  const browser = await puppeteer.launch({ headless: 'new', args: ['--no-sandbox'] });
  const page = await browser.newPage();
  page.on('pageerror', err => console.log('PAGE_ERROR:', err.message));
  
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
  await new Promise(r => setTimeout(r, 10000));
  
  const title = await page.title();
  console.log('Title:', title);
  
  // Check what page we're on and for errors
  const state = await page.evaluate(() => {
    const boundary = document.querySelector('.error-boundary-card');
    const snackbars = document.querySelectorAll('.mud-snackbar-content');
    const alerts = document.querySelectorAll('.mud-alert-message');
    const spinner = document.querySelector('.global-loading-overlay, .loading-spinner');
    return {
      onErrorBoundary: !!boundary,
      boundaryText: boundary ? boundary.innerText.substring(0, 300) : null,
      snackbars: Array.from(snackbars).map(s => s.innerText.substring(0, 200)),
      alerts: Array.from(alerts).map(a => a.innerText.substring(0, 200)),
      isLoading: !!spinner,
      url: window.location.href
    };
  });
  console.log(JSON.stringify(state, null, 2));
  
  await browser.close();
})().catch(e => console.log('FATAL:', e.message));
