# 🛍️ OrderSphere

A modern, fully-featured e-commerce order management system built with **Clean Architecture** and **Domain-Driven Design** principles. OrderSphere provides a complete shopping experience from product browsing through secure checkout, with real-time cart synchronization, inventory management, and asynchronous order processing.

## ✨ Key Features

- 🛒 **Shopping Cart Management** — Add, remove, and update product quantities with persistent storage
- 📦 **Product Catalog** — Browse products with inventory tracking and category filtering
- 💳 **Secure Checkout** — Support for multiple payment methods (Invoice, Credit Card, PayPal)
- 📊 **Order Management** — Track orders with status lifecycle (Created → Paid → Shipped → Delivered)
- 📧 **Email Notifications** — Email verification, password reset links, and order updates
- 🔐 **User Authentication** — Secure sign-up/login with email verification
- 🔄 **Real-time Cart Sync** — Instant cart updates across browser sessions
- 🏗️ **Event-Driven Architecture** — Asynchronous order processing via Azure Service Bus
- 📱 **Responsive UI** — Modern Blazor Server application with Material Design

## 🏛️ Architecture Overview

OrderSphere follows **Clean Architecture** with clear separation of concerns:

```
┌─────────────────────────────────────────┐
│     OrderSphere.UI (Blazor Server)      │ ← User Interface
├─────────────────────────────────────────┤
│    OrderSphere.Application (CQRS)       │ ← Commands & Queries
│    - Commands (write operations)        │
│    - Queries (read operations)          │
│    - MediatR request handlers           │
├─────────────────────────────────────────┤
│   OrderSphere.Infrastructure            │ ← Data Access & Services
│   - EF Core DbContext (PostgreSQL)      │
│   - Azure Email Service                 │
│   - Azure Service Bus Publisher         │
├─────────────────────────────────────────┤
│   OrderSphere.Domain (DDD)              │ ← Business Logic
│   - Entities (Order, Product, Cart)     │
│   - Value Objects (Address)             │
│   - Domain Events                       │
│   - Enums & Errors                      │
└─────────────────────────────────────────┘
```

**Key Patterns:**
- **CQRS**: Command Query Responsibility Segregation via MediatR
- **DDD**: Domain-Driven Design with rich domain models
- **Event Sourcing**: Order events published to Azure Service Bus
- **Result Pattern**: Functional error handling with Result<T> types
- **Entity Auditing**: Automatic CreatedAt, UpdatedAt, IsDeleted tracking

## 🛠️ Technology Stack

| Component | Technology |
|-----------|-----------|
| **Language & Framework** | .NET 10.0, C# 13.0 |
| **Database** | PostgreSQL 16+ with Entity Framework Core 10 |
| **UI Framework** | Blazor Server + MudBlazor Material Design |
| **CQRS Pattern** | MediatR |
| **Message Queue** | Azure Service Bus |
| **Email Service** | Azure Communication Services |
| **Logging** | Serilog with OpenTelemetry support |
| **Orchestration** | .NET Aspire (containers) |
| **Authentication** | ASP.NET Core Identity |

## 📂 Project Structure

