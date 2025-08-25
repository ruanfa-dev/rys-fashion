# FashionML: AI-Powered Fashion E-commerce Platform

## 1. Product Overview

### Core Value Proposition
FashionML is an intelligent fashion e-commerce platform that demonstrates successful integration of machine learning services with traditional e-commerce architecture. The system provides personalized product recommendations and visual similarity search for fashion items, serving as a comprehensive case study for ML integration patterns in e-commerce applications.

**Key Achievements:**
- Seamless integration between ASP.NET Core backend and Python ML services
- Real-time recommendation engine with sub-2-second response times
- Visual similarity search using deep learning feature extraction
- Scalable architecture supporting 1000+ products with room for growth
- Cost-effective implementation using open-source technologies

### Target Audience
- **Primary**: Computer science students and thesis evaluators studying ML system integration
- **Secondary**: Small to medium fashion retailers seeking AI-enhanced shopping experiences
- **Tertiary**: Developers learning practical ML integration patterns in web applications

## 2. Functional Specifications

### 2.1 Core E-commerce Platform Foundation
**Product Management System:**
- **Product Catalog**: Multi-level categorization (Category → Collection → Tag hierarchy)
  - Categories: Men, Women, Kids, Accessories (4 main categories)
  - Collections: Seasonal collections (Spring/Summer, Fall/Winter, Holiday)
  - Tags: Style tags (casual, formal, sporty, vintage, minimalist)
- **Product Entity Structure**:
  - Base Product: Name, description, brand, price, images (minimum 3 per product)
  - Product Variants: Size options (XS, S, M, L, XL, XXL), Color variants (minimum 3 colors per product)
  - Inventory Management: Stock levels, SKU tracking, low-stock alerts
- **Admin Dashboard**: Product CRUD operations, bulk import/export, image management

**User Management System:**
- **Authentication**: Email/password registration, login, password reset
- **User Profiles**: Basic info, shipping addresses, payment methods, order history
- **User Behavior Tracking**: Page views, product interactions, purchase history, cart abandonment

**Shopping & Order Management:**
- **Shopping Cart**: Add/remove items, quantity updates, variant selection, session persistence
- **Checkout Process**: Guest checkout option, address management, order confirmation
- **Order Management**: Order status tracking, order history, basic order analytics

### 2.2 ML-Powered Recommendation Engine (60% Focus)
**Content-Based Filtering:**
- **Product Attribute Analysis**: Color extraction (RGB values + color names), style classification, price range analysis
- **Visual Feature Extraction**: CNN-based feature vectors (512-dimensional) using pre-trained ResNet50
- **Attribute Similarity**: Weighted scoring based on category, color, price, style tags
- **Implementation**: Real-time similar product suggestions (5-10 items) on product detail pages

**Popularity-Based Recommendations:**
- **View/Purchase Tracking**: Simple counters for product popularity metrics
- **Trending Products**: Most viewed/purchased items in last 30 days
- **Category Popularity**: Popular items within specific categories
- **Implementation**: Homepage featured products and "Trending Now" sections

**User Preference Learning:**
- **Interaction Tracking**: Log user views, cart additions, purchases
- **Category Preferences**: Track user's most browsed categories
- **Simple Personalization**: Show popular items from user's preferred categories
- **Implementation**: Personalized homepage recommendations for returning users

**Hybrid Recommendation System:**
- **Algorithm Fusion**: 50% content-based + 30% popularity + 20% user preferences for optimal results
- **Personalized Homepage**: Top 8-12 recommended products based on user history and popular items
- **Category-Specific Recommendations**: Tailored suggestions within browsed categories
- **New User Handling**: Popularity-based recommendations for users with no history

**Performance Requirements:**
- Recommendation generation: < 2 seconds
- Cache hit ratio: > 80% for frequently accessed recommendations
- Accuracy target: > 60% user engagement with recommended items

### 2.3 Visual Search and Discovery (40% Focus)
**Image Processing Pipeline:**
- **Feature Extraction**: Pre-trained CNN models (ResNet50 or EfficientNet-B0) for 512-dim feature vectors
- **Image Preprocessing**: Resize to 224x224, normalization, data augmentation for robustness
- **Vector Storage**: FAISS index for efficient similarity search with cosine distance
- **Batch Processing**: Nightly feature extraction for new products

