# Holdings extension · LOCAL BETA / 持仓扩展待审核

No store publication or automatic installation. See [the full guide](../docs/STOCK_BETA.md).

1. Manually load this folder from the browser extensions developer page.
2. Open your holdings page yourself. Click the extension and read current tables.
3. Choose the correct table; expand the full list yourself and verify loaded-row count.
4. Copy the selected table, then paste into Encourager, or download JSON and import it.
5. Review symbols, shares, optional per-share cost and USD currency before confirmation.

Only `activeTab` and `scripting` are requested. No persistent host permissions, background reader, cookies, brokerage API, orders or network client. Only recognized symbol/share/cost/currency columns are exported; account columns are excluded. Virtualized/offscreen/unloaded rows, canvas UIs, restricted pages and cross-origin frames are not automatically collected. Clipboard copying is user-triggered; if blocked, select the preview and press Ctrl+C.

Generic extraction is tested on synthetic DOM tables. Actual broker-specific layouts and the installed extension flow are pending user review. Do not claim support for every broker. Use `../examples/holdings-table.html` as a synthetic table fixture (file URL access is a browser setting the user must choose; an ordinary local HTTP fixture can also be used).
