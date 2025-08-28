namespace Infrastructure.Persistence.Constants;

public static class Schema
{
    // ===========================================
    // PHASE 1: CORE FOUNDATION (Weeks 1-2) - Essential infrastructure
    // ===========================================
    
    // Authentication & Authorization - Full Spree complexity maintained
    public const string Users = "users";
    public const string Roles = "roles";
    public const string UserRoles = "user_roles";
    public const string RoleClaims = "role_claims";
    public const string UserClaims = "user_claims";
    public const string UserLogins = "user_logins";
    public const string UserTokens = "user_tokens";
    public const string RefreshTokens = "refresh_tokens";
    
    // Customer Management - Extended for fashion retail
    public const string Customers = "customers";
    public const string CustomerAddresses = "customer_addresses"; // Multiple shipping/billing addresses
    public const string CustomerWishlists = "customer_wishlists"; // Multiple wishlists
    public const string WishlistItems = "wishlist_items";
    public const string NewsletterSubscriptions = "newsletter_subscriptions";
    
    // Multi-Location Infrastructure - Essential for 2-3 locations
    public const string StoreLocations = "store_locations";
    public const string LocationOperatingHours = "location_operating_hours";
    public const string LocationContacts = "location_contacts";
    
    // Taxonomy System - Full Spree complexity for fashion categorization
    public const string Taxonomies = "taxonomies"; // Root systems: Categories, Occasions, Seasons
    public const string Taxons = "taxons"; // Hierarchical: Women > Clothing > Dresses > Casual
    public const string ProductTaxons = "product_taxons"; // Many-to-many relationships
    
    // ===========================================
    // PHASE 2: PRODUCT CORE (Weeks 2-3) - E-commerce foundation
    // ===========================================
    
    // Product Architecture - Spree-inspired complexity
    public const string Products = "products";
    public const string ProductMasterData = "product_master_data"; // Master catalog info
    public const string ProductImages = "product_images";
    public const string ProductVideos = "product_videos"; // Fashion videos/360 views
    
    // Product Attributes & Properties - Fashion-specific
    public const string Properties = "properties"; // Color, Material, Care Instructions
    public const string ProductProperties = "product_properties";
    public const string PropertyValues = "property_values"; // Predefined values
    
    // Product Options System - Size/Color/Style variations  
    public const string OptionTypes = "option_types"; // Size, Color, Material, Fit
    public const string OptionValues = "option_values"; // XS, Red, Cotton, Slim
    public const string ProductOptionTypes = "product_option_types";
    
    // Variants - Individual SKUs with pricing
    public const string Variants = "variants";
    public const string VariantImages = "variant_images"; // Color-specific images
    public const string VariantOptionValues = "variant_option_values";
    public const string Prices = "prices"; // Multi-currency, location-based pricing
    
    // ===========================================
    // PHASE 3: INVENTORY & ORDERS (Weeks 3-4) - Business operations
    // ===========================================
    
    // Advanced Inventory - Multi-location complexity
    public const string StockLocations = "stock_locations"; // Warehouses + stores
    public const string InventoryUnits = "inventory_units"; // Individual stock items
    public const string StockItems = "stock_items"; // Variant stock per location
    public const string StockMovements = "stock_movements"; // Audit trail
    public const string StockTransfers = "stock_transfers"; // Inter-location transfers
    
    // Order Management - Full e-commerce workflow
    public const string Orders = "orders";
    public const string LineItems = "line_items";
    public const string OrderPromotions = "order_promotions";
    public const string Adjustments = "adjustments"; // Taxes, discounts, fees
    
    // Fulfillment & Shipping
    public const string Shipments = "shipments";
    public const string ShippingMethods = "shipping_methods";
    public const string ShippingRates = "shipping_rates";
    public const string ShippingCategories = "shipping_categories"; // Fashion-specific
    
    // Payment Processing
    public const string Payments = "payments";
    public const string PaymentMethods = "payment_methods";
    public const string PaymentSources = "payment_sources"; // Saved cards
    public const string Refunds = "refunds";
    
    // ===========================================
    // PHASE 4: ML & RECOMMENDATIONS (Weeks 5-7) - AI features
    // ===========================================
    
    // ML Feature Storage - Visual & behavioral data
    public const string ProductFeatures = "product_features"; // CNN embeddings
    public const string ProductColorPalettes = "product_color_palettes"; // Extracted colors
    public const string VisualSimilarityScores = "visual_similarity_scores";
    public const string StyleEmbeddings = "style_embeddings"; // Style vectors
    
