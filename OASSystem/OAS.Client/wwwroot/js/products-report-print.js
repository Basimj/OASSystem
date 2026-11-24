export function printReport(title, subtitle, filters, headers, rows, totals, identityCount) {
    const frame = document.createElement('iframe');
    frame.style.cssText = 'position:fixed;left:0;bottom:0;width:0;height:0;border:0';
    document.body.append(frame);
    const doc = frame.contentDocument;
    doc.documentElement.dir = 'rtl'; doc.documentElement.lang = 'ar'; doc.title = title;
    const style = doc.createElement('style');
    style.textContent = '@page{size:A4 landscape;margin:9mm}body{font:10px Cairo,Tahoma,Arial,sans-serif;color:#172342}h1{font-size:17px;text-align:center}p{margin:4px 0}table{border-collapse:collapse;width:100%;margin-top:8px}th,td{border:1px solid #cbd5e1;padding:4px;text-align:center}thead{display:table-header-group;background:#e8edf5}tr{break-inside:avoid}.total{background:#e8edf5;font-weight:bold}';
    doc.head.append(style);
    const node=(tag,text,parent,span=1)=>{const el=doc.createElement(tag);el.textContent=text; if(span>1)el.colSpan=span; parent.append(el); return el;};
    node('h1',title,doc.body); node('p',subtitle,doc.body); node('p',filters.join(' | '),doc.body);
    const table=node('table','',doc.body), thead=node('thead','',table), tr=node('tr','',thead), tbody=node('tbody','',table);
    headers.forEach(h=>node('th',h,tr));
    rows.forEach(row=>{const r=node('tr','',tbody); row.forEach(v=>node('td',v,r));});
    const total=node('tr','',tbody); total.className='total'; node('td','إجمالي النتائج', total, identityCount); totals.forEach(v=>node('td',v,total));
    const win=frame.contentWindow; win.addEventListener('afterprint',()=>frame.remove(),{once:true}); win.focus(); win.print();
}