```
src/
├── OrderSphere.Domain/              # Core business logic (DDD)
│   ├── Entities/                    # Order, Product, Cart, Category
│   ├── ValueObjects/                # Address
│   ├── Events/                      # Domain events (OrderCreatedEvent)
│   ├── Enums/                       # OrderStatus, PaymentMethod, etc.
│   ├── Errors/                      # Business error definitions
│   └── Primitives/                  # Result<T>, Error, ICommand
│
├── OrderSphere.Application/         # Use cases (CQRS)
│   ├── Features/                    # Commands & Queries
│   │   ├── Cart/                    # AddToCart, RemoveFromCart, GetCart
│   │   ├── Products/                # GetProducts, GetProductBySlug
│   │   └── Orders/                  # CheckoutCart, CreateOrder
│   ├── Models/                      # DTOs (ProductDto, CartDto)
│   ├── ServiceBus/                  # Event publishers
│   └── Abstraction/                 # Interfaces (ICommand, IQuery, IDbContext)
│
├── OrderSphere.Infrastructure/      # Data access & external services
│   ├── Persistence/                 # OrderSphereDbContext
│   ├── EntityConfigurations/        # EF Core Fluent API configs
│   ├── Email/                       # Azure Email Service
│   ├── ServiceBus/                  # Azure Service Bus Publisher
│   └── Interceptors/                # Database audit interceptors
│
├── OrderSphere.UI/                  # Blazor Server application
│   ├── Components/                  # Blazor components & pages
│   │   ├── Pages/                   # Product, Cart, Checkout, Account
│   │   ├── Layouts/                 # MainLayout, Header, Footer
│   │   └── Account/                 # Login, Register, Profile
│   ├── Services/                    # CartService, CurrentUserService
│   └── Program.cs                   # Startup configuration
│
├── OrderSphere.AppHost/             # .NET Aspire orchestration
│   └── AppHost.cs                   # Container configurations
│
└── OrderSphere.ServiceDefaults/     # Shared service configuration
```

## 🚀 Getting Started

### Prerequisites

- **.NET 10.0 SDK** or later
- **Docker** or **Podman** (for running PostgreSQL and Azure Service Bus emulator)
- **Visual Studio 2022** (v17.12+) or **VS Code** with C# extension

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/OrderSphere.git
   cd OrderSphere
   ```

2. **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

3. **Run with .NET Aspire** (recommended)
   ```bash
   cd src/OrderSphere.AppHost
   dotnet run
   ```
   This will:
   - Start PostgreSQL on port `5432`
   - Start Azure Service Bus emulator
   - Start the Blazor UI on `http://localhost:5000`
   - Open Aspire Dashboard on `http://localhost:15191`

4. **Or run locally without Aspire**
   ```bash
   cd src/OrderSphere.UI
   dotnet run
   ```
   - UI: `http://localhost:5000`
   - Ensure PostgreSQL is running on `localhost:5432`

### Default Credentials & Ports

| Service | URL/Port | Default |
|---------|----------|---------|
| **Blazor UI** | `http://localhost:5000` | N/A |
| **Aspire Dashboard** | `http://localhost:15191` | N/A |
| **PostgreSQL** | `localhost:5432` | postgres:root |
| **PgAdmin** | `http://localhost:5050` | admin@example.com:admin |
| **Service Bus Emulator** | `localhost:5672` | Enabled |

## ⚙️ Configuration

### appsettings.json

