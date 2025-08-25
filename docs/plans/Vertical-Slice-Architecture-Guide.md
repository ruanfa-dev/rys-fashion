# Vertical Slice Architecture Guide for Rys.Fashion

## Overview

This guide outlines the vertical slice architecture implementation for the Rys.Fashion e-commerce system. Each vertical slice represents a complete feature that cuts through all layers of the application, containing everything needed to implement a specific business capability.

## Architecture Principles

### Core Concepts
- **Vertical Slices**: Features are organized by business capability, not technical concerns
- **Feature Cohesion**: Related functionality is grouped together
- **Minimal Coupling**: Slices have minimal dependencies on each other
- **Domain-Driven**: Organization follows business domains rather than technical layers

### Benefits
- **Easier Navigation**: Developers can find all related code in one place
- **Parallel Development**: Teams can work on different features independently
- **Reduced Cognitive Load**: Smaller, focused modules are easier to understand
- **Better Testing**: Each slice can be tested independently

## Folder Structure

### Application Layer Structure

```
src/
├── Application/
│   ├── Application.csproj
│   ├── Commons/
│   │   ├── Behaviors/
│   │   │   ├── LoggingBehavior.cs
│   │   │   ├── ValidationBehavior.cs
│   │   │   ├── PerformanceBehavior.cs
│   │   │   └── CachingBehavior.cs
│   │   ├── Notifications/
│   │   │   ├── INotificationService.cs
│   │   │   ├── EmailNotificationService.cs
│   │   │   └── PushNotificationService.cs
│   │   ├── Security/
│   │   │   ├── ICurrentUserService.cs
│   │   │   ├── IAuthorizationService.cs
│   │   │   └── SecurityExtensions.cs
│   │   ├── Exceptions/
│   │   │   ├── ValidationException.cs
│   │   │   ├── NotFoundException.cs
│   │   │   ├── ForbiddenAccessException.cs
│   │   │   └── ConflictException.cs
│   │   └── Extensions/
│   │       ├── ErrorOrExtensions.cs
│   │       ├── MappingExtensions.cs
│   │       └── QueryExtensions.cs
│   ├── Domain/
│   │   ├── Products/
│   │   │   ├── Entities/
│   │   │   │   ├── Product.cs
│   │   │   │   ├── ProductImage.cs
│   │   │   │   ├── ProductVariant.cs
│   │   │   │   └── ProductReview.cs
│   │   │   ├── ValueObjects/
│   │   │   │   ├── ProductId.cs
│   │   │   │   ├── Sku.cs
│   │   │   │   ├── Price.cs
│   │   │   │   ├── Dimensions.cs
│   │   │   │   └── Weight.cs
│   │   │   ├── Events/
│   │   │   │   ├── ProductCreatedEvent.cs
│   │   │   │   ├── ProductUpdatedEvent.cs
│   │   │   │   ├── ProductDiscontinuedEvent.cs
│   │   │   │   └── ProductReviewAddedEvent.cs
│   │   │   ├── Enums/
│   │   │   │   ├── ProductStatus.cs
│   │   │   │   ├── ProductType.cs
│   │   │   │   └── Gender.cs
│   │   │   └── Specifications/
│   │   │       ├── ProductByIdSpec.cs
│   │   │       ├── ProductsByCategoySpec.cs
│   │   │       └── ActiveProductsSpec.cs
│   │   ├── Categories/
│   │   │   ├── Entities/
│   │   │   │   ├── Category.cs
│   │   │   │   └── CategoryHierarchy.cs
│   │   │   ├── ValueObjects/
│   │   │   │   ├── CategoryId.cs
│   │   │   │   └── CategoryPath.cs
│   │   │   ├── Events/
│   │   │   │   ├── CategoryCreatedEvent.cs
│   │   │   │   └── CategoryUpdatedEvent.cs
│   │   │   └── Specifications/
│   │   │       ├── RootCategoriesSpec.cs
│   │   │       └── CategoryByPathSpec.cs
│   │   ├── Customers/
│   │   │   ├── Entities/
│   │   │   │   ├── Customer.cs
│   │   │   │   ├── CustomerProfile.cs
│   │   │   │   └── CustomerAddress.cs
│   │   │   ├── ValueObjects/
│   │   │   │   ├── CustomerId.cs
│   │   │   │   ├── Email.cs
│   │   │   │   ├── PhoneNumber.cs
│   │   │   │   └── Address.cs
│   │   │   ├── Events/
│   │   │   │   ├── CustomerRegisteredEvent.cs
│   │   │   │   ├── CustomerProfileUpdatedEvent.cs
│   │   │   │   └── CustomerAddressChangedEvent.cs
│   │   │   └── Specifications/
│   │   │       ├── CustomerByEmailSpec.cs
│   │   │       └── ActiveCustomersSpec.cs
│   │   ├── Orders/
│   │   │   ├── Entities/
│   │   │   │   ├── Order.cs
│   │   │   │   ├── OrderItem.cs
│   │   │   │   ├── ShoppingCart.cs
│   │   │   │   └── CartItem.cs
│   │   │   ├── ValueObjects/
│   │   │   │   ├── OrderId.cs
│   │   │   │   ├── OrderNumber.cs
│   │   │   │   ├── Money.cs
│   │   │   │   └── Quantity.cs
│   │   │   ├── Events/
│   │   │   │   ├── OrderCreatedEvent.cs
│   │   │   │   ├── OrderShippedEvent.cs
│   │   │   │   ├── OrderCancelledEvent.cs
│   │   │   │   └── CartItemAddedEvent.cs
│   │   │   ├── Enums/
│   │   │   │   ├── OrderStatus.cs
│   │   │   │   └── PaymentStatus.cs
│   │   │   └── Specifications/
│   │   │       ├── OrdersByCustomerSpec.cs
│   │   │       └── OrdersByStatusSpec.cs
│   │   ├── Inventory/
│   │   │   ├── Entities/
│   │   │   │   ├── ProductInventory.cs
│   │   │   │   ├── InventoryMovement.cs
│   │   │   │   └── Warehouse.cs
│   │   │   ├── ValueObjects/
│   │   │   │   ├── InventoryId.cs
│   │   │   │   ├── StockLevel.cs
│   │   │   │   └── Location.cs
│   │   │   ├── Events/
│   │   │   │   ├── StockUpdatedEvent.cs
│   │   │   │   ├── LowStockAlertEvent.cs
│   │   │   │   └── StockMovementEvent.cs
│   │   │   ├── Enums/
│   │   │   │   ├── MovementType.cs
│   │   │   │   └── StockStatus.cs
│   │   │   └── Specifications/
│   │   │       ├── LowStockProductsSpec.cs
│   │   │       └── InventoryByWarehouseSpec.cs
│   │   ├── Attributes/
│   │   │   ├── Entities/
│   │   │   │   ├── Attribute.cs
│   │   │   │   ├── AttributeValue.cs
│   │   │   │   └── AttributeGroup.cs
│   │   │   ├── ValueObjects/
│   │   │   │   ├── AttributeId.cs
│   │   │   │   └── AttributeName.cs
│   │   │   ├── Events/
│   │   │   │   ├── AttributeCreatedEvent.cs
│   │   │   │   └── AttributeValueAddedEvent.cs
│   │   │   ├── Enums/
│   │   │   │   └── AttributeType.cs
│   │   │   └── Specifications/
│   │   │       └── AttributesByTypeSpec.cs
│   │   ├── Collections/
│   │   │   ├── Entities/
│   │   │   │   ├── Collection.cs
│   │   │   │   └── CollectionProduct.cs
│   │   │   ├── ValueObjects/
│   │   │   │   ├── CollectionId.cs
│   │   │   │   └── CollectionName.cs
│   │   │   ├── Events/
│   │   │   │   ├── CollectionCreatedEvent.cs
│   │   │   │   └── ProductAddedToCollectionEvent.cs
│   │   │   ├── Enums/
│   │   │   │   └── CollectionType.cs
│   │   │   └── Specifications/
│   │   │       ├── ActiveCollectionsSpec.cs
│   │   │       └── SeasonalCollectionsSpec.cs
│   │   └── Shared/
│   │       ├── Interfaces/
│   │       │   ├── IRepository.cs
│   │       │   ├── ISpecification.cs
│   │       │   └── IUnitOfWork.cs
│   │       ├── Events/
│   │       │   └── BaseEvent.cs
│   │       └── ValueObjects/
│   │           ├── CreatedBy.cs
│   │           ├── ModifiedBy.cs
│   │           └── AuditInfo.cs
│   ├── Features/
│   │   ├── DependencyInjection.cs
│   │   ├── Products/
│   │   │   ├── Create/
│   │   │   │   ├── CreateProductCommand.cs
│   │   │   │   ├── CreateProductCommandHandler.cs
│   │   │   │   ├── CreateProductCommandValidator.cs
│   │   │   │   └── CreateProductRequest.cs
│   │   │   ├── Update/
│   │   │   │   ├── UpdateProductCommand.cs
│   │   │   │   ├── UpdateProductCommandHandler.cs
│   │   │   │   ├── UpdateProductCommandValidator.cs
│   │   │   │   └── UpdateProductRequest.cs
│   │   │   ├── Delete/
│   │   │   │   ├── DeleteProductCommand.cs
│   │   │   │   └── DeleteProductCommandHandler.cs
│   │   │   ├── GetById/
│   │   │   │   ├── GetProductByIdQuery.cs
│   │   │   │   ├── GetProductByIdQueryHandler.cs
│   │   │   │   └── ProductDetailResponse.cs
│   │   │   ├── GetAll/
│   │   │   │   ├── GetProductsQuery.cs
│   │   │   │   ├── GetProductsQueryHandler.cs
│   │   │   │   └── ProductListResponse.cs
│   │   │   ├── Search/
│   │   │   │   ├── SearchProductsQuery.cs
│   │   │   │   ├── SearchProductsQueryHandler.cs
│   │   │   │   ├── ProductSearchFilter.cs
│   │   │   │   └── ProductSearchResponse.cs
│   │   │   ├── AddReview/
│   │   │   │   ├── AddProductReviewCommand.cs
│   │   │   │   ├── AddProductReviewCommandHandler.cs
│   │   │   │   ├── AddProductReviewCommandValidator.cs
│   │   │   │   └── ProductReviewRequest.cs
│   │   │   ├── UploadImages/
│   │   │   │   ├── UploadProductImagesCommand.cs
│   │   │   │   ├── UploadProductImagesCommandHandler.cs
│   │   │   │   └── ProductImageUploadRequest.cs
│   │   │   ├── GetRecommendations/
│   │   │   │   ├── GetProductRecommendationsQuery.cs
│   │   │   │   ├── GetProductRecommendationsQueryHandler.cs
│   │   │   │   └── ProductRecommendationResponse.cs
│   │   │   └── Common/
│   │   │       ├── ProductMappingProfile.cs
│   │   │       ├── ProductParam.cs
│   │   │       ├── ProductImageParam.cs
│   │   │       └── ProductReviewParam.cs
│   │   ├── Categories/
│   │   │   ├── Create/
│   │   │   │   ├── CreateCategoryCommand.cs
│   │   │   │   ├── CreateCategoryCommandHandler.cs
│   │   │   │   ├── CreateCategoryCommandValidator.cs
│   │   │   │   └── CreateCategoryRequest.cs
│   │   │   ├── Update/
│   │   │   │   ├── UpdateCategoryCommand.cs
│   │   │   │   ├── UpdateCategoryCommandHandler.cs
│   │   │   │   └── UpdateCategoryRequest.cs
│   │   │   ├── GetHierarchy/
│   │   │   │   ├── GetCategoryHierarchyQuery.cs
│   │   │   │   ├── GetCategoryHierarchyQueryHandler.cs
│   │   │   │   └── CategoryHierarchyResponse.cs
│   │   │   ├── GetProducts/
│   │   │   │   ├── GetCategoryProductsQuery.cs
│   │   │   │   ├── GetCategoryProductsQueryHandler.cs
│   │   │   │   └── CategoryProductsResponse.cs
│   │   │   └── Common/
│   │   │       ├── CategoryMappingProfile.cs
│   │   │       └── CategoryParam.cs
│   │   ├── Customers/
│   │   │   ├── Register/
│   │   │   │   ├── RegisterCustomerCommand.cs
│   │   │   │   ├── RegisterCustomerCommandHandler.cs
│   │   │   │   ├── RegisterCustomerCommandValidator.cs
│   │   │   │   └── CustomerRegistrationRequest.cs
│   │   │   ├── UpdateProfile/
│   │   │   │   ├── UpdateCustomerProfileCommand.cs
│   │   │   │   ├── UpdateCustomerProfileCommandHandler.cs
│   │   │   │   └── UpdateProfileRequest.cs
│   │   │   ├── AddAddress/
│   │   │   │   ├── AddCustomerAddressCommand.cs
│   │   │   │   ├── AddCustomerAddressCommandHandler.cs
│   │   │   │   └── AddAddressRequest.cs
│   │   │   ├── GetProfile/
│   │   │   │   ├── GetCustomerProfileQuery.cs
│   │   │   │   ├── GetCustomerProfileQueryHandler.cs
│   │   │   │   └── CustomerProfileResponse.cs
│   │   │   ├── GetOrderHistory/
│   │   │   │   ├── GetCustomerOrderHistoryQuery.cs
│   │   │   │   ├── GetCustomerOrderHistoryQueryHandler.cs
│   │   │   │   └── CustomerOrderHistoryResponse.cs
│   │   │   └── Common/
│   │   │       ├── CustomerMappingProfile.cs
│   │   │       ├── CustomerParam.cs
│   │   │       └── CustomerAddressParam.cs
│   │   ├── Orders/
│   │   │   ├── Create/
│   │   │   │   ├── CreateOrderCommand.cs
│   │   │   │   ├── CreateOrderCommandHandler.cs
│   │   │   │   ├── CreateOrderCommandValidator.cs
│   │   │   │   └── CreateOrderRequest.cs
│   │   │   ├── UpdateStatus/
│   │   │   │   ├── UpdateOrderStatusCommand.cs
│   │   │   │   ├── UpdateOrderStatusCommandHandler.cs
│   │   │   │   └── UpdateOrderStatusRequest.cs
│   │   │   ├── Cancel/
│   │   │   │   ├── CancelOrderCommand.cs
│   │   │   │   └── CancelOrderCommandHandler.cs
│   │   │   ├── GetById/
│   │   │   │   ├── GetOrderByIdQuery.cs
│   │   │   │   ├── GetOrderByIdQueryHandler.cs
│   │   │   │   └── OrderDetailResponse.cs
│   │   │   ├── GetAll/
│   │   │   │   ├── GetOrdersQuery.cs
│   │   │   │   ├── GetOrdersQueryHandler.cs
│   │   │   │   └── OrderListResponse.cs
│   │   │   ├── AddToCart/
│   │   │   │   ├── AddToCartCommand.cs
│   │   │   │   ├── AddToCartCommandHandler.cs
│   │   │   │   └── AddToCartRequest.cs
│   │   │   ├── UpdateCartItem/
│   │   │   │   ├── UpdateCartItemCommand.cs
│   │   │   │   ├── UpdateCartItemCommandHandler.cs
│   │   │   │   └── UpdateCartItemRequest.cs
│   │   │   ├── GetCart/
│   │   │   │   ├── GetShoppingCartQuery.cs
│   │   │   │   ├── GetShoppingCartQueryHandler.cs
│   │   │   │   └── ShoppingCartResponse.cs
│   │   │   ├── Checkout/
│   │   │   │   ├── CheckoutCommand.cs
│   │   │   │   ├── CheckoutCommandHandler.cs
│   │   │   │   ├── CheckoutCommandValidator.cs
│   │   │   │   └── CheckoutRequest.cs
│   │   │   └── Common/
│   │   │       ├── OrderMappingProfile.cs
│   │   │       ├── OrderParam.cs
│   │   │       ├── OrderItemParam.cs
│   │   │       └── CartItemParam.cs
│   │   ├── Inventory/
│   │   │   ├── UpdateStock/
│   │   │   │   ├── UpdateStockCommand.cs
│   │   │   │   ├── UpdateStockCommandHandler.cs
│   │   │   │   └── UpdateStockRequest.cs
│   │   │   ├── GetStock/
│   │   │   │   ├── GetProductStockQuery.cs
│   │   │   │   ├── GetProductStockQueryHandler.cs
│   │   │   │   └── ProductStockResponse.cs
│   │   │   ├── GetLowStock/
│   │   │   │   ├── GetLowStockProductsQuery.cs
│   │   │   │   ├── GetLowStockProductsQueryHandler.cs
│   │   │   │   └── LowStockProductsResponse.cs
│   │   │   ├── TransferStock/
│   │   │   │   ├── TransferStockCommand.cs
│   │   │   │   ├── TransferStockCommandHandler.cs
│   │   │   │   └── TransferStockRequest.cs
│   │   │   └── Common/
│   │   │       ├── InventoryMappingProfile.cs
│   │   │       └── InventoryParam.cs
│   │   ├── Wishlist/
│   │   │   ├── AddItem/
│   │   │   │   ├── AddWishlistItemCommand.cs
│   │   │   │   ├── AddWishlistItemCommandHandler.cs
│   │   │   │   └── AddWishlistItemRequest.cs
│   │   │   ├── RemoveItem/
│   │   │   │   ├── RemoveWishlistItemCommand.cs
│   │   │   │   └── RemoveWishlistItemCommandHandler.cs
│   │   │   ├── GetItems/
│   │   │   │   ├── GetWishlistItemsQuery.cs
│   │   │   │   ├── GetWishlistItemsQueryHandler.cs
│   │   │   │   └── WishlistItemsResponse.cs
│   │   │   └── Common/
│   │   │       ├── WishlistMappingProfile.cs
│   │   │       └── WishlistItemParam.cs
│   │   ├── Recommendations/
│   │   │   ├── GetSimilarProducts/
│   │   │   │   ├── GetSimilarProductsQuery.cs
│   │   │   │   ├── GetSimilarProductsQueryHandler.cs
│   │   │   │   └── SimilarProductsResponse.cs
│   │   │   ├── GetPersonalized/
│   │   │   │   ├── GetPersonalizedRecommendationsQuery.cs
│   │   │   │   ├── GetPersonalizedRecommendationsQueryHandler.cs
│   │   │   │   └── PersonalizedRecommendationsResponse.cs
│   │   │   ├── TrainModel/
│   │   │   │   ├── TrainRecommendationModelCommand.cs
│   │   │   │   └── TrainRecommendationModelCommandHandler.cs
│   │   │   └── Common/
│   │   │       ├── RecommendationMappingProfile.cs
│   │   │       └── RecommendationParam.cs
│   │   ├── Search/
│   │   │   ├── SearchProducts/
│   │   │   │   ├── SearchProductsQuery.cs
│   │   │   │   ├── SearchProductsQueryHandler.cs
│   │   │   │   ├── ProductSearchFilter.cs
│   │   │   │   └── ProductSearchResponse.cs
│   │   │   ├── GetFilters/
│   │   │   │   ├── GetSearchFiltersQuery.cs
│   │   │   │   ├── GetSearchFiltersQueryHandler.cs
│   │   │   │   └── SearchFiltersResponse.cs
│   │   │   ├── AutoComplete/
│   │   │   │   ├── GetSearchSuggestionsQuery.cs
│   │   │   │   ├── GetSearchSuggestionsQueryHandler.cs
│   │   │   │   └── SearchSuggestionsResponse.cs
│   │   │   └── Common/
│   │   │       ├── SearchMappingProfile.cs
│   │   │       └── SearchResultParam.cs
│   │   ├── Analytics/
│   │   │   ├── GetSalesReport/
│   │   │   │   ├── GetSalesReportQuery.cs
│   │   │   │   ├── GetSalesReportQueryHandler.cs
│   │   │   │   └── SalesReportResponse.cs
│   │   │   ├── GetProductMetrics/
│   │   │   │   ├── GetProductMetricsQuery.cs
│   │   │   │   ├── GetProductMetricsQueryHandler.cs
│   │   │   │   └── ProductMetricsResponse.cs
│   │   │   ├── GetCustomerInsights/
│   │   │   │   ├── GetCustomerInsightsQuery.cs
│   │   │   │   ├── GetCustomerInsightsQueryHandler.cs
│   │   │   │   └── CustomerInsightsResponse.cs
│   │   │   └── Common/
│   │   │       ├── AnalyticsMappingProfile.cs
│   │   │       └── AnalyticsParam.cs
│   │   └── Collections/
│   │       ├── Create/
│   │       │   ├── CreateCollectionCommand.cs
│   │       │   ├── CreateCollectionCommandHandler.cs
│   │       │   ├── CreateCollectionCommandValidator.cs
│   │       │   └── CreateCollectionRequest.cs
│   │       ├── AddProduct/
│   │       │   ├── AddProductToCollectionCommand.cs
│   │       │   ├── AddProductToCollectionCommandHandler.cs
│   │       │   └── AddProductToCollectionRequest.cs
│   │       ├── GetProducts/
│   │       │   ├── GetCollectionProductsQuery.cs
│   │       │   ├── GetCollectionProductsQueryHandler.cs
│   │       │   └── CollectionProductsResponse.cs
│   │       ├── GetAll/
│   │       │   ├── GetCollectionsQuery.cs
│   │       │   ├── GetCollectionsQueryHandler.cs
│   │       │   └── CollectionsResponse.cs
│   │       └── Common/
│   │           ├── CollectionMappingProfile.cs
│   │           └── CollectionParam.cs
│   └── Infrastructure/
│       ├── DependencyInjection.cs
│       └── Persistence/
│           ├── Configurations/
│           │   ├── ProductConfiguration.cs
│           │   ├── CategoryConfiguration.cs
│           │   ├── CustomerConfiguration.cs
│           │   ├── OrderConfiguration.cs
│           │   ├── InventoryConfiguration.cs
│           │   └── CollectionConfiguration.cs
│           ├── Constants/
│           │   └── Schema.cs
│           ├── Repositories/
│           │   ├── IProductRepository.cs
│           │   ├── ProductRepository.cs
│           │   ├── ICategoryRepository.cs
│           │   ├── CategoryRepository.cs
│           │   ├── ICustomerRepository.cs
│           │   ├── CustomerRepository.cs
│           │   ├── IOrderRepository.cs
│           │   ├── OrderRepository.cs
│           │   ├── IInventoryRepository.cs
│           │   └── InventoryRepository.cs
│           ├── Migrations/
│           └── ApplicationDbContext.cs
```

