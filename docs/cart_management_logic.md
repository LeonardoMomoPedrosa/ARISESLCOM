# Cart Management Logic Documentation

## Overview

This document explains the business logic for managing shopping cart items, including automatic quantity updates for duplicate products and automatic management of special packaging for live products.

---

## 1. Add Item to Cart Logic

### Business Rule

When adding a product to the cart, the system must check if the product already exists in the cart. If it exists, **increment the quantity** instead of creating a new cart line item.

### Process Flow

1. **Receive Product ID** to add to cart
2. **Load Current Cart** from session/storage
3. **Search for Existing Item**:
   - Query cart items by `ProductId` (primary key)
   - If item found: `ExistingQuantity = item.Quantity`
   - If not found: `ExistingQuantity = 0`

4. **Decision Logic**:
   - **If `ExistingQuantity > 0`**:
     - Update existing item: `NewQuantity = ExistingQuantity + 1`
     - **Do NOT** create a new cart line item
   - **If `ExistingQuantity == 0`**:
     - Create new cart item with `Quantity = 1`
     - Add item to cart

5. **Save Cart** (triggers validation and automatic package management)

### Key Points

- **Primary Key**: Product identification is done by `ProductId` (PKId)
- **No Duplicates**: Same product cannot appear multiple times in cart
- **Quantity Accumulation**: Each "Add to Cart" action increments quantity by 1
- **Transparent to User**: User sees single line item with updated quantity

---

## 2. CheckLivePackage Logic

### Business Rule

The system must automatically manage a special packaging product based on the presence of "live products" in the cart. Live products require special packaging for shipping.

### Live Product Identification

**Live products** are identified by their **Product Sub-Type ID** (`id_subtipo` field in database).

**Live Product Type IDs** (configurable):
- Type 19
- Type 20
- Type 21
- Type 54

**Database Reference:**
- Table: `tbProdutos`
- Field: `id_subtipo` (references `tbTipoProduto`)
- Live products have `id_subtipo` matching one of the configured type IDs

### Special Packaging Product

**Product ID**: `1354` (configurable constant)
- This is a special product representing packaging fee for live animals
- Should not be included in shipment calculations or product recommendations
- Automatically managed by the system

### Process Flow

The `CheckLivePackage` logic runs **automatically** whenever the cart is saved (after any cart modification).

#### Step 1: Check for Live Products
```
hasLive = EXISTS cart item WHERE cartItem.ProductModel.IdSubTipo IN [19, 20, 21, 54]
```

#### Step 2: Check for Special Packaging Product
```
hasPackage = EXISTS cart item WHERE cartItem.ProductModel.PKId == 1354
```

#### Step 3: Apply Rules

**Scenario A: Has Live Products AND No Package**
- **Action**: Automatically add special packaging product (ID 1354)
- **Quantity**: Always 1 (regardless of number of live products)
- **Business Reason**: Live products require special packaging for safe transport

**Scenario B: No Live Products AND Has Package**
- **Action**: Automatically remove special packaging product (ID 1354)
- **Business Reason**: Special packaging not needed if no live products

**Scenario C: Has Live Products AND Has Package**
- **Action**: No change (already correct state)
- Package remains in cart

**Scenario D: No Live Products AND No Package**
- **Action**: No change (already correct state)

### When CheckLivePackage Runs

The logic is executed:
1. **After adding item** to cart
2. **After updating quantity** of any item
3. **After removing item** from cart
4. **Before saving cart** to session/storage

### Implementation Notes

- **Automatic**: User does not manually add/remove the packaging product
- **Transparent**: Package product appears/disappears automatically
- **Quantity**: Package product is always quantity 1 (never multiple)
- **Validation**: Package product should be excluded from:
  - Shipment weight calculations (if configured)
  - Product recommendations
  - Regular product listings

---

## 3. Cart Save Process

### Complete Flow

When saving cart, the following sequence occurs:

1. **Validate Cart State**
   - Check for duplicate products (should not occur after AddCartItem logic)
   - Validate quantities (must be > 0)

2. **Execute CheckLivePackage**
   - Verify live products presence
   - Add/remove packaging product as needed

3. **Persist Cart**
   - Save to session/storage
   - Serialize cart model

### Error Handling

- If special packaging product (1354) cannot be found in catalog:
  - Log warning
  - Continue without adding package (do not break cart save)
  - Cart remains functional

---

## 4. Database Schema Requirements

### Product Table Structure

**Table**: `tbProdutos`

**Required Fields**:
- `pkid` (INT, PRIMARY KEY) - Product ID
- `id_subtipo` (VARCHAR/INT) - Product sub-type ID (used to identify live products)
- `peso` (INT) - Product weight in grams
- `preco` (DECIMAL) - Product price
- `estoque` (BIT) - Stock availability flag
- `nome_new` (VARCHAR) - Product name
- `nome_foto` (VARCHAR) - Product image filename
- Other product fields as needed

### Product Type Reference

**Table**: `tbTipoProduto`
- Maps `id_subtipo` to product type descriptions
- Used to identify live product categories

### Cart Storage

Cart data can be stored in:
- **Session Storage** (server-side)
- **LocalStorage** (client-side)
- **Database** (persistent)

**Cart Model Structure**:
```
CartModel
├── CartItems[] (List)
│   └── CartItem
│       ├── ProductModel (ProductId, Name, Price, Weight, etc.)
│       └── Quantity (INT)
└── Other cart metadata (Coupon, etc.)
```

---

## 5. Business Rules Summary

