# Simplified ML Recommendation System Guide

## Understanding the Recommendation System (Simplified Version)

Since you mentioned being unfamiliar with collaborative filtering, let me break this down into simpler, more manageable pieces.

## 1. Content-Based Recommendations (EASY - Start Here)

**What it does**: Recommends products similar to what the user is currently viewing
**How it works**: Compare product attributes (color, category, price, style)

### Simple Implementation:

```python
def get_similar_products(product_id, limit=5):
    current_product = get_product(product_id)
    all_products = get_all_products()
    
    similarities = []
    for product in all_products:
        if product.id == product_id:
            continue
            
        # Simple similarity scoring
        score = 0
        
        # Same category = +50 points
        if product.category == current_product.category:
            score += 50
            
        # Similar price (+/- 20%) = +30 points
        price_diff = abs(product.price - current_product.price) / current_product.price
        if price_diff <= 0.2:
            score += 30
            
        # Same brand = +20 points
        if product.brand == current_product.brand:
            score += 20
            
        similarities.append((product, score))
    
    # Sort by score and return top results
    similarities.sort(key=lambda x: x[1], reverse=True)
    return [item[0] for item in similarities[:limit]]
```

## 2. Visual Similarity (MEDIUM - Add After Content-Based Works)

**What it does**: Find products that look similar in images
**How it works**: Compare image features using pre-trained CNN

### Simple Implementation:

```python
import numpy as np
from sklearn.metrics.pairwise import cosine_similarity

def get_visually_similar_products(product_id, limit=5):
    # Get image features for current product
    current_features = get_product_image_features(product_id)
    
    # Get all product features
    all_features = get_all_product_features()
    
    similarities = []
    for other_product_id, features in all_features.items():
        if other_product_id == product_id:
            continue
            
        # Calculate cosine similarity between feature vectors
        similarity = cosine_similarity([current_features], [features])[0][0]
        similarities.append((other_product_id, similarity))
    
    # Sort by similarity and return top results
    similarities.sort(key=lambda x: x[1], reverse=True)
    return [item[0] for item in similarities[:limit]]
```

## 3. Popularity-Based Recommendations (EASY - Good for New Users)

**What it does**: Show most popular/trending products
**How it works**: Count views, purchases, and show top items

### Simple Implementation:

```python
def get_popular_products(category=None, limit=8):
    # Get products with view/purchase counts
    if category:
        products = get_products_by_category(category)
    else:
        products = get_all_products()
    
    # Sort by popularity (views + purchases * 5)
    popular_products = []
    for product in products:
        popularity_score = product.view_count + (product.purchase_count * 5)
        popular_products.append((product, popularity_score))
    
    popular_products.sort(key=lambda x: x[1], reverse=True)
    return [item[0] for item in popular_products[:limit]]
```

## 4. Simple User Behavior Tracking (EASY)

**What it does**: Track what users look at and buy
**How it works**: Log user actions and use for simple recommendations

### Implementation:

```python
# Track user interactions
def track_user_interaction(user_id, product_id, interaction_type):
    interaction = UserInteraction(
        user_id=user_id,
        product_id=product_id,
        interaction_type=interaction_type,  # 'view', 'cart_add', 'purchase'
        timestamp=datetime.now()
    )
    db.save(interaction)

# Get user's favorite categories
def get_user_preferred_categories(user_id, limit=3):
    interactions = get_user_interactions(user_id)
    category_counts = {}
    
    for interaction in interactions:
        category = interaction.product.category
        if category not in category_counts:
            category_counts[category] = 0
        category_counts[category] += 1
    
    # Return top categories
    sorted_categories = sorted(category_counts.items(), key=lambda x: x[1], reverse=True)
    return [cat[0] for cat in sorted_categories[:limit]]
```

## 5. Simple Hybrid System (MEDIUM - Combine Everything)

**What it does**: Combine different recommendation methods
**How it works**: Mix content-based + visual + popularity

### Implementation:

```python
def get_hybrid_recommendations(user_id, product_id=None, limit=8):
    recommendations = []
    
    if product_id:
        # If viewing a product, get similar ones
        content_similar = get_similar_products(product_id, limit=4)
        visual_similar = get_visually_similar_products(product_id, limit=4)
        recommendations.extend(content_similar[:2])
        recommendations.extend(visual_similar[:2])
    else:
        # For homepage, use user preferences + popular
        if user_id:
            preferred_categories = get_user_preferred_categories(user_id)
            for category in preferred_categories:
                popular_in_category = get_popular_products(category, limit=2)
                recommendations.extend(popular_in_category)
        
        # Fill remaining slots with overall popular products
        while len(recommendations) < limit:
            popular = get_popular_products(limit=limit-len(recommendations))
            recommendations.extend(popular)
    
    # Remove duplicates and return
    unique_recommendations = list(set(recommendations))
    return unique_recommendations[:limit]
```

## Simplified Database Tables

```sql
-- Track user behavior (simple version)
CREATE TABLE UserInteractions (
    Id SERIAL PRIMARY KEY,
    UserId INT,
    ProductId INT,
    InteractionType VARCHAR(20), -- 'view', 'cart_add', 'purchase'
    Timestamp TIMESTAMP DEFAULT NOW()
);

-- Store popularity metrics (simple version)
CREATE TABLE ProductPopularity (
    ProductId INT PRIMARY KEY,
    ViewCount INT DEFAULT 0,
    CartAddCount INT DEFAULT 0,
    PurchaseCount INT DEFAULT 0,
    LastUpdated TIMESTAMP DEFAULT NOW()
);

-- Store image features (for visual similarity)
CREATE TABLE ProductImageFeatures (
    ProductId INT PRIMARY KEY,
    ImageFeatures TEXT, -- JSON string of feature vector
    ExtractionDate TIMESTAMP DEFAULT NOW()
);
```

## Implementation Order (Start Simple!)

### Phase 1: Basic Content Recommendations
1. **Week 5**: Implement simple attribute-based similarity
2. **Week 6**: Add to product pages as "Similar Products"
3. **Test**: Make sure it works before moving on

### Phase 2: Add Popularity
1. **Week 6**: Implement view/purchase tracking
2. **Week 7**: Add popular products for homepage
3. **Test**: Verify tracking works correctly

### Phase 3: Add Visual Similarity
1. **Week 8**: Extract image features using pre-trained model
2. **Week 8**: Implement visual similarity search
3. **Test**: Compare visual recommendations with manual checking

### Phase 4: Combine Everything
1. **Week 9**: Create simple hybrid system
2. **Week 10**: Fine-tune and optimize
3. **Test**: A/B test different combinations

## Key Simplifications for Your Thesis

1. **Skip Complex Collaborative Filtering**: Too complex for timeline
2. **Use Simple Similarity Metrics**: Easier to understand and debug
3. **Focus on Integration**: Show how ML works with e-commerce
4. **Start with Rules**: Use business logic before ML complexity

This approach will give you a working recommendation system that's:
- ✅ Easy to understand and implement
- ✅ Sufficient for thesis demonstration
- ✅ Expandable if you have extra time
- ✅ Focused on integration rather than complex algorithms
