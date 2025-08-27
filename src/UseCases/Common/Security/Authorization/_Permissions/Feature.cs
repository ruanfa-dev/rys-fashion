//using Serilog;

//namespace Application.Common.Security.Authorization.Permissions;

//// Core enums
//public static partial class Feature
//{
//    [Flags]
//    public enum PermissionResource : ulong
//    {
//        // Core Spree Resources
//        Store = 1UL << 0,
//        Product = 1UL << 1,
//        Variant = 1UL << 2,
//        OptionType = 1UL << 3,
//        OptionValue = 1UL << 4,
//        Property = 1UL << 5,
//        Taxonomy = 1UL << 6,
//        Taxon = 1UL << 7,
//        Classification = 1UL << 8,
//        Order = 1UL << 9,
//        LineItem = 1UL << 10,
//        Shipment = 1UL << 11,
//        Carton = 1UL << 12,
//        Payment = 1UL << 13,
//        PaymentMethod = 1UL << 14,
//        ShippingMethod = 1UL << 15,
//        ShippingCategory = 1UL << 16,
//        Adjustment = 1UL << 17,
//        ReturnAuthorization = 1UL << 18,
//        Refund = 1UL << 19,
//        StockItem = 1UL << 20,
//        StockLocation = 1UL << 21,
//        StockMovement = 1UL << 22,
//        User = 1UL << 23,
//        Address = 1UL << 24,
//        Promotion = 1UL << 25,
//        PromotionCategory = 1UL << 26,
//        Zone = 1UL << 27,
//        TaxCategory = 1UL << 28,
//        TaxRate = 1UL << 29,
//        StoreCredit = 1UL << 30,
//        Image = 1UL << 31,
//        Review = 1UL << 32,
//        Wishlist = 1UL << 33,

//        // System Administration
//        Role = 1UL << 34,
//        Permission = 1UL << 35,
//        Configuration = 1UL << 36,
//        AuditLog = 1UL << 37,
//        Webhook = 1UL << 38,

//        // StoreResource values 
//        Store_Product = 1UL << 39,
//        Store_Variant = 1UL << 40,
//        Store_Taxonomy = 1UL << 41,
//        Store_Taxon = 1UL << 42,
//        Store_Order = 1UL << 43,
//        Store_Address = 1UL << 44,
//        Store_Wishlist = 1UL << 45,
//        Store_Review = 1UL << 46,
//        Store_User = 1UL << 47
//    }

//    [Flags]
//    public enum PermissionAction : ulong
//    {
//        // Admin
//        Create = 1UL << 0,
//        Read = 1UL << 1,
//        Update = 1UL << 2,
//        Delete = 1UL << 3,
//        List = 1UL << 4,
//        Approve = 1UL << 5,
//        Cancel = 1UL << 6,
//        Complete = 1UL << 7,
//        Ship = 1UL << 8,
//        Return = 1UL << 9,
//        Refund = 1UL << 10,
//        Adjust = 1UL << 11,
//        Transfer = 1UL << 12,
//        Publish = 1UL << 13,
//        Feature = 1UL << 14,
//        Manage = 1UL << 15,
//        Configure = 1UL << 16,
//        Export = 1UL << 17,
//        Import = 1UL << 18,
//        Curate = 1UL << 19,
//        Add = 1UL << 20,
//        Remove = 1UL << 21,
//        Place = 1UL << 22
//    }
//}

//public static partial class Feature
//{
//    public record struct Permission(ulong Resource, ulong Action)
//    {
//        public const string SEPARATOR = ".";
//        public override readonly string ToString() => $"{Resource:X16}{SEPARATOR}{Action:X16}";

//        public readonly string ToClaimValue() => ToString();

//        public static Permission FromClaimValue(string claimValue)
//        {
//            var parts = claimValue.Split(SEPARATOR);
//            if (parts.Length != 2)
//                throw new ArgumentException("Invalid claim value format", nameof(claimValue));

//            return new Permission(
//                Convert.ToUInt64(parts[0], 16),
//                Convert.ToUInt64(parts[1], 16)
//            );
//        }

//        public readonly string GetResourceName() =>
//            Enum.GetName(typeof(PermissionResource), Resource) ?? "Unknown";

//        public readonly string GetActionName() =>
//            Enum.GetName(typeof(PermissionAction), Action) ?? "Unknown";

//        public readonly string GetFriendlyName() =>
//            $"{GetResourceName()}.{GetActionName()}";
//    }

