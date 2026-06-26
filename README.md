# Ledger-Core

## Enterprise Financial Ledger Platform

**Engineered by Super Washington Banga**

---

## Overview

Ledger-Core is an enterprise-oriented financial ledger platform built on .NET 10 and designed around immutable double-entry accounting principles.

The platform explores how modern financial infrastructure can be engineered using Clean Architecture, Domain-Driven Design (DDD), CQRS, distributed systems patterns, and cloud-native deployment practices.

Ledger-Core is intended to provide a foundational financial engine capable of supporting:

* Digital Banking Platforms
* Mobile Money Systems
* Merchant Payment Networks
* Agency Banking Solutions
* Payment Gateways
* Cross-Border Payment Platforms
* Banking-as-a-Service (BaaS)
* Savings Platforms
* Investment Platforms
* Treasury Systems
* Settlement Networks
* Financial Super Applications

The platform is built around a single foundational rule:

> The ledger is the source of truth.

All balances, statements, settlements, fees, taxes, and financial positions are derived from immutable ledger transactions.

No balance is treated as the authoritative financial record.

---

# Current Status

**Version:** 1.0

Current validation status:

| Category                       | Status          |
| ------------------------------ | --------------- |
| Build Validation               | Passing         |
| Integration Tests              | 26 / 26 Passing |
| Architecture Tests             | Passing         |
| Double-Entry Enforcement       | Verified        |
| Optimistic Concurrency Control | Verified        |
| Distributed Idempotency        | Verified        |
| Transaction Atomicity          | Verified        |
| Containerized Deployment       | Verified        |

Ledger-Core v1.0 establishes a mathematically consistent and concurrency-safe financial core suitable for pilot deployments, architecture demonstrations, and fintech MVP environments.

---

# Core Architectural Principles

## 1. Ledger-Centric Design

The ledger is the only authoritative financial record.

All balances and financial states are projections derived from ledger entries.

The system does not store mutable balances as financial truth.

---

## 2. Double-Entry Accounting

Every financial event must balance.

For every debit there must be an equal and corresponding credit.

Invariant:

```text
Sum(All Ledger Entries Within Transaction) = 0
```

Any imbalance causes transaction posting to fail.

---

## 3. Immutable Financial History

Financial history is append-only.

Allowed operations:

* Insert LedgerTransaction
* Insert LedgerEntry
* Insert ReversalTransaction

Forbidden operations:

* Update LedgerTransaction
* Delete LedgerTransaction
* Update LedgerEntry
* Delete LedgerEntry

Corrections are performed through reversal transactions.

---

## 4. Clean Architecture

Dependencies always flow inward.

```text
API
 │
 ▼
Application
 │
 ▼
Domain
 │
 ▼
Infrastructure
```

Business rules remain isolated from frameworks, databases, and external systems.

---

## 5. Domain-Driven Design

The Domain Layer owns business truth.

The domain is responsible for:

* Financial rules
* Ledger invariants
* Account behavior
* Transaction validation
* Regulatory constraints

Infrastructure cannot define business rules.

---

## 6. Security by Design

Every request must pass through validation before execution.

```text
Authentication
    ↓
Authorization
    ↓
Validation
    ↓
Idempotency
    ↓
Business Rules
    ↓
Transaction Execution
    ↓
Outbox Publication
```

---

# Architecture

## High-Level System Design

```text
Clients
   │
   ▼
LedgerCore.Api
   │
   ▼
LedgerCore.Application
   │
   ▼
LedgerCore.Domain
   │
   ▼
LedgerCore.Infrastructure
   │
 ┌─┼─────────────┐
 ▼ ▼             ▼
SQL Server     Redis     RabbitMQ
```

---

# Solution Structure

```text
Ledger-Core

src/
├── LedgerCore.Domain
├── LedgerCore.Application
├── LedgerCore.Infrastructure
└── LedgerCore.Api

tests/
├── LedgerCore.UnitTests
├── LedgerCore.IntegrationTests
├── LedgerCore.ArchitectureTests
└── LedgerCore.PerformanceTests
```

---

# Technology Stack

| Area                | Technology            |
| ------------------- | --------------------- |
| Platform            | .NET 10               |
| Language            | C#                    |
| API                 | ASP.NET Core          |
| Architecture        | Clean Architecture    |
| Patterns            | CQRS, DDD             |
| Mediator            | MediatR               |
| Validation          | FluentValidation      |
| Database            | Azure SQL Edge        |
| ORM                 | Entity Framework Core |
| Cache               | Redis                 |
| Messaging           | RabbitMQ              |
| Logging             | Serilog               |
| Telemetry           | OpenTelemetry         |
| Containers          | Docker                |
| Testing             | xUnit                 |
| Concurrency Control | RowVersion OCC        |

