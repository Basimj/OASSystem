export function printReport(mode, subtitle, headers, rows, metrics) {
    const frame = document.createElement('iframe');
    frame.style.cssText = 'position:fixed;left:0;bottom:0;width:0;height:0;border:0';
    document.body.append(frame);
    const doc = frame.contentDocument;
    doc.documentElement.dir = 'rtl'; doc.documentElement.lang = 'ar'; doc.title = 'الصناديق والبنوك';
    const style = doc.createElement('style');
    style.textContent = '@page{size:A4 landscape;margin:8mm}body{font:9px Tahoma,Arial,sans-serif;color:#172342}h1{font-size:17px;text-align:center}p{margin:5px 0}table{border-collapse:collapse;width:100%;margin-top:8px}th,td{border:1px solid #cbd5e1;padding:3px;text-align:right;overflow-wrap:anywhere}thead{display:table-header-group;background:#e8edf5}tr{break-inside:avoid}th,td{print-color-adjust:exact;-webkit-print-color-adjust:exact}.metrics{width:60%;background:#e8edf5}.total{background:#e8edf5;font-weight:bold}';
    doc.head.append(style);
    const node=(tag,text,parent)=>{const el=doc.createElement(tag);el.textContent=text;parent.append(el);return el;};
    node('h1','الصناديق والبنوك — '+mode,doc.body);node('p',subtitle,doc.body);
    const table=node('table','',doc.body), head=node('tr','',node('thead','',table)), body=node('tbody','',table);
    headers.forEach(h=>node('th',h,head));
    rows.forEach(row=>{const tr=node('tr','',body);if(row[0]==='الإجمالي')tr.className='total';row.forEach(v=>node('td',v,tr));});
    const totals=node('table','',doc.body);totals.className='metrics';
    metrics.forEach(row=>{const tr=node('tr','',totals);row.forEach(v=>node('td',v,tr));});
    const win=frame.contentWindow;win.addEventListener('afterprint',()=>frame.remove(),{once:true});win.focus();win.print();
}
