/* 仅用户点击后执行，导出所需列 / Run only after a user click and export required columns only. */
function extractHoldings() {
  const clean = value => String(value || '').replace(/\s+/g, ' ').trim().slice(0, 200);
  const normalized = value => clean(value).toLowerCase().replace(/[\s_\/-]/g, '');
  const aliases = {
    Symbol: ['symbol', 'ticker', 'stockcode', '代码', '股票代码', '证券代码', '代碼', '股票代碼', '銘柄', 'ティッカー', '종목코드', 'símbolo'],
    Quantity: ['quantity', 'qty', 'shares', 'position', '数量', '持仓数量', '股数', '數量', '持倉數量', '保有数量', '수량', 'cantidad'],
    AverageCost: ['averagecost', 'avgcost', 'costpershare', 'costbasisperunit', '成本价', '平均成本', '成本價', '平均取得単価', '평균단가', 'costemedio'],
    Currency: ['currency', '币种', '幣別', '幣種', '通貨', '통화', 'moneda']
  };
  const tables = Array.from(document.querySelectorAll('table,[role="table"],[role="grid"]')).slice(0, 100);
  const results = [];
  for (const table of tables) {
    if (table.getBoundingClientRect().width === 0 || getComputedStyle(table).visibility === 'hidden') continue;
    const rows = Array.from(table.querySelectorAll('tr,[role="row"]')).slice(0, 1001).map(row =>
      Array.from(row.querySelectorAll('th,td,[role="cell"],[role="gridcell"],[role="columnheader"],[role="rowheader"]')).map(cell => clean(cell.innerText)));
    const header = rows.findIndex(row => row.some(value => aliases.Symbol.includes(normalized(value))) && row.some(value => aliases.Quantity.includes(normalized(value))));
    if (header < 0) continue;
    const columns = Object.entries(aliases).map(([name, values]) => ({name, index: rows[header].findIndex(value => values.includes(normalized(value)))})).filter(column => column.index >= 0);
    const filtered = rows.slice(header + 1).filter(row => row.some(Boolean)).map(row => columns.map(column => row[column.index] || ''));
    if (!filtered.length) continue;
    results.push({schema:'slaves-holdings-table-v1', rows:[columns.map(column => column.name), ...filtered], loadedRows:filtered.length, truncated:rows.length >= 1001});
  }
  return results;
}
if (typeof module !== 'undefined') module.exports = {extractHoldings};
