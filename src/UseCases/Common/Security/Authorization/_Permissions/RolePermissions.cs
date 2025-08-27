//using System.Numerics; 
//using Serilog;
//using Application.Common.Security.Authorization.Roles; 

//namespace Application.Common.Security.Authorization.Permissions;

//public static partial class Feature
//{
//    public static class RolePermissions
//    {
//        public static readonly Dictionary<ulong, ulong> Admin = CreateAdminPermissions();
//        public static readonly Dictionary<ulong, ulong> StoreManager = CreateStoreManagerPermissions();
//        public static readonly Dictionary<ulong, ulong> Merchandiser = CreateMerchandiserPermissions();
//        public static readonly Dictionary<ulong, ulong> InventoryManager = CreateInventoryManagerPermissions();
//        public static readonly Dictionary<ulong, ulong> OrderManager = CreateOrderManagerPermissions();
//        public static readonly Dictionary<ulong, ulong> CustomerService = CreateCustomerServicePermissions();
//        public static readonly Dictionary<ulong, ulong> MarketingManager = CreateMarketingManagerPermissions();
//        public static readonly Dictionary<ulong, ulong> ContentManager = CreateContentManagerPermissions();
//        public static readonly Dictionary<ulong, ulong> WarehouseStaff = CreateWarehouseStaffPermissions();
//        public static readonly Dictionary<ulong, ulong> SalesAssociate = CreateSalesAssociatePermissions();
//        public static readonly Dictionary<ulong, ulong> Customer = CreateCustomerPermissions();
//        public static readonly Dictionary<ulong, ulong> Anonymous = CreateAnonymousPermissions();

//        public static Dictionary<ulong, ulong> GetRolePermissions(string roleName)
//        {
//            return roleName switch
//            {
//                DefaultRole.Admin => Admin,
//                DefaultRole.StoreManager => StoreManager,
//                DefaultRole.Merchandiser => Merchandiser,
//                DefaultRole.InventoryManager => InventoryManager,
//                DefaultRole.OrderManager => OrderManager,
//                DefaultRole.CustomerService => CustomerService,
//                DefaultRole.MarketingManager => MarketingManager,
//                DefaultRole.ContentManager => ContentManager,
//                DefaultRole.WarehouseStaff => WarehouseStaff,
//                DefaultRole.SalesAssociate => SalesAssociate,
//                DefaultRole.Customer => Customer,
//                _ => new Dictionary<ulong, ulong>()
//            };
//        }

//        private static Dictionary<ulong, ulong> CreateAdminPermissions()
//        {
//            return new Dictionary<ulong, ulong>
//            {
//                { PermissionResource.Store, Admin.Store.All },
//                { PermissionResource.Product, Admin.Product.All },
//                { PermissionResource.Variant, Admin.Variant.All },
//                { PermissionResource.OptionType, Admin.OptionType.All },
//                { PermissionResource.OptionValue, Admin.OptionValue.All },
//                { PermissionResource.Property, Admin.Property.All },
//                { PermissionResource.Taxonomy, Admin.Taxonomy.All },
//                { PermissionResource.Taxon, Admin.Taxon.All },
//                { PermissionResource.Classification, Admin.Classification.All },
//                { PermissionResource.Order, Admin.Order.All },
//                { PermissionResource.LineItem, Admin.LineItem.All },
//                { PermissionResource.Shipment, Admin.Shipment.All },
//                { PermissionResource.Carton, Admin.Carton.All },
//                { PermissionResource.Payment, Admin.Payment.All },
//                { PermissionResource.PaymentMethod, Admin.PaymentMethod.All },
//                { PermissionResource.ShippingMethod, Admin.ShippingMethod.All },
//                { PermissionResource.ShippingCategory, Admin.ShippingCategory.All },
//                { PermissionResource.Adjustment, Admin.Adjustment.All },
//                { PermissionResource.ReturnAuthorization, Admin.ReturnAuthorization.All },
//                { PermissionResource.Refund, Admin.Refund.All },
//                { PermissionResource.StockItem, Admin.StockItem.All },
//                { PermissionResource.StockLocation, Admin.StockLocation.All },
//                { PermissionResource.StockMovement, Admin.StockMovement.All },
//                { PermissionResource.User, Admin.User.All },
//                { PermissionResource.Address, Admin.Address.All },
//                { PermissionResource.Promotion, Admin.Promotion.All },
//                { PermissionResource.PromotionCategory, Admin.PromotionCategory.All },
//                { PermissionResource.Zone, Admin.Zone.All },
//                { PermissionResource.TaxCategory, Admin.TaxCategory.All },
//                { PermissionResource.TaxRate, Admin.TaxRate.All },
//                { PermissionResource.StoreCredit, Admin.StoreCredit.All },
//                { PermissionResource.Image, Admin.Image.All },
//                { PermissionResource.Review, Admin.Review.All },
//                { PermissionResource.Wishlist, Admin.Wishlist.All },
//                { PermissionResource.Role, Admin.Role.All },
//                { PermissionResource.Permission, Admin.Permission.All },
//                { PermissionResource.Configuration, Admin.Configuration.All },
//                { PermissionResource.AuditLog, Admin.AuditLog.All },
//                { PermissionResource.Webhook, Admin.Webhook.All }
//            };
//        }

