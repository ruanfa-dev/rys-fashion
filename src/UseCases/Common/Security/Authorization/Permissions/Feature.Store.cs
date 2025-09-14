namespace UseCases.Common.Security.Authorization.Permissions;

public static partial class Feature
{
    public static class Store
    {
        public static readonly string[] All =
        [
            .. Product.All,
            .. Order.All,
            .. Cart.All,
            .. Wishlist.All,
            .. Review.All,
            .. Profile.All
        ];

        public static class Product
        {
            public const string View = "Feature.Store.Product.View";
            public const string Search = "Feature.Store.Product.Search";
            public const string Browse = "Feature.Store.Product.Browse";

            public static readonly string[] All = [View, Search, Browse];
        }

        public static class Order
        {
            public const string Create = "Feature.Store.Order.Create";
            public const string View = "Feature.Store.Order.View";
            public const string Update = "Feature.Store.Order.Update";
            public const string Cancel = "Feature.Store.Order.Cancel";
            public const string Track = "Feature.Store.Order.Track";

            public static readonly string[] All = [Create, View, Update, Cancel, Track];
        }

        public static class Cart
        {
            public const string Add = "Feature.Store.Cart.Add";
            public const string Remove = "Feature.Store.Cart.Remove";
            public const string Update = "Feature.Store.Cart.Update";
            public const string View = "Feature.Store.Cart.View";
            public const string Clear = "Feature.Store.Cart.Clear";

            public static readonly string[] All = [Add, Remove, Update, View, Clear];
        }

        public static class Wishlist
        {
            public const string Add = "Feature.Store.Wishlist.Add";
            public const string Remove = "Feature.Store.Wishlist.Remove";
            public const string View = "Feature.Store.Wishlist.View";
            public const string Share = "Feature.Store.Wishlist.Share";

            public static readonly string[] All = [Add, Remove, View, Share];
        }

        public static class Review
        {
            public const string Create = "Feature.Store.Review.Create";
            public const string Update = "Feature.Store.Review.Update";
            public const string Delete = "Feature.Store.Review.Delete";
            public const string View = "Feature.Store.Review.View";

            public static readonly string[] All = [Create, Update, Delete, View];
        }

        public static class Profile
        {
            public const string View = "Feature.Store.Profile.View";
            public const string Update = "Feature.Store.Profile.Update";
            public const string Delete = "Feature.Store.Profile.Delete";

            public static readonly string[] All = [View, Update, Delete];
        }
    }
}