### Web.Api Layer Structure

```
src/
├── Web.Api/
│   ├── Web.Api.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Controllers/
│   │   ├── ApiControllerBase.cs
│   │   ├── ProductsController.cs
│   │   ├── CategoriesController.cs
│   │   ├── CustomersController.cs
│   │   ├── OrdersController.cs
│   │   ├── InventoryController.cs
│   │   ├── WishlistController.cs
│   │   ├── RecommendationsController.cs
│   │   ├── SearchController.cs
│   │   ├── AnalyticsController.cs
│   │   └── CollectionsController.cs
│   ├── Infrastructure/
│   │   ├── DependencyInjection.cs
│   │   ├── Configuration/
│   │   │   ├── SwaggerConfiguration.cs
│   │   │   ├── CorsConfiguration.cs
│   │   │   ├── AuthenticationConfiguration.cs
│   │   │   └── DatabaseConfiguration.cs
│   │   ├── Middleware/
│   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   ├── RequestLoggingMiddleware.cs
│   │   │   ├── PerformanceMiddleware.cs
│   │   │   └── SecurityHeadersMiddleware.cs
│   │   ├── Filters/
│   │   │   ├── ValidationFilter.cs
│   │   │   ├── AuthorizationFilter.cs
│   │   │   └── CacheFilter.cs
│   │   └── Services/
│   │       ├── CurrentUserService.cs
│   │       ├── DateTimeService.cs
│   │       └── FileUploadService.cs
│   └── Properties/
│       └── launchSettings.json
```