---

# Domain Model

## Account

Represents a financial ledger account.

Key properties:

* Id
* AccountNumber
* AccountType
* Status
* Currency
* KycTier
* RowVersion

Balances are projections.

No mutable balance field exists.

---

## Money

Immutable value object.

Rules:

* Currency aware
* Immutable
* Prevents invalid arithmetic
* Enforces domain validation

---

## LedgerTransaction

Aggregate root responsible for:

* Double-entry validation
* Currency consistency
* Transaction posting
* Domain event generation

---

## LedgerEntry

Represents an individual debit or credit movement within a transaction.

Rules:

* Cannot be zero-value
* Must belong to a transaction
* Must participate in a balanced transaction

---

# Financial Guarantees

The following invariants are enforced by the domain model.

### Invariant 1

A transaction must contain at least two entries.

### Invariant 2

Every transaction must balance.

```text
Sum(Entries) = 0
```

### Invariant 3

Zero-value entries are prohibited.

### Invariant 4

Mixed currencies are prohibited within a transaction.

### Invariant 5

Financial operations are atomic.

A transaction either commits entirely or rolls back entirely.

### Invariant 6

Concurrency conflicts result in rejection rather than silent overwrite.

---

# Reliability & Consistency

## Optimistic Concurrency Control

Ledger-Core uses SQL Server RowVersion tokens.

Benefits:

* Prevents double spending
* Prevents lost updates
* Detects concurrent modifications

```text
Read
 ↓
Validate
 ↓
Save
 ↓
Detect Conflict
 ↓
Retry or Reject
```

---

## Distributed Idempotency

Duplicate requests are blocked using Redis-backed idempotency controls.

```text
Receive Request
      ↓
Check Idempotency Key
      ↓
Acquire Distributed Lock
      ↓
Execute Once
      ↓
Cache Result
```

This protects against:

* Network retries
* Client resubmissions
* Duplicate posting

---

## Outbox Pattern

Successful transactions generate domain events.

```text
Transaction
    ↓
Outbox Table
    ↓
Background Processor
    ↓
RabbitMQ
```

This ensures reliable event publication.

---

# Local Development & Deployment

The platform is designed as a containerized development environment that closely mirrors the intended cloud deployment topology.

## Prerequisites

* Docker Engine
* Docker Compose
* Git

## Environment Configuration

Create a local environment file from the provided template:

```bash
cp .env.example .env
```

Update the values with secure credentials before starting the platform.

## Start the Platform

```bash
docker compose up --build -d
```

This launches:

* Ledger API
* Azure SQL Edge
* Redis
* RabbitMQ

## Verify Services

```bash
docker ps
```

## Access Documentation

Interactive API documentation:

```text
http://localhost:5000/
```

Health endpoint:

```text
http://localhost:5000/health/live
```

---

# Roadmap

## Version 1.1 — Operational Hardening

* OpenID Connect Integration
* Azure Entra ID Integration
* Refresh Token Rotation
* Dead Letter Queues
* Retry Policies
* Rate Limiting
* Threat Protection Controls
* Application Insights Integration
* Distributed Tracing
* Outbox Resilience Testing

## Version 2.0 — Scale & Intelligence

* Event-Driven Read Models
* Account Balance Projections
* Multi-Tenant Banking Platform
* Database Partitioning
* Horizontal Sharding
* AI-Assisted Fraud Detection
* Behavioral Risk Analysis
* Geographic Anomaly Detection
* Banking-as-a-Service Enablement

---

# Long-Term Vision

Ledger-Core began as an engineering exploration into how modern financial infrastructure can be built using strong accounting guarantees, resilient distributed systems, and cloud-native architecture.

The long-term objective is to evolve Ledger-Core into a platform capable of supporting a broad financial ecosystem that lowers barriers to access and reduces the cost of financial services.

Potential future applications include:

* Payments
* Savings
* Investments
* Digital Wallets
* Merchant Services
* Lending Platforms
* Insurance Integrations
* Wealth Management
* Capital Markets Access
* Treasury Operations
* Cross-Border Transfers
* Banking-as-a-Service
* Financial Super Applications

The ambition is to provide a robust financial foundation upon which individuals, businesses, fintechs, and institutions can build secure, scalable, accessible, and economically efficient financial services.

---

# Guiding Principle

> Balances are projections.
>
> Ledger entries are truth.
>
> Every financial state change must originate from a balanced, immutable, append-only ledger transaction.

Any feature, optimization, or architectural decision that violates this principle is considered architecturally invalid.
