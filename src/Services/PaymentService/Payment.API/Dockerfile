FROM mcr.microsoft.com/dotnet/sdk:8.0 as build
WORKDIR /app
EXPOSE 80

# Copy all .csproj files and restore dependencies in separate layers. 
# Use the same COPY command across all Dockerfiles in the project to maximize Docker cache efficiency.
COPY STEMify-Backend.sln STEMify-Backend.sln

# Classroom Service
COPY src/Services/ClassroomService/Classroom.API/Classroom.API.csproj src/Services/ClassroomService/Classroom.API/Classroom.API.csproj
COPY src/Services/ClassroomService/Classroom.Application/Classroom.Application.csproj src/Services/ClassroomService/Classroom.Application/Classroom.Application.csproj
COPY src/Services/ClassroomService/Classroom.Domain/Classroom.Domain.csproj src/Services/ClassroomService/Classroom.Domain/Classroom.Domain.csproj
COPY src/Services/ClassroomService/Classroom.Infrastructure/Classroom.Infrastructure.csproj src/Services/ClassroomService/Classroom.Infrastructure/Classroom.Infrastructure.csproj

# Identity Service
COPY src/Services/Identity/Identity.Web/Identity.Web.csproj src/Services/Identity/Identity.Web/Identity.Web.csproj
COPY src/Services/Identity/Identity.Application/Identity.Application.csproj src/Services/Identity/Identity.Application/Identity.Application.csproj
COPY src/Services/Identity/Identity.Domain/Identity.Domain.csproj src/Services/Identity/Identity.Domain/Identity.Domain.csproj
COPY src/Services/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj src/Services/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj
COPY src/Services/Identity/Identity.API/Identity.API.csproj src/Services/Identity/Identity.API/Identity.API.csproj

# Resource Service
COPY src/Services/Resource/Resource.API/Resource.API.csproj src/Services/Resource/Resource.API/Resource.API.csproj
COPY src/Services/Resource/Resource.Application/Resource.Application.csproj src/Services/Resource/Resource.Application/Resource.Application.csproj
COPY src/Services/Resource/Resource.Domain/Resource.Domain.csproj src/Services/Resource/Resource.Domain/Resource.Domain.csproj
COPY src/Services/Resource/Resource.Infrastructure/Resource.Infrastructure.csproj src/Services/Resource/Resource.Infrastructure/Resource.Infrastructure.csproj

# Product Service
COPY src/Services/ProductService/Product.API/Product.API.csproj src/Services/ProductService/Product.API/Product.API.csproj
COPY src/Services/ProductService/Product.Application/Product.Application.csproj src/Services/ProductService/Product.Application/Product.Application.csproj
COPY src/Services/ProductService/Product.Domain/Product.Domain.csproj src/Services/ProductService/Product.Domain/Product.Domain.csproj
COPY src/Services/ProductService/Product.Infrastructure/Product.Infrastructure.csproj src/Services/ProductService/Product.Infrastructure/Product.Infrastructure.csproj

# Order Service
COPY src/Services/OrderService/Order.API/Order.API.csproj src/Services/OrderService/Order.API/Order.API.csproj
COPY src/Services/OrderService/Order.Application/Order.Application.csproj src/Services/OrderService/Order.Application/Order.Application.csproj
COPY src/Services/OrderService/Order.Domain/Order.Domain.csproj src/Services/OrderService/Order.Domain/Order.Domain.csproj
COPY src/Services/OrderService/Order.Infrastructure/Order.Infrastructure.csproj src/Services/OrderService/Order.Infrastructure/Order.Infrastructure.csproj

# Payment Service
COPY src/Services/PaymentService/Payment.API/Payment.API.csproj src/Services/PaymentService/Payment.API/Payment.API.csproj
COPY src/Services/PaymentService/Payment.Application/Payment.Application.csproj src/Services/PaymentService/Payment.Application/Payment.Application.csproj
COPY src/Services/PaymentService/Payment.Domain/Payment.Domain.csproj src/Services/PaymentService/Payment.Domain/Payment.Domain.csproj
COPY src/Services/PaymentService/Payment.Infrastructure/Payment.Infrastructure.csproj src/Services/PaymentService/Payment.Infrastructure/Payment.Infrastructure.csproj

# Emulator Service
COPY src/Services/EmulatorService/Emulator.API/Emulator.API.csproj src/Services/EmulatorService/Emulator.API/Emulator.API.csproj
COPY src/Services/EmulatorService/Emulator.Service/Emulator.Service.csproj src/Services/EmulatorService/Emulator.Service/Emulator.Service.csproj
COPY src/Services/EmulatorService/Emulator.Repository/Emulator.Repository.csproj src/Services/EmulatorService/Emulator.Repository/Emulator.Repository.csproj

