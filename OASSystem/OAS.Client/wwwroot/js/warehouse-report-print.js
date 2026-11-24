// Print all filtered rows; values are inserted as text, not HTML.
export function printWarehouseReport(title, subtitle, headers, groups, totals, identityCount, movement, prefix) {
    const frame=document.createElement('iframe');
    frame.style.cssText='position:fixed;left:0;bottom:0;width:0;height:0;border:0';
    document.body.append(frame);
    const doc=frame.contentDocument;
    doc.documentElement.dir='rtl';doc.documentElement.lang='ar';doc.title=title;
    const style=doc.createElement('style');
    style.textContent='@page{size:A4 landscape;margin:9mm}body{font:10px Cairo,Tahoma,Arial,sans-serif;color:#172342}h1{text-align:center;font-size:17px}p{margin:4px 0}table{border-collapse:collapse;width:100%;margin-top:8px}th,td{border:1px solid #cbd5e1;padding:4px;text-align:center}thead{display:table-header-group;background:#e8edf5}tr{break-inside:avoid}.group{background:#f4f6f8;font-weight:bold}.total{background:#e8edf5;font-weight:bold}';doc.head.append(style);
    const node=(tag,text,parent,span=1)=>{const e=doc.createElement(tag);e.textContent=text;if(span>1)e.colSpan=span;parent.append(e);return e;};
    node('h1',title,doc.body);node('p',subtitle,doc.body);
    const table=node('table','',doc.body),head=node('thead','',table);
    if(movement){const top=node('tr','',head);for(const [label,span] of [['بيانات الصنف',3],[prefix+' الواردة',5],[prefix+' الصادرة',4],['المتبقي',1]])node('th',label,top,span);}
    const h=node('tr','',head);for(const label of headers)node('th',label,h);
    const body=node('tbody','',table);
    function summary(label,values){const tr=node('tr','',body);tr.className='total';node('td',label,tr,identityCount);for(const v of values)node('td',v,tr);}
    for(const g of groups){if(g.title){const tr=node('tr','',body);tr.className='group';node('td',g.title,tr,headers.length);}for(const row of g.rows){const tr=node('tr','',body);for(const v of row)node('td',v,tr);}if(g.title)summary('إجمالي المخزن',g.totals);}
    summary('إجمالي النتائج',totals);
    const win=frame.contentWindow;win.addEventListener('afterprint',()=>frame.remove(),{once:true});win.focus();win.print();
}
