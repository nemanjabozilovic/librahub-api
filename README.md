# LibraHub

A modern microservices-based digital library management platform designed to facilitate book cataloging, purchasing, content delivery, and user engagement through a distributed, event-driven architecture.

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Technology Stack](#technology-stack)
- [Project Structure](#project-structure)
- [Services](#services)
- [Key Features](#key-features)
- [Getting Started](#getting-started)
- [Documentation](#documentation)
- [Development](#development)

## Overview

LibraHub is a distributed system built using microservices architecture principles, implementing Clean Architecture and Domain-Driven Design (DDD) patterns. The platform enables users to browse a digital book catalog, purchase books, manage their personal library, track reading progress, and receive notifications about new releases and announcements.

The system is designed with scalability, maintainability, and resilience in mind, utilizing asynchronous event-driven communication between services to ensure loose coupling and high availability.

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Client Applications                      │
│         (Web Application, Mobile App, API Clients)           │
└────────────────────────────┬────────────────────────────────┘
                             │
                             │ HTTPS
                             │
┌────────────────────────────▼────────────────────────────────┐
│                  API Gateway (YARP)                         │
│         Routing | Authentication | Load Balancing            │
└────────────────────────────┬────────────────────────────────┘
                             │
        ┌────────────────────┼────────────────────┐
        │                    │                    │
        ▼                    ▼                    ▼
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│   Identity   │    │   Catalog    │    │   Orders     │
│   Service    │    │   Service    │    │   Service    │
└──────┬───────┘    └──────┬───────┘    └──────┬───────┘
       │                   │                   │
       │                   │                   │
       ▼                   ▼                   ▼
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│   Library    │    │   Content    │    │Notifications │
│   Service    │    │   Service    │    │   Service    │
└──────┬───────┘    └──────┬───────┘    └──────┬───────┘
       │                   │                   │
       └───────────────────┼───────────────────┘
                           │
                           │ RabbitMQ (Events)
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                    Infrastructure                            │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  │
│  │PostgreSQL│  │ RabbitMQ │  │  Redis   │  │  MinIO   │  │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Clean Architecture Layers

Each microservice follows Clean Architecture principles with the following layers:

- **API Layer** (`LibraHub.{Service}.Api`): Controllers, DTOs, Middleware, and HTTP endpoints
- **Application Layer** (`LibraHub.{Service}.Application`): Use cases, Commands, Queries, Handlers, and Validators
- **Domain Layer** (`LibraHub.{Service}.Domain`): Entities, Value Objects, Domain Logic, and Domain Events
- **Infrastructure Layer** (`LibraHub.{Service}.Infrastructure`): Repositories, DbContext, External Services, and Background Workers

### Architectural Patterns

- **Microservices Architecture**: Independent, loosely coupled services with separate databases
- **Event-Driven Architecture**: Asynchronous communication via RabbitMQ using domain events
- **CQRS (Command Query Responsibility Segregation)**: Separation of read and write operations using MediatR
- **Outbox Pattern**: Guaranteed event delivery through transactional outbox
- **Idempotency Pattern**: Protection against duplicate requests using idempotency keys
- **API Gateway Pattern**: Centralized entry point using YARP (Yet Another Reverse Proxy)

## Technology Stack

### Core Technologies

- **.NET 8.0**: Primary development framework (LTS version)
- **ASP.NET Core 8.0**: Web framework for building RESTful APIs
- **Entity Framework Core 8.0**: Object-relational mapping framework
- **PostgreSQL 16**: Primary relational database for all services
- **RabbitMQ**: Message broker for event-driven communication
- **Redis**: In-memory cache and session store
- **MinIO**: S3-compatible object storage for digital content

### Supporting Technologies

- **YARP (Yet Another Reverse Proxy)**: API Gateway implementation
- **MediatR**: Mediator pattern implementation for CQRS
- **FluentValidation**: Validation framework with fluent API
- **JWT (JSON Web Tokens)**: Authentication and authorization
- **SignalR**: Real-time notifications
- **Docker & Docker Compose**: Containerization and orchestration
- **OpenTelemetry**: Observability and distributed tracing
- **FluentEmail**: Email service integration

## Project Structure

```
LibraHub/
│
├── services/                          # Microservices
│   ├── Identity/                      # Authentication & Authorization
│   ├── Catalog/                       # Book Catalog Management
│   ├── Content/                       # Digital Content Management
│   ├── Orders/                        # Order & Payment Processing
│   ├── Library/                       # User Library & Entitlements
│   ├── Notifications/                 # Notification System
│   └── Gateway/                       # API Gateway
│
├── shared/                            # Shared Code
│   ├── BuildingBlocks/                # Common Building Blocks
│   │   ├── Abstractions/              # Interfaces
│   │   ├── Auth/                      # Authentication
│   │   ├── Messaging/                 # RabbitMQ Integration
│   │   ├── Outbox/                    # Outbox Pattern
│   │   ├── Storage/                   # MinIO Integration
│   │   └── ...
│   │
│   └── Contracts/                    # Event Contracts
│       ├── Identity/
│       ├── Catalog/
│       ├── Orders/
│       ├── Library/
│       └── Content/
│
├── infra/                             # Infrastructure Scripts
│   └── scripts/
│       ├── init-local.ps1             # Windows initialization
│       ├── init-local.sh              # Linux/Mac initialization
│       ├── stop-local.ps1             # Windows stop script
│       └── stop-local.sh              # Linux/Mac stop script
│
├── docs/                              # Documentation
│   ├── UML_*.puml                     # UML Diagrams
│   └── UML_*_README.md                # Service Documentation
│
├── docker-compose.yml                 # Infrastructure services
├── global.json                        # .NET SDK version
├── LibraHub.sln                      # Solution file
└── README.md                          # This file
```

## Services

### Identity Service

Manages user authentication, authorization, and user profile management.

**Key Features:**
- User registration and authentication
- JWT token generation and refresh
- Email verification
- Password reset functionality
- User profile management
- Role-based access control (Admin, Librarian, User)
- User management for administrators

**Database:** `librahub_identity`

**Port:** `60950`

### Catalog Service

Manages the book catalog, pricing, and announcements.

**Key Features:**
- Book CRUD operations
- Book search and filtering
- Pricing policy management
- Promotional pricing
- Announcement management
- Book status lifecycle (Draft, Published, Unlisted, Removed)
- Statistics and analytics

**Database:** `librahub_catalog`

**Port:** `60960`

### Content Service

Handles digital content storage and delivery.

**Key Features:**
- Book cover upload and retrieval
- Book edition upload (PDF, EPUB, etc.)
- Content streaming with access tokens
- Access control and validation
- Object storage integration (MinIO)

**Database:** `librahub_content`

**Port:** `60970`

### Orders Service

Manages order processing and payment integration.

**Key Features:**
- Order creation and management
- Payment gateway integration
- Payment processing (initiate, capture)
- Order cancellation
- Refund processing
- Order statistics

**Database:** `librahub_orders`

**Port:** `60980`

### Library Service

Manages user entitlements and reading progress.

**Key Features:**
- Entitlement management (grant, revoke)
- Reading progress tracking
- Personal library management
- Book snapshot synchronization
- Access control validation

**Database:** `librahub_library`

**Port:** `60990`

### Notifications Service

Handles user notifications via multiple channels.

**Key Features:**
- In-app notifications
- Email notifications
- Real-time notifications (SignalR)
- Notification preferences management
- Notification history

**Database:** `librahub_notifications`

**Port:** `61000`

### Gateway Service

Centralized API Gateway for routing and authentication.

**Key Features:**
- Request routing to backend services
- JWT authentication and authorization
- CORS policy management
- Load balancing
- Request/response transformation
- Swagger documentation aggregation

**Port:** `5000`

## Key Features

### Business Capabilities

1. **User Management**
   - Secure authentication with JWT tokens
   - Role-based access control
   - User profile management
   - Email verification

2. **Book Catalog**
   - Advanced search and filtering
   - Book metadata management
   - Pricing and promotional management
   - Announcement system

3. **Content Delivery**
   - Secure content streaming
   - Multiple format support (PDF, EPUB)
   - Access token-based authorization
   - Cover image management

4. **Order Processing**
   - Multi-item order creation
   - Payment gateway integration
   - Order lifecycle management
   - Refund processing

5. **Library Management**
   - Entitlement tracking
   - Reading progress synchronization
   - Personal library organization
   - Access validation

6. **Notifications**
   - Multi-channel notifications (Email, In-App, Real-Time)
   - User preference management
   - Event-driven notification triggers

### Technical Features

- **Event-Driven Communication**: Asynchronous messaging via RabbitMQ
- **Distributed Transactions**: Saga pattern for cross-service operations
- **Idempotency**: Protection against duplicate operations
- **Observability**: Distributed tracing with OpenTelemetry
- **Health Checks**: Service health monitoring
- **API Documentation**: Swagger/OpenAPI for all services
- **Containerization**: Docker-based deployment

## Getting Started

### Prerequisites

- **.NET 8.0 SDK** (or later)
- **Docker Desktop** (for infrastructure services)
- **PowerShell** (Windows) or **Bash** (Linux/Mac)
- **Git** (for cloning the repository)

### Quick Start

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd LibraHub
   ```

2. **Initialize and start all services**

   **Windows (PowerShell):**
   ```powershell
   .\infra\scripts\init-local.ps1
   ```

   **Linux/Mac (Bash):**
   ```bash
   ./infra/scripts/init-local.sh
   ```

   This script will:
   - Start infrastructure services (PostgreSQL, RabbitMQ, Redis, MinIO, pgAdmin, Papercut SMTP)
   - Build and start all microservices
   - Wait for services to be ready

3. **Verify services are running**

   Access the following endpoints:
   - **API Gateway**: http://localhost:5000
   - **Identity Service**: http://localhost:60950
   - **Catalog Service**: http://localhost:60960
   - **Content Service**: http://localhost:60970
   - **Orders Service**: http://localhost:60980
   - **Library Service**: http://localhost:60990
   - **Notifications Service**: http://localhost:61000

   Infrastructure services:
   - **RabbitMQ Management**: http://localhost:15672 (username: `librahub_mq`, password: `R@bb1tMQ_L1br@Hub_2026!S3cur3_P@ss`)
   - **pgAdmin**: http://localhost:5050 (email: `admin@librahub.com`, password: `admin`)
   - **MinIO Console**: http://localhost:9001 (username: `minioadmin`, password: `minioadmin`)
   - **Papercut SMTP**: http://localhost:8082

### Manual Setup

If you prefer to set up services manually:

1. **Start infrastructure services**
   ```bash
   docker-compose up -d
   ```

2. **Start individual services**

   Each service has its own `docker-compose.yml` file:
   ```bash
   cd services/Identity
   docker-compose up -d
   ```

   Repeat for each service (Catalog, Content, Orders, Library, Notifications, Gateway).

3. **Run database migrations**

   Migrations are automatically applied when services start. To manually run migrations:
   ```bash
   dotnet ef database update --project services/Identity/src/LibraHub.Identity.Infrastructure
   ```

### Stopping Services

**Windows (PowerShell):**
```powershell
.\infra\scripts\stop-local.ps1
```

**Linux/Mac (Bash):**
```bash
./infra/scripts/stop-local.sh
```

Or manually:
```bash
docker-compose down
```

### Viewing Logs

View logs for a specific service:
```bash
docker-compose logs -f [service-name]
```

For example:
```bash
docker-compose logs -f catalog-api
```

## Development

### Building the Solution

```bash
dotnet build LibraHub.sln
```

### Running Tests

```bash
dotnet test LibraHub.sln
```

### Code Structure

Each service follows Clean Architecture:

```
LibraHub.{Service}/
├── Api/                    # Controllers, DTOs, Middleware
├── Application/            # Commands, Queries, Handlers
├── Domain/                 # Entities, Value Objects, Events
└── Infrastructure/         # Repositories, DbContext, Clients
```

### Database Migrations

Create a new migration:
```bash
dotnet ef migrations add MigrationName --project services/{Service}/src/LibraHub.{Service}.Infrastructure
```

Apply migrations:
```bash
dotnet ef database update --project services/{Service}/src/LibraHub.{Service}.Infrastructure
```

### Environment Variables

Services use environment variables for configuration. Key variables:

- `ConnectionStrings__{Service}Db`: Database connection string
- `ConnectionStrings__RabbitMQ`: RabbitMQ connection string
- `ConnectionStrings__Redis`: Redis connection string
- `Storage__Endpoint`: MinIO endpoint
- `Storage__AccessKey`: MinIO access key
- `Storage__SecretKey`: MinIO secret key

### API Documentation

Each service exposes Swagger documentation at:
- `http://localhost:{port}/swagger`

The Gateway aggregates all service documentation at:
- `http://localhost:5000/swagger`