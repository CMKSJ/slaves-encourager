/* 虚构页面测试，无真实浏览器账户 / Synthetic page tests without browser accounts. */
const assert=require('node:assert/strict');
const fs=require('node:fs');
const vm=require('node:vm');
const path=require('node:path');
const {extractHoldings}=require('../browser-extension/extractor.js');
let checks=0;
function check(value,label){assert.ok(value,label);checks++;process.stdout.write(`PASS ${label}\n`);}
function cell(innerText){return {innerText};}
function table(rows,width=900){return {getBoundingClientRect:()=>({width}),querySelectorAll:()=>rows.map(values=>({querySelectorAll:()=>values.map(cell)}))};}
global.getComputedStyle=()=>({visibility:'visible'});
global.document={querySelectorAll:()=>[
 table([['Symbol','Quantity','Average Cost','Currency','Account'],['AAPL','10','150','USD','SENSITIVE ACCOUNT'],['MSFT','5','300','USD','SENSITIVE ACCOUNT']]),
 table([['Date','Amount'],['2026-01-01','5000']]),
 table([['Symbol','Quantity'],['HIDDEN','99']],0)
]};
let results=extractHoldings();
check(results.length===1,'Only visible supported holdings tables are returned');
check(results[0].rows.length===3 && results[0].rows[1][0]==='AAPL','Loaded holdings and explicit headers preserved');
check(!JSON.stringify(results).includes('SENSITIVE ACCOUNT'),'Account column is never exported');
check(results[0].rows[0].join(',')==='Symbol,Quantity,AverageCost,Currency','Only required columns exported with stable schema');
global.document={querySelectorAll:()=>[table([['股票代码','持仓数量','币种'],['AAPL','10','USD']])]};
check(extractHoldings()[0].rows[1][1]==='10','Chinese headers supported');
global.document={querySelectorAll:()=>[table([['Symbol','Qty'],['AAPL','1']])]};
check(extractHoldings()[0].rows[0].length===2,'Missing currency stays absent and requires app confirmation');
global.document={querySelectorAll:()=>[table([['Symbol','Quantity'],...Array.from({length:1100},()=>['AAPL','1'])])]};
check(extractHoldings()[0].truncated===true,'Virtual/large tables explicitly marked for completeness review');
const manifest=JSON.parse(fs.readFileSync(path.join(__dirname,'../browser-extension/manifest.json'),'utf8'));
check(manifest.manifest_version===3 && manifest.permissions.join(',')==='activeTab,scripting','Minimal user-triggered Manifest V3 permissions');
check(!manifest.host_permissions && !manifest.background && !manifest.content_scripts,'No always-on host permissions or background reader');
for(const name of ['extractor.js','popup.js']){
 const code=fs.readFileSync(path.join(__dirname,'../browser-extension',name),'utf8');
 new vm.Script(code,{filename:name});
 check(!/\bfetch\s*\(|XMLHttpRequest|chrome\.cookies|chrome\.webRequest/.test(code),`${name}: no network, cookies or request interception`);
}
process.stdout.write(`ALL ${checks} EXTENSION CHECKS PASSED\n`);
