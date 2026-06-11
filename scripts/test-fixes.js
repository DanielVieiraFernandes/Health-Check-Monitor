// Testa os fixes: MudSelect status HTTP + MudSwitch customizado
const puppeteer = require('puppeteer');
const BASE = 'http://localhost:5000';

(async () => {
  const browser = await puppeteer.launch({ headless: 'new', args: ['--no-sandbox'] });
  const page = await browser.newPage();
  await page.setViewport({ width: 1366, height: 768 });

  let step = 0;
  const wait = ms => new Promise(r => setTimeout(r, ms));

  console.log('=== Navegando para login ===');
  const resp = await page.goto(BASE + '/', { waitUntil: 'networkidle2', timeout: 30000 });
  console.log('  Status:', resp.status(), 'URL:', resp.url());
  await wait(5000);

  // Verificar se chegou na página de login
  const pageText = await page.evaluate(() => document.body.innerText.substring(0, 300));
  console.log('  Conteúdo:', pageText.replace(/\n/g, ' | '));

  // Login - preencher campos
  const emailInput = await page.$('input[type="text"], input:not([type])');
  const allInputs = await page.$$('input');
  console.log('  Inputs:', allInputs.length);
  
  if (allInputs.length >= 2) {
    await allInputs[0].click({ clickCount: 3 });
    await allInputs[0].type('admin@gmail.com');
    await allInputs[1].click({ clickCount: 3 });
    await allInputs[1].type('123456');
    console.log('  Credenciais preenchidas');
  }

  // Clicar ENTRAR
  await page.evaluate(() => {
    const btns = document.querySelectorAll('button, a');
    for (const b of btns) {
      if (b.textContent.toUpperCase().trim() === 'ENTRAR') {
        b.click();
        return 'clicado';
      }
    }
    return 'não encontrado';
  }).then(r => console.log('  Botão ENTRAR:', r));

  await wait(5000);
  console.log('  URL após login:', page.url());

  // Ir para Sistemas
  console.log('\n=== Indo para Sistemas ===');
  await page.goto(BASE + '/sistemas', { waitUntil: 'networkidle2', timeout: 30000 });
  await wait(5000);
  console.log('  URL:', page.url());
  const bodyText = await page.evaluate(() => document.body.innerText.substring(0, 400));
  console.log('  Conteúdo:', bodyText.replace(/\n/g, ' | '));

  // Verificar se Blazor está rodando
  const blazorInfo = await page.evaluate(() => {
    const hasBlazor = typeof Blazor !== 'undefined';
    const scripts = document.scripts.length;
    const bodyChildren = document.body.children.length;
    const bodyHTML = document.body.innerHTML.length;
    const allHTML = document.documentElement.outerHTML.length;
    return {hasBlazor, scripts, bodyChildren, bodyHTML, allHTML};
  });
  console.log('  Blazor info:', JSON.stringify(blazorInfo));
  
  // Verificar se estamos na página correta (não redirecionou pro login)
  if (bodyText.includes('Entrar') || bodyText.includes('Email')) {
    console.log('  ❌ Redirecionado para login!');
    await page.screenshot({ path: '.hermes/test-fixes-debug.png' });
    await browser.close();
    process.exit(1);
  }

  // Clicar NOVO SISTEMA com page.click() (evento real via CDP, não sintético)
  console.log('\n=== Clicando NOVO SISTEMA ===');
  const btn = await page.$('button.mud-button-filled-primary');
  if (!btn) {
    // fallback: procurar por texto
    const btns = await page.$$('button');
    let found = null;
    for (const b of btns) {
      const text = await b.evaluate(el => el.textContent.trim());
      if (text.toUpperCase().includes('NOVO SISTEMA')) { found = b; break; }
    }
    if (found) await found.click();
    else console.log('  Botão não encontrado');
  } else {
    console.log('  Botão encontrado, clicando...');
    await btn.click();
  }
  await wait(3000);

  // Verificar dialog
  const dialogEl = await page.$('.mud-dialog');
  if (!dialogEl) {
    console.log('  ❌ Dialog não abriu!');
    await page.screenshot({ path: '.hermes/test-fixes-debug2.png' });
    await browser.close();
    process.exit(1);
  }
  console.log('  ✅ Dialog aberto');

  // Tirar screenshot
  await page.screenshot({ path: '.hermes/test-fixes-dialog.png' });
  console.log('  Screenshot: .hermes/test-fixes-dialog.png');

  // === TESTE 1: MudSwitch ===
  console.log('\n=== TESTE 1: MudSwitch "Status code customizado" ===');
  const switchEl = await page.$('.mud-switch-button');
  if (!switchEl) {
    console.log('  ❌ Switch não encontrado');
  } else {
    const checkedBefore = await switchEl.evaluate(el => el.getAttribute('aria-checked'));
    console.log('  Antes do click: aria-checked=' + checkedBefore);

    await switchEl.click();
    await wait(1000);

    const checkedAfter = await switchEl.evaluate(el => el.getAttribute('aria-checked'));
    console.log('  Depois do click: aria-checked=' + checkedAfter);

    // Verificar campo de texto customizado
    const customInput = await page.$('input[aria-label*="Digite"]');
    console.log('  Campo customizado:', customInput ? '✅ VISÍVEL' : '❌ NÃO apareceu');

    if (checkedAfter === 'true' && customInput) {
      console.log('  ✅ Bug 2 CORRIGIDO!');
    } else {
      console.log('  ❌ Bug 2 PERSISTE');
    }

    // Desligar switch de novo
    await switchEl.click();
    await wait(500);
  }

  // === TESTE 2: MudSelect status HTTP ===
  console.log('\n=== TESTE 2: MudSelect "Status HTTP esperado" ===');
  
  const selectValue = await page.evaluate(() => {
    const labels = document.querySelectorAll('.mud-input-label');
    for (const l of labels) {
      if (l.textContent.includes('Status HTTP')) {
        const input = l.parentElement.querySelector('input');
        return input ? input.value : 'sem input';
      }
    }
    return 'não encontrado';
  });
  console.log('  Valor antes:', selectValue);

  // Clicar no select
  const clicked = await page.evaluate(() => {
    const labels = document.querySelectorAll('.mud-input-label');
    for (const l of labels) {
      if (l.textContent.includes('Status HTTP')) {
        const control = l.closest('.mud-input-control');
        if (control) {
          control.click();
          return 'clicado';
        }
      }
    }
    return 'não encontrado';
  });
  console.log('  Click:', clicked);
  await wait(1000);

  // Verificar dropdown aberto
  const popoverItems = await page.evaluate(() => {
    const items = document.querySelectorAll('.mud-list-item');
    return Array.from(items).map(i => i.textContent.trim());
  });
  console.log('  Itens dropdown:', popoverItems.length, popoverItems.slice(0, 3));

  if (popoverItems.length > 0) {
    // Selecionar "404 Not Found"
    const found404 = await page.evaluate(() => {
      const items = document.querySelectorAll('.mud-list-item');
      for (const item of items) {
        if (item.textContent.includes('404')) {
          item.click();
          return true;
        }
      }
      return false;
    });
    console.log('  404 clicado:', found404);
    await wait(500);

    // Verificar valor
    const newValue = await page.evaluate(() => {
      const labels = document.querySelectorAll('.mud-input-label');
      for (const l of labels) {
        if (l.textContent.includes('Status HTTP')) {
          const input = l.parentElement.querySelector('input');
          return input ? input.value : 'sem input';
        }
      }
      return 'não encontrado';
    });
    console.log('  Valor depois:', newValue);

    if (newValue && newValue !== 'Escolha um valor') {
      console.log('  ✅ Bug 1 CORRIGIDO!');
    } else {
      console.log('  ❌ Bug 1 PERSISTE');
    }
  } else {
    console.log('  ❌ Dropdown não abriu');
  }

  // Screenshot final
  await page.screenshot({ path: '.hermes/test-fixes-final.png' });
  console.log('\n=== Screenshot final: .hermes/test-fixes-final.png ===');

  // Erros de console
  const consoleErrors = [];
  page.on('console', msg => {
    if (msg.type() === 'error') consoleErrors.push(msg.text());
  });

  await browser.close();
  console.log('✅ Teste concluído.\n');
})();