Key configuration sections:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ordersphere;Username=postgres;Password=root;"
  },
  "MailServiceConfiguration": {
    "ConnectionString": "endpoint=https://...;accesskey=...",
    "SenderAddress": "noreply@yourdomain.com"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Environment Variables

- `ASPNETCORE_ENVIRONMENT`: `Development` or `Production`
- `ConnectionStrings__DefaultConnection`: Database connection string
- `MailServiceConfiguration__ConnectionString`: Azure Email Service connection
- `MailServiceConfiguration__SenderAddress`: Sender email address

## 💡 Core Features Deep Dive

### 🛒 Shopping Cart

**Add to Cart**
- Creates a new cart if customer doesn't have one
- Validates product exists and has sufficient stock
- Increases quantity if product already in cart
- Uses database transactions for consistency

```
Command: AddToCartCommand
  ├── CustomerId: Guid
  ├── ProductId: Guid
  └── Quantity: int
Response: Result<CartDto>
```

**Get Cart**
- Retrieves customer's complete cart with all items
- Enriches items with current product details
- Returns empty result if no cart exists

**Remove & Update**
- Remove products from cart
- Adjust quantities with automatic deletion when empty

### 📦 Product Catalog

**Get All Products**
- Browse active products with available stock
- Includes category information
- Filters out inactive items

**Get Product by Slug**
- Retrieve single product using URL-friendly slug
- Used for product detail pages
- Example: `/products/apple-macbook-pro-15`

### 💳 Checkout & Order Processing

**Checkout Process**
1. Validate cart exists and contains items
2. Reduce product stock for each item
3. Publish `CheckoutCartEvent` to Azure Service Bus
4. Collect shipping address & payment method

**Order Processing** (Asynchronous)
- Service Bus subscriber listens for `CheckoutCartEvent`
- Creates `Order` entity with status `Created`
- Updates inventory atomically
- Enables loose coupling between checkout and fulfillment

### 🔐 User Authentication

- **Registration**: Email-based signup with verification
- **Login**: Secure credential validation
- **Email Verification**: Confirmation link sent via Azure Email Service
- **Password Reset**: Secure reset link generation
- **Roles & Claims**: Support for role-based authorization

## 🗄️ Database Schema Overview

### Core Entities

| Entity | Purpose | Key Properties |
|--------|---------|-----------------|
| **Order** | Customer orders | OrderId, CustomerId, Status, ShippingAddress, PaymentMethod, CreatedAt |
| **OrderItem** | Order line items | OrderItemId, OrderId, ProductId, Quantity, Price (captured) |
| **Product** | Catalog items | ProductId, Name, Slug, Price, Stock, CategoryId, SKU |
| **Cart** | Shopping carts | CartId, CustomerId, Items (collection) |
| **CartItem** | Cart line items | CartItemId, CartId, ProductId, Quantity |
| **Category** | Product groups | CategoryId, Name, Description, IsActive |
| **ApplicationUser** | Authentication | UserId, FirstName, LastName, Email |

### Audit Trails

All entities inherit from `AuditableEntity`:
- `Id`: Unique identifier (Guid)
- `CreatedAt`: Entity creation timestamp
- `UpdatedAt`: Last modification timestamp
- `IsDeleted`: Soft delete flag

## 🧪 Development

### Running the Application

**Development with hot-reload:**
```bash
dotnet watch run
```

**Running specific project:**
```bash
cd src/OrderSphere.UI && dotnet run
```

### Database Migrations

```bash
# Create a new migration
dotnet ef migrations add MigrationName -p src/OrderSphere.Infrastructure -s src/OrderSphere.UI

# Apply migrations
dotnet ef database update -p src/OrderSphere.Infrastructure -s src/OrderSphere.UI

# Revert to previous migration
dotnet ef database update PreviousMigration -p src/OrderSphere.Infrastructure -s src/OrderSphere.UI
```

### Code Style

- **Language Features**: C# 13.0, nullable reference types enabled
- **Naming**: PascalCase for public members, camelCase for local variables
- **Async**: All I/O operations are async (no `.Result` or `.Wait()`)
- **Error Handling**: Use Result<T> pattern, not exceptions for business logic

## 📋 MediatR Commands & Queries

### Commands (Write Operations)

```
Carts:
  - AddToCartCommand
  - RemoveFromCartCommand
  - DecreaseCartItemQuantityCommand
  - CheckoutCartCommand

Orders:
  - CreateOrderCommand
```

### Queries (Read Operations)

```
Carts:
  - GetCartQuery

Products:
  - GetProductQuery (all products)
  - GetProductBySlugQuery (single product)
```

## 🚢 Deployment

### Production Considerations

1. **Database**: Use managed PostgreSQL service (AWS RDS, Azure Database)
2. **Email Service**: Configure Azure Communication Services credentials
3. **Service Bus**: Switch to Azure Service Bus (from emulator)
4. **Logging**: Configure Serilog to export to Application Insights
5. **Authentication**: Use Azure AD or similar identity provider
6. **Secrets**: Use Azure Key Vault or equivalent for sensitive config

### Environment Setup

```bash
# Production build
dotnet publish -c Release -o ./publish

# Container deployment
docker build -t ordersphere .
docker run -p 5000:5000 ordersphere
```

## 📝 License

MIT License — see [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📧 Contact & Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/OrderSphere/issues)
- **Email**: support@yourdomain.com
- **Documentation**: [Wiki](https://github.com/yourusername/OrderSphere/wiki)

---

**Built with ❤️ using Clean Architecture & Domain-Driven Design**