    // Recommendation Engine
    public const string UserInteractions = "user_interactions"; // Views, clicks, purchases
    public const string UserPreferences = "user_preferences"; // Learned preferences
    public const string SimilarProducts = "similar_products"; // Pre-computed similarities
    public const string RecommendationSets = "recommendation_sets"; // Cached recommendations
    public const string RecommendationLogs = "recommendation_logs"; // Performance tracking
    
    // Fashion-specific ML features
    public const string SeasonalTrends = "seasonal_trends"; // Trend analysis
    public const string OutfitRecommendations = "outfit_recommendations"; // Complete looks
    public const string StyleMatching = "style_matching"; // Cross-category matching
    
    // Behavioral Analytics
    public const string UserSessions = "user_sessions"; // Session tracking
    public const string ClickstreamEvents = "clickstream_events"; // Detailed interactions
    public const string SearchQueries = "search_queries"; // Search behavior
    public const string AbandonedCarts = "abandoned_carts"; // Cart analysis
    
    // ===========================================
    // PHASE 5: PROMOTIONS & MARKETING (Week 8) - Business growth
    // ===========================================
    
    // Promotion Engine - Spree-inspired complexity
    public const string Promotions = "promotions";
    public const string PromotionRules = "promotion_rules";
    public const string PromotionActions = "promotion_actions";
    public const string PromotionCategories = "promotion_categories"; // Fashion seasons/events
    
    // Marketing & CRM
    public const string EmailCampaigns = "email_campaigns";
    public const string CustomerSegments = "customer_segments";
    public const string LoyaltyPrograms = "loyalty_programs";
    public const string LoyaltyPoints = "loyalty_points";
    
    // ===========================================
    // PHASE 6: REVIEWS & SOCIAL (Week 9) - Community features
    // ===========================================
    
    // Product Reviews & Ratings
    public const string ProductReviews = "product_reviews";
    public const string ReviewImages = "review_images"; // User-generated content
    public const string ReviewHelpfulness = "review_helpfulness"; // Vote system
    public const string ReviewModerationQueue = "review_moderation_queue";
    
    // Social Features
    public const string UserFollows = "user_follows"; // Fashion influencers
    public const string ProductShares = "product_shares"; // Social sharing
    public const string OutfitPosts = "outfit_posts"; // User styling posts
    
    // ===========================================
    // PHASE 7: ANALYTICS & REPORTING (Week 10) - Business intelligence
    // ===========================================
    
    // Business Analytics
    public const string SalesReports = "sales_reports";
    public const string InventoryReports = "inventory_reports";
    public const string CustomerAnalytics = "customer_analytics";
    public const string ProductPerformance = "product_performance";
    
    // ML Performance Tracking
    public const string ModelPerformanceMetrics = "model_performance_metrics";
    public const string RecommendationAccuracy = "recommendation_accuracy";
    public const string SearchAnalytics = "search_analytics";
    public const string ConversionTracking = "conversion_tracking";
    
    // Operational Reports
    public const string InventoryAlerts = "inventory_alerts";
    public const string FulfillmentMetrics = "fulfillment_metrics";
    public const string CustomerServiceTickets = "customer_service_tickets";
    public const string ReturnReasons = "return_reasons"; // Fashion-specific returns

    // ===========================================
    // OPTIONAL EXTENSIONS - Future enhancements
    // ===========================================

    // Advanced Fashion Features (implement if time permits)
    // public const string FashionTrends = "fashion_trends";
    // public const string PersonalStyleProfiles = "personal_style_profiles";
    // public const string VirtualFittingData = "virtual_fitting_data";
    // public const string SustainabilityMetrics = "sustainability_metrics";

    // Multi-tenant Support (for scaling)
    // public const string Tenants = "tenants";
    // public const string TenantUsers = "tenant_users";
    // public const string TenantConfigurations = "tenant_configurations";

    // Advanced Integrations
    // public const string ExternalInventorySources = "external_inventory_sources";
    // public const string SupplierIntegrations = "supplier_integrations";
    // public const string DropshippingOrders = "dropshipping_orders";


    // Testing & QA (for development purposes)
    public const string TodoItems = "todo_items";
    public const string TodoLists = "todo_lists";
}