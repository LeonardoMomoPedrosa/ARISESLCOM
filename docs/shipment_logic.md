# Shipment Calculation Logic Documentation

## Overview

This document explains the shipment calculation logic for the Aquanimal e-commerce website. The system supports multiple shipment methods, each with its own pricing rules and business logic.

**Main Shipment Methods:**
- **Transp** (Transportadora) - Primary ground transportation
- **PAC** (Correios PAC) - Postal service for non-live products
- **Aeroporto** (GOL Airlines) - Airport pickup for non-SP states
- **Buslog** - Alternative ground transportation with fixed pricing
- **Cliente Retira** - Local pickup (free)

---

## 1. Weight Calculation

### Process
The system calculates shipment weight based on cart items.

1. **Cart Weight Calculation**
   - Sums all products: `Σ (Quantity × ProductWeight)` in **grams**
   - Each product has a `Peso` property stored in grams

2. **Conversion to Kilograms**:
   - Formula: `weight_kg = CEILING(total_grams / 1000)`
   - Example: 1500 grams = 2 kg, 999 grams = 1 kg

**Important:** The weight is always rounded **up** to the next whole kilogram.

---

## 2. CEP and Location Determination

### CEP Validation
- Format: `^\d{5}-?\d{3}$` (e.g., "12345-678" or "12345678")
- Validation uses `ViaCEP` API for existence check

### State and City Classification
1. **Extract 5-digit prefix** from CEP
2. **Determine State**:
   - Query `tbCEP` table: `SELECT estado WHERE cep_from <= @cep AND cep_to >= @cep`
   - Returns state code (e.g., "SP", "RJ", "MG")

3. **Classify as Capital or Interior**:
   - Query `tbCEPEstadoMetro` to check if CEP is in a metropolitan area
   - **Capital**: If CEP exists in `tbCEPEstadoMetro`
   - **Interior**: If CEP does not exist in `tbCEPEstadoMetro`

---

## 3. Shipment Method: Transp (Transportadora)

**Method Code:** `FRETE_TRANSPORTADORA`

### Availability
- **Always available** for all destinations

### Pricing Logic

The pricing depends on **weight**, **state**, and **location type** (capital vs. interior):

#### Case 1: Weight = 0 kg
- **Amount:** R$ 0.00

#### Case 2: Weight ≤ 30 kg
**For Capital:**
- Query `tbTamex` table: `SELECT TOP 1 preco WHERE estado LIKE @estado AND kg >= @peso ORDER BY kg ASC`
- Amount = Capital price

**For Interior:**
- Amount = Capital price + Interior price
- Capital: Query `tbTamex` (same as above)
- Interior: Query `tbVaspex` table: `SELECT TOP 1 preco WHERE estado LIKE @estado AND kg >= @peso ORDER BY kg ASC`

#### Case 3: Weight > 30 kg
**For Capital:**
```
Base (30kg) = GetCapitalFretePreco(state, 30)
Additional per kg = GetCapitalFretePreco(state, 111)
Amount = Base + (Weight - 30) × Additional
```

**For Interior:**
```
Base (30kg) = GetInteriorFretePreco(state, 30)
Additional per kg = GetInteriorFretePreco(state, 111)
Amount = Base + (Weight - 30) × Additional
```

### Database Tables
- **`tbTamex`**: Capital prices by state and weight
  - Columns: `estado`, `kg`, `preco`
- **`tbVaspex`**: Interior prices by state and weight
  - Columns: `estado`, `kg`, `preco`

### Default Values
- If no price found in database: **R$ 1,500.00**
- **Minimum freight:** R$ 18.80 (applied after calculation)

---

## 4. Shipment Method: PAC (Correios)

**Method Code:** `FRETE_PAC`

### Availability
- **Only available when cart does NOT contain live products** (`HasLive() == false`)
- Live products are identified by product type IDs: `[19, 20, 21, 54]`

### Pricing Logic

1. **Get Weight** (in kg, rounded up)
2. **Call Correios API**:
   - Endpoint: `https://api.correios.com.br/preco/v1/nacional/03298`
   - Parameters:
     - `cepDestino`: Destination CEP
     - `cepOrigem`: Origin CEP (configured: "05448000")
     - `psObjeto`: Weight in kg
   - Service Code: `03298` (PAC)

3. **Parse Response**:
   - Extract `pcFinal` value from API response
   - Handle format conversion:
     - Brazilian format: `"1.647,00"` → `1647.00`
     - Remove thousands separator (`.`), replace decimal separator (`,` → `.`)
   - Parse as decimal using `InvariantCulture`

### Configuration
- **Origin CEP:** `05448000` (from `appsettings.json`)
- **API Base URL:** `https://api.correios.com.br/`
- Requires authentication token (Bearer token)

### Error Handling
- If API call fails, PAC option is **silently excluded** from shipment options
- Exception is caught but not propagated

### Minimum Freight
- **Minimum freight:** R$ 18.80 (applied after API response)

---

## 5. Shipment Method: Aeroporto (GOL Airlines)

**Method Code:** `FRETE_AEROPORTO`

### Availability
- **Only available for destinations OUTSIDE São Paulo state** (UF ≠ "SP")
- Customer must pick up the package at a selected airport location

### Pricing Logic

#### Case 1: Weight ≤ 0 kg
- **Amount:** R$ 0.00

#### Case 2: Weight ≤ 40 kg
- Query `tbGol` table: `SELECT TOP 1 preco WHERE estado LIKE @estado AND estado NOT LIKE '%Adicional%' AND kg >= @peso ORDER BY kg ASC`
- Amount = Base price for weight

#### Case 3: Weight > 40 kg
```
Base (40kg) = GetGolShipmentDBPrice(state, 40)
Additional per kg = GetGolAdditionalDBPrice(state)
Amount = Base + (Weight - 40) × Additional
```

**Additional Price Query:**
- `SELECT TOP 1 preco FROM tbGol WHERE estado LIKE @estado AND estado LIKE '%Adicional%' AND kg = 1`

### Database Tables
- **`tbGol`**: Base prices and additional prices by state
  - Columns: `estado`, `kg`, `preco`
  - Special rows: `estado LIKE '%Adicional%'` for excess weight pricing

### Default Values
- If no price found in database: **R$ 1,500.00**
- **Minimum freight:** R$ 18.80 (applied after calculation)

### Additional Requirements
- Customer must select a specific airport location for pickup
- Airport locations are stored separately and must be selected during checkout

---

## 6. Shipment Method: Buslog

**Method Code:** `FRETE_BUSLOG`

### Availability
- Only available if:
  1. State exists in `tbBusLog` table with a valid price (> 0)
  2. Query returns a non-zero amount

### Pricing Logic

1. **Get Weight** (in kg, rounded up)
2. **Get CEP Information** via Correios API:
   - Endpoint: Correios CEP lookup
   - Extract: `nomeMunicipio` (city name) and `uf` (state)
3. **Query Base Price**:
   - Query `tbBusLog`: `SELECT TOP 1 preco WHERE estado LIKE @estado AND estado NOT LIKE '%Adicional%' AND kg >= CEILING(@peso) ORDER BY kg ASC`
   - Note: Uses `CEILING(@peso)` for weight matching
4. **Determine Delivery Type**:
   - Query `tbbusloglocal`: `SELECT estado WHERE nomeMunicipio COLLATE Latin1_general_CI_AI = @city`
   - **Deliver**: If city exists in `tbbusloglocal` → Direct delivery to customer
   - **Pickup**: If city does NOT exist → Customer must pick up at a Buslog location

### Delivery Types
- **Buslog Deliver** (`BuslogType.Deliver`): Direct delivery to customer address
- **Buslog Pickup** (`BuslogType.Pickup`): Customer picks up at selected Buslog location

### Database Tables
- **`tbBusLog`**: Pricing by state and weight
  - Columns: `estado`, `kg`, `preco`
  - Special rows: `estado LIKE '%Adicional%'` for excess weight (not used in current logic)
- **`tbbusloglocal`**: Cities eligible for direct delivery
  - Column: `nomeMunicipio`

### Important Notes
- If CEP is not found in Correios API, Buslog is **disabled** (`BuslogAllowed = false`)
- City name matching uses case-insensitive collation: `Latin1_general_CI_AI`
- If `amount = 0` after query, Buslog is **disabled**

### Minimum Freight
- **No explicit minimum** applied (unlike other methods)
- Price is used as-is from database (or 0 if not found)

---

## 7. Shipment Method: Cliente Retira (Local Pickup)

**Method Code:** `FRETE_CLIENTE_RETIRA`

### Availability
- **Always available**

### Pricing Logic
- **Amount:** R$ 0.00 (free)

### Special Behavior
- If selected, the system **automatically removes** product ID `1354` (special packaging fee) from the cart
- This product is only added for live products when shipping is required

---

## 8. Minimum Freight Value

### Applied To
- **Transp** (Transportadora)
- **PAC** (Correios)
- **Aeroporto** (GOL)

### Rule
```csharp
if (calculated_amount < 18.80) {
    amount = 18.80;
}
```

**Not Applied To:**
- Buslog (uses database value as-is)
- Cliente Retira (always free)

---

## 9. Shipment Options Generation Flow

The `GetShipmentOptions(string cep)` method returns a list of available shipment methods:

