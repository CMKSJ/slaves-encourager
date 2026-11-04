/* 弹窗只在本地预览和复制，不访问服务器 / The popup previews and copies locally without server access. */
const labels = {
  'zh': ['持仓导出 · BETA','先在已登录的持仓页点击读取，再选择表格。仅导出代码、股数、可选成本与币种。','读取当前表格','复制到鼓励器','保存 JSON','仅当前已加载行。请展开完整持仓并核对行数；不读取密码、Cookie 或下单。','没有找到支持的持仓表。可改用复制、文件或截图。','已复制，回到鼓励器点击“粘贴持仓页”。','复制失败，请选中文字后按 Ctrl+C。','读取失败；此页面可能受限，请改用其他导入方式。'],
  'zh-Hant': ['持倉匯出 · BETA','在已登入的持倉頁點選讀取，再選擇表格。僅匯出代碼、股數、選填成本與幣別。','讀取目前表格','複製至鼓勵器','儲存 JSON','僅目前已載入列。請展開完整持倉並核對筆數；不讀密碼、Cookie 或下單。','找不到支援的持倉表，可改用複製、檔案或截圖。','已複製，回到鼓勵器貼上。','複製失敗，請選取文字後按 Ctrl+C。','讀取失敗，頁面可能受限制；請改用其他方式。'],
  'en': ['Export holdings · BETA','On your loaded holdings page, read and select a table. Exports symbols, shares, optional per-share cost and currency only.','Read current tables','Copy to Encourager','Save JSON','Loaded rows only. Expand the complete list and verify its count. No passwords, cookies or orders.','No supported holdings table. Try paste, file or screenshot.','Copied. Paste it in Encourager.','Copy failed. Select the text and press Ctrl+C.','Read failed; this page may be restricted. Use another import method.'],
  'ja': ['保有株の出力 · BETA','読み込み済みの保有株ページで表を選択します。コード・株数・任意の取得単価・通貨のみ出力。','現在の表を読む','応援アプリへコピー','JSONを保存','読み込み済みの行のみです。全件を展開し件数を確認してください。パスワード・Cookie・注文は読みません。','対応する表がありません。貼付・ファイル・画像をお試しください。','コピーしました。アプリへ貼り付けてください。','コピーできません。文字を選択してCtrl+Cを押してください。','取得できません。制限されたページの可能性があります。'],
  'es': ['Exportar cartera · BETA','Lee y elige una tabla cargada. Solo exporta símbolos, acciones, coste por acción opcional y moneda.','Leer tablas','Copiar a Encourager','Guardar JSON','Solo filas cargadas. Expande la lista completa y verifica el número. Sin claves, cookies ni órdenes.','No hay tabla compatible. Usa texto, archivo o captura.','Copiado. Pega en Encourager.','Error al copiar. Selecciona y pulsa Ctrl+C.','No se pudo leer; puede ser una página restringida.'],
  'ko': ['보유 주식 내보내기 · BETA','로드된 보유 주식 페이지에서 표를 선택하세요. 코드, 수량, 선택 원가, 통화만 내보냅니다.','현재 표 읽기','앱으로 복사','JSON 저장','로드된 행만 읽습니다. 전체 목록을 펼치고 개수를 확인하세요. 암호, 쿠키, 주문은 읽지 않습니다.','지원하는 표가 없습니다. 텍스트, 파일, 스크린샷을 사용하세요.','복사 완료. 앱에 붙여넣으세요.','복사 실패. 텍스트를 선택하고 Ctrl+C를 누르세요.','읽기 실패. 제한된 페이지일 수 있습니다.']
};
const language = navigator.language || 'en';
const code = /^zh-(TW|HK|MO|Hant)/i.test(language) ? 'zh-Hant' : language.startsWith('zh') ? 'zh' : language.split('-')[0];
const t = labels[code] || labels.en;
const byId = id => document.getElementById(id);
['title','help','read','copy','download','warning'].forEach((id,index) => {byId(id).textContent=t[index];});
let tables=[];
function show() {
  const selected=tables[Number(byId('tables').value)];
  byId('output').value=selected ? JSON.stringify({schema:selected.schema,rows:selected.rows},null,2) : '';
  byId('copy').disabled=byId('download').disabled=!selected;
  if(selected) byId('status').textContent=`${selected.loadedRows} rows${selected.truncated?' · TRUNCATED':''} · VERIFY COMPLETENESS`;
}
byId('read').addEventListener('click',async()=>{
  try {
    const [tab]=await chrome.tabs.query({active:true,currentWindow:true});
    const result=await chrome.scripting.executeScript({target:{tabId:tab.id},func:extractHoldings});
    tables=result[0]?.result || [];
    byId('tables').replaceChildren(...tables.map((table,index)=>{const option=document.createElement('option');option.value=String(index);option.textContent=`${index+1} · ${table.loadedRows} rows`;return option;}));
    show();if(!tables.length)byId('status').textContent=t[6];
  } catch {byId('status').textContent=t[9];}
});
byId('tables').addEventListener('change',show);
byId('copy').addEventListener('click',async()=>{try{await navigator.clipboard.writeText(byId('output').value);byId('status').textContent=t[7];}catch{byId('output').focus();byId('output').select();byId('status').textContent=t[8];}});
byId('download').addEventListener('click',()=>{const url=URL.createObjectURL(new Blob([byId('output').value],{type:'application/json'}));const anchor=document.createElement('a');anchor.href=url;anchor.download='holdings-table.json';anchor.click();setTimeout(()=>URL.revokeObjectURL(url),1000);});