# Cart Service
COPY src/Services/CartService/Cart.API/Cart.API.csproj src/Services/CartService/Cart.API/Cart.API.csproj
COPY src/Services/CartService/Cart.Application/Cart.Application.csproj src/Services/CartService/Cart.Application/Cart.Application.csproj
COPY src/Services/CartService/Cart.Domain/Cart.Domain.csproj src/Services/CartService/Cart.Domain/Cart.Domain.csproj
COPY src/Services/CartService/Cart.Infrastructure/Cart.Infrastructure.csproj src/Services/CartService/Cart.Infrastructure/Cart.Infrastructure.csproj

# Gateway Service
COPY src/ApiGateways/ApiGateway/ApiGateway.csproj src/ApiGateways/ApiGateway/ApiGateway.csproj

# Notification Service
COPY src/Services/Notification/Notification.API/Notification.API.csproj src/Services/Notification/Notification.API/Notification.API.csproj
COPY src/Services/Notification/Notification.Application/Notification.Application.csproj src/Services/Notification/Notification.Application/Notification.Application.csproj
COPY src/Services/Notification/Notification.Domain/Notification.Domain.csproj src/Services/Notification/Notification.Domain/Notification.Domain.csproj
COPY src/Services/Notification/Notification.Infrastructure/Notification.Infrastructure.csproj src/Services/Notification/Notification.Infrastructure/Notification.Infrastructure.csproj

# Building Blocks
COPY src/BuildingBlocks/Caching/Caching.csproj src/BuildingBlocks/Caching/Caching.csproj
COPY src/BuildingBlocks/Contracts/Contracts.csproj src/BuildingBlocks/Contracts/Contracts.csproj
COPY src/BuildingBlocks/Common.Logging/Common.Logging.csproj src/BuildingBlocks/Common.Logging/Common.Logging.csproj
COPY src/BuildingBlocks/EventBus.Messages/EventBus.Messages.csproj src/BuildingBlocks/EventBus.Messages/EventBus.Messages.csproj
COPY src/BuildingBlocks/Infrastructure/Infrastructure.csproj src/BuildingBlocks/Infrastructure/Infrastructure.csproj
COPY src/BuildingBlocks/Shared/Shared.csproj src/BuildingBlocks/Shared/Shared.csproj
COPY src/BuildingBlocks/Authorization/Authorization.csproj src/BuildingBlocks/Authorization/Authorization.csproj

# Aspire
COPY src/Services/STEMify-Backend/STEMify-Backend.AppHost/STEMify-Backend.AppHost.csproj src/Services/STEMify-Backend/STEMify-Backend.AppHost/STEMify-Backend.AppHost.csproj
COPY src/Services/STEMify-Backend/STEMify-Backend.ServiceDefaults/STEMify-Backend.ServiceDefaults.csproj src/Services/STEMify-Backend/STEMify-Backend.ServiceDefaults/STEMify-Backend.ServiceDefaults.csproj

# Hangfire
COPY src/Services/Hangfire.API/Hangfire.API.csproj src/Services/Hangfire.API/Hangfire.API.csproj

# Test
COPY tests/Identity/Domain.UnitTests/Domain.UnitTests.csproj tests/Identity/Domain.UnitTests/Domain.UnitTests.csproj

# Saga 
COPY src/Saga.Orchestrator/Saga.Orchestrator/Saga.Orchestrator.csproj src/Saga.Orchestrator/Saga.Orchestrator/Saga.Orchestrator.csproj

# Restore package deps
RUN dotnet restore STEMify-Backend.sln

# Copy the app folders over
COPY src/Services/PaymentService/Payment.API src/Services/PaymentService/Payment.API
COPY src/Services/PaymentService/Payment.Application src/Services/PaymentService/Payment.Application
COPY src/Services/PaymentService/Payment.Domain src/Services/PaymentService/Payment.Domain
COPY src/Services/PaymentService/Payment.Infrastructure src/Services/PaymentService/Payment.Infrastructure

COPY src/BuildingBlocks/Caching src/BuildingBlocks/Caching
COPY src/BuildingBlocks/Contracts src/BuildingBlocks/Contracts
COPY src/BuildingBlocks/Common.Logging src/BuildingBlocks/Common.Logging
COPY src/BuildingBlocks/EventBus.Messages src/BuildingBlocks/EventBus.Messages
COPY src/BuildingBlocks/Infrastructure src/BuildingBlocks/Infrastructure
COPY src/BuildingBlocks/Shared src/BuildingBlocks/Shared
COPY src/Services/STEMify-Backend/STEMify-Backend.ServiceDefaults src/Services/STEMify-Backend/STEMify-Backend.ServiceDefaults

WORKDIR /app/src/Services/PaymentService/Payment.API
RUN dotnet publish -c Release -o /app/src/Services/PaymentService/out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/src/Services/PaymentService/out .
ENTRYPOINT [ "dotnet", "Payment.API.dll" ]