| Rule | Description |
|------|-------------|
| **Duplicate Prevention** | Same product cannot appear twice; quantity is incremented instead |
| **Live Product Detection** | Products with `id_subtipo` in [19, 20, 21, 54] are considered "live" |
| **Auto Package Add** | If cart has live products and no package (ID 1354), add package automatically |
| **Auto Package Remove** | If cart has no live products but has package (ID 1354), remove package automatically |
| **Package Quantity** | Special packaging product is always quantity 1 |
| **Package Exclusion** | Package product (1354) should be excluded from recommendations and shipment weight calculations |

---

## 6. Configuration Constants

### Live Product Type IDs
- **Configurable list**: `[19, 20, 21, 54]`
- **Purpose**: Identifies which product sub-types require special packaging
- **Storage**: Can be stored in configuration file, constants, or database

### Special Packaging Product ID
- **Default Value**: `1354`
- **Purpose**: Product ID representing special packaging fee
- **Behavior**: 
  - Automatically added when live products present
  - Automatically removed when no live products
  - Should not be manually added/removed by users

---

## 7. Example Scenarios

### Scenario 1: Adding First Product (Non-Live)
```
Cart: Empty
Action: Add Product A (id_subtipo = 10)
Result: 
  - Cart has 1 item: Product A (qty: 1)
  - No package added (not live product)
```

### Scenario 2: Adding Duplicate Product
```
Cart: Product A (qty: 2)
Action: Add Product A again
Result:
  - Cart has 1 item: Product A (qty: 3)
  - No new line item created
```

### Scenario 3: Adding Live Product
```
Cart: Product A (id_subtipo = 10, qty: 1)
Action: Add Product B (id_subtipo = 19, live product)
Result:
  - Cart has 2 items: 
    - Product A (qty: 1)
    - Product B (qty: 1)
  - Package Product (ID 1354, qty: 1) automatically added
  - Total: 3 items
```

### Scenario 4: Removing Last Live Product
```
Cart: 
  - Product A (id_subtipo = 10, qty: 1)
  - Product B (id_subtipo = 19, live, qty: 1)
  - Package Product (ID 1354, qty: 1)
Action: Remove Product B
Result:
  - Cart has 1 item: Product A (qty: 1)
  - Package Product automatically removed
  - Total: 1 item
```

### Scenario 5: Updating Live Product Quantity
```
Cart:
  - Product A (id_subtipo = 19, live, qty: 2)
  - Package Product (ID 1354, qty: 1)
Action: Update Product A quantity to 5
Result:
  - Cart has 2 items:
    - Product A (qty: 5)
    - Package Product (ID 1354, qty: 1) - still present
  - Package remains (live product still in cart)
```

---

## 8. Implementation Considerations

### Performance

- **Cart Lookup**: Use efficient search (hash map/dictionary) for O(1) product lookup
- **Live Product Check**: Cache live product type IDs to avoid repeated lookups
- **Package Product**: Cache package product details (fetch once, reuse)

### Data Integrity

- **Validation**: Ensure package product (1354) exists in product catalog
- **Fallback**: If package product not found, log error but don't break cart operations
- **Consistency**: Always check live products before saving cart

### User Experience

- **Transparency**: Package product appears/disappears automatically
- **Notification**: Optional: Show message when package is automatically added/removed
- **Read-Only**: Package product should be marked as read-only in UI (user cannot manually remove)

### Error Scenarios

1. **Package Product Missing**: 
   - Log warning
   - Continue without package
   - Cart remains functional

2. **Invalid Product ID**:
   - Validate product exists before adding
   - Return error if product not found

3. **Cart State Corruption**:
   - Validate cart structure before operations
   - Reset to empty cart if corruption detected

---

## 9. Integration Points

### Shipment Calculation

- **Exclude Package Product**: Package product (1354) should be excluded from:
  - Weight calculations for shipment
  - Product count for recommendations
  - Price calculations (if package is free/add-on)

### Checkout Process

- **Package Validation**: During checkout, verify package is present if live products exist
- **Freight Type**: If freight type is "Local Pickup", package product may need to be removed (implementation-specific)

### Order Creation

- **Package Inclusion**: Package product should be included in final order
- **Order Items**: Package appears as separate line item in order

---

## 10. Testing Checklist

### Add Item Tests

- [ ] Add new product → Creates new cart item
- [ ] Add existing product → Increments quantity
- [ ] Add multiple different products → All appear separately
- [ ] Add same product multiple times → Quantity accumulates correctly

### Live Package Tests

- [ ] Add live product → Package automatically added
- [ ] Add non-live product → No package added
- [ ] Add live + non-live → Package added
- [ ] Remove last live product → Package automatically removed
- [ ] Remove non-live product → Package remains (if live products still present)
- [ ] Update live product quantity → Package remains
- [ ] Update non-live product quantity → Package behavior unchanged

### Edge Cases

- [ ] Cart empty → No package
- [ ] Package product missing from catalog → Graceful handling
- [ ] Multiple live products → Single package (quantity 1)
- [ ] Package product manually added → Should be removed if no live products

---

## 11. Migration Notes

When implementing this logic in a new system:

1. **Identify Live Product Types**: Determine which product sub-types require special packaging
2. **Define Package Product ID**: Set the product ID for special packaging
3. **Implement AddCartItem Logic**: Ensure duplicate products increment quantity
4. **Implement CheckLivePackage**: Add automatic package management
5. **Update Cart Save**: Ensure CheckLivePackage runs on every cart save
6. **Test Scenarios**: Verify all scenarios from section 10

---

## Revision History

- **2025-01-XX**: Initial documentation created
- Based on cart management logic from SITECOM e-commerce platform

