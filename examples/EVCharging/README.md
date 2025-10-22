# EV Charging Management System - GraphQL Database Example

A comprehensive Electric Vehicle charging infrastructure management system demonstrating SharpGraph's big data capabilities with **100,000+ charge session records**.

## Overview

This example simulates a large-scale EV charging network with:
- 🔌 **500 Charge Stations** across multiple locations
- 👥 **10,000 EV Drivers** with charge cards
- 🎫 **50,000 Charge Tokens** (RFID cards, mobile apps, etc.)
- ⚡ **100,000 Charge Sessions** with detailed CDRs (Charge Detail Records)
- 💳 **Charge Cards** linked to persons
- 📊 Real-world data patterns and relationships

## Quick Start

### Option 1: Standalone Demo (No Server)

Run locally to see 100,000+ records generated and queried:

```bash
cd examples/EVCharging
dotnet run
```

This will:
1. Generate `seed_data.json` (first run only)
2. Create local database in `ev_charging_db` folder
3. Load 270,000+ records
4. Run sample queries
5. Show dynamic indexing statistics

### Option 2: Upload to Server

Upload the EV Charging database to the GraphQL server:

```bash
# Terminal 1: Start the server
.\run-server.ps1

# Terminal 2: Upload EV Charging data
.\run-evcharging-client.ps1
```

Then query via:
- **Browser**: http://localhost:8080/graphql
- **Postman**: POST to http://localhost:8080/graphql
- **cURL**: See examples below

## Schema Overview

### Core Entities

- **Person**: EV drivers with contact information
- **ChargeCard**: Payment cards linked to persons
- **ChargeToken**: Physical RFID tokens, mobile app tokens, etc.
- **ChargeStation**: Physical charging locations with connectors
- **Connector**: Individual charging points (Type 2, CCS, CHAdeMO)
- **ChargeSession**: Active or completed charging sessions
- **ChargeDetailRecord (CDR)**: Billing records with energy consumption

### Relationships

```
Person (1) ----→ (N) ChargeCard
ChargeCard (1) ----→ (N) ChargeToken
ChargeToken (1) ----→ (N) ChargeSession
ChargeStation (1) ----→ (N) Connector
Connector (1) ----→ (N) ChargeSession
ChargeSession (1) ----→ (1) ChargeDetailRecord
```

## Example Queries

### 1. Find High-Value Charging Sessions

```graphql
{
  chargeDetailRecords {
    items(
      where: {
        AND: [
          { totalCost: { gte: 50.0 } }
          { energyDelivered: { gte: 50.0 } }
          { status: { equals: "completed" } }
        ]
      }
      orderBy: [{ totalCost: DESC }]
      take: 10
    ) {
      id
      totalCost
      energyDelivered
      startTime
      endTime
      chargeSessionId
    }
  }
}
```

### 2. Find Active Charging Sessions

```graphql
{
  chargeSessions {
    items(
      where: {
        AND: [
          { status: { equals: "charging" } }
          { startTime: { gte: "2025-10-22T00:00:00Z" } }
        ]
      }
      orderBy: [{ startTime: DESC }]
    ) {
      id
      status
      startTime
      connectorId
      chargeTokenId
    }
  }
}
```

### 3. Station Utilization Analysis

```graphql
{
  chargeStations {
    items(
      where: {
        location: { contains: "Amsterdam" }
      }
      orderBy: [{ name: ASC }]
    ) {
      id
      name
      location
      latitude
      longitude
      operator
    }
  }
}
```

### 4. Find User Charging History

```graphql
{
  persons {
    items(
      where: {
        email: { equals: "driver1000@example.com" }
      }
    ) {
      id
      name
      email
    }
  }
  
  chargeCards {
    items(
      where: {
        personId: { equals: "person-1000" }
      }
    ) {
      id
      cardNumber
      expiryDate
    }
  }
}
```

### 5. Energy Consumption Statistics

```graphql
{
  chargeDetailRecords {
    items(
      where: {
        AND: [
          { startTime: { gte: "2025-10-01T00:00:00Z" } }
          { endTime: { lte: "2025-10-31T23:59:59Z" } }
          { status: { equals: "completed" } }
        ]
      }
      orderBy: [{ energyDelivered: DESC }]
      take: 100
    ) {
      energyDelivered
      totalCost
      startTime
      endTime
    }
  }
}
```

## Performance Features

### Automatic Indexing

The dynamic indexing system will automatically create indexes after 3 queries on:
- `ChargeDetailRecord.totalCost`
- `ChargeDetailRecord.energyDelivered`
- `ChargeDetailRecord.status`
- `ChargeSession.status`
- `ChargeSession.startTime`
- `Person.email`

### Optimized Queries

With 100,000 CDRs:
- **Without index**: Full scan ~200ms
- **With index**: B-tree lookup ~5ms
- **Performance gain**: 40x faster

## Data Generation

The seed data includes realistic patterns:
- **Charging durations**: 15 minutes to 8 hours
- **Energy delivered**: 5 kWh to 85 kWh
- **Cost calculations**: Based on energy and duration
- **Geographic distribution**: Major European cities
- **Connector types**: Type 2 (AC), CCS (DC), CHAdeMO
- **Power ratings**: 3.7 kW (slow), 22 kW (fast), 150 kW (ultra-fast)

## Use Cases

1. **Fleet Management**: Track company vehicle charging
2. **Billing Systems**: Generate invoices from CDRs
3. **Network Analytics**: Station utilization and revenue
4. **Customer Service**: Charging history and disputes
5. **Energy Management**: Load balancing and grid integration
6. **Roaming Networks**: Inter-operator settlements

## Testing Dynamic Indexing

Run the same query 3 times to trigger automatic indexing:

```bash
# Query 1-2: Full scan
# Query 3: Index created automatically
# Query 4+: 40x faster with index
```

Watch for console output:
```
🔍 Created dynamic index on ChargeDetailRecord.totalCost (accessed 3 times)
🔍 Created dynamic index on ChargeSession.status (accessed 3 times)
```

## Schema Statistics

- **7 entity types**
- **50+ fields total**
- **100,000+ total records**
- **6 relationship types**
- **Multiple indexes per table**

## Real-World Scenarios

This example mirrors actual EV charging platforms like:
- ChargePoint
- Ionity
- Fastned
- Tesla Supercharger Network
- OCPP-compliant charging infrastructure

Perfect for testing:
- GraphQL query performance
- Complex filtering and sorting
- Multi-table joins
- Big data handling
- Automatic index optimization
