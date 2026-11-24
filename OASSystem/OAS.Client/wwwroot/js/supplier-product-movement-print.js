// Standalone landscape print with two-level headers, group totals and grand total.
// All values are inserted as text, not interpreted as markup.
export function printGroupedReport(title, subtitle, filters, headers, identityCount, prefix, groups, totals) {
    const frame = document.createElement('iframe');
    frame.style.cssText = 'position:fixed;left:0;bottom:0;width:0;height:0;border:0';
    document.body.append(frame);
    const doc = frame.contentDocument;
    doc.documentElement.dir = 'rtl'; doc.documentElement.lang = 'ar'; doc.title = title;
    const style = doc.createElement('style');
    style.textContent = '@page{size:A4 landscape;margin:9mm}body{font:10px Cairo,Tahoma,Arial,sans-serif;color:#172342}h1{font-size:17px;text-align:center}p{margin:4px 0}table{border-collapse:collapse;width:100%;margin-top:8px}th,td{border:1px solid #cbd5e1;padding:4px;text-align:center}thead{display:table-header-group;background:#e8edf5}tr{break-inside:avoid}.group{background:#f4f6f8;font-weight:bold;text-align:right}.subtotal{background:#fff8df;font-weight:bold}.grand{background:#e8edf5;font-weight:bold}';
    doc.head.append(style);
    function node(tag,text,parent,span=1) {const el=doc.createElement(tag);el.textContent=text; if(span>1)el.colSpan=span;parent.append(el);return el;}
    node('h1',title,doc.body);node('p',subtitle,doc.body);node('p',filters.join(' | '),doc.body);
    const table=node('table','',doc.body), thead=node('thead','',table), top=node('tr','',thead), header=node('tr','',thead), body=node('tbody','',table);
    for(const [label,span] of [['بيانات الصنف',identityCount],[prefix+' الوارد',3],[prefix+' المرتجع والصرف',2],[prefix+' المبيعات',2],['المتبقي',1]])node('th',label,top,span);
    for(const h of headers)node('th',h,header);
    function summary(label,values,kind) {const tr=node('tr','',body);tr.className=kind;node('td',label,tr,identityCount);for(const v of values)node('td',v,tr);}
    for(const g of groups) {const tr=node('tr','',body);node('td',g.title,tr,headers.length).className='group';for(const row of g.rows){const r=node('tr','',body);for(const v of row)node('td',v,r);}summary('إجمالي المجموعة',g.totals,'subtotal');}
    summary('الإجمالي العام',totals,'grand');
    const win=frame.contentWindow;win.addEventListener('afterprint',()=>frame.remove(),{once:true});win.focus();win.print();
}
