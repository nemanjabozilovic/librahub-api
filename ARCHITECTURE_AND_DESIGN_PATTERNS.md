# LibraHub - Arhitektura i Design Pattern-i

## 📋 Sadržaj

1. [Arhitekturni Pattern-i](#1-arhitekturni-pattern-i)
   - [1.1 Clean Architecture](#11-clean-architecture)
   - [1.2 Domain-Driven Design (DDD)](#12-domain-driven-design-ddd)
   - [1.3 Microservices Architecture](#13-microservices-architecture)
   - [1.4 CQRS (Command Query Responsibility Segregation)](#14-cqrs-command-query-responsibility-segregation)
   - [1.5 Event-Driven Architecture](#15-event-driven-architecture)

2. [Design Pattern-i](#2-design-pattern-i)
   - [2.1 Repository Pattern](#21-repository-pattern)
   - [2.2 Unit of Work Pattern](#22-unit-of-work-pattern)
   - [2.3 Factory Pattern](#23-factory-pattern)
   - [2.4 Strategy Pattern](#24-strategy-pattern)
   - [2.5 Mediator Pattern](#25-mediator-pattern)
   - [2.6 Result Pattern](#26-result-pattern)
   - [2.7 Specification Pattern](#27-specification-pattern)
   - [2.8 Value Object Pattern](#28-value-object-pattern)
   - [2.9 Domain Events](#29-domain-events)

3. [Infrastructure Pattern-i](#3-infrastructure-pattern-i)
   - [3.1 Outbox Pattern](#31-outbox-pattern)
   - [3.2 Inbox Pattern](#32-inbox-pattern)
   - [3.3 API Gateway Pattern](#33-api-gateway-pattern)
   - [3.4 Idempotency Pattern](#34-idempotency-pattern)

4. [Cross-Cutting Concerns](#4-cross-cutting-concerns)
   - [4.1 Middleware Pattern](#41-middleware-pattern)
   - [4.2 Options Pattern](#42-options-pattern)
   - [4.3 Dependency Injection](#43-dependency-injection)
   - [4.4 Extension Methods](#44-extension-methods)

5. [Error Handling](#5-error-handling)
   - [5.1 Result Pattern za Error Handling](#51-result-pattern-za-error-handling)
   - [5.2 Global Exception Handling](#52-global-exception-handling)

---

## 1. Arhitekturni Pattern-i

### 1.1 Clean Architecture

**Opis**: LibraHub koristi Clean Architecture princip sa jasno definisanim slojevima i zavisnostima.

**Struktura slojeva**:

```
┌─────────────────────────────────────┐
│          API Layer                  │  ← Controllers, DTOs
├─────────────────────────────────────┤
│       Application Layer             │  ← Use Cases (CQRS), Validators
├─────────────────────────────────────┤
│         Domain Layer                │  ← Entities, Value Objects, Domain Logic
├─────────────────────────────────────┤
│      Infrastructure Layer           │  ← Persistence, External Services
└─────────────────────────────────────┘
```

**Primer strukture za jedan servis (Catalog)**:

```
LibraHub.Catalog.Api/
  ├── Controllers/          (API endpoints)
  ├── Dtos/                 (Data Transfer Objects)
  └── Extensions/           (DI configuration)

LibraHub.Catalog.Application/
  ├── Books/
  │   ├── Commands/        (Command handlers)
  │   └── Queries/         (Query handlers)
  ├── Abstractions/        (Repository interfaces)
  └── Validators/          (FluentValidation validators)

LibraHub.Catalog.Domain/
  ├── Books/              (Book entity, Value Objects)
  ├── Promotions/         (Promotion entities)
  └── Errors/             (Domain errors)

LibraHub.Catalog.Infrastructure/
  ├── Persistence/        (DbContext, Configurations)
  ├── Repositories/       (Repository implementations)
  └── Messaging/          (Event consumers)
```

**Karakteristike**:
- **Dependency Rule**: Slojevi su organizovani tako da zavisnosti idu samo ka unutra
- **Domain Layer je nezavisan**: Ne zavisi od bilo kog drugog sloja
- **Application Layer** zavisi samo od Domain layera
- **Infrastructure Layer** implementira interfejse definisane u Application i Domain layerima

**Primer koda - Clean Architecture Dependency**:

```csharp
// Domain Layer - Nema zavisnosti
namespace LibraHub.Catalog.Domain.Books;

public class Book
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    // ... domain logic
}

// Application Layer - Zavisi samo od Domain
namespace LibraHub.Catalog.Application.Books.Commands;

public class CreateBookHandler : IRequestHandler<CreateBookCommand, Result<Guid>>
{
    private readonly IBookRepository _repository; // Interface iz Application.Abstractions
    
    public async Task<Result<Guid>> Handle(CreateBookCommand request, ...)
    {
        var book = new Book(Guid.NewGuid(), request.Title); // Domain entity
        await _repository.AddAsync(book, cancellationToken);
        return Result.Success(book.Id);
    }
}

// Infrastructure Layer - Implementira Application interfejse
namespace LibraHub.Catalog.Infrastructure.Repositories;

public class BookRepository : IBookRepository // Interface iz Application.Abstractions
{
    private readonly CatalogDbContext _context; // Infrastructure persistence
    
    public async Task AddAsync(Book book, CancellationToken cancellationToken)
    {
        await _context.Books.AddAsync(book, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

---

### 1.2 Domain-Driven Design (DDD)

**Opis**: Projekt koristi DDD principe sa fokusom na domain model i business logiku.

#### 1.2.1 Aggregate Roots

**Aggregate Root** je glavna entitet koja kontroliše pristup svim entitetima unutar aggregate-a.

**Primer - Book Aggregate**:

```csharp
namespace LibraHub.Catalog.Domain.Books;

// Book je Aggregate Root
public class Book
{
    private readonly List<BookAuthor> _authors = new();
    public IReadOnlyCollection<BookAuthor> Authors => _authors.AsReadOnly();
    
    private readonly List<BookCategory> _categories = new();
    public IReadOnlyCollection<BookCategory> Categories => _categories.AsReadOnly();
    
    // Business invariants
    public void Publish(PricingPolicy pricingPolicy, BookContentState? contentState)
    {
        if (Status == BookStatus.Removed)
        {
            throw new InvalidOperationException("Cannot publish removed book");
        }
        
        // Validacija business pravila
        ValidatePublishingRequirements(pricingPolicy, contentState);
        
        Status = BookStatus.Published;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // Encapsulated operations
    public void AddAuthor(string authorName)
    {
        if (string.IsNullOrWhiteSpace(authorName))
        {
            throw new ArgumentException("Author name cannot be empty", nameof(authorName));
        }
        
        if (_authors.Any(a => a.Name == authorName))
        {
            return; // Idempotent operation
        }
        
        _authors.Add(new BookAuthor(Id, authorName));
        UpdatedAt = DateTime.UtcNow;
    }
}
```

**Karakteristike**:
- **Encapsulation**: Privatna polja (`_authors`, `_categories`) sa read-only kolekcijama
- **Business Logic**: Sva logika je u entity-ju, ne u service-u
- **Invariants**: Validacija business pravila u metodama
- **Aggregate Boundary**: Svi child entiteti se čuvaju kroz Aggregate Root

#### 1.2.2 Value Objects

**Value Object** je objekat definisan samo po vrednosti, ne po identitetu.

**Primer - Money Value Object**:

```csharp
namespace LibraHub.Catalog.Domain.Books;

public class Money
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    
    private Money() { } // For EF Core
    
    public Money(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        }
        
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency cannot be empty", nameof(currency));
        }
        
        Amount = amount;
        Currency = currency;
    }
    
    public static Money Zero(string currency) => new(0, currency);
    
    public bool IsFree => Amount == 0;
    
    // Value objects should implement equality by value
    public override bool Equals(object? obj)
    {
        if (obj is Money other)
        {
            return Amount == other.Amount && Currency == other.Currency;
        }
        return false;
    }
    
    public override int GetHashCode() => HashCode.Combine(Amount, Currency);
}
```

**Primer - Isbn Value Object**:

```csharp
public class Isbn
{
    public string Value { get; private set; } = string.Empty;
    
    private Isbn() { } // For EF Core
    
    public Isbn(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("ISBN cannot be empty", nameof(value));
        }
        
        if (value.Length < 10 || value.Length > 17)
        {
            throw new ArgumentException("ISBN must be between 10 and 17 characters", nameof(value));
        }
        
        Value = value;
    }
    
    // Implicit conversion za lakše korišćenje
    public static implicit operator string(Isbn isbn) => isbn.Value;
    public static implicit operator Isbn(string value) => new(value);
}
```

**Karakteristike Value Objects**:
- **Immutability**: Vrednosti se ne menjaju nakon kreiranja
- **Value-based Equality**: Dva objekta su jednaka ako imaju iste vrednosti
- **Self-validation**: Validacija se dešava u konstruktoru
- **No Identity**: Nemaju ID, identifikuju se po vrednosti

#### 1.2.3 Rich Domain Model

**Domain entiteti sadrže business logiku**, ne samo podatke.

**Primer - User Entity sa Business Logic-om**:

```csharp
namespace LibraHub.Identity.Domain.Users;

public class User
{
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockedOutUntil { get; private set; }
    
    // Business logic u domain entitetu
    public void RecordFailedLogin(int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= maxAttempts)
        {
            LockedOutUntil = DateTime.UtcNow.Add(lockoutDuration);
        }
    }
    
    public bool IsLockedOut(DateTime utcNow)
    {
        return LockedOutUntil.HasValue && LockedOutUntil.Value > utcNow;
    }
    
    public void RecordSuccessfulLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockedOutUntil = null;
    }
    
    // Business rules
    public void Disable(string reason)
    {
        Status = UserStatus.Disabled;
        // Može se dodati audit log, notifikacija, itd.
    }
}
```

#### 1.2.4 Domain Events

Domain Events omogućavaju komunikaciju između aggregate-a bez direktnih zavisnosti.

**Primer - Order Aggregate sa State Transitions**:

```csharp
namespace LibraHub.Orders.Domain.Orders;

public class Order
{
    public OrderStatus Status { get; private set; }
    
    public void StartPayment()
    {
        if (Status != OrderStatus.Created)
        {
            throw new InvalidOperationException($"Cannot start payment for order in {Status} status");
        }
        
        Status = OrderStatus.PaymentPending;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void MarkAsPaid()
    {
        if (Status != OrderStatus.PaymentPending)
        {
            throw new InvalidOperationException($"Cannot mark order as paid when status is {Status}");
        }
        
        Status = OrderStatus.Paid;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Paid)
        {
            throw new InvalidOperationException("Cannot cancel paid order. Use refund instead.");
        }
        
        Status = OrderStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

---

### 1.3 Microservices Architecture

**Opis**: LibraHub je podeljen u nezavisne mikroservise sa sopstvenim bazama podataka.

**Servisi**:
1. **Identity Service** - Autentifikacija, autorizacija, korisnici
2. **Catalog Service** - Knjige, promocije, obaveštenja
3. **Content Service** - Upload i streaming sadržaja
4. **Orders Service** - Narudžbine i plaćanja
5. **Library Service** - Biblioteka korisnika, entitlements
6. **Notifications Service** - Notifikacije korisnicima
7. **Gateway Service** - API Gateway (YARP)

**Komunikacija između servisa**:

```
┌─────────────┐
│   Gateway   │
└──────┬──────┘
       │
       ├──→ Identity API
       ├──→ Catalog API
       ├──→ Orders API
       │
       └──→ RabbitMQ ←──┐
                        │
            ┌───────────┴───────────┐
            │                       │
       OrderPaid Event      BookPublished Event
            │                       │
       Library Service    Notifications Service
```

**Karakteristike**:
- **Database per Service**: Svaki servis ima sopstvenu bazu podataka
- **Asynchronous Communication**: Event-driven komunikacija kroz RabbitMQ
- **API Gateway**: Centralizovan pristup preko Gateway servisa
- **Service Independence**: Svaki servis može biti nezavisno deploy-ovan

---

### 1.4 CQRS (Command Query Responsibility Segregation)

**Opis**: Separira čitanje i pisanje podataka kroz različite modele.

**Struktura**:

```
Commands (Write)
  ├── CreateBookCommand
  ├── UpdateBookCommand
  └── PublishBookCommand
      ↓
  Command Handlers
      ↓
  Domain Model (Write)

Queries (Read)
  ├── GetBookQuery
  ├── SearchBooksQuery
  └── GetAnnouncementsQuery
      ↓
  Query Handlers
      ↓
  Read Model (Projections)
```

**Primer - Command Handler**:

```csharp
namespace LibraHub.Catalog.Application.Books.Commands.CreateBook;

public class CreateBookCommand : IRequest<Result<Guid>>
{
    public string Title { get; init; } = string.Empty;
}

public class CreateBookHandler(
    IBookRepository bookRepository,
    IOutboxWriter outboxWriter) : IRequestHandler<CreateBookCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateBookCommand request, CancellationToken cancellationToken)
    {
        // Create domain entity
        var book = new Book(Guid.NewGuid(), request.Title);
        
        // Persist
        await bookRepository.AddAsync(book, cancellationToken);
        
        // Publish event
        await outboxWriter.WriteAsync(
            new Contracts.Catalog.V1.BookCreatedV1
            {
                BookId = book.Id,
                Title = book.Title,
                CreatedAt = book.CreatedAt
            },
            Contracts.Common.EventTypes.BookCreated,
            cancellationToken);
        
        return Result.Success(book.Id);
    }
}
```

**Primer - Query Handler**:

```csharp
namespace LibraHub.Catalog.Application.Books.Queries.SearchBooks;

public class SearchBooksQuery : IRequest<Result<SearchBooksResponse>>
{
    public string? SearchTerm { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class SearchBooksHandler(
    IBookRepository bookRepository) : IRequestHandler<SearchBooksQuery, Result<SearchBooksResponse>>
{
    public async Task<Result<SearchBooksResponse>> Handle(SearchBooksQuery request, CancellationToken cancellationToken)
    {
        var books = await bookRepository.SearchAsync(
            request.SearchTerm,
            request.Page,
            request.PageSize,
            cancellationToken);
        
        var totalCount = await bookRepository.CountSearchAsync(
            request.SearchTerm,
            cancellationToken);
        
        return Result.Success(new SearchBooksResponse
        {
            Books = books.Select(b => new BookDto
            {
                Id = b.Id,
                Title = b.Title,
                Status = b.Status.ToString()
            }).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }
}
```

**Karakteristike**:
- **Separation**: Commands i Queries su potpuno odvojeni
- **MediatR**: Koristi MediatR biblioteku za dispatch
- **Optimized Reads**: Query model može biti optimizovan za čitanje
- **Business Logic**: Commands sadrže business logiku, Queries samo vraćaju podatke

---

### 1.5 Event-Driven Architecture

**Opis**: Servisi komuniciraju asinhrono kroz event-e.

**Event Flow**:

```
Service A (Producer)
  │
  ├─→ Outbox Table (Transaction)
  │
  └─→ Outbox Publisher Worker
           │
           ├─→ RabbitMQ Exchange
           │
           └─→ Event Consumer (Service B)
                    │
                    └─→ Inbox Table (Idempotency)
                            │
                            └─→ Business Logic
```

**Primer - Event Production**:

```csharp
// Orders Service - Produkuje OrderPaid event
public class CapturePaymentHandler
{
    public async Task<Result> Handle(CapturePaymentCommand request, ...)
    {
        // Update order
        order.MarkAsPaid();
        await orderRepository.UpdateAsync(order, cancellationToken);
        
        // Write event to outbox (u istoj transakciji)
        await outboxWriter.WriteAsync(
            new Contracts.Orders.V1.OrderPaidV1
            {
                OrderId = order.Id,
                UserId = order.UserId,
                Items = order.Items.Select(i => new OrderPaidItemV1
                {
                    BookId = i.BookId,
                    Price = i.FinalPrice.Amount
                }).ToList(),
                PaidAt = DateTime.UtcNow
            },
            Contracts.Common.EventTypes.OrderPaid,
            cancellationToken);
        
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitTransactionAsync(cancellationToken);
        
        return Result.Success();
    }
}
```

**Primer - Event Consumption**:

```csharp
// Library Service - Konzumira OrderPaid event
public class OrderPaidConsumer
{
    public async Task HandleAsync(OrderPaidV1 @event, CancellationToken cancellationToken)
    {
        // Idempotency check
        var messageId = $"OrderPaid_{@event.OrderId}";
        if (await inboxRepository.IsProcessedAsync(messageId, cancellationToken))
        {
            return; // Already processed
        }
        
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Create entitlements
            foreach (var item in @event.Items)
            {
                var entitlement = new Entitlement(
                    Guid.NewGuid(),
                    @event.UserId,
                    item.BookId,
                    EntitlementSource.Purchase,
                    @event.OrderId);
                
                await entitlementRepository.AddAsync(entitlement, cancellationToken);
            }
            
            // Mark as processed
            await inboxRepository.MarkAsProcessedAsync(messageId, cancellationToken);
            
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
```

**Karakteristike**:
- **Asynchronous**: Events se obrađuju asinhrono
- **Reliable**: Outbox pattern osigurava da se event-ovi ne izgube
- **Idempotent**: Inbox pattern osigurava idempotent processing
- **Loose Coupling**: Servisi su slabo povezani kroz event-e

---

## 2. Design Pattern-i

### 2.1 Repository Pattern

**Opis**: Abstrakcija između domain logike i data access logike.

**Struktura**:

```
Application Layer
  └── IBookRepository (Interface)
           │
           │
Infrastructure Layer
  └── BookRepository (Implementation)
           │
           └── DbContext (Entity Framework)
```

**Primer - Repository Interface**:

```csharp
namespace LibraHub.Catalog.Application.Abstractions;

public interface IBookRepository
{
    Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<List<Book>> SearchAsync(
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    
    Task<int> CountSearchAsync(
        string? searchTerm,
        CancellationToken cancellationToken = default);
    
    Task AddAsync(Book book, CancellationToken cancellationToken = default);
    
    Task UpdateAsync(Book book, CancellationToken cancellationToken = default);
}
```

**Primer - Repository Implementation**:

```csharp
namespace LibraHub.Catalog.Infrastructure.Repositories;

public class BookRepository : IBookRepository
{
    private readonly CatalogDbContext _context;
    
    public BookRepository(CatalogDbContext context)
    {
        _context = context;
    }
    
    public async Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Books
            .Include(b => b.Authors)
            .Include(b => b.Categories)
            .Include(b => b.Tags)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }
    
    public async Task<List<Book>> SearchAsync(
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = BuildSearchQuery(searchTerm);
        
        return await query
            .OrderBy(b => b.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
    
    private IQueryable<Book> BuildSearchQuery(string? searchTerm)
    {
        var query = _context.Books
            .Include(b => b.Authors)
            .AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = $"%{searchTerm.ToLowerInvariant()}%";
            query = query.Where(b =>
                EF.Functions.ILike(b.Title, term) ||
                (b.Description != null && EF.Functions.ILike(b.Description, term)) ||
                b.Authors.Any(a => EF.Functions.ILike(a.Name, term)));
        }
        
        return query;
    }
}
```

**Karakteristike**:
- **Abstraction**: Application layer ne zna o EF Core
- **Testability**: Lako se može mock-ovati za unit testove
- **Flexibility**: Može se promeniti implementacija bez promene Application layer-a
- **Domain Focus**: Repository radi sa Domain entitetima, ne DTOs

---

### 2.2 Unit of Work Pattern

**Opis**: Upravlja transakcijama i koordinacijom između više repository-ja.

**Primer - Unit of Work Interface**:

```csharp
namespace LibraHub.BuildingBlocks.Abstractions;

public interface IUnitOfWork
{
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

**Primer - Unit of Work Implementation**:

```csharp
namespace LibraHub.BuildingBlocks.Persistence;

public class UnitOfWork<TDbContext>(TDbContext context) : IUnitOfWork<TDbContext> 
    where TDbContext : DbContext
{
    private IDbContextTransaction? _transaction;
    
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    }
    
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
    
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
    
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
```

**Primer - Korišćenje**:

```csharp
public class CreateOrderHandler
{
    public async Task<Result<Guid>> Handle(CreateOrderCommand request, ...)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // Multiple operations u jednoj transakciji
            await orderRepository.AddAsync(order, cancellationToken);
            await outboxWriter.WriteAsync(event, eventType, cancellationToken);
            
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            
            return Result.Success(order.Id);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
```

**Karakteristike**:
- **Transaction Management**: Centralizovano upravljanje transakcijama
- **Atomicity**: Svi operacije su atomične
- **Consistency**: Osigurava konzistentnost podataka

---

### 2.3 Factory Pattern

**Opis**: Kreira objekte bez direktnog instanciranja.

**Primer - Money Factory**:

```csharp
public class Money
{
    public static Money Zero(string currency) => new(0, currency);
    
    public static Money FromAmount(decimal amount, string currency) => new(amount, currency);
}
```

**Primer - Order Factory Method**:

```csharp
public class Order
{
    public static Order Create(
        Guid id,
        Guid userId,
        List<OrderItem> items,
        Money subtotal,
        Money vatTotal,
        Money total)
    {
        return new Order(id, userId, items, subtotal, vatTotal, total);
    }
}
```

---

### 2.4 Strategy Pattern

**Opis**: Omogućava izbor algoritma u runtime-u.

**Primer - Payment Gateway Strategy**:

```csharp
namespace LibraHub.Orders.Application.Abstractions;

public interface IPaymentGateway
{
    Task<PaymentResult> InitiatePaymentAsync(
        Guid orderId,
        Money amount,
        PaymentProvider provider,
        CancellationToken cancellationToken = default);
    
    Task<PaymentResult> CapturePaymentAsync(
        string providerReference,
        CancellationToken cancellationToken = default);
}

// Concrete Strategy
public class MockPaymentGateway : IPaymentGateway
{
    public async Task<PaymentResult> InitiatePaymentAsync(
        Guid orderId,
        Money amount,
        PaymentProvider provider,
        CancellationToken cancellationToken = default)
    {
        // Mock payment logic
        var providerReference = $"mock_{orderId}_{Guid.NewGuid():N}";
        return PaymentResult.Succeeded(providerReference);
    }
    
    public async Task<PaymentResult> CapturePaymentAsync(
        string providerReference,
        CancellationToken cancellationToken = default)
    {
        return PaymentResult.Succeeded(providerReference);
    }
}

// Usage
public class StartPaymentHandler
{
    private readonly IPaymentGateway _paymentGateway;
    
    public async Task<Result<Guid>> Handle(StartPaymentCommand request, ...)
    {
        var result = await _paymentGateway.InitiatePaymentAsync(
            order.Id,
            order.Total,
            request.Provider,
            cancellationToken);
        
        if (result.IsSuccess)
        {
            order.StartPayment();
            // ...
        }
    }
}
```

**Karakteristike**:
- **Flexibility**: Može se promeniti strategija bez promene klijenta
- **Open/Closed Principle**: Otvoren za proširenje, zatvoren za modifikaciju
- **Dependency Injection**: Strategije se inject-uju kroz DI

---

### 2.5 Mediator Pattern

**Opis**: MediatR se koristi za komunikaciju između komponenti bez direktnih referenci.

**Struktura**:

```
Controller
  │
  ├─→ IMediator.Send(Command)
  │       │
  │       └─→ CommandHandler.Handle()
  │
  └─→ IMediator.Send(Query)
          │
          └─→ QueryHandler.Handle()
```

**Primer - Korišćenje**:

```csharp
[ApiController]
[Route("books")]
public class BooksController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateBook(
        [FromBody] CreateBookRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateBookCommand { Title = request.Title };
        var result = await mediator.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }
    
    [HttpGet]
    public async Task<IActionResult> SearchBooks(
        [FromQuery] string? searchTerm,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken)
    {
        var query = new SearchBooksQuery(searchTerm, page, 20);
        var result = await mediator.Send(query, cancellationToken);
        return result.ToActionResult(this);
    }
}
```

**Karakteristike**:
- **Decoupling**: Controller ne zna o Handler-im
- **Single Responsibility**: Svaki Handler ima jednu odgovornost
- **Pipeline**: MediatR podržava pipeline behaviors (validacija, logging, itd.)

---

### 2.6 Result Pattern

**Opis**: Eksplicitno predstavlja uspeh ili neuspeh operacije.

**Implementacija**:

```csharp
namespace LibraHub.BuildingBlocks.Results;

public class Result
{
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; private set; }
    
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error != null)
            throw new InvalidOperationException("Successful result cannot have an error");
        if (!isSuccess && error == null)
            throw new InvalidOperationException("Failed result must have an error");
        
        IsSuccess = isSuccess;
        Error = error;
    }
    
    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);
    
    public static Result<T> Success<T>(T value) => new(value, true, null);
    public static Result<T> Failure<T>(Error error) => new(default!, false, error);
}

public class Result<T> : Result
{
    public T Value { get; private set; }
    
    internal Result(T value, bool isSuccess, Error? error)
        : base(isSuccess, error)
    {
        Value = value;
    }
}

public class Error
{
    public string Code { get; init; }
    public string Message { get; init; }
    public Dictionary<string, object>? Details { get; init; }
    
    public static Error NotFound(string message) => new()
    {
        Code = "NOT_FOUND",
        Message = message
    };
    
    public static Error Validation(string message) => new()
    {
        Code = "VALIDATION_ERROR",
        Message = message
    };
}
```

**Primer - Korišćenje**:

```csharp
public class CreateBookHandler
{
    public async Task<Result<Guid>> Handle(CreateBookCommand request, ...)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Result.Failure<Guid>(Error.Validation("Title is required"));
        }
        
        var book = new Book(Guid.NewGuid(), request.Title);
        await bookRepository.AddAsync(book, cancellationToken);
        
        return Result.Success(book.Id);
    }
}

// Usage
var result = await mediator.Send(command, cancellationToken);
if (result.IsSuccess)
{
    return Ok(result.Value);
}
else
{
    return BadRequest(result.Error);
}
```

**Karakteristike**:
- **Explicit Error Handling**: Greške su eksplicitne, ne exception-i
- **Type Safety**: Compiler osigurava da se Value ne pristupa kada je Failure
- **Composable**: Rezultati se mogu kombinovati

---

### 2.7 Specification Pattern

**Opis**: Encapsulira business logiku za filtriranje i validaciju.

**Primer - Promotion Rule Application**:

```csharp
public class PromotionRule
{
    public PromotionScope AppliesToScope { get; private set; }
    public List<string>? ScopeValues { get; private set; }
    public List<Guid>? Exclusions { get; private set; }
    
    public bool AppliesTo(Book book)
    {
        // Exclusion check
        if (Exclusions != null && Exclusions.Contains(book.Id))
        {
            return false;
        }
        
        // Scope-based check
        return AppliesToScope switch
        {
            PromotionScope.All => true,
            PromotionScope.Category => ScopeValues?.Any(c => 
                book.Categories.Any(bc => bc.Name == c)) ?? false,
            PromotionScope.Book => ScopeValues?.Contains(book.Id.ToString()) ?? false,
            PromotionScope.Author => ScopeValues?.Any(a => 
                book.Authors.Any(ba => ba.Name == a)) ?? false,
            _ => false
        };
    }
}
```

---

### 2.8 Value Object Pattern

**Videti sekciju 1.2.2** - Value Objects u DDD.

---

### 2.9 Domain Events

**Videti sekciju 1.5** - Event-Driven Architecture.

---

## 3. Infrastructure Pattern-i

### 3.1 Outbox Pattern

**Opis**: Osigurava reliable event publishing kroz database transakcije.

**Struktura**:

```
Business Logic
  │
  ├─→ Save Entity
  │
  ├─→ Write to Outbox Table (same transaction)
  │
  └─→ Commit Transaction
           │
           │
Outbox Publisher Worker (Background Service)
  │
  ├─→ Read Pending Messages
  │
  ├─→ Publish to RabbitMQ
  │
  └─→ Mark as Processed
```

**Primer - OutboxWriter**:

```csharp
namespace LibraHub.BuildingBlocks.Outbox;

public class OutboxEventPublisher<TDbContext> : IOutboxWriter 
    where TDbContext : DbContext
{
    private readonly TDbContext _context;
    
    public async Task WriteAsync<T>(
        T integrationEvent,
        string eventType,
        CancellationToken cancellationToken = default) where T : class
    {
        var payload = JsonSerializer.Serialize(integrationEvent, _jsonOptions);
        
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Payload = payload,
            CreatedAt = DateTime.UtcNow
        };
        
        await _context.Set<OutboxMessage>().AddAsync(outboxMessage, cancellationToken);
        // Ne poziva SaveChangesAsync ovde - poziva se u UnitOfWork
    }
}
```

**Primer - Outbox Publisher Worker**:

```csharp
public abstract class OutboxPublisherWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var messages = await GetPendingMessagesAsync(50, cancellationToken);
            
            foreach (var message in messages)
            {
                try
                {
                    // Publish to RabbitMQ
                    _channel.BasicPublish(
                        exchange: _exchangeName,
                        routingKey: message.EventType,
                        body: Encoding.UTF8.GetBytes(message.Payload));
                    
                    await MarkAsProcessedAsync(message.Id, cancellationToken);
                }
                catch (Exception ex)
                {
                    await MarkAsFailedAsync(message.Id, ex.Message, cancellationToken);
                }
            }
            
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
    }
}
```

**Karakteristike**:
- **Reliability**: Events se ne gube čak i ako RabbitMQ nije dostupan
- **Transactional**: Event i business data se čuvaju u istoj transakciji
- **Eventually Consistent**: Events se procesiraju eventualno

---

### 3.2 Inbox Pattern

**Opis**: Osigurava idempotent processing event-ova.

**Struktura**:

```
RabbitMQ Message
  │
  └─→ Event Consumer
           │
           ├─→ Check Inbox (messageId)
           │
           ├─→ If processed → Skip
           │
           ├─→ If not processed:
           │       │
           │       ├─→ Process Business Logic
           │       │
           │       └─→ Mark as Processed (same transaction)
           │
           └─→ Commit Transaction
```

**Primer - Inbox Repository**:

```csharp
public interface IInboxRepository
{
    Task<bool> IsProcessedAsync(string messageId, CancellationToken cancellationToken = default);
    
    Task MarkAsProcessedAsync(string messageId, CancellationToken cancellationToken = default);
}

// Usage in Consumer
public class OrderPaidConsumer
{
    public async Task HandleAsync(OrderPaidV1 @event, CancellationToken cancellationToken)
    {
        var messageId = $"OrderPaid_{@event.OrderId}";
        
        if (await inboxRepository.IsProcessedAsync(messageId, cancellationToken))
        {
            return; // Already processed - idempotent
        }
        
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Process business logic
            // ...
            
            // Mark as processed
            await inboxRepository.MarkAsProcessedAsync(messageId, cancellationToken);
            
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
```

**Karakteristike**:
- **Idempotency**: Event se obrađuje samo jednom
- **Reliability**: Duplicate messages ne uzrokuju probleme
- **Transactional**: Inbox mark i business logic u istoj transakciji

---

### 3.3 API Gateway Pattern

**Opis**: Centralizovan ulazni tačka za sve API zahteve.

**Implementacija - YARP (Yet Another Reverse Proxy)**:

```csharp
// Gateway Program.cs
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

app.MapReverseProxy();
```

**Konfiguracija**:

```json
{
  "ReverseProxy": {
    "Routes": {
      "identity-route": {
        "Match": {
          "Path": "/api/auth/{**catch-all}"
        },
        "ClusterId": "identity-cluster"
      },
      "catalog-route": {
        "Match": {
          "Path": "/api/books/{**catch-all}"
        },
        "ClusterId": "catalog-cluster"
      }
    },
    "Clusters": {
      "identity-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://identity-api:8080"
          }
        }
      }
    }
  }
}
```

**Karakteristike**:
- **Routing**: Usmerava zahteve na odgovarajuće servise
- **Authentication**: Centralizovana autentifikacija
- **Rate Limiting**: Može se primeniti globalno
- **Load Balancing**: Distribuira zahteve

---

### 3.4 Idempotency Pattern

**Opis**: Osigurava da se isti zahtev može bezbedno ponoviti.

**Implementacija - Middleware**:

```csharp
namespace LibraHub.BuildingBlocks.Idempotency;

public class IdempotencyKeyMiddleware
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";
    
    public async Task InvokeAsync(
        HttpContext context,
        IIdempotencyStore idempotencyStore)
    {
        if (context.Request.Method != "GET" && 
            context.Request.Headers.TryGetValue(IdempotencyKeyHeader, out var value))
        {
            var idempotencyKey = value.ToString();
            
            // Check if already processed
            var existingResponse = await idempotencyStore.GetResponseAsync(
                idempotencyKey,
                context.RequestAborted);
            
            if (existingResponse != null)
            {
                // Return cached response
                context.Response.StatusCode = existingResponse.StatusCode;
                context.Response.ContentType = existingResponse.ContentType;
                await context.Response.Body.WriteAsync(
                    existingResponse.Body,
                    context.RequestAborted);
                return;
            }
            
            // Capture response
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;
            
            await next(context);
            
            // Store response if successful
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseBytes = await ReadStreamAsync(responseBody);
                
                await idempotencyStore.StoreResponseAsync(
                    idempotencyKey,
                    context.Response.StatusCode,
                    context.Response.ContentType ?? "application/json",
                    responseBytes,
                    context.RequestAborted);
            }
            
            // Copy response back
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream, context.RequestAborted);
            context.Response.Body = originalBodyStream;
        }
        else
        {
            await next(context);
        }
    }
}
```

**Usage**:

```http
POST /api/orders
Idempotency-Key: unique-request-id-123
Content-Type: application/json

{
  "bookIds": ["..."]
}
```

**Karakteristike**:
- **Safety**: Duplicate requests ne uzrokuju duplikate akcija
- **Retry-Friendly**: Klijenti mogu bezbedno ponoviti zahtev
- **Performance**: Cached response se vraća bez procesiranja

---

## 4. Cross-Cutting Concerns

### 4.1 Middleware Pattern

**Opis**: Pipeline za obradu HTTP zahteva.

**Primer - Exception Handling Middleware**:

```csharp
namespace LibraHub.BuildingBlocks.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        
        var error = new Error("INTERNAL_ERROR", "An internal error occurred");
        var response = new { code = error.Code, message = error.Message };
        
        var json = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(json);
    }
}
```

**Registracija**:

```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

**Karakteristike**:
- **Pipeline**: Middleware se izvršava u redosledu registracije
- **Early Termination**: Middleware može zaustaviti pipeline
- **Cross-Cutting**: Logika se primenjuje na sve zahteve

---

### 4.2 Options Pattern

**Opis**: Tipizovana konfiguracija kroz IOptions<T>.

**Primer - Options Class**:

```csharp
namespace LibraHub.Orders.Infrastructure.Options;

public class MockPaymentOptions
{
    public bool UseAmountBasedFailure { get; set; }
    public List<string> FailureAmountEndings { get; set; } = new();
    public int FailureProbabilityPercent { get; set; }
    public List<string> FailureReasons { get; set; } = new();
}
```

**Registracija**:

```csharp
builder.Services.Configure<MockPaymentOptions>(
    builder.Configuration.GetSection("MockPayment"));
```

**Korišćenje**:

```csharp
public class MockPaymentGateway
{
    private readonly MockPaymentOptions _options;
    
    public MockPaymentGateway(IOptions<MockPaymentOptions> options)
    {
        _options = options.Value;
    }
}
```

**Karakteristike**:
- **Type Safety**: Compile-time provera
- **Validation**: Može se validirati kroz DataAnnotations
- **Hierarchical**: Podržava nested konfiguraciju

---

### 4.3 Dependency Injection

**Opis**: Inversion of Control pattern kroz .NET DI container.

**Registracija servisa**:

```csharp
// Extension method pattern za organizaciju
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogApplicationServices(
        this IServiceCollection services)
    {
        // MediatR
        services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssembly(typeof(ApplicationAssembly).Assembly));
        
        // FluentValidation
        services.AddValidatorsFromAssembly(typeof(ApplicationAssembly).Assembly);
        
        // Repositories
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IPricingRepository, PricingRepository>();
        
        return services;
    }
}
```

**Korišćenje**:

```csharp
public class CreateBookHandler(
    IBookRepository bookRepository,  // Injected
    IOutboxWriter outboxWriter)      // Injected
    : IRequestHandler<CreateBookCommand, Result<Guid>>
{
    // Constructor injection
}
```

**Karakteristike**:
- **Lifetime Management**: Scoped, Transient, Singleton
- **Interface-based**: Zavisnosti su preko interfejsa
- **Testability**: Lako se mock-uje

---

### 4.4 Extension Methods

**Opis**: Proširenje postojećih klasa bez modifikacije.

**Primer - Result Extensions**:

```csharp
public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        ControllerBase controller)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(result.Value);
        }
        
        return result.Error?.Code switch
        {
            "NOT_FOUND" => controller.NotFound(result.Error),
            "VALIDATION_ERROR" => controller.BadRequest(result.Error),
            _ => controller.StatusCode(500, result.Error)
        };
    }
}
```

**Korišćenje**:

```csharp
var result = await mediator.Send(command, cancellationToken);
return result.ToActionResult(this);
```

---

## 5. Error Handling

### 5.1 Result Pattern za Error Handling

**Videti sekciju 2.6** - Result Pattern.

### 5.2 Global Exception Handling

**Videti sekciju 4.1** - Exception Handling Middleware.

---

## Završne Napomene

### Best Practices

1. **Domain Logic u Entities**: Business logika pripada domain entitetima
2. **Thin Controllers**: Controllers samo delegiraju zahteve MediatR-u
3. **Explicit Errors**: Koristi Result pattern umesto exception-a za business greške
4. **Transaction Boundaries**: Koristi Unit of Work za transakcije
5. **Idempotency**: Uvek razmisli o idempotentnosti operacija
6. **Event Sourcing Ready**: Outbox pattern omogućava laku migraciju na Event Sourcing

### Performance Considerations

1. **Eager Loading**: Koristi `.Include()` za eager loading gde je potrebno
2. **Projections**: Koristi projections za optimizaciju query-ja
3. **Batch Processing**: Outbox publisher obrađuje poruke u batch-ovima
4. **Async/Await**: Sve I/O operacije su asinhrone

### Testing Strategy

1. **Unit Tests**: Testiraj domain entitete i value objects
2. **Integration Tests**: Testiraj repository implementacije
3. **Contract Tests**: Testiraj event contracts
4. **E2E Tests**: Testiraj kroz API Gateway

---

**Verzija dokumentacije**: 1.0  
**Poslednja ažuriranje**: 2024-12-23