**Visual Search Functionality:**
- **Upload Interface**: Drag-and-drop image upload with preview and crop functionality
- **Similarity Search**: Return top 10 visually similar products with confidence scores
- **Filter Integration**: Combine visual similarity with category, price, and attribute filters
- **Mobile Optimization**: Camera capture integration for mobile visual search

**Visual Discovery Features:**
- **"Find Similar Styles"**: Button on each product page triggering visual similarity search
- **Visual Product Clustering**: Group similar products for browsing navigation
- **Trend Detection**: Identify popular visual features and styles from user interactions

### 2.4 Data Processing and Analytics Pipeline
**ML Data Pipeline:**
- **Feature Extraction Service**: Asynchronous processing of product images
- **User Behavior Analytics**: Real-time tracking and batch processing of user interactions
- **Recommendation Performance Tracking**: Click-through rates, conversion rates, user engagement metrics
- **A/B Testing Framework**: Split testing for different recommendation algorithms

**Database Design:**
- **Product Features Table**: ProductId, FeatureVector (binary), ExtractionDate, ModelVersion
- **User Interactions Table**: UserId, ProductId, InteractionType, Timestamp, SessionId
- **Recommendation Logs**: UserId, RecommendedProducts, Algorithm, Timestamp, UserAction
- **Similarity Scores Cache**: ProductId1, ProductId2, SimilarityScore, LastUpdated

### 2.5 System Integration Architecture
**API Design:**
- **RESTful Endpoints**: Standardized HTTP API with OpenAPI documentation
- **Authentication**: JWT tokens for user sessions, API key authentication for internal services
- **Rate Limiting**: 1000 requests/hour per user, 100 requests/minute for ML endpoints
- **Error Handling**: Structured error responses with appropriate HTTP status codes

**Microservice Communication:**
- **Message Queue Integration**: RabbitMQ for asynchronous ML processing tasks
- **Caching Strategy**: Redis for user sessions, recommendation results, and frequently accessed data
- **Service Discovery**: Simple configuration-based service registration
- **Health Monitoring**: Basic health check endpoints for all services

**Performance Optimization:**
- **Database Indexing**: Optimized indexes for product searches and user queries
- **CDN Integration**: Image delivery optimization with local file storage
- **Connection Pooling**: Efficient database connection management
- **Lazy Loading**: Deferred loading of ML features and recommendations

## 3. Technical Specifications

### Architecture Overview
**Monolithic Core with ML Microservices Pattern**
```
┌─────────────────────────────────────────────────────────────────┐
│                        Frontend Layer                           │
│  Vue.js 3 + Bootstrap 5 + Axios (Port 3000)                   │
└─────────────────────┬───────────────────────────────────────────┘
                      │ HTTP/REST API
┌─────────────────────▼───────────────────────────────────────────┐
│                   ASP.NET Core API Gateway                     │
│  Authentication, Rate Limiting, Request Routing (Port 5000)    │
└─────────────────────┬───────────────────────────────────────────┘
                      │
        ┌─────────────┼─────────────────────────────────┐
        │             │                                 │
┌───────▼──────┐ ┌───▼────────┐ ┌─────────────▼──────────┐
│   E-commerce │ │    ML      │ │    Background          │
│   Services   │ │  Services  │ │    Workers             │
│              │ │            │ │                        │
│ • Products   │ │ • Features │ │ • Image Processing     │
│ • Users      │ │ • Recommendations │ • Analytics       │
│ • Orders     │ │ • Similarity│ │ • Batch Jobs          │
│ • Cart       │ │   Search   │ │                        │
└──────┬───────┘ └───┬────────┘ └────────────┬───────────┘
       │             │                       │
┌──────▼─────────────▼───────────────────────▼───────────┐
│              Data Layer                               │
│  PostgreSQL + pgvector + Redis Cache                  │
└───────────────────────────────────────────────────────┘
```

### Technology Stack Details

**Backend Technologies:**
- **ASP.NET Core 8.0**: Web API with minimal APIs, dependency injection, built-in logging
  - **NuGet Packages**: Microsoft.AspNetCore.Authentication.JwtBearer, Microsoft.EntityFrameworkCore.PostgreSQL, Swashbuckle.AspNetCore
  - **Keywords**: Program.cs, Controllers, Services, Middleware, Dependency Injection
  - **Patterns**: Repository Pattern, Service Layer, CQRS, MediatR
- **Entity Framework Core 8.0**: Code-first approach, migrations, lazy loading
  - **Keywords**: DbContext, DbSet, migrations, Code-First, Fluent API
  - **Database**: PostgreSQL provider, connection pooling, query optimization
- **PostgreSQL 15+**: Primary database with pgvector extension for vector similarity search
  - **Extensions**: pgvector for ML features, uuid-ossp for UUIDs
  - **Keywords**: JSONB, arrays, full-text search, vector operations
  - **Performance**: Indexing, query optimization, connection pooling
- **Redis 7.0**: Session management, caching layer, rate limiting store
  - **Use Cases**: Session storage, ML result caching, rate limiting
  - **Keywords**: Cache-aside pattern, TTL, Redis pub/sub, data structures
- **RabbitMQ 3.12**: Message broker for async ML processing tasks
  - **Keywords**: Message queues, publishers, consumers, routing keys
  - **Patterns**: Publish/Subscribe, Work Queues, RPC patterns

**ML Technologies:**
- **Python 3.11**: Core ML runtime environment
  - **Virtual Environment**: venv, pip, requirements.txt
  - **Keywords**: asyncio, type hints, dataclasses, context managers
- **FastAPI 0.104+**: High-performance async API framework for ML services
  - **Keywords**: async/await, Pydantic models, dependency injection, APIRouter
  - **Features**: Automatic OpenAPI docs, request validation, response models
- **PyTorch 2.1**: Deep learning framework for model inference
  - **Keywords**: torch.nn, torchvision.models, torch.jit, CUDA support
  - **Models**: ResNet50, EfficientNet, pre-trained models, transfer learning
- **scikit-learn 1.3**: Traditional ML algorithms and preprocessing
  - **Keywords**: cosine_similarity, preprocessing, feature_extraction, clustering
  - **Algorithms**: KMeans, TF-IDF, similarity metrics, dimensionality reduction
- **OpenCV 4.8**: Image processing and computer vision tasks
  - **Keywords**: cv2, image preprocessing, color space conversion, filtering
  - **Features**: Image loading, resizing, cropping, format conversion
- **NumPy/Pandas**: Data manipulation and numerical computing
  - **Keywords**: arrays, vectorization, data frames, numerical operations
  - **Performance**: Vectorized operations, memory optimization
- **FAISS 1.7**: Facebook AI Similarity Search for efficient vector operations
  - **Keywords**: IndexFlatIP, IndexIVF, similarity search, vector databases
  - **Performance**: GPU acceleration, index optimization, search speed
- **Pillow (PIL)**: Image processing and manipulation
  - **Keywords**: Image.open, resize, crop, format conversion, EXIF data
  - **Formats**: JPEG, PNG, WebP, image optimization

**Frontend Technologies:**
- **Vue.js 3.3**: Progressive framework with Composition API
  - **Keywords**: Composition API, reactivity, components, directives
  - **State Management**: Pinia, reactive state, stores
  - **Routing**: Vue Router, route guards, lazy loading
- **Bootstrap 5.3**: CSS framework for responsive design
  - **Keywords**: Grid system, components, utilities, responsive breakpoints
  - **Components**: Cards, modals, navbars, forms, buttons
- **Axios 1.5**: HTTP client for API communication
  - **Keywords**: HTTP requests, interceptors, async/await, error handling
  - **Features**: Request/response interceptors, automatic JSON parsing
- **Vue Router 4**: Client-side routing
  - **Keywords**: Route guards, lazy loading, nested routes, navigation
- **Pinia 2.1**: State management library
  - **Keywords**: Stores, state, getters, actions, composition API integration

**Development & DevOps:**
- **Docker & Docker Compose**: Containerization and local development
  - **Keywords**: Dockerfile, docker-compose.yml, multi-stage builds, volumes
  - **Services**: PostgreSQL, Redis, RabbitMQ, application containers
- **Visual Studio 2022**: Primary .NET development IDE
  - **Features**: IntelliSense, debugging, NuGet package manager, Git integration
- **VS Code**: Python and frontend development
  - **Extensions**: Python, Vue.js, Docker, REST Client
- **Git**: Version control with feature branch workflow
  - **Keywords**: Feature branches, pull requests, merge strategies
- **Swagger/OpenAPI**: API documentation and testing
  - **Keywords**: OpenAPI 3.0, Swagger UI, API testing, documentation

### Database Schema Design

**Core E-commerce Tables:**
```sql
-- Categories Table
CREATE TABLE Categories (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Slug VARCHAR(100) UNIQUE NOT NULL,
    Description TEXT,
    ParentId INT REFERENCES Categories(Id),
    CreatedAt TIMESTAMP DEFAULT NOW()
);

-- Products Table
CREATE TABLE Products (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Slug VARCHAR(255) UNIQUE NOT NULL,
    Description TEXT,
    Brand VARCHAR(100),
    BasePrice DECIMAL(10,2) NOT NULL,
    CategoryId INT REFERENCES Categories(Id),
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP DEFAULT NOW(),
    UpdatedAt TIMESTAMP DEFAULT NOW()
);

-- Product Images Table
CREATE TABLE ProductImages (
    Id SERIAL PRIMARY KEY,
    ProductId INT REFERENCES Products(Id),
    ImageUrl VARCHAR(500) NOT NULL,
    AltText VARCHAR(255),
    SortOrder INT DEFAULT 0,
    IsMain BOOLEAN DEFAULT false
);

-- Product Variants Table
CREATE TABLE ProductVariants (
    Id SERIAL PRIMARY KEY,
    ProductId INT REFERENCES Products(Id),
    SKU VARCHAR(100) UNIQUE NOT NULL,
    Price DECIMAL(10,2),
    Color VARCHAR(50),
    Size VARCHAR(20),
    Stock INT DEFAULT 0,
    IsActive BOOLEAN DEFAULT true
);

-- Users Table
CREATE TABLE Users (
    Id SERIAL PRIMARY KEY,
    Email VARCHAR(255) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    FirstName VARCHAR(100),
    LastName VARCHAR(100),
    CreatedAt TIMESTAMP DEFAULT NOW(),
    LastLoginAt TIMESTAMP
);
```

**ML-Specific Tables:**
```sql
-- Product Features Table (for ML)
CREATE TABLE ProductFeatures (
    ProductId INT PRIMARY KEY REFERENCES Products(Id),
    FeatureVector BYTEA NOT NULL, -- Serialized numpy array
    ColorHistogram JSONB, -- RGB color distribution
    DominantColors JSONB, -- Top 3 dominant colors
    ExtractionDate TIMESTAMP DEFAULT NOW(),
    ModelVersion VARCHAR(50) DEFAULT 'resnet50-v1'
);

-- User Behavior Tracking
CREATE TABLE UserInteractions (
    Id SERIAL PRIMARY KEY,
    UserId INT REFERENCES Users(Id),
    ProductId INT REFERENCES Products(Id),
    InteractionType VARCHAR(20) NOT NULL, -- 'view', 'cart_add', 'purchase'
    SessionId VARCHAR(100),
    Timestamp TIMESTAMP DEFAULT NOW(),
    DurationSeconds INT -- time spent on product page
);

-- Recommendation Logs
CREATE TABLE RecommendationLogs (
    Id SERIAL PRIMARY KEY,
    UserId INT REFERENCES Users(Id),
    ProductId INT REFERENCES Products(Id), -- product viewed
    RecommendedProductIds INT[], -- array of recommended product IDs
    Algorithm VARCHAR(50), -- 'content_based', 'collaborative', 'hybrid'
    Timestamp TIMESTAMP DEFAULT NOW(),
    UserAction VARCHAR(20) -- 'clicked', 'ignored', 'purchased'
);

-- Pre-computed Similarity Scores
CREATE TABLE ProductSimilarities (
    ProductId1 INT REFERENCES Products(Id),
    ProductId2 INT REFERENCES Products(Id),
    SimilarityScore FLOAT NOT NULL,
    SimilarityType VARCHAR(20), -- 'visual', 'attribute', 'collaborative'
    LastUpdated TIMESTAMP DEFAULT NOW(),
    PRIMARY KEY (ProductId1, ProductId2, SimilarityType)
);
```

### API Architecture

**ASP.NET Core Controllers:**
```csharp
// Product API Endpoints
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    // GET /api/products?category=women&page=1&limit=20
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
        [FromQuery] ProductFilterDto filter)

    // GET /api/products/123
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDetailDto>> GetProduct(int id)

    // GET /api/products/123/recommendations
    [HttpGet("{id}/recommendations")]
    public async Task<ActionResult<List<ProductDto>>> GetRecommendations(int id)

    // POST /api/products/visual-search
    [HttpPost("visual-search")]
    public async Task<ActionResult<List<ProductDto>>> VisualSearch(
        [FromForm] IFormFile image)
}

// ML Service Integration
[ApiController]
[Route("api/ml")]
public class MLController : ControllerBase
{
    // POST /api/ml/extract-features
    [HttpPost("extract-features")]
    public async Task<ActionResult> ExtractFeatures([FromBody] FeatureExtractionRequest request)

    // GET /api/ml/similar-products/123
    [HttpGet("similar-products/{productId}")]
    public async Task<ActionResult<List<SimilarProductDto>>> GetSimilarProducts(int productId)

    // POST /api/ml/recommendations
    [HttpPost("recommendations")]
    public async Task<ActionResult<List<ProductDto>>> GetPersonalizedRecommendations(
        [FromBody] RecommendationRequest request)
}
```

**Python FastAPI ML Services:**
```python
# FastAPI ML Service Structure
from fastapi import FastAPI, UploadFile, File
from pydantic import BaseModel
import torch
import numpy as np

app = FastAPI(title="FashionML Service", version="1.0.0")

class FeatureExtractionResponse(BaseModel):
    product_id: int
    feature_vector: List[float]
    dominant_colors: List[str]
    processing_time: float

class SimilarProductResponse(BaseModel):
    product_id: int
    similarity_score: float
    similarity_type: str

@app.post("/extract-features", response_model=FeatureExtractionResponse)
async def extract_features(
    product_id: int,
    image: UploadFile = File(...)
):
    # Implementation details in development phase
    pass

@app.get("/similar-products/{product_id}")
async def get_similar_products(
    product_id: int,
    limit: int = 10,
    similarity_threshold: float = 0.7
) -> List[SimilarProductResponse]:
    # Implementation details in development phase
    pass

@app.post("/recommendations")
async def get_recommendations(
    user_id: int,
    product_context: Optional[int] = None,
    limit: int = 8
) -> List[SimilarProductResponse]:
    # Implementation details in development phase
    pass
```

### Integration Patterns

**1. API Gateway Pattern:**
- Central entry point for all client requests
- Authentication and authorization handling
- Request routing to appropriate services
- Rate limiting and throttling

**2. Event-Driven Architecture:**
- Product updates trigger feature extraction
- User interactions logged asynchronously
- Recommendation model updates via events

**3. Caching Strategy:**
- Redis for user sessions (TTL: 30 minutes)
- Product recommendations cache (TTL: 1 hour)
- Feature vectors cache (TTL: 24 hours)
- Database query result caching

**4. Error Handling & Resilience:**
- Circuit breaker pattern for ML service calls
- Graceful degradation when ML services unavailable
- Retry policies with exponential backoff
- Comprehensive logging and monitoring

## 4. MVP Scope

### Phase 1: Core E-commerce Foundation (Weeks 1-4)

**Week 1: Project Setup & Database**
- [ ] Initialize ASP.NET Core 8.0 Web API project
- [ ] Set up PostgreSQL database with Docker
- [ ] Create Entity Framework models and migrations
- [ ] Implement basic product CRUD operations
- [ ] Set up Swagger documentation
- [ ] Create initial seed data (100 fashion products from Kaggle dataset)

**Week 2: User Management & Authentication**
- [ ] Implement user registration/login with JWT
- [ ] Create user profile management
- [ ] Set up role-based authorization (Admin/Customer)
- [ ] Implement password reset functionality
- [ ] Add user session tracking

**Week 3: Product Catalog & Shopping Cart**
- [ ] Build product listing with pagination and filtering
- [ ] Implement product detail pages
- [ ] Create shopping cart functionality
- [ ] Add product variant selection (size, color)
- [ ] Implement inventory management

**Week 4: Basic Frontend & Admin Panel**
- [ ] Set up Vue.js 3 project with Bootstrap 5
- [ ] Create responsive product catalog pages
- [ ] Build basic admin panel for product management
- [ ] Implement image upload functionality
- [ ] Connect frontend to API endpoints

**Week 4 Deliverables:**
- Functional e-commerce website with 100+ products
- User registration and shopping cart
- Admin panel for product management
- Responsive UI using Bootstrap templates

### Phase 2: ML Integration & Recommendation Engine (Weeks 5-7)

**Week 5: ML Service Setup & Feature Extraction**
- [ ] Set up Python FastAPI ML service
- [ ] Install and configure PyTorch, scikit-learn, OpenCV
- [ ] Implement ResNet50 feature extraction pipeline
- [ ] Create batch processing script for existing products
- [ ] Set up Redis for ML result caching
- [ ] Add ML feature storage to database

**Week 6: Recommendation System Implementation**
- [ ] Build content-based filtering algorithm
- [ ] Implement user interaction tracking
- [ ] Create collaborative filtering using user behavior
- [ ] Develop hybrid recommendation engine (70% content + 30% collaborative)
- [ ] Add recommendation API endpoints
- [ ] Implement "Similar Products" on product pages

**Week 7: Recommendation Integration & Testing**
- [ ] Integrate recommendations into frontend
- [ ] Add personalized homepage recommendations
- [ ] Implement recommendation performance tracking
- [ ] Create A/B testing framework
- [ ] Optimize recommendation response times (<2 seconds)
- [ ] Add recommendation analytics dashboard

**Week 7 Deliverables:**
- Working recommendation system with measurable accuracy
- Content-based and collaborative filtering algorithms
- Real-time product recommendations on all pages
- Basic analytics tracking recommendation performance

### Phase 3: Visual Search & System Polish (Weeks 8-10)

**Week 8: Visual Search Implementation**
- [ ] Implement image upload and preprocessing
- [ ] Create visual similarity search using FAISS
- [ ] Add "Find Similar Styles" button on product pages
- [ ] Implement visual search results page
- [ ] Add image-based product discovery
- [ ] Optimize visual search performance

**Week 9: Advanced Features & Performance**
- [ ] Implement advanced caching strategies
- [ ] Add background job processing with RabbitMQ
- [ ] Create comprehensive error handling
- [ ] Implement rate limiting and security measures
- [ ] Add performance monitoring and logging
- [ ] Optimize database queries and indexing

**Week 10: Testing, Documentation & Deployment**
- [ ] Comprehensive testing (unit, integration, performance)
- [ ] Create API documentation and user guides
- [ ] Performance benchmarking and optimization
- [ ] Prepare deployment configuration
- [ ] Write thesis documentation
- [ ] Create demo data and presentation materials

**Week 10 Deliverables:**
- Complete fashion e-commerce platform with ML integration
- Visual search functionality with similarity scoring
- Comprehensive documentation and thesis materials
- Performance metrics and benchmarking results

### MVP Success Criteria

**Technical Metrics:**
- ✅ 500+ products with complete ML feature extraction
- ✅ Recommendation response time: < 2 seconds
- ✅ Visual search accuracy: > 70% relevant results
- ✅ System uptime: > 99% during testing period
- ✅ Database query performance: < 100ms for product listings

**Functional Requirements:**
- ✅ Complete e-commerce workflow (browse → cart → checkout)
- ✅ Personalized recommendations on homepage and product pages
- ✅ Visual similarity search with upload functionality
- ✅ Admin panel for product and inventory management
- ✅ User behavior tracking and analytics

**Academic Deliverables:**
- ✅ Comprehensive architecture documentation
- ✅ ML integration patterns and best practices guide
- ✅ Performance analysis and benchmarking report
- ✅ Source code with extensive comments and documentation
- ✅ Demo presentation showcasing system capabilities

### Development Environment Setup

**Required Software:**
```bash
# Backend Development
- Visual Studio 2022 or VS Code
- .NET 8.0 SDK
- PostgreSQL 15+
- Redis 7.0
- Docker Desktop

# ML Development  
- Python 3.11
- PyTorch 2.1
- CUDA Toolkit (if GPU available)
- Jupyter Notebook (for experimentation)

# Frontend Development
- Node.js 18+
- Vue CLI or Vite
- Bootstrap 5.3
```