//        private static Dictionary<ulong, ulong> CreateStoreManagerPermissions()
//        {
//            return new Dictionary<ulong, ulong>
//            {
//                { PermissionResource.Product, Admin.Product.All },
//                { PermissionResource.Variant, Admin.Variant.All },
//                { PermissionResource.Order, Admin.Order.All },
//                { PermissionResource.Shipment, Admin.Shipment.All },
//                { PermissionResource.Carton, Admin.Carton.All },
//                { PermissionResource.Payment, Admin.Payment.ReadOnly },
//                { PermissionResource.StockItem, Admin.StockItem.All },
//                { PermissionResource.User, Admin.User.All },
//                { PermissionResource.Address, Admin.Address.All },
//                { PermissionResource.ReturnAuthorization, Admin.ReturnAuthorization.All },
//                { PermissionResource.Refund, Admin.Refund.ManageAll },
//                { PermissionResource.Taxonomy, Admin.Taxonomy.ReadOnly },
//                { PermissionResource.Taxon, Admin.Taxon.ReadOnly }
//            };
//        }

//        private static Dictionary<ulong, ulong> CreateMerchandiserPermissions()
//        {
//            return new Dictionary<ulong, ulong>
//            {
//                { PermissionResource.Product, Admin.Product.All },
//                { PermissionResource.Variant, Admin.Variant.All },
//                { PermissionResource.OptionType, Admin.OptionType.All },
//                { PermissionResource.OptionValue, Admin.OptionValue.All },
//                { PermissionResource.Property, Admin.Property.All },
//                { PermissionResource.Taxonomy, Admin.Taxonomy.All },
//                { PermissionResource.Taxon, Admin.Taxon.All },
//                { PermissionResource.Classification, Admin.Classification.All },
//                { PermissionResource.Image, Admin.Image.All },
//                { PermissionResource.StockItem, Admin.StockItem.ReadOnly }
//            };
//        }

//        private static Dictionary<ulong, ulong> CreateInventoryManagerPermissions()
//        {
//            return new Dictionary<ulong, ulong>
//            {
//                { PermissionResource.Product, Admin.Product.ReadOnly },
//                { PermissionResource.Variant, Admin.Variant.ReadOnly },
//                { PermissionResource.StockItem, Admin.StockItem.All },
//                { PermissionResource.StockLocation, Admin.StockLocation.All },
//                { PermissionResource.StockMovement, Admin.StockMovement.All },
//                { PermissionResource.Order, Admin.Order.ReadOnly },
//                { PermissionResource.Shipment, Admin.Shipment.ReadOnly }
//            };
//        }

//        private static Dictionary<ulong, ulong> CreateOrderManagerPermissions()
//        {
//            return new Dictionary<ulong, ulong>
//            {
//                { PermissionResource.Order, Admin.Order.All },
//                { PermissionResource.LineItem, Admin.LineItem.All },
//                { PermissionResource.Shipment, Admin.Shipment.All },
//                { PermissionResource.Carton, Admin.Carton.All },
//                { PermissionResource.Payment, Admin.Payment.ReadOnly },
//                { PermissionResource.User, Admin.User.ReadOnly },
//                { PermissionResource.Address, Admin.Address.ReadOnly },
//                { PermissionResource.ReturnAuthorization, Admin.ReturnAuthorization.All },
//                { PermissionResource.Refund, Admin.Refund.All },
//                { PermissionResource.StockItem, Admin.StockItem.ReadOnly }
//            };
//        }