//    public static class PermissionOperations
//    {
//        private static readonly ILogger _logger = Serilog.Log.ForContext(typeof(PermissionOperations));

//        public static bool HasPermission(Dictionary<ulong, ulong> userPermissions, ulong resource, ulong action)
//        {
//            bool has = userPermissions.TryGetValue(resource, out var actions) && (actions & action) == action;
//            _logger.Information("Checking permission: Resource={Resource}, Action={Action}, HasPermission={Has}", resource, action, has);
//            return has;
//        }

//        public static void GrantPermission(Dictionary<ulong, ulong> userPermissions, ulong resource, ulong action)
//        {
//            userPermissions.TryGetValue(resource, out var actions);
//            userPermissions[resource] = actions | action;
//            _logger.Information("Granting permission: Resource={Resource}, Action={Action}", resource, action);
//        }

//        public static void RevokePermission(Dictionary<ulong, ulong> userPermissions, ulong resource, ulong action)
//        {
//            if (userPermissions.TryGetValue(resource, out var actions))
//            {
//                actions &= ~action;
//                if (actions == 0)
//                {
//                    userPermissions.Remove(resource);
//                }
//                else
//                {
//                    userPermissions[resource] = actions;
//                }
//                _logger.Information("Revoking permission: Resource={Resource}, Action={Action}", resource, action);
//            }
//        }

//        public static bool HasAnyPermission(Dictionary<ulong, ulong> userPermissions, params (ulong Resource, ulong Action)[] permissions)
//        {
//            foreach (var (resource, action) in permissions)
//            {
//                if (HasPermission(userPermissions, resource, action))
//                {
//                    _logger.Information("User has at least one permission: Resource={Resource}, Action={Action}", resource, action);
//                    return true;
//                }
//            }
//            _logger.Information("User has none of the specified permissions");
//            return false;
//        }

//        public static bool HasAllPermissions(Dictionary<ulong, ulong> userPermissions, params (ulong Resource, ulong Action)[] permissions)
//        {
//            foreach (var (resource, action) in permissions)
//            {
//                if (!HasPermission(userPermissions, resource, action))
//                {
//                    _logger.Information("User missing permission: Resource={Resource}, Action={Action}", resource, action);
//                    return false;
//                }
//            }
//            _logger.Information("User has all specified permissions");
//            return true;
//        }

//        public static void GrantMultiplePermissions(Dictionary<ulong, ulong> userPermissions, params (ulong Resource, ulong Action)[] permissions)
//        {
//            foreach (var (resource, action) in permissions)
//            {
//                GrantPermission(userPermissions, resource, action);
//            }
//            _logger.Information("Granted multiple permissions");
//        }

//        public static void RevokeMultiplePermissions(Dictionary<ulong, ulong> userPermissions, params (ulong Resource, ulong Action)[] permissions)
//        {
//            foreach (var (resource, action) in permissions)
//            {
//                RevokePermission(userPermissions, resource, action);
//            }
//            _logger.Information("Revoked multiple permissions");
//        }

//        public static void MergePermissions(Dictionary<ulong, ulong> userPermissions, Dictionary<ulong, ulong> permissionsToMerge)
//        {
//            foreach (var (resource, actions) in permissionsToMerge)
//            {
//                userPermissions.TryGetValue(resource, out var existingActions);
//                userPermissions[resource] = existingActions | actions;
//            }
//            _logger.Information("Merged permissions");
//        }

//        public static bool IsValidPermission(ulong resource, ulong action)
//        {
//            return Enum.IsDefined(typeof(PermissionResource), resource) && Enum.IsDefined(typeof(PermissionAction), action) && resource != 0 && action != 0;
//        }

//        public static string[] GetMissingPermissions(Dictionary<ulong, ulong> userPermissions, Dictionary<ulong, ulong> requiredPermissions)
//        {
//            var missing = new List<string>();
//            foreach (var (reqResource, reqActions) in requiredPermissions)
//            {
//                userPermissions.TryGetValue(reqResource, out var userActions);
//                var missingActions = reqActions & ~userActions;
//                if (missingActions != 0)
//                {
//                    for (int bit = 0; bit < 64; bit++)
//                    {
//                        var actionBit = 1UL << bit;
//                        if ((missingActions & actionBit) != 0)
//                        {
//                            missing.Add($"{GetResourceName(reqResource)}.{GetActionName(actionBit)}");
//                        }
//                    }
//                }
//            }
//            return missing.ToArray();
//        }

