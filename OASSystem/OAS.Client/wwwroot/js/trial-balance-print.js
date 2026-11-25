export function printTrialBalance(subtitle, groups, rows, totals, aggregate) {
    const frame = document.createElement('iframe');
    frame.style.cssText = 'position:fixed;left:0;bottom:0;width:0;height:0;border:0';
    document.body.append(frame);
    const doc = frame.contentDocument;
    doc.documentElement.dir = 'rtl'; doc.documentElement.lang = 'ar'; doc.title = 'ميزان المراجعة';
    const style = doc.createElement('style');
    style.textContent = '@page{size:A4 landscape;margin:10mm}body{font:11px Cairo,Tahoma,Arial,sans-serif;color:#203654}h1{text-align:center;font-size:18px}p{font-size:10px;line-height:1.5}table{border-collapse:collapse;width:100%}th,td{border:1px solid #cbd5e1;padding:4px;text-align:center}thead{display:table-header-group}th,.total{background:#e8edf5;font-weight:bold}tr{break-inside:avoid}.parent{font-weight:bold}td:nth-child(2){text-align:right}.debit{color:#d12f3c}.credit{color:#13803a}*{print-color-adjust:exact;-webkit-print-color-adjust:exact}';
    doc.head.append(style);
    function node(tag, text, parent) { const el=doc.createElement(tag); el.textContent=text; parent.append(el); return el; }
    node('h1','ميزان المراجعة',doc.body); node('p',subtitle,doc.body);
    const table=node('table','',doc.body), head=node('thead','',table), top=node('tr','',head), lower=node('tr','',head), body=node('tbody','',table);
    node('th','رقم الحساب',top).rowSpan=2; node('th','اسم الحساب',top).rowSpan=2;
    for(const group of groups){node('th',group,top).colSpan=2;node('th','مدين',lower);node('th','دائن',lower);}
    rows.forEach((row,index)=>{ const tr=node('tr','',body);if(aggregate[index])tr.className='parent';row.forEach((v,i)=>{const td=node('td',v,tr);if(i>=2)td.className=i%2===0?'debit':'credit';}); });
    const total=node('tr','',body); total.className='total';node('td','إجمالي النتائج',total).colSpan=2;totals.forEach((v,i)=>node('td',v,total).className=i%2===0?'debit':'credit');
    const win=frame.contentWindow; win.addEventListener('afterprint',()=>frame.remove(),{once:true}); win.focus();win.print();
}