### Test Structure

```
tests/
├── Application.UnitTests/
│   ├── Application.UnitTests.csproj
│   ├── Features/
│   │   ├── Products/
│   │   │   ├── Create/
│   │   │   │   ├── CreateProductCommandHandlerTests.cs
│   │   │   │   └── CreateProductCommandValidatorTests.cs
│   │   │   ├── Update/
│   │   │   │   ├── UpdateProductCommandHandlerTests.cs
│   │   │   │   └── UpdateProductCommandValidatorTests.cs
│   │   │   ├── GetById/
│   │   │   │   └── GetProductByIdQueryHandlerTests.cs
│   │   │   ├── Search/
│   │   │   │   └── SearchProductsQueryHandlerTests.cs
│   │   │   └── TestData/
│   │   │       ├── ProductTestData.cs
│   │   │       └── ProductFactory.cs
│   │   ├── Orders/
│   │   │   ├── Create/
│   │   │   │   ├── CreateOrderCommandHandlerTests.cs
│   │   │   │   └── CreateOrderCommandValidatorTests.cs
│   │   │   ├── Checkout/
│   │   │   │   ├── CheckoutCommandHandlerTests.cs
│   │   │   │   └── CheckoutCommandValidatorTests.cs
│   │   │   └── TestData/
│   │   │       ├── OrderTestData.cs
│   │   │       └── OrderFactory.cs
│   │   ├── Customers/
│   │   │   ├── Register/
│   │   │   │   ├── RegisterCustomerCommandHandlerTests.cs
│   │   │   │   └── RegisterCustomerCommandValidatorTests.cs
│   │   │   └── TestData/
│   │   │       └── CustomerTestData.cs
│   │   └── Shared/
│   │       ├── TestBase.cs
│   │       ├── MockDbContext.cs
│   │       └── TestFixtures/
│   │           ├── DatabaseFixture.cs
│   │           └── AutoMapperFixture.cs
│   └── Domain/
│       ├── Products/
│       │   ├── Entities/
│       │   │   ├── ProductTests.cs
│       │   │   └── ProductVariantTests.cs
│       │   ├── ValueObjects/
│       │   │   ├── ProductIdTests.cs
│       │   │   ├── SkuTests.cs
│       │   │   └── PriceTests.cs
│       │   └── Events/
│       │       └── ProductCreatedEventTests.cs
│       ├── Orders/
│       │   ├── Entities/
│       │   │   ├── OrderTests.cs
│       │   │   └── ShoppingCartTests.cs
│       │   └── ValueObjects/
│       │       ├── OrderIdTests.cs
│       │       └── MoneyTests.cs
│       └── Customers/
│           ├── Entities/
│           │   └── CustomerTests.cs
│           └── ValueObjects/
│               ├── EmailTests.cs
│               └── AddressTests.cs
├── Application.IntegrationTests/
│   ├── Application.IntegrationTests.csproj
│   ├── Features/
│   │   ├── Products/
│   │   │   ├── ProductsControllerTests.cs
│   │   │   └── ProductWorkflowTests.cs
│   │   ├── Orders/
│   │   │   ├── OrdersControllerTests.cs
│   │   │   └── CheckoutWorkflowTests.cs
│   │   └── Customers/
│   │       └── CustomersControllerTests.cs
│   ├── Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── ProductRepositoryTests.cs
│   │   │   ├── OrderRepositoryTests.cs
│   │   │   └── CustomerRepositoryTests.cs
│   │   └── TestDatabase/
│   │       ├── TestDbContextFactory.cs
│   │       └── DatabaseSeeder.cs
│   └── Common/
│       ├── TestWebApplicationFactory.cs
│       ├── IntegrationTestBase.cs
│       └── TestData/
│           ├── SeedData.cs
│           └── TestConstants.cs
└── SharedKernel.UnitTests/
    ├── SharedKernel.UnitTests.csproj
    ├── Domains/
    │   └── Primitives/
    │       ├── EntityTests.cs
    │       └── ValueObjectTests.cs
    └── Models/
        ├── Filter/
        ├── Paging/
        ├── Search/
        └── Sort/
```