### Order of Addition
1. **Transp** (Transportadora) - Always added first
2. **PAC** (Correios) - Added if `!HasLive()` (no live products)
3. **Aeroporto** - Added if destination state ≠ "SP"
4. **Buslog** - Added if `BuslogAllowed == true`
5. **Cliente Retira** - Always added last

### Special Cases
- PAC failures are silently ignored (exception caught)
- Buslog is excluded if CEP lookup fails or price is 0

---

## 10. Caching Strategy

All database queries are cached using `ICacheRegionService`:

### Cache Keys
- Capital Frete: `SiteCacheKeyUtil.GetCapitalFretePrecoKey(estado, peso)`
- Interior Frete: `SiteCacheKeyUtil.GetInteriorFretePrecoKey(estado, peso)`
- GOL Frete: `SiteCacheKeyUtil.GetGolShipmentPriceKey(estado, peso)`
- Buslog Frete: `SiteCacheKeyUtil.GetBuslogShipmentPriceKey(estado, peso)`
- CEP Estado: `SiteCacheKeyUtil.GetCEPEstadoKey(cepPart)`
- Buslog Delivery: `SiteCacheKeyUtil.GetDeliveryBuslogIndKey(city)`

### Cache Duration
- **24 hours (1440 minutes)** for all shipment-related queries
- **Correios API responses**: Cached per CEP + weight combination

### Cache Invalidation
- Manual cache clearing or expiration after 24 hours

---

## 11. Business Rules Summary

| Rule | Description |
|------|-------------|
| **Live Products** | If cart contains live products (types 19, 20, 21, 54), PAC is excluded |
| **SP State** | Aeroporto is excluded for São Paulo destinations |
| **Weight Calculation** | Always rounded UP to next whole kilogram |
| **Minimum Freight** | R$ 18.80 for Transp, PAC, and Aeroporto |
| **Special Packaging** | Product 1354 is automatically added for live products (unless Cliente Retira) |
| **Buslog Availability** | Only available if state exists in `tbBusLog` with price > 0 |
| **CEP Validation** | Must be valid format and exist in ViaCEP/ViaCEP APIs |

---

## 12. Database Schema Reference

### Key Tables

#### `tbTamex`
- **Purpose:** Capital city freight prices
- **Columns:** `estado`, `kg`, `preco`

#### `tbVaspex`
- **Purpose:** Interior city freight prices
- **Columns:** `estado`, `kg`, `preco`

#### `tbGol`
- **Purpose:** Airport (GOL Airlines) freight prices
- **Columns:** `estado`, `kg`, `preco`
- **Special:** Rows with `estado LIKE '%Adicional%'` for excess weight

#### `tbBusLog`
- **Purpose:** Buslog freight prices
- **Columns:** `estado`, `kg`, `preco`
- **Special:** Rows with `estado LIKE '%Adicional%'` (currently unused)

#### `tbbusloglocal`
- **Purpose:** Cities eligible for Buslog direct delivery
- **Columns:** `nomeMunicipio`

#### `tbCEP`
- **Purpose:** CEP to State mapping
- **Columns:** `cep_from`, `cep_to`, `estado`

#### `tbCEPEstadoMetro`
- **Purpose:** CEP to Metropolitan area mapping
- **Columns:** CEP range fields

---

## 13. API Integrations

### Correios API
- **Service:** Correios PAC price calculation
- **Authentication:** Bearer token
- **Endpoint:** `/preco/v1/nacional/03298`
- **Caching:** Yes (per CEP + weight)

### ViaCEP API
- **Service:** CEP validation and address lookup
- **Authentication:** None (public API)
- **Endpoint:** ViaCEP public API
- **Usage:** CEP existence validation

---

## 14. Example Calculations

### Example 1: Transp to Capital, 25 kg
```
State: RJ
Location: Capital
Weight: 25 kg

Query: tbTamex WHERE estado LIKE 'RJ' AND kg >= 25
Result: R$ 85.00 (example)

Final: MAX(R$ 85.00, R$ 18.80) = R$ 85.00
```

### Example 2: Transp to Interior, 35 kg
```
State: MG
Location: Interior
Weight: 35 kg

Base (30kg): Interior = R$ 100.00
Additional per kg: R$ 5.00
Excess: 35 - 30 = 5 kg

Calculation: R$ 100.00 + (5 × R$ 5.00) = R$ 125.00
Final: MAX(R$ 125.00, R$ 18.80) = R$ 125.00
```

### Example 3: Aeroporto, 45 kg
```
State: BA
Weight: 45 kg

Base (40kg): R$ 120.00
Additional per kg: R$ 8.00
Excess: 45 - 40 = 5 kg

Calculation: R$ 120.00 + (5 × R$ 8.00) = R$ 160.00
Final: MAX(R$ 160.00, R$ 18.80) = R$ 160.00
```