**Local Development Stack:**
```yaml
# docker-compose.yml
version: '3.8'
services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: fashionml
      POSTGRES_USER: admin
      POSTGRES_PASSWORD: password
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: admin
      RABBITMQ_DEFAULT_PASS: password

volumes:
  postgres_data:
```

### Risk Mitigation Plan

**Technical Risks:**
1. **ML Model Performance**: Use pre-trained models initially, optimize later
2. **Database Performance**: Implement proper indexing and query optimization
3. **Integration Complexity**: Start with simple REST APIs, add complexity gradually
4. **Timeline Overrun**: Prioritize core features, defer nice-to-have features

**Academic Risks:**
1. **Insufficient Novelty**: Focus on integration patterns rather than ML innovation
2. **Scope Creep**: Stick to defined MVP, document future enhancements
3. **Documentation Debt**: Write documentation continuously, not at the end

## 5. Business Model

### Key Differentiators
- **Academic Focus**: Open-source codebase for educational purposes
- **Integration Architecture**: Demonstrates best practices for ML/e-commerce integration
- **Cost-Effective**: No expensive cloud ML services required
- **Scalable Design**: Architecture supports growth from hundreds to thousands of products
- **Performance Optimized**: Efficient similarity search and caching strategies

### Potential Business Model (Post-Thesis)
**SaaS Platform for Small Retailers:**
- Monthly subscription: $50-200/month based on product catalog size
- Setup and integration services: $500-2000 one-time fee
- Custom ML model training: $1000-5000 per project
- White-label licensing: $10,000-50,000 annually

**Revenue Streams:**
1. Subscription fees for platform usage
2. Professional services for integration
3. Custom algorithm development
4. Training and consulting services

### Market Opportunity
- Small fashion retailers lacking ML capabilities
- Educational institutions teaching ML integration
- Developers learning e-commerce architecture
- Research community studying recommendation systems

## 6. Marketing Plan

### Target Audience Segments

**Primary: Academic Community**
- Computer science students working on similar projects
- Researchers studying recommendation systems
- Professors teaching ML integration courses
- Thesis supervisors and evaluators

**Secondary: Developer Community**
- Full-stack developers learning ML integration
- E-commerce developers interested in recommendations
- Open-source contributors
- Tech bloggers and content creators

**Tertiary: Small Business Owners**
- Fashion retailers with limited tech resources
- Startup founders building e-commerce platforms
- Digital agencies serving retail clients

### Marketing Strategy

**Academic Outreach:**
- Publish thesis findings in relevant conferences
- Share code repository on academic platforms
- Present at university tech talks
- Contribute to ML/e-commerce research papers

**Developer Community Engagement:**
- Open-source the project on GitHub
- Write technical blog posts about architecture decisions
- Create tutorial series on ML integration
- Participate in developer conferences and meetups

**Content Marketing:**
- Technical documentation and case studies
- Architecture decision records (ADRs)
- Performance benchmarking reports
- Integration best practices guides

### Marketing Channels

**Digital Channels:**
- GitHub repository with comprehensive documentation
- Technical blog (Medium, Dev.to)
- LinkedIn articles and posts
- YouTube tutorials and demos
- Twitter/X for community engagement

**Academic Channels:**
- University thesis presentation
- Academic conference submissions
- Research paper publications
- Professor and peer networks

**Community Channels:**
- Stack Overflow contributions
- Reddit posts in relevant subreddits
- Discord/Slack developer communities
- Local tech meetups and presentations

**Success Metrics:**
- GitHub stars and forks (target: 100+ stars)
- Blog post engagement (target: 1000+ views per post)
- Academic citations (target: 5+ citations)
- Community contributions (target: 10+ contributors)
- Professional networking connections (target: 50+ relevant connections)

---

**Project Timeline**: August 9, 2025 - October 23, 2025 (10.5 weeks)
**Budget**: Under 1M VND (~$40 USD)
**Team Size**: 1 developer
**Academic Deliverable**: Thesis demonstrating successful ML integration in e-commerce architecture
