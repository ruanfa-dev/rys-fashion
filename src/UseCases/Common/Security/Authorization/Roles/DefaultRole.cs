namespace UseCases.Common.Security.Authorization.Roles;

public static class DefaultRole
{
    // System
    public const string Admin = "System.Admin";
    public const string StoreManager = "System.StoreManager";
    public const string Merchandiser = "System.Merchandiser";
    public const string InventoryManager = "System.InventoryManager";
    public const string OrderManager = "System.OrderManager";
    public const string CustomerService = "System.CustomerService";
    public const string MarketingManager = "System.MarketingManager";
    public const string ContentManager = "System.ContentManager";
    public const string WarehouseStaff = "System.WarehouseStaff";
    public const string SalesAssociate = "System.SalesAssociate";

    // Storefront
    public const string Anonymous = "Storefront.Anonymous";
    public const string Customer = "Storefront.Customer";

    public static readonly string[] AllRoles =
    [
        Admin,
        StoreManager,
        Merchandiser,
        InventoryManager,
        OrderManager,
        CustomerService,
        MarketingManager,
        ContentManager,
        WarehouseStaff,
        SalesAssociate,

        Anonymous,
        Customer
    ];

    public static readonly string[] SystemRoles =
    [
        Admin,
        StoreManager,
        Merchandiser,
        InventoryManager,
        OrderManager,
        CustomerService,
        MarketingManager,
        ContentManager,
        WarehouseStaff,
        SalesAssociate
    ];

    public static readonly string[] StorefrontRoles =
    [
        Anonymous,
        Customer
    ];
}