//        private static string GetResourceName(ulong resource) => Enum.GetName(typeof(PermissionResource), resource) ?? "Unknown";
//        private static string GetActionName(ulong action) => Enum.GetName(typeof(PermissionAction), action) ?? "Unknown";
//    }
//}

//// Admin permissions
//public static partial class Feature
//{
//    public static partial class Admin
//    {
//        public static class Store
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;
//            public const ulong Configure  = (ulong)PermissionAction.Configure;

//            public const ulong ManageAll = Create | Update | Delete | Configure;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class Product
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;
//            public const ulong Publish  = (ulong)PermissionAction.Publish;
//            public const ulong Feature  = (ulong)PermissionAction.Feature;
//            public const ulong Curate  = (ulong)PermissionAction.Curate;

//            public const ulong ManageAll = Create | Update | Delete | Publish | Feature | Curate;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class Variant
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update | Delete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class OptionType
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update | Delete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class OptionValue
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update | Delete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class Property
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update | Delete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class Taxonomy
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;
//            public const ulong Curate  = (ulong)PermissionAction.Curate;

//            public const ulong ManageAll = Create | Update | Delete | Curate;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class Taxon
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update | Delete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class Classification
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update | Delete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class Order
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;
//            public const ulong Approve  = (ulong)PermissionAction.Approve;
//            public const ulong Cancel  = (ulong)PermissionAction.Cancel;
//            public const ulong Complete  = (ulong)PermissionAction.Complete;

//            public const ulong ManageAll = Create | Update | Delete | Approve | Cancel | Complete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class LineItem
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update | Delete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class Shipment
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong List  = (ulong)PermissionAction.List;
//            public const ulong Ship  = (ulong)PermissionAction.Ship;

//            public const ulong ManageAll = Create | Update | Ship;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class Carton
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class Payment
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong List  = (ulong)PermissionAction.List;
//            public const ulong Approve  = (ulong)PermissionAction.Approve;
//            public const ulong Refund  = (ulong)PermissionAction.Refund;

//            public const ulong ManageAll = Create | Update | Approve | Refund;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class PaymentMethod
//        {
//            public const ulong Create  =(ulong)PermissionResource.PaymentMethod | (ulong)PermissionAction.Create;// 
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update | Delete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class ShippingMethod
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update | Delete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class ShippingCategory
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update | Delete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class Adjustment
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class ReturnAuthorization
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong List  = (ulong)PermissionAction.List;
//            public const ulong Approve  = (ulong)PermissionAction.Approve;

//            public const ulong ManageAll = Create | Update | Approve;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class Refund
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class StockItem
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong List  = (ulong)PermissionAction.List;
//            public const ulong Adjust  = (ulong)PermissionAction.Adjust;

//            public const ulong ManageAll = Create | Update | Adjust;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class StockLocation
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update | Delete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class StockMovement
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong List  = (ulong)PermissionAction.List;
//            public const ulong Transfer  = (ulong)PermissionAction.Transfer;

//            public const ulong ManageAll = Create | Transfer;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class User
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update | Delete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class Address
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update | Delete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class Promotion
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update | Delete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class PromotionCategory
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update | Delete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class Zone
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update | Delete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class TaxCategory
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update | Delete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class TaxRate
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update | Delete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class StoreCredit
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class Image
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update | Delete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class Review
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;
//            public const ulong Approve  = (ulong)PermissionAction.Approve;

//            public const ulong ManageAll = Create | Update | Delete | Approve;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class Wishlist
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update | Delete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class Role
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update | Delete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class Permission
//        {
//            public const ulong Create  = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class Configuration
//        {
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Configure  = (ulong)PermissionAction.Configure;

//            public const ulong ManageAll = Update | Configure;
//            public const ulong ReadOnly = Read;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class AuditLog
//        {
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong List  = (ulong)PermissionAction.List;
//            public const ulong Export  = (ulong)PermissionAction.Export;

//            public const ulong ManageAll = Export;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }

//        public static class Webhook
//        {
//            public const ulong Create = (ulong)PermissionAction.Create;
//            public const ulong Read  = (ulong)PermissionAction.Read;
//            public const ulong Update  = (ulong)PermissionAction.Update;
//            public const ulong Delete  = (ulong)PermissionAction.Delete;
//            public const ulong List  = (ulong)PermissionAction.List;

//            public const ulong ManageAll = Create | Update | Delete;
//            public const ulong ReadOnly = Read | List;
//            public const ulong All = ManageAll | ReadOnly;
//        }
//    }
//}

