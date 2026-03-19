# STEMify Backend - Comprehensive STEM Education Platform

[![Build and Push Docker Images](https://github.com/Capstone-STEMify/STEMify-Backend/actions/workflows/docker-publish.yml/badge.svg)](https://github.com/Capstone-STEMify/STEMify-Backend/actions/workflows/docker-publish.yml)
[![Azure Deployment](https://github.com/Capstone-STEMify/STEMify-Backend/actions/workflows/azure-pro.yml/badge.svg)](https://github.com/Capstone-STEMify/STEMify-Backend/actions/workflows/azure-pro.yml)
![MIT License](https://img.shields.io/badge/License-MIT-yellow.svg)

![.NET 8](https://img.shields.io/badge/.NET-8-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Python](https://img.shields.io/badge/Python-3.11-3776AB?style=for-the-badge&logo=python&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-5C2D91?style=for-the-badge&logo=dot-net&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-316192?style=for-the-badge&logo=postgresql&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-DC382D?style=for-the-badge&logo=redis&logoColor=white)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-FF6600?style=for-the-badge&logo=rabbitmq&logoColor=white)
![gRPC](https://img.shields.io/badge/gRPC-00ADD8?style=for-the-badge&logo=grpc&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)
![Azure](https://img.shields.io/badge/Azure-0078D4?style=for-the-badge&logo=microsoft-azure&logoColor=white)
![Aspire](https://img.shields.io/badge/Aspire-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)

> **A comprehensive, enterprise-grade STEM education platform backend built with .NET 8 microservices architecture, featuring AI-powered insights, e-commerce capabilities, and cutting-edge cloud-native technologies.**

## 📑 Table of Contents

- [Project Overview](#-project-overview)
- [Technologies - Libraries](#technologies---libraries)
- [Application Architecture](#application-architecture)
- [Architecture Overview](#architecture-overview)
- [Core Services Architecture](#core-services-architecture)
- [Building Blocks](#building-blocks)
- [API Gateway & Communication](#api-gateway--communication)
- [Data Architecture](#data-architecture)
- [Deployment & Infrastructure](#deployment--infrastructure)
- [CI/CD Pipeline](#cicd-pipeline)
- [Testing Strategy](#testing-strategy)
- [Security Implementation](#security-implementation)
- [Monitoring & Observability](#monitoring--observability)
- [Getting Started](#getting-started)
- [Project Structure](#-project-structure)
- [Configuration Management](#configuration-management)
- [Advanced Features](#-advanced-features)
- [Contributing](#-contributing)
- [Documentation](#-documentation)
- [License](#-license)
- [Team](#-team)
- [Educational Impact](#-educational-impact)

##  Project Overview

STEMify is a revolutionary educational platform designed to transform STEM (Science, Technology, Engineering, and Mathematics) learning experiences. This backend provides a robust, scalable microservices architecture that powers interactive classrooms, comprehensive user management, educational content delivery, AI-powered analytics, e-commerce functionality, and real-time collaboration tools.

### 🌟 Key Features
- **Multi-tenant user management** with role-based access control and bulk provisioning
- **Interactive virtual classrooms** with real-time collaboration
- **Comprehensive course management** with structured learning paths
- **AI-powered insights** with RAG (Retrieval-Augmented Generation) for class and student analytics
- **E-commerce platform** with product catalog, shopping cart, orders, and payments
- **Advanced notification system** with multiple delivery channels
- **3D assembly emulator** for hands-on learning experiences
- **Real-time progress tracking** and analytics
- **Scalable infrastructure** designed for educational institutions
- **Cloud-native deployment** with Azure Container Apps and Aspire

## Technologies - Libraries

### **Core Frameworks & Languages**
- ✔️ **[`.NET 8`](https://dotnet.microsoft.com/download)** - .NET Framework and .NET Core, including ASP.NET and ASP.NET Core
- ✔️ **[`Python 3.11`](https://www.python.org/downloads/)** - Python programming language for AI Service
- ✔️ **[`ASP.NET Core`](https://dotnet.microsoft.com/apps/aspnet)** - Web framework for building web APIs and web applications
- ✔️ **[`FastAPI`](https://fastapi.tiangolo.com/)** - Modern, fast web framework for building APIs with Python

### **Databases & Storage**
- ✔️ **[`PostgreSQL 17`](https://www.postgresql.org/)** - Advanced open-source relational database
- ✔️ **[`MongoDB`](https://www.mongodb.com/)** - NoSQL document database
- ✔️ **[`Redis`](https://redis.io/)** - In-memory data structure store, used as cache and message broker
- ✔️ **[`Qdrant`](https://qdrant.tech/)** - Vector database for AI-powered semantic search

### **Message Brokers & Communication**
- ✔️ **[`RabbitMQ`](https://www.rabbitmq.com/)** - Message broker for asynchronous communication
- ✔️ **[`gRPC`](https://grpc.io/)** - High-performance RPC framework for service-to-service communication
- ✔️ **[`SignalR`](https://dotnet.microsoft.com/apps/aspnet/signalr)** - Real-time web functionality for bi-directional communication

### **Cloud & Infrastructure**
- ✔️ **[`Azure Container Apps`](https://azure.microsoft.com/en-us/products/container-apps/)** - Serverless container platform
- ✔️ **[`.NET Aspire`](https://learn.microsoft.com/en-us/dotnet/aspire/)** - Cloud-ready stack for building observable, production-ready, distributed applications
- ✔️ **[`Docker`](https://www.docker.com/)** - Containerization platform
- ✔️ **[`YARP`](https://microsoft.github.io/reverse-proxy/)** - Reverse proxy for API Gateway

### **AI & Machine Learning**
- ✔️ **[`OpenAI API`](https://platform.openai.com/)** - Large language models for AI-powered features
- ✔️ **[`LangChain`](https://www.langchain.com/)** - Framework for developing applications powered by language models
- ✔️ **[`Text Embeddings`](https://platform.openai.com/docs/guides/embeddings)** - Vector embeddings for semantic search

### **ORM & Data Access**
- ✔️ **[`Entity Framework Core`](https://learn.microsoft.com/en-us/ef/core/)** - Object-relational mapping (ORM) framework
- ✔️ **[`MediatR`](https://github.com/jbogard/MediatR)** - Simple mediator implementation for .NET (CQRS pattern)

### **Authentication & Authorization**
- ✔️ **[`OpenIddict`](https://github.com/openiddict/openiddict-core)** - Versatile OpenID Connect stack for ASP.NET Core
- ✔️ **[`ASP.NET Core Identity`](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)** - Membership system for authentication

### **Observability & Monitoring**
- ✔️ **[`Serilog`](https://serilog.net/)** - Diagnostic logging library for .NET
- ✔️ **[`OpenTelemetry`](https://opentelemetry.io/)** - Observability framework for distributed tracing and metrics
- ✔️ **[`Prometheus`](https://prometheus.io/)** - Monitoring and alerting toolkit
- ✔️ **[`Grafana`](https://grafana.com/)** - Analytics and monitoring platform
- ✔️ **[`Loki`](https://grafana.com/oss/loki/)** - Log aggregation system
- ✔️ **[`Tempo`](https://grafana.com/oss/tempo/)** - Distributed tracing backend

### **Background Jobs**
- ✔️ **[`Hangfire`](https://www.hangfire.io/)** - Background job processing library for .NET

### **Testing**
- ✔️ **[`NUnit`](https://nunit.org/)** - Unit testing framework for .NET
- ✔️ **[`pytest`](https://docs.pytest.org/)** - Testing framework for Python
- ✔️ **[`Moq`](https://github.com/moq/moq4)** - Mocking library for .NET
- ✔️ **[`FluentAssertions`](https://fluentassertions.com/)** - Set of extension methods for assertions

### **Other Libraries**
- ✔️ **[`MassTransit`](https://masstransit.io/)** - Distributed application framework for .NET
- ✔️ **[`FluentValidation`](https://docs.fluentvalidation.net/)** - Popular .NET library for building strongly-typed validation rules
- ✔️ **[`AutoMapper`](https://automapper.org/)** - Object-to-object mapping library

## Application Architecture

![Application Architecture](docs/System_Design.png)

*High-level architecture diagram showing microservices, communication patterns, and infrastructure components.*

## Architecture Overview

### Architectural Patterns

#### **Domain-Driven Design (DDD) with Clean Architecture**
- **Domain Layer**: Core business logic and entities
- **Application Layer**: Use cases and business workflows (CQRS with MediatR)
- **Infrastructure Layer**: External concerns and data persistence
- **Presentation Layer**: API controllers, gRPC services, and web interfaces

#### **Microservices Architecture**
- **Service Independence**: Each service operates autonomously with its own database
- **Database per Service**: Isolated data storage for scalability
- **API Gateway**: Centralized routing and authentication (YARP)
- **Event-Driven Communication**: Asynchronous message processing via RabbitMQ
- **Polyglot Services**: .NET 8 (C#) and Python (FastAPI) services

#### **CQRS Pattern Implementation**
- **Command/Query Separation**: Clear distinction between read and write operations
- **MediatR Integration**: Decoupled request/response handling
- **Optimized Data Access**: Separate models for different use cases
- **Read Models**: Optimized projections for query performance

### Communication Patterns

#### **1. Synchronous Communication (HTTP/gRPC)**
```csharp
// Service-to-service communication via gRPC
services.AddGrpcClient<GrpcUser.GrpcUserClient>(options =>
{
    options.Address = new Uri(configuration["GrpcIdentityUrl"]);
});
```

#### **2. Asynchronous Communication (Event-Driven)**
```csharp
// RabbitMQ message publishing via MassTransit
await publishEndpoint.Publish(new CourseCreatedEvent
{
    CourseId = course.Id,
    Title = course.Title,
    CreatedByUserId = course.CreatedByUserId
});
```

#### **3. Real-time Communication (SignalR)**
```csharp
// Real-time notifications via SignalR
await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", notification);
```

## Core Services Architecture

### **Identity Service** - Authentication & Authorization Hub

#### **Architecture Layers**
```
Identity.Web (MVC) ←→ Identity.API (gRPC/REST) ←→ Identity.Application ←→ Identity.Domain ←→ Identity.Infrastructure
```

#### **Key Components**
- **User Management**: Multi-role system (Guest, Member, Teacher, Staff, Admin)
- **OpenIddict Integration**: OAuth 2.0 and OpenID Connect support
- **Bulk Provisioning**: CSV-based user invitation and onboarding
- **Organization Management**: Multi-tenant organization support with groups
- **License Management**: Subscription-based license assignment
- **JWT Token Management**: Secure authentication with refresh tokens
- **Google OAuth**: External authentication support

#### **Domain Entities**
```csharp
public abstract class ApplicationUser : IdentityUser<Guid>, IAggregateRoot<Guid>
{
    public UserRole Role { get; protected set; }
    public UserStatus Status { get; protected set; }
    public abstract string FirstName { get; protected set; }
    public abstract string LastName { get; protected set; }
}

public class OrganizationUser : BaseEntity<Guid>
{
    public int OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public OrganizationRole OrganizationRole { get; private set; }
    public int? GroupId { get; private set; }
}
```

### **Classroom Service** - Virtual Learning Environment

#### **Core Features**
- **Classroom Management**: Create, configure, and manage virtual classrooms
- **Student Enrollment**: Automated enrollment workflows
- **Progress Tracking**: Real-time learning progress monitoring
- **Resource Integration**: Seamless content delivery
- **Group Management**: Organize students into groups with grade levels

#### **Domain Entities**
```csharp
public class Classroom : EntityBase<int>
{
    public string Name { get; set; }
    public string Grade { get; set; }
    public Guid TeacherId { get; set; }
    public string ClassCode { get; set; }
    public ClassroomStatus Status { get; set; }
    public virtual ICollection<ClassroomResource> Resources { get; set; }
}
```

### **Resource Service** - Educational Content Management

#### **Content Structure**
```
Course → Lessons → Sections → Learning Materials
```

#### **Domain Entities**
```csharp
public class Course : EntityAuditBase<int>
{
    public string Title { get; set; }
    public string Code { get; set; }
    public CourseStatus Status { get; set; }
    public CourseLevel Level { get; set; }
    public virtual ICollection<Lesson> Lessons { get; set; }
}
```

#### **Content Management Features**
- **Multi-format Support**: Text, images, videos, interactive content
- **Version Control**: Content revision management
- **Approval Workflows**: Content review and publishing
- **Search & Discovery**: Advanced content search capabilities

### **AI Service** - Intelligent Analytics & Insights

#### **Technology Stack**
- **Framework**: FastAPI (Python 3.11)
- **Vector Database**: Qdrant for semantic search
- **LLM**: OpenAI (configurable for Azure OpenAI or local models)
- **RAG Pipeline**: LangChain-based retrieval-augmented generation

#### **Core Features**
- **Class Insights**: Analyze classroom performance, identify weak topics, students needing support
- **Student Insights**: Individual student progress analysis, strengths and weaknesses
- **Actionable Recommendations**: AI-generated suggestions for teachers
- **Content Generation**: AI-assisted educational content creation
- **Context Building**: Intelligent context assembly from multiple data sources

#### **Architecture**
```
app/
├── api/              # HTTP and gRPC endpoints
├── features/         # Feature modules (insights, recommendations, etc.)
├── core/             # Core services (LLM, embedding, vector store, RAG)
├── infrastructure/   # External integrations
└── common/           # Shared utilities
```

### **Product Service** - E-commerce Catalog

#### **Core Features**
- **Product Management**: Create, update, and manage educational products
- **Category Management**: Hierarchical product categorization
- **Inventory Management**: Stock tracking and availability
- **Product Search**: Advanced search and filtering capabilities

### **Cart Service** - Shopping Cart Management (Deprecated)

#### **Core Features**
- **Cart Operations**: Add, update, remove items
- **Cart Persistence**: User-specific cart storage
- **Cart Validation**: Product availability and pricing validation

### **Order Service** - Order Processing

#### **Core Features**
- **Order Management**: Create, track, and manage orders
- **Order Status**: Multi-stage order lifecycle
- **Subscription Management**: License-based subscription handling
- **Organization Integration**: Link orders to organizations

### **Payment Service** - Payment Processing  (Deprecated)

#### **Core Features**
- **Payment Processing**: Handle payment transactions
- **Payment Methods**: Multiple payment gateway support
- **Payment Status Tracking**: Real-time payment status updates
- **Refund Management**: Handle refunds and cancellations

### **Emulator Service** - 3D Assembly Simulation

#### **Core Features**
- **3D Model Management**: Store and manage 3D assembly models
- **Assembly Simulation**: Interactive 3D assembly experiences
- **Progress Tracking**: Track student assembly progress

### **Notification Service** - Multi-Channel Communication

#### **Notification Types**
- **Real-time Notifications**: Instant delivery via SignalR
- **Email Notifications**: SendGrid integration
- **Push Notifications**: FCM (Firebase Cloud Messaging)
- **In-app Notifications**: Persistent notification storage

#### **Event-Driven Architecture**
```csharp
// Course creation triggers notification
public class CourseCreatedConsumer : IConsumer<CourseCreatedEvent>
{
    public async Task Consume(ConsumeContext<CourseCreatedEvent> context)
    {
        var notification = new Notification
        {
            UserId = context.Message.CreatedByUserId,
            Title = "Course Created",
            Message = $"Course '{context.Message.Title}' has been created successfully"
        };
        await _notificationService.CreateAsync(notification);
    }
}
```

### **Saga Orchestrator** - Distributed Transaction Management

#### **Saga Pattern Implementation**
- **Choreography-based**: Services coordinate through events
- **Compensation Logic**: Rollback mechanisms for failed operations
- **Event Sourcing**: Complete audit trail of business operations
- **Consistency Guarantees**: Eventual consistency with compensation

### **Hangfire API** - Background Job Processing

#### **Job Types**
- **Recurring Jobs**: Scheduled maintenance tasks
- **Fire-and-Forget**: Asynchronous processing
- **Delayed Jobs**: Time-based execution
- **Continuations**: Job dependency chains

## Building Blocks

### **Shared Components**

#### **Common.Logging**
- **Structured Logging**: Serilog-based logging with enrichment
- **Metrics**: Custom metrics for each service (IdentityMetrics, ClassroomMetrics, etc.)
- **Logging Behavior**: MediatR pipeline behavior for automatic logging

#### **Contracts**
- **Domain Contracts**: Base entities, aggregate roots, domain events
- **Persistence Abstractions**: Repository and unit of work patterns
- **Service Abstractions**: Common service interfaces

#### **EventBus.Messages**
- **Integration Events**: Cross-service event definitions
- **Event Types**: License, Payment, Resource, Subscription events

#### **Infrastructure**
- **Persistence**: Common data access patterns
- **Resilience**: Circuit breaker, retry policies
- **Idempotency**: Request deduplication
- **Health Checks**: Service health monitoring

#### **Shared**
- **Protos**: gRPC service definitions (58 proto files)
- **DTOs**: Shared data transfer objects
- **Enums**: Common enumerations
- **Extensions**: Utility extension methods
- **Exceptions**: Shared exception types

#### **Caching**
- **Redis Integration**: Distributed caching layer
- **Cache Keys**: Centralized cache key management
- **Cache Statistics**: Performance monitoring

#### **Authorization**
- **Permission System**: Organization-level permission management
- **Role Permissions**: Role-based permission mapping
- **Policy Handlers**: Custom authorization policy handlers

## API Gateway & Communication

### **YARP Reverse Proxy Configuration**
```csharp
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        // Authentication header passthrough
        builderContext.AddRequestTransform(async transformContext =>
        {
            // Preserve JWT tokens and user context
        });
    });
```

### **Service Routing**
- **Identity Service**: `/identity/**` → Authentication & user management
- **Classroom Service**: `/classroom/**` → Virtual classroom operations
- **Resource Service**: `/resource/**` → Content management
- **Notification Service**: `/notification/**` → Communication hub
- **Product Service**: `/product/**` → Product catalog
- **Order Service**: `/order/**` → Order management
- **Payment Service  (Deprecated)** : `/payment/**` → Payment processing
- **AI Service**: `/ai/**` → AI-powered insights

### **Authentication & Authorization**
```csharp
// Role-based policies
options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
options.AddPolicy("TeacherOrStudent", policy => policy.RequireRole("Teacher", "Student"));
options.AddPolicy("EducationalStaff", policy => policy.RequireRole("Admin", "Teacher", "Staff"));
```

## Data Architecture

### **Database Strategy**
- **PostgreSQL 17**: Primary relational database for .NET services
- **MongoDB**: Document storage for certain services (if needed)
- **Database per Service**: Isolated data storage
- **Connection Pooling**: Optimized database connections
- **Migration Management**: Entity Framework Core migrations

### **Caching Strategy**
- **Redis**: Distributed caching layer
- **Hybrid Cache**: Memory + Redis combination
- **Cache Invalidation**: Event-driven cache updates
- **Performance Optimization**: Reduced database load

### **Vector Database**
- **Qdrant**: Vector database for AI Service semantic search
- **Embeddings**: Text embedding storage for RAG pipeline
- **Similarity Search**: Fast semantic similarity queries

### **Event Store**
- **Domain Events**: Business operation tracking
- **Integration Events**: Service communication
- **Event Persistence**: Complete audit trail
- **Event Replay**: System recovery capabilities

## Deployment & Infrastructure

### **Azure Aspire Orchestration**

The platform uses **.NET Aspire** for cloud-native orchestration and service discovery:

```csharp
// STEMify-Backend.AppHost
var identity = builder.AddProject<Projects.Identity_API>("identity-api");
var classroom = builder.AddProject<Projects.Classroom_API>("classroom-api");
var resource = builder.AddProject<Projects.Resource_API>("resource-api");
// ... other services
```

#### **Aspire Benefits**
- **Service Discovery**: Automatic service registration and discovery
- **Health Monitoring**: Centralized health check dashboard
- **Configuration Management**: Unified configuration across services
- **Observability**: Integrated logging, metrics, and tracing

### **Multi-Environment Support**

#### **Development Environment**
```yaml
# docker-compose.local.yml
services:
  postgres:
    image: postgres:17
    ports: ["5432:5432"]
  redis:
    image: redis:latest
    ports: ["6379:6379"]
  rabbitmq:
    image: rabbitmq:3-management-alpine
    ports: ["5672:5672", "15672:15672"]
  mongo:
    image: mongo
    ports: ["27018:27017"]
```

#### **Production Environment (Azure Container Apps)**
```yaml
# azure.yaml
services:
  app:
    language: dotnet
    project: ./src/Services/STEMify-Backend/STEMify-Backend.AppHost
    host: containerapp
```

### **Azure Container Apps Configuration**

#### **Port Configuration**
Each service uses unique ports in Azure Container Apps:
- **Identity Web**: Port 8084
- **Identity API**: Port 8083
- **Classroom API**: Port 8081
- **Resource API**: Port 8082
- **API Gateway**: Port 8085
- **AI Service**: Port 8086 (Python/FastAPI)

#### **Service Discovery**
```csharp
// Internal communication URLs with unique ports
services__classroom-api__http__0: "http://classroom-api:8081"
services__resource-api__http__0: "http://resource-api:8082"
services__identity-api__http__0: "http://identity-api:8083"
services__identity__http__0: "http://identity:8084"
services__apigateway__http__0: "http://apigateway:8085"
services__ai-service__http__0: "http://ai-service:8086"
```

### **Infrastructure as Code (Bicep)**
```bicep
// main.bicep
resource postgresServer 'Microsoft.DBforPostgreSQL/servers@2023-06-01-preview' = {
  name: '${namePrefix}-postgres'
  location: location
  properties: {
    administratorLogin: 'postgres'
    administratorLoginPassword: postgresPassword
    version: '17'
    sslEnforcement: 'Enabled'
  }
}
```

## CI/CD Pipeline

### **GitHub Actions Workflows**

#### **1. Dynamic CI for Services**
```yaml
# ci-script.yml
- **Smart Change Detection**: Only builds affected services
- **Parallel Execution**: Optimized build performance
- **Test Coverage**: Comprehensive testing with results artifacts
- **Cross-Platform**: Ubuntu runners for consistency
```

#### **2. Azure Production Deployment**
```yaml
# azure-pro.yml
- **Automated Deployment**: Push to STEM-deploy branch
- **Infrastructure Provisioning**: Bicep templates via azd
- **Service Deployment**: Container Apps with health checks
- **Environment Management**: Production-ready configurations
```

#### **3. Docker Image Publishing**
```yaml
# docker-publish.yml
- **Multi-service Builds**: Build and push Docker images for all services
- **Azure Container Registry**: Centralized image registry
- **Tag Management**: Semantic versioning for images
```

## Testing Strategy

### **Test Pyramid**
```
    🔺 E2E Tests (Few)
   🔺🔺 Integration Tests
  🔺🔺🔺 Unit Tests (Many)
```

### **Test Coverage**
- **Identity Domain**: Comprehensive unit tests
- **Application Layer**: Business logic validation
- **Infrastructure**: Data access and external service tests
- **API Integration**: End-to-end workflow testing

### **Testing Tools**
- **NUnit**: Primary testing framework (.NET)
- **pytest**: Testing framework (Python/AI Service)
- **Moq**: Mocking and test doubles
- **FluentAssertions**: Readable assertions
- **TestContainers**: Isolated test environments

## Security Implementation

### **Authentication & Authorization**
- **JWT Tokens**: Secure stateless authentication
- **OpenID Connect**: Industry-standard identity protocol
- **Role-Based Access Control**: Granular permission management
- **Organization-Level Permissions**: Multi-tenant permission system
- **Google OAuth**: External authentication support

### **Data Protection**
- **Encryption at Rest**: Database-level encryption
- **TLS/SSL**: Secure communication channels
- **Input Validation**: Comprehensive request sanitization (FluentValidation)
- **SQL Injection Prevention**: Parameterized queries via EF Core

### **Security Headers**
```csharp
// Security middleware configuration
app.UseHsts();
app.UseHttpsRedirection();
app.UseForwardedHeaders();
```

## Monitoring & Observability

### **Health Checks**
```csharp
// Comprehensive health monitoring
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Name == "database-initialization" || check.Name == "database"
});
app.MapHealthChecks("/health/live");
```

### **Logging Infrastructure**
- **Serilog**: Structured logging with enrichment (.NET services)
- **Python Logging**: Structured logging for AI Service
- **OpenTelemetry**: Distributed tracing and metrics
- **Log Aggregation**: Centralized log management
- **Performance Monitoring**: Request/response timing

### **Metrics & Alerting**
- **Application Metrics**: Business KPIs and performance indicators
- **Custom Metrics**: Service-specific metrics (IdentityMetrics, ClassroomMetrics, etc.)
- **Infrastructure Monitoring**: Resource utilization and health
- **Custom Dashboards**: Real-time system visibility
- **Automated Alerting**: Proactive issue detection

## Getting Started

### **Prerequisites**
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (8.0.404 or later)
- [Python 3.11+](https://www.python.org/downloads/) (for AI Service)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [PostgreSQL 17](https://www.postgresql.org/download/)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- [Azure Developer CLI](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/) (for Azure deployment)

### **Quick Start**

#### **1. Clone and Setup**
```bash
git clone <repository-url>
cd STEMify-Backend
dotnet restore
```

#### **2. Local Development**

**Start Infrastructure:**
```bash
# Start local infrastructure (PostgreSQL, Redis, RabbitMQ, MongoDB)
docker-compose -f docker-compose.local.yml up -d
```

**Run .NET Services:**
```bash
# Run Identity Service
cd src/Services/Identity/Identity.Web
dotnet run

# Run Classroom Service
cd src/Services/ClassroomService/Classroom.API
dotnet run

# Run Resource Service
cd src/Services/Resource/Resource.API
dotnet run

# Run API Gateway
cd src/ApiGateways/ApiGateway
dotnet run
```

**Run AI Service (Python):**
```bash
cd src/Services/AIService
python -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate
pip install -r requirements.txt
python -m app.main
```

**Run with Aspire:**
```bash
cd src/Services/STEMify-Backend/STEMify-Backend.AppHost
dotnet run
```

#### **3. Docker Development**
```bash
# Full stack with Docker
docker-compose up -d

# Access services
# Identity Web: http://localhost:5000
# Identity API: http://localhost:7002
# Classroom API: http://localhost:7001
# Resource API: http://localhost:7003
# Notification API: http://localhost:7004
# API Gateway: http://localhost:6001
# AI Service: http://localhost:8000
```

#### **4. Azure Deployment**
```bash
# Install Azure Developer CLI
azd install

# Login to Azure
azd auth login

# Deploy to Azure
azd up
```

## 📁 Project Structure

```
STEMify-Backend/
├── 📁 src/
│   ├── BuildingBlocks/              # Shared components
│   │   ├── Common.Logging/          # Centralized logging & metrics
│   │   ├── Contracts/               # Domain contracts & abstractions
│   │   ├── EventBus.Messages/       # Integration events
│   │   ├── Infrastructure/          # Common infrastructure
│   │   ├── Shared/                  # DTOs, utilities, protos (58 proto files)
│   │   ├── Caching/                 # Redis caching layer
│   │   └── Authorization/           # Permission system
│   │
│   ├── Services/
│   │   ├── Identity/                # Authentication & authorization
│   │   │   ├── Identity.Web/       # MVC web interface
│   │   │   ├── Identity.API/       # gRPC/REST API
│   │   │   ├── Identity.Application/
│   │   │   ├── Identity.Domain/
│   │   │   └── Identity.Infrastructure/
│   │   │
│   │   ├── ClassroomService/        # Virtual classroom management
│   │   ├── Resource/                # Educational content management
│   │   ├── Notification/            # Multi-channel notifications
│   │   ├── AIService/               # AI-powered insights (Python/FastAPI)
│   │   ├── ProductService/          # Product catalog
│   │   ├── CartService/             # Shopping cart
│   │   ├── OrderService/            # Order processing
│   │   ├── PaymentService/          # Payment processing
│   │   ├── EmulatorService/         # 3D assembly simulation
│   │   ├── Hangfire.API/            # Background job processing
│   │   └── STEMify-Backend/         # Aspire AppHost
│   │
│   ├── Saga.Orchestrator/           # Distributed transaction orchestration
│   └── ApiGateways/                 # API Gateway (YARP)
│
├── 🧪 tests/                        # Comprehensive test suite
│   └── Identity/                   # Identity service tests
│
├── infra/                          # Infrastructure as Code (Bicep)
├── config/                          # Observability configs (Prometheus, Grafana, Loki, Tempo)
├── 📚 docs/                         # Documentation
└── .github/workflows/              # CI/CD pipelines
```

## Configuration Management

### **Environment-Specific Settings**
```json
// Development
{
  "ConnectionStrings": {
    "stemifyidentity": "Server=localhost;Database=stemifyidentity;..."
  },
  "Kestrel": {
    "Endpoints": {
      "WebApi": { "Url": "http://+:80" },
      "Grpc": { "Url": "http://+:5002" }
    }
  }
}

// Production (Azure Container Apps)
{
  "PORT": 8080,
  "ConnectionStrings": {
    "stemifyidentity": "{aspire-connection-string}"
  }
}
```

### **AI Service Configuration**
```python
# app/infrastructure/config/settings.py
class Settings:
    # LLM Configuration
    openai_api_key: str
    openai_model: str = "gpt-4"
    
    # Vector Database
    qdrant_url: str
    qdrant_api_key: Optional[str] = None
    
    # RAG Configuration
    embedding_model: str = "text-embedding-3-large"
    chunk_size: int = 1000
    chunk_overlap: int = 200
```

## 🌟 Advanced Features

### **Resilience Patterns**
- **Circuit Breaker**: Automatic failure isolation
- **Retry Policies**: Exponential backoff strategies
- **Timeout Management**: Request lifecycle control
- **Fallback Mechanisms**: Graceful degradation

### **Performance Optimization**
- **Response Caching**: HTTP-level caching
- **Compression**: Gzip/Brotli compression
- **Connection Pooling**: Database optimization
- **Async/Await**: Non-blocking operations
- **Vector Search**: Fast semantic similarity queries

### **Scalability Features**
- **Horizontal Scaling**: Container-based scaling
- **Load Balancing**: Traffic distribution
- **Auto-scaling**: Dynamic resource allocation
- **Database Sharding**: Data distribution strategies
- **Agent Pooling**: AI agent instance pooling for better performance

## 🤝 Contributing

### **Development Guidelines**
1. **Fork the repository**
2. **Create feature branch**: `git checkout -b feature/amazing-feature`
3. **Follow coding standards**:
   - Clean Architecture principles
   - Comprehensive unit testing
   - Conventional commit messages
   - Code review process
4. **Submit pull request**

### **Code Quality Standards**
- **Clean Code**: Readable and maintainable code
- **SOLID Principles**: Object-oriented design
- **Test Coverage**: Minimum 80% coverage
- **Performance**: Response time < 200ms for API calls
- **Security**: OWASP compliance

## 📚 Documentation

- [Development Guide](DEVELOPMENT_GUIDE.md) - Detailed setup and coding guidelines
- [API Documentation](docs/) - Service endpoints and contracts
- [AI Service Architecture](src/Services/AIService/ARCHITECTURE.md) - AI Service detailed architecture
- [Deployment Guide](docs/DEPLOYMENT_GUIDE.md) - Infrastructure and deployment instructions

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👥 Team

**STEMify Development Team**
- **Backend Architecture**: Microservices design and implementation
- **Cloud Infrastructure**: Azure deployment and optimization
- **AI/ML Integration**: Intelligent analytics and insights
- **Educational Technology**: Learning experience innovation
- **Quality Assurance**: Testing and performance optimization

## 🎓 Educational Impact

STEMify revolutionizes STEM education by providing:
- **Interactive Learning Environments**: Engaging virtual classrooms
- **Real-time Collaboration**: Seamless student-teacher interaction
- **AI-Powered Insights**: Data-driven learning analytics and recommendations
- **E-commerce Platform**: Seamless product and subscription management
- **Progress Analytics**: Comprehensive learning progress tracking
- **Scalable Infrastructure**: Support for educational institutions of all sizes
- **Accessibility**: Inclusive learning for diverse student populations

---

*Built with ❤️ for STEM education innovation and powered by cutting-edge cloud-native technologies*

## 🔗 Quick Links

- [Deploy to Azure](docs/DEPLOYMENT_GUIDE.md)
- [Development Setup](DEVELOPMENT_GUIDE.md)
- [API Documentation](docs/)
- [Testing Guide](docs/TESTING_FUNDAMENTALS_GUIDE.md)
- [Security Guidelines](SECURITY.md)
- [ Performance Monitoring](docs/monitoring/)
