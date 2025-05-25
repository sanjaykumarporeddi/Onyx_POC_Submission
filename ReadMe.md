# Onyx Products API - Coding Test Submission

This repository contains the .NET 8 "Products" Web API developed for the Onyx Capital Group Senior Developer Coding Test.

## Features

*   **Anonymous Health Check:** `GET /health`
*   **Secured Product Endpoints (JWT Authentication):**
    *   `POST /api/products`: Create a product (Admin role).
    *   `GET /api/products`: List products (Authenticated user).
        *   Supports filtering by colour: `?colour={value}`
    *   Other CRUD operations (GET by ID/Name, PUT, PATCH, DELETE) also implemented with appropriate security.
*   **Event Publishing:** Publishes `ProductChangedEvent` to a message bus (Azure Service Bus) on CUD operations.
*   **Unit & Integration Tests:** Comprehensive test suite using xUnit and Moq.
*   **API Documentation:** Swagger/OpenAPI available at `/swagger` in development.

## Technology Stack

*   .NET 8 / ASP.NET Core 8
*   Entity Framework Core 8 (PostgreSQL)
*   Serilog (Logging)
*   Azure Service Bus (Messaging)
*   xUnit, Moq (Testing)

## Project Structure

*   `Onyx.Services.ProductAPI/`: Main Web API project.
*   `Onyx.MessageBus/`: Message bus abstraction and Azure Service Bus implementation.
*   `Onyx.Common.Shared/`: Shared DTOs/Enums.
*   `Onyx.Services.ProductAPI.Tests/`: Unit and integration tests.
*   `architecture_flow.png`: System architecture diagram.

## Setup & Running

### Prerequisites

*   .NET 8 SDK
*   PostgreSQL Server
*   (Optional) Azure Service Bus for live event publishing.

### Configuration

1.  **Clone:** `git clone https://github.com/sanjaykumarporeddi/Onyx_POC_Submission.git`
2.  **User Secrets (for `Onyx.Services.ProductAPI` project):**
    *   `ConnectionStrings:DefaultConnection`: Your PostgreSQL connection string.
    *   `ApiSettings:Secret`: A strong JWT secret (min 32 chars).
    *   `ConnectionStrings:AzureServiceBusConnectionString`: (Optional) Your Azure Service Bus connection string.
    *   Example: `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=OnyxProductDbDev;Username=user;Password=pass"`
3.  **Database Migrations:**
    Navigate to `Onyx.Services.ProductAPI` and run: `dotnet ef database update`

### Running the API

From the `Onyx.Services.ProductAPI` directory: 
dotnet run

### Running Tests
From the Onyx.Services.ProductAPI.Tests directory:
dotnet test

### Architecture
Refer to Microservices_Onyx.pptx for a Architecture diagram illustrating this service within a sample microservices event-driven architecture. The Products API publishes events to a service bus, allowing other services (e.g., Orders, Shopping Cart) to react to product changes, promoting loose coupling and scalability.
