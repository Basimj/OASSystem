export function printIncomeStatement(subtitle, headers, rows, summaries) {
    const frame = document.createElement('iframe');
    frame.style.cssText = 'position:fixed;left:0;bottom:0;width:0;height:0;border:0'; document.body.append(frame);
    const doc=frame.contentDocument; doc.documentElement.dir='rtl';doc.documentElement.lang='ar';doc.title='قائمة الدخل';
    const style=doc.createElement('style');style.textContent='@page{size:A4 landscape;margin:9mm}body{font:11px Cairo,Tahoma,Arial,sans-serif;color:#172342}h1{text-align:center;font-size:18px}table{border-collapse:collapse;width:100%}th,td{border:1px solid #cbd5e1;padding:5px;text-align:center}thead{display:table-header-group}th,.summary td{background:#e8edf5;font-weight:bold}tr{break-inside:avoid}';doc.head.append(style);
    const node=(tag,text,parent)=>{const el=doc.createElement(tag);el.textContent=text;parent.append(el);return el;};
    node('h1','قائمة الدخل',doc.body);node('p',subtitle,doc.body);const table=node('table','',doc.body),head=node('tr','',node('thead','',table)),body=node('tbody','',table);
    headers.forEach(h=>node('th',h,head));rows.forEach((row,i)=>{const tr=node('tr','',body);if(summaries[i])tr.className='summary';row.forEach(v=>node('td',v,tr));});
    const win=frame.contentWindow;win.addEventListener('afterprint',()=>frame.remove(),{once:true});win.focus();win.print();
}
