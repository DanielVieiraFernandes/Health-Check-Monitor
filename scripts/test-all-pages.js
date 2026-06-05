const puppeteer = require('puppeteer');

async function testPage(page, name, url) {
  console.log(`\n=== ${name}: ${url} ===`);
  try {
    await page.goto(url, { waitUntil: 'networkidle2', timeout: 30000 });
    await new Promise(r => setTimeout(r, 3000));
    
    const state = await page.evaluate(() => {
      const errors = [];
      document.querySelectorAll('.mud-alert, .mud-snackbar-content, .error-boundary-card').forEach(e => {
        const t = e.textContent.trim().substring(0, 200);
        if (t && !t.includes('sucesso')) errors.push(t);
      });
      const navLinks = Array.from(document.querySelectorAll('.mud-nav-link'))
        .map(l => ({ text: l.textContent.trim(), active: l.classList.contains('active') }));
      const title = document.querySelector('.page-hero h4, .page-hero h5')?.textContent?.trim() || '';
      const loading = !!document.querySelector('.global-loading-overlay');
      const empty = document.body.innerText.trim().length < 50;
      return { errors, navLinks, title, loading, empty };
    });
    
    const status = state.errors.length > 0 ? '❌ ERROS' : 
                   state.empty ? '⚠️ VAZIA' : 
                   state.loading ? '⏳ CARREGANDO' : '✅ OK';
    
    console.log(`  Status: ${status}`);
    console.log(`  Título: "${state.title}"`);
    if (state.errors.length) state.errors.forEach(e => console.log(`  Erro: ${e}`));
    if (state.navLinks.length) console.log(`  Menu: ${state.navLinks.map(l => l.text + (l.active ? '*' : '')).join(', ')}`);
    if (state.empty) console.log('  AVISO: Página parece vazia (pode precisar de autenticação)');
    if (state.loading) console.log('  AVISO: Ainda carregando...');
    
    return { name, status, ...state };
  } catch(e) {
    console.log(`  ❌ FALHA: ${e.message}`);
    return { name, status: '❌ FALHA', error: e.message };
  }
}

(async () => {
  console.log('🔍 TESTE COMPLETO DO HEALTHCHECK\n');
  
  // Connect to browserless via SSH tunnel
  const browser = await puppeteer.connect({ 
    browserWSEndpoint: 'ws://localhost:3000',
    defaultViewport: { width: 1366, height: 768 }
  });
  
  const page = await browser.newPage();
  page.on('pageerror', err => console.log('  [JS]', err.message.substring(0, 100)));
  
  // 1. LOGIN
  console.log('=== LOGIN ===');
  await page.goto('http://2.24.105.55:5000/', { waitUntil: 'networkidle2', timeout: 30000 });
  await new Promise(r => setTimeout(r, 3000));
  
  const inputs = await page.$$('input');
  if (inputs.length >= 2) {
    await inputs[0].type('admin@gmail.com');
    await inputs[1].type('123456');
    // Click Entrar button
    const btns = await page.$$('button');
    let clicked = false;
    for (const btn of btns) {
      const text = await page.evaluate(el => el.textContent, btn);
      if (text.toUpperCase().includes('ENTRAR')) {
        await btn.click();
        clicked = true;
        break;
      }
    }
    if (!clicked) await page.keyboard.press('Enter');
    await new Promise(r => setTimeout(r, 6000));
  }
  
  const loggedIn = await page.evaluate(() => !!document.querySelector('.mud-nav-link'));
  console.log(`Login: ${loggedIn ? '✅ OK' : '❌ FALHOU'}\n`);
  
  if (!loggedIn) { console.log('Login falhou, abortando.'); await browser.close(); return; }
  
  // 2. DASHBOARD
  const r1 = await testPage(page, 'DASHBOARD', 'http://2.24.105.55:5000/home');
  
  // 3. SISTEMAS
  await page.evaluate(() => {
    const links = document.querySelectorAll('.mud-nav-link');
    links.forEach(l => { if (l.textContent.includes('Sistemas')) l.click(); });
  });
  await new Promise(r => setTimeout(r, 4000));
  const r2 = await testPage(page, 'SISTEMAS', 'http://2.24.105.55:5000/sistemas');
  
  // 4. AUDITORIA
  await page.evaluate(() => {
    document.querySelectorAll('.mud-nav-link').forEach(l => {
      if (l.textContent.includes('Auditoria')) l.click();
    });
  });
  await new Promise(r => setTimeout(r, 4000));
  const r3 = await testPage(page, 'AUDITORIA', 'http://2.24.105.55:5000/auditoria');
  
  // 5. CONFIGURAÇÕES
  await page.evaluate(() => {
    document.querySelectorAll('.mud-nav-link').forEach(l => {
      if (l.textContent.includes('Configurações')) l.click();
    });
  });
  await new Promise(r => setTimeout(r, 4000));
  const r4 = await testPage(page, 'CONFIGURAÇÕES', 'http://2.24.105.55:5000/configuracoes');
  
  // SUMMARY
  console.log('\n========================================');
  console.log('📊 RESUMO');
  console.log('========================================');
  [r1, r2, r3, r4].forEach(r => {
    console.log(`${r.name.padEnd(18)} ${r.status}`);
  });
  
  await browser.close();
  console.log('\n✅ Teste concluído.');
})().catch(e => console.error('FATAL:', e.message));
