const puppeteer = require('puppeteer');
(async () => {
  const browser = await puppeteer.launch({ headless: 'new', args: ['--no-sandbox'] });
  const page = await browser.newPage();
  await page.setViewport({ width: 1366, height: 768 });
  
  await page.goto('http://2.24.105.55:5000/', { waitUntil: 'networkidle2', timeout: 30000 });
  await new Promise(r => setTimeout(r, 4000));
  
  // Fill login
  const inputs = await page.$$('input');
  if (inputs.length >= 2) {
    await inputs[0].click();
    await inputs[0].type('admin@gmail.com');
    await inputs[1].click();
    await inputs[1].type('123456');
    
    // Click Entrar button
    const buttons = await page.$$('button');
    for (const btn of buttons) {
      const text = await page.evaluate(el => el.textContent, btn);
      if (text.includes('ENTRAR')) {
        await btn.click();
        break;
      }
    }
    await new Promise(r => setTimeout(r, 5000));
  }
  
  await page.screenshot({ path: '.visual-verify/menu_check.png', fullPage: false });
  console.log('OK: .visual-verify/menu_check.png');
  await browser.close();
})().catch(e => { console.log('FAIL: ' + e.message); process.exit(1); });
