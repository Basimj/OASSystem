namespace OAS.Client.Purchasing.Workspace;

public sealed class PurchasingWorkspaceState : IPurchasingWorkspaceState
{
    private readonly List<PurchasingTabState> _tabs=[];
    public Guid ActiveTabId {get;set;}
    public IReadOnlyList<PurchasingTabState> Tabs=>_tabs;
    public PurchasingTabState? ActiveTab=>Find(ActiveTabId)??_tabs.FirstOrDefault();

    public PurchasingTabState OpenList(PurchasingEntityType type)
    {
        var tab=_tabs.FirstOrDefault(x=>x.EntityType==type&&x.IsListTab);
        if(tab is null){tab=new(type,null,true,ListTitle(type),false);_tabs.Add(tab);} ActiveTabId=tab.TabId; return tab;
    }
    public PurchasingTabState OpenNew(PurchasingEntityType type,string title)
    { var tab=new PurchasingTabState(type,null,false,title,true);_tabs.Add(tab);ActiveTabId=tab.TabId;return tab; }
    public PurchasingTabState OpenRecord(PurchasingEntityType type,Guid id,string title)
    { var tab=_tabs.FirstOrDefault(x=>x.EntityType==type&&!x.IsListTab&&x.EntityId==id); if(tab is null){tab=new(type,id,false,title,true);_tabs.Add(tab);} else tab.Title=title; ActiveTabId=tab.TabId;return tab; }
    public PurchasingTabState? Find(Guid id)=>_tabs.FirstOrDefault(x=>x.TabId==id);
    public bool Close(Guid id){var tab=Find(id);if(tab is null||!tab.CanClose)return false;var removed=_tabs.Remove(tab);if(removed&&ActiveTabId==id){var list=_tabs.FirstOrDefault(x=>x.EntityType==tab.EntityType&&x.IsListTab);ActiveTabId=list?.TabId??Guid.Empty;}return removed;}
    public IReadOnlyList<PurchasingTabState> VisibleTabs(PurchasingEntityType type)=>_tabs.Where(x=>x.EntityType==type).ToArray();
    private static string ListTitle(PurchasingEntityType t)=>t switch{PurchasingEntityType.SupplierCatalog=>"كتالوج المورد",PurchasingEntityType.PurchaseRequests=>"طلبات الشراء",PurchasingEntityType.PurchaseOrders=>"أوامر الشراء",PurchasingEntityType.PurchaseReceipts=>"الاستلامات",PurchasingEntityType.PurchaseInvoices=>"فواتير المشتريات",_=>"المشتريات"};
}
