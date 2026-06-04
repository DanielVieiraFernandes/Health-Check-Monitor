#!/usr/bin/env node

/**
 * visual-verify.js — Responsive layout verification for HealthCheck
 * 
 * Takes screenshots at multiple viewport sizes and generates a report.
 * Usage: node scripts/visual-verify.js [--url=http://localhost:5000] [--pages=/,/dashboard,...]
 */

const puppeteer = require('puppeteer');
const path = require('path');
const fs = require('fs');

// ── Config ──────────────────────────────────────────────────
const BASE_URL = process.env.VERIFY_URL || 'http://localhost:5000';
const PAGES = (process.env.VERIFY_PAGES || '/').split(',').map(p => p.trim());
const OUTPUT_DIR = process.env.VERIFY_OUTPUT || path.join(__dirname, '..', '.visual-verify');
const VIEWPORTS = {
  'mobile-sm':  { width: 375,  height: 812,  label: 'Mobile Pequeno (375×812)' },
  'mobile-lg':  { width: 430,  height: 932,  label: 'Mobile Grande (430×932)' },
  'tablet':     { width: 768,  height: 1024, label: 'Tablet (768×1024)' },
  'laptop':     { width: 1366, height: 768,  label: 'Laptop (1366×768)' },
  'desktop':    { width: 1920, height: 1080, label: 'Desktop (1920×1080)' },
};
const WAIT_MS = 3000; // wait for Blazor WASM/server to render

// Parse CLI args
const args = process.argv.slice(2);
args.forEach(arg => {
  if (arg.startsWith('--url='))      process.env.VERIFY_URL = arg.split('=')[1];
  if (arg.startsWith('--pages='))    process.env.VERIFY_PAGES = arg.split('=')[1];
  if (arg.startsWith('--output='))   process.env.VERIFY_OUTPUT = arg.split('=')[1];
});

// Re-resolve after possible overrides
const finalUrl = process.env.VERIFY_URL;
const finalPages = process.env.VERIFY_PAGES.split(',').map(p => p.trim());
const finalOutput = process.env.VERIFY_OUTPUT;

async function main() {
  console.log(`\n🔍 Visual Verification — HealthCheck`);
  console.log(`   Base URL : ${finalUrl}`);
  console.log(`   Pages    : ${finalPages.join(', ')}`);
  console.log(`   Output   : ${finalOutput}\n`);

  // Clean output dir
  if (fs.existsSync(finalOutput)) {
    fs.rmSync(finalOutput, { recursive: true, force: true });
  }
  fs.mkdirSync(finalOutput, { recursive: true });

  const browser = await puppeteer.launch({
    headless: 'new',
    args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'],
    defaultViewport: null,
  });

  const results = [];
  let failures = 0;

  try {
    for (const pagePath of finalPages) {
      for (const [vpKey, vp] of Object.entries(VIEWPORTS)) {
        const page = await browser.newPage();
        await page.setViewport({ width: vp.width, height: vp.height });

        const url = `${finalUrl}${pagePath}`;
        const safePage = pagePath === '/' ? 'home' : pagePath.replace(/^\/+/, '').replace(/[^a-zA-Z0-9-]/g, '_');
        const filename = `${safePage}_${vpKey}.png`;
        const filepath = path.join(finalOutput, filename);

        try {
          console.log(`   📸 ${vp.label.padEnd(25)} → ${url}`);
          await page.goto(url, { waitUntil: 'networkidle2', timeout: 30000 });
          await new Promise(r => setTimeout(r, WAIT_MS)); // Blazor render delay

          await page.screenshot({
            path: filepath,
            fullPage: true,
          });

          const pageTitle = await page.title().catch(() => 'N/A');
          results.push({
            page: pagePath,
            viewport: vpKey,
            label: vp.label,
            width: vp.width,
            height: vp.height,
            file: filepath,
            title: pageTitle,
            status: 'ok',
          });
          console.log(`      ✅ ${filename}`);
        } catch (err) {
          failures++;
          results.push({
            page: pagePath,
            viewport: vpKey,
            label: vp.label,
            width: vp.width,
            height: vp.height,
            file: null,
            title: null,
            status: 'error',
            error: err.message,
          });
          console.log(`      ❌ ${filename} — ${err.message}`);
        } finally {
          await page.close();
        }
      }
    }
  } finally {
    await browser.close();
  }

  // Write report
  const report = {
    timestamp: new Date().toISOString(),
    baseUrl: finalUrl,
    pages: finalPages,
    total: results.length,
    ok: results.filter(r => r.status === 'ok').length,
    failures,
    results,
  };

  const reportPath = path.join(finalOutput, 'report.json');
  fs.writeFileSync(reportPath, JSON.stringify(report, null, 2));

  console.log(`\n📊 Report: ${reportPath}`);
  console.log(`   ${report.ok}/${report.total} screenshots OK, ${failures} failures\n`);

  // Return JSON for Hermes
  console.log('__HERMES_RESULT__');
  console.log(JSON.stringify(report));
  process.exit(failures > 0 ? 1 : 0);
}

main().catch(err => {
  console.error('Fatal:', err.message);
  process.exit(2);
});