//        private static Dictionary<ulong, ulong> CreateCustomerServicePermissions()
//        {
//            return new Dictionary<ulong, ulong>
//            {
//                { PermissionResource.User, Admin.User.All },
//                { PermissionResource.Address, Admin.Address.All },
//                { PermissionResource.Order, Admin.Order.ReadOnly },
//                { PermissionResource.ReturnAuthorization, Admin.ReturnAuthorization.All },
//                { PermissionResource.Refund, Admin.Refund.ManageAll },
//                { PermissionResource.Product, Admin.Product.ReadOnly },
//                { PermissionResource.Variant, Admin.Variant.ReadOnly }
//            };
//        }

//        private static Dictionary<ulong, ulong> CreateMarketingManagerPermissions()
//        {
//            return new Dictionary<ulong, ulong>
//            {
//                { PermissionResource.Promotion, Admin.Promotion.All },
//                { PermissionResource.PromotionCategory, Admin.PromotionCategory.All },
//                { PermissionResource.Taxonomy, Admin.Taxonomy.ManageAll },
//                { PermissionResource.Product, Admin.Product.Feature | Admin.Product.ReadOnly },
//                { PermissionResource.User, Admin.User.ReadOnly }
//            };
//        }

//        private static Dictionary<ulong, ulong> CreateContentManagerPermissions()
//        {
//            return new Dictionary<ulong, ulong>
//            {
//                { PermissionResource.Taxonomy, Admin.Taxonomy.All },
//                { PermissionResource.Taxon, Admin.Taxon.All },
//                { PermissionResource.Product, Admin.Product.Curate | Admin.Product.Feature },
//                { PermissionResource.Image, Admin.Image.All },
//                { PermissionResource.Review, Admin.Review.All }
//            };
//        }

//        private static Dictionary<ulong, ulong> CreateWarehouseStaffPermissions()
//        {
//            return new Dictionary<ulong, ulong>
//            {
//                { PermissionResource.StockItem, Admin.StockItem.ReadOnly },
//                { PermissionResource.StockMovement, Admin.StockMovement.ManageAll },
//                { PermissionResource.Order, Admin.Order.ReadOnly },
//                { PermissionResource.Shipment, Admin.Shipment.ManageAll },
//                { PermissionResource.Carton, Admin.Carton.ManageAll },
//                { PermissionResource.Product, Admin.Product.ReadOnly },
//                { PermissionResource.Variant, Admin.Variant.ReadOnly }
//            };
//        }

//        private static Dictionary<ulong, ulong> CreateSalesAssociatePermissions()
//        {
//            return new Dictionary<ulong, ulong>
//            {
//                { PermissionResource.User, Admin.User.ManageAll },
//                { PermissionResource.Address, Admin.Address.ManageAll },
//                { PermissionResource.Order, Admin.Order.Create },
//                { PermissionResource.Product, Admin.Product.ReadOnly },
//                { PermissionResource.Variant, Admin.Variant.ReadOnly },
//                { PermissionResource.StockItem, Admin.StockItem.ReadOnly },
//                { PermissionResource.Taxonomy, Admin.Taxonomy.ReadOnly },
//                { PermissionResource.Taxon, Admin.Taxon.ReadOnly }
//            };
//        }

//        private static Dictionary<ulong, ulong> CreateCustomerPermissions()
//        {
//            return new Dictionary<ulong, ulong>
//            {
//                { PermissionResource.Store_Product, Store.Product.All },
//                { PermissionResource.Store_Variant, Store.Variant.All },
//                { PermissionResource.Store_Taxonomy, Store.Taxonomy.All },
//                { PermissionResource.Store_Taxon, Store.Taxon.All },
//                { PermissionResource.Store_Order, Store.Order.All },
//                { PermissionResource.Store_Address, Store.Address.All },
//                { PermissionResource.Store_Wishlist, Store.Wishlist.All },
//                { PermissionResource.Store_Review, Store.Review.All },
//                { PermissionResource.Store_User, Store.User.All }
//            };
//        }

//        private static Dictionary<ulong, ulong> CreateAnonymousPermissions()
//        {
//            return new Dictionary<ulong, ulong>
//            {
//                { PermissionResource.Store_Product, Store.Product.Read },
//                { PermissionResource.Store_Taxonomy, Store.Taxonomy.Read },
//                { PermissionResource.Store_Taxon, Store.Taxon.Read },
//                { PermissionResource.Store_Review, Store.Review.Read }
//            };
//        }
//    }
//}