## Implementation Guidelines

### 1. Command and Query Separation (CQRS)

Each feature should separate commands (write operations) from queries (read operations):

```csharp
// Command Example
public record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    int CategoryId,
    List<string> Tags) : ICommand<Result<int>>;

// Query Example
public record GetProductByIdQuery(int ProductId) : IQuery<Result<ProductDetailResponse>>;
```

### 2. Request/Response Pattern

Use explicit request and response models for API contracts:

```csharp
// Request Model
public class CreateProductRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required decimal Price { get; init; }
    public required int CategoryId { get; init; }
    public List<string> Tags { get; init; } = [];
}

// Response Model
public class ProductDetailResponse
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public decimal Price { get; init; }
    public CategoryDto Category { get; init; } = null!;
    public List<ProductImageDto> Images { get; init; } = [];
    public List<string> Tags { get; init; } = [];
}
```

### 3. Domain Events

Each aggregate should raise domain events for important business operations:

```csharp
public class Product : AuditableEntity<int>
{
    public static Product Create(string name, string description, decimal price)
    {
        var product = new Product(name, description, price);
        product.AddDomainEvent(new ProductCreatedEvent(product.Id, product.Name));
        return product;
    }
}
```

### 4. Validation

Use FluentValidation for command validation:

```csharp
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
            
        RuleFor(x => x.Price)
            .GreaterThan(0)
            .LessThan(10000);
    }
}
```

