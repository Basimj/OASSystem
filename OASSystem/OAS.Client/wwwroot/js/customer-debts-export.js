export function saveWorkbook(base64, filename) {
    const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
    const url = URL.createObjectURL(new Blob([bytes], {type:'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'}));
    const link = document.createElement('a');
    link.href = url; link.download = filename; document.body.append(link); link.click(); link.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
}

// Print all filtered, sorted rows without printing the application shell.
// Use textContent, not HTML concatenation, for names and other user data.
export function printReport(title, subtitle, filters, headers, rows) {
    const frame = document.createElement('iframe');
    frame.style.cssText = 'position:fixed;left:0;bottom:0;width:0;height:0;border:0';
    document.body.append(frame);
    const doc = frame.contentDocument;
    doc.documentElement.dir = 'rtl'; doc.documentElement.lang = 'ar'; doc.title = title;
    const style = doc.createElement('style');
    style.textContent = '@page{size:A4 landscape;margin:10mm}body{font:11px Tahoma,Arial,sans-serif;color:#172342}h1{font-size:20px}p{margin:5px 0}table{border-collapse:collapse;width:100%;margin-top:12px}th,td{border:1px solid #c8d4e6;padding:5px;text-align:right}thead{display:table-header-group;background:#e1e9fa}tr{break-inside:avoid}tbody tr:nth-child(even){background:#f1f5fd}';
    doc.head.append(style);
    function node(tag,text,parent){const el=doc.createElement(tag);el.textContent=text;parent.append(el);return el;}
    node('h1',title,doc.body);node('p',subtitle,doc.body);node('p',filters.join(' | '),doc.body);
    const table=node('table','',doc.body), thead=node('thead','',table), head=node('tr','',thead), body=node('tbody','',table);
    for(const h of headers)node('th',h,head);
    for(const row of rows){const tr=node('tr','',body);for(const value of row)node('td',value,tr);}
    const win=frame.contentWindow;
    win.addEventListener('afterprint',()=>frame.remove(),{once:true});
    win.focus();win.print();
}
