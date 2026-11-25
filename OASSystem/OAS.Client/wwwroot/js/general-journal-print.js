export function printReport(subtitle, headers, rows) {
    const frame = document.createElement('iframe');
    frame.style.cssText = 'position:fixed;left:0;bottom:0;width:0;height:0;border:0';
    document.body.append(frame);
    const doc = frame.contentDocument;
    doc.documentElement.dir = 'rtl'; doc.documentElement.lang = 'ar'; doc.title = 'اليومية العامة';
    const style = doc.createElement('style');
    style.textContent = '@page{size:A4 landscape;margin:9mm}body{font:10px Tahoma,Arial,sans-serif;color:#172342}h1{font-size:18px;text-align:center}p{margin:5px 0}table{border-collapse:collapse;width:100%;margin-top:8px}th,td{border:1px solid #cbd5e1;padding:4px;text-align:right;overflow-wrap:anywhere}thead{display:table-header-group;background:#e8edf5}tr{break-inside:avoid}.total{background:#e8edf5;font-weight:bold}.debit{color:#d12f3c}.credit{color:#13803a}th,td{print-color-adjust:exact;-webkit-print-color-adjust:exact}';
    doc.head.append(style);
    const node=(tag,text,parent)=>{const el=doc.createElement(tag);el.textContent=text;parent.append(el);return el;};
    node('h1','اليومية العامة',doc.body);node('p',subtitle,doc.body);
    const table=node('table','',doc.body), head=node('tr','',node('thead','',table)), body=node('tbody','',table);
    headers.forEach(h=>node('th',h,head));
    rows.forEach(row=>{
        const tr=node('tr','',body);
        if(row[0].startsWith('إجمالي القيد ')||row[0]==='الإجمالي العام')tr.className='total';
        row.forEach((v,i)=>{const td=node('td',v,tr); if(headers[i]==='مدين')td.className='debit'; if(headers[i]==='دائن')td.className='credit';});
    });
    const win=frame.contentWindow;
    win.addEventListener('afterprint',()=>frame.remove(),{once:true});win.focus();win.print();
}