### 5. Mapping

Use AutoMapper profiles for each feature:

```csharp
public class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        CreateMap<Product, ProductDto>();
        CreateMap<CreateProductRequest, CreateProductCommand>();
        CreateMap<Product, ProductDetailResponse>();
    }
}
```

## Benefits of This Structure

### 1. **Feature Cohesion**
- All related code for a feature is in one place
- Easy to understand the complete flow of a feature
- Reduced context switching when working on features

### 2. **Team Collaboration**
- Multiple teams can work on different features simultaneously
- Clear ownership boundaries
- Reduced merge conflicts

### 3. **Testing Strategy**
- Each feature can be tested independently
- Clear test organization matching feature structure
- Easy to identify test coverage gaps

### 4. **Scalability**
- Features can be extracted into microservices if needed
- Clear boundaries for splitting the application
- Independent deployment of features

### 5. **Maintainability**
- Changes are localized to specific features
- Easy to remove or modify features
- Clear dependencies between features

## Migration from Current Structure

To migrate your existing Todo example to this structure:

1. **Keep the existing structure** as a reference
2. **Create new features** following the proposed structure
3. **Gradually migrate** existing code to the new structure
4. **Update tests** to match the new organization
5. **Update documentation** to reflect the new architecture

## Next Steps

1. Choose one feature to implement first (e.g., Products)
2. Create the complete vertical slice for that feature
3. Set up the testing structure for the feature
4. Iterate and refine the structure based on learnings
5. Apply the same pattern to other features

This structure provides a solid foundation for your fashion e-commerce system while maintaining clean architecture principles and supporting future growth.