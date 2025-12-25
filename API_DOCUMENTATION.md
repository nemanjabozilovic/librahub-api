# LibraHub - API Dokumentacija

## 📋 Sadržaj

1. [Pregled projekta](#pregled-projekta)
2. [Arhitektura](#arhitektura)
3. [Servisi](#servisi)
4. [API Endpoint-i](#api-endpoint-i)
5. [Autentifikacija i Autorizacija](#autentifikacija-i-autorizacija)
6. [Modeli podataka (DTO)](#modeli-podataka-dto)
7. [Konfiguracija](#konfiguracija)
8. [Pokretanje projekta](#pokretanje-projekta)

---

## Pregled projekta

**LibraHub** je mikroservisna platforma za digitalnu biblioteku koja omogućava korisnicima da kupuju, čitaju i upravljaju elektronskim knjigama. Sistem je podignut na **.NET 8** tehnologiji i koristi Clean Architecture princip sa Domain-Driven Design (DDD) pristupom.

### Tehnologije

- **Backend**: .NET 8, ASP.NET Core
- **Baza podataka**: PostgreSQL 16
- **Message Broker**: RabbitMQ
- **Storage**: MinIO (S3-compatible)
- **Cache**: Redis
- **Gateway**: YARP (Yet Another Reverse Proxy)
- **Docker**: Containerizacija svih servisa

---

## Arhitektura

Projekat koristi **mikroservisnu arhitekturu** sa sledećim servisima:

### Gateway Servis
- **Lokacija**: `services/Gateway`
- **Port**: 5000 (HTTP), 5001 (HTTPS)
- **Svrha**: Centralni API Gateway koji rutira zahteve ka backend servisima i upravlja autentifikacijom/autorizacijom

### Backend Servisi

1. **Identity** (`services/Identity`) - Port: 60950
2. **Catalog** (`services/Catalog`) - Port: 60960
3. **Content** (`services/Content`) - Port: 60970
4. **Orders** (`services/Orders`) - Port: 60980
5. **Library** (`services/Library`) - Port: 60990
6. **Notifications** (`services/Notifications`) - Port: 61000

### Infrastruktura

- **PostgreSQL**: Port 5432
- **RabbitMQ Management**: Port 15672
- **Redis**: Port 6379
- **pgAdmin**: Port 5050

---

## Servisi

### 1. Identity Service

**Svrha**: Upravljanje korisnicima, autentifikacija i autorizacija

**Funkcionalnosti**:
- Registracija korisnika
- Login/Logout
- Email verifikacija
- Password reset
- Token refresh
- Upravljanje korisničkim profilima
- Upravljanje ulogama (Admin, Librarian, User)

**Baza podataka**: `librahub_identity`

### 2. Catalog Service

**Svrha**: Upravljanje katalogom knjiga, cenama, promocijama i obaveštenjima

**Funkcionalnosti**:
- CRUD operacije za knjige
- Upravljanje cenama
- Promocione kampanje
- Obaveštenja (announcements)
- Pretraga knjiga

**Baza podataka**: `librahub_catalog`

**Uloge**:
- `Librarian`: Može kreirati, ažurirati i objavljivati knjige
- `Admin`: Može uklanjati knjige

### 3. Content Service

**Svrha**: Upravljanje sadržajem knjiga (cover slike, e-book fajlovi)

**Funkcionalnosti**:
- Upload cover slika
- Upload e-book izdanja
- Generisanje read tokena za pristup sadržaju
- Streaming sadržaja

**Baza podataka**: `librahub_content`

**Storage**: MinIO buckets (`covers`, `editions`)

### 4. Orders Service

**Svrha**: Upravljanje narudžbinama i plaćanjima

**Funkcionalnosti**:
- Kreiranje narudžbina
- Plaćanje (mock payment gateway)
- Otkazivanje narudžbina
- Povraćaj novca (refund)

**Baza podataka**: `librahub_orders`

### 5. Library Service

**Svrha**: Upravljanje korisničkom bibliotekom i čitanjem

**Funkcionalnosti**:
- Lista kupljenih knjiga korisnika
- Praćenje napretka čitanja
- Upravljanje entitlementima

**Baza podataka**: `librahub_library`

### 6. Notifications Service

**Svrha**: Slanje notifikacija korisnicima

**Funkcionalnosti**:
- Notifikacije za nove knjige
- Notifikacije za promocije
- Notifikacije za narudžbine
- Podešavanja notifikacija

**Baza podataka**: `librahub_notifications`

---

## API Endpoint-i

### Base URL

**Development**: `http://localhost:5000` (Gateway)

Svi endpoint-i su rutirani kroz Gateway i imaju prefix `/api`.

---

### 🔐 Identity & Auth

#### Registracija
```
POST /api/auth/register
Content-Type: application/json

{
  "email": "string",
  "password": "string",
  "firstName": "string",
  "lastName": "string",
  "phone": "string?",
  "dateOfBirth": "YYYY-MM-DD"
}

Response: 204 No Content
Body: Empty

Napomene:
- Nakon uspešne registracije, korisnik dobija email sa verification link-om
- Link format: http://localhost:3000/verify-email?token={token}
- Token ističe za 7 dana
```

#### Login
```
POST /api/auth/login
Content-Type: application/json

{
  "email": "string",
  "password": "string"
}

Response: 200 OK
{
  "accessToken": "string",
  "refreshToken": "string",
  "expiresAt": "2024-01-01T00:00:00Z"
}
```

#### Refresh Token
```
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "string"
}

Response: 200 OK
{
  "accessToken": "string",
  "refreshToken": "string",
  "expiresAt": "2024-01-01T00:00:00Z"
}
```

#### Email Verifikacija
```
POST /api/auth/verify-email
Content-Type: application/json

{
  "token": "string"
}

Response: 200 OK
Body: Empty

Napomene:
- Token se dobija iz email-a (query parameter u link-u)
- Token može biti korišćen samo jednom
- Ako je email već verifikovan, vraća 200 OK (idempotent)
```

#### Forgot Password
```
POST /api/auth/forgot-password
Content-Type: application/json

{
  "email": "string"
}

Response: 200 OK
```

#### Reset Password
```
POST /api/auth/reset-password
Content-Type: application/json

{
  "token": "string",
  "newPassword": "string",
  "confirmPassword": "string"
}

Response: 200 OK
```

#### Complete Registration
```
POST /api/users/complete-registration
Content-Type: application/json

{
  "token": "string",
  "firstName": "string",
  "lastName": "string",
  "phone": "string?",
  "dateOfBirth": "YYYY-MM-DD"
}

Response: 200 OK
```

#### Get Me (Current User)
```
GET /api/me
Authorization: Bearer {accessToken}

Response: 200 OK
{
  "userId": "guid",
  "email": "string",
  "roles": ["string"],
  "emailVerified": boolean,
  "status": "string"
}
```

Status values: "Active", "Disabled", "Pending"

---

### 📚 Catalog

#### Pretraga knjiga
```
GET /api/books?searchTerm={term}&page={page}&pageSize={size}
Authorization: Not required

Response: 200 OK
{
  "books": [
    {
      "id": "guid",
      "title": "string",
      "description": "string?",
      "status": "string", // Draft, Published, Unlisted
      "authors": ["string"]
    }
  ],
  "totalCount": 0,
  "page": 1,
  "pageSize": 20,
  "totalPages": 0
}
```

#### Dohvatanje knjige
```
GET /api/books/{id}
Authorization: Not required

Response: 200 OK
{
  "id": "guid",
  "title": "string",
  "description": "string?",
  "language": "string?",
  "publisher": "string?",
  "publicationDate": "YYYY-MM-DDTHH:mm:ssZ?",
  "isbn": "string?",
  "status": "string", // Draft, Published, Unlisted
  "authors": ["string"],
  "categories": ["string"],
  "tags": ["string"],
  "pricing": {
    "price": 0.0,
    "currency": "string",
    "vatRate": 0.0?,
    "promoPrice": 0.0?,
    "promoStartDate": "YYYY-MM-DDTHH:mm:ssZ?",
    "promoEndDate": "YYYY-MM-DDTHH:mm:ssZ?"
  }?
}
```

#### Kreiranje knjige
```
POST /api/books
Authorization: Bearer {accessToken} (Librarian role required)
Content-Type: application/json

{
  "title": "string"
}

Response: 201 Created
{
  "value": "guid" // Book ID
}
```

#### Ažuriranje knjige
```
PUT /api/books/{id}
Authorization: Bearer {accessToken} (Librarian role required)
Content-Type: application/json

{
  "description": "string?",
  "language": "string?",
  "publisher": "string?",
  "publicationDate": "YYYY-MM-DD?",
  "isbn": "string?",
  "authors": ["string"]?,
  "categories": ["string"]?,
  "tags": ["string"]?
}

Response: 204 No Content
```

#### Postavljanje cene
```
POST /api/books/{id}/pricing
Authorization: Bearer {accessToken} (Librarian role required)
Content-Type: application/json

{
  "price": 0.0,
  "currency": "string", // e.g., "USD", "EUR", "RSD"
  "vatRate": 0.0, // e.g., 0.20 for 20%
  "promoPrice": 0.0?,
  "promoStartDate": "YYYY-MM-DDTHH:mm:ssZ"?,
  "promoEndDate": "YYYY-MM-DDTHH:mm:ssZ"?
}

Response: 204 No Content
```

#### Objava knjige
```
POST /api/books/{id}/publish
Authorization: Bearer {accessToken} (Librarian role required)

Response: 204 No Content
```

#### Uklanjanje sa liste
```
POST /api/books/{id}/unlist
Authorization: Bearer {accessToken} (Librarian role required)

Response: 204 No Content
```

#### Brisanje knjige
```
POST /api/books/{id}/remove
Authorization: Bearer {accessToken} (Admin role required)
Content-Type: application/json

{
  "reason": "string"
}

Response: 204 No Content
```

#### Obaveštenja
```
GET /api/announcements?page={page}&pageSize={size}
Authorization: Not required

Response: 200 OK
{
  "items": [
    {
      "id": "guid",
      "title": "string",
      "content": "string",
      "publishedAt": "YYYY-MM-DDTHH:mm:ssZ"
    }
  ],
  "totalCount": 0
}
```

#### Promocije
```
GET /api/promotions?page={page}&pageSize={size}
Authorization: Bearer {accessToken} (Librarian role required)

POST /api/promotions
Authorization: Bearer {accessToken} (Librarian role required)
Content-Type: application/json

{
  "name": "string",
  "description": "string",
  "startDate": "YYYY-MM-DDTHH:mm:ssZ",
  "endDate": "YYYY-MM-DDTHH:mm:ssZ",
  "rules": [
    {
      "type": "string", // e.g., "DiscountPercentage"
      "value": 0.0,
      "conditions": {}
    }
  ]
}
```

#### Cene (Quote)
```
POST /api/pricing/quote
Authorization: Not required
Content-Type: application/json

{
  "items": [
    {
      "bookId": "guid",
      "quantity": 1
    }
  ]
}

Response: 200 OK
{
  "items": [
    {
      "bookId": "guid",
      "quantity": 1,
      "unitPrice": 0.0,
      "vatAmount": 0.0,
      "totalPrice": 0.0
    }
  ],
  "subtotal": 0.0,
  "vatTotal": 0.0,
  "total": 0.0,
  "currency": "string"
}
```

---

### 📦 Orders

#### Kreiranje narudžbine
```
POST /api/orders
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "bookIds": ["guid"]
}

Response: 201 Created
{
  "orderId": "guid"
}
```

#### Početak plaćanja
```
POST /api/orders/{orderId}/start-payment
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "provider": "string" // e.g., "MockPayment"
}

Response: 200 OK
{
  "value": "guid" // Payment ID
}
```

#### Potvrda plaćanja
```
POST /api/orders/{orderId}/capture-payment
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "paymentId": "guid",
  "providerReference": "string"
}

Response: 200 OK
```

#### Otkazivanje narudžbine
```
POST /api/orders/{orderId}/cancel
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "reason": "string?"
}

Response: 200 OK
```

#### Dohvatanje narudžbine
```
GET /api/orders/{orderId}
Authorization: Bearer {accessToken}

Response: 200 OK
{
  "id": "guid",
  "userId": "guid",
  "status": "string", // Pending, Paid, Cancelled, Refunded
  "totalAmount": 0.0,
  "currency": "string",
  "items": [
    {
      "bookId": "guid",
      "title": "string",
      "price": 0.0,
      "quantity": 1
    }
  ],
  "payments": [...],
  "createdAt": "YYYY-MM-DDTHH:mm:ssZ"
}
```

#### Moje narudžbine
```
GET /api/orders?page={page}&pageSize={size}
Authorization: Bearer {accessToken}

Response: 200 OK
{
  "items": [...], // Array of orders
  "totalCount": 0,
  "page": 1,
  "pageSize": 20
}
```

---

### 📖 Library

#### Moje knjige
```
GET /api/my/books?skip={skip}&take={take}
Authorization: Bearer {accessToken}

Response: 200 OK
{
  "items": [
    {
      "bookId": "guid",
      "title": "string",
      "authors": ["string"],
      "coverUrl": "string?",
      "purchasedAt": "YYYY-MM-DDTHH:mm:ssZ",
      "progress": {
        "percentage": 0.0,
        "lastPage": 0,
        "lastReadAt": "YYYY-MM-DDTHH:mm:ssZ?"
      }
    }
  ],
  "totalCount": 0
}
```

#### Ažuriranje napretka čitanja
```
POST /api/my/books/{bookId}/progress
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "percentage": 0.0, // 0-100
  "lastPage": 0
}

Response: 200 OK
```

#### Dohvatanje napretka
```
GET /api/my/books/{bookId}/progress
Authorization: Bearer {accessToken}

Response: 200 OK
{
  "bookId": "guid",
  "percentage": 0.0,
  "lastPage": 0,
  "lastReadAt": "YYYY-MM-DDTHH:mm:ssZ?"
}
```

---

### 📄 Content

#### Generisanje Read Tokena
```
POST /api/books/{bookId}/read-token?format={format}&version={version}
Authorization: Bearer {accessToken}

Response: 200 OK
{
  "token": "string"
}
```

#### Streaming sadržaja
```
GET /api/stream?token={readToken}

Response: 200 OK (File stream)
Content-Type: application/pdf (ili drugi format)
```

#### Upload Cover slike
```
POST /api/books/{bookId}/cover
Authorization: Bearer {accessToken} (Librarian role required)
Content-Type: multipart/form-data

file: (image file)

Response: 200 OK
```

#### Upload Edition fajla
```
POST /api/books/{bookId}/editions
Authorization: Bearer {accessToken} (Librarian role required)
Content-Type: multipart/form-data

file: (ebook file)
format: "string" // e.g., "PDF", "EPUB"
version: int

Response: 200 OK
```

---

### 🔔 Notifications

#### Moje notifikacije
```
GET /api/notifications?skip={skip}&take={take}
Authorization: Bearer {accessToken}

Response: 200 OK
{
  "items": [
    {
      "id": "guid",
      "title": "string",
      "message": "string",
      "type": "string", // e.g., "BOOK_PUBLISHED", "ORDER_PAID"
      "read": boolean,
      "createdAt": "YYYY-MM-DDTHH:mm:ssZ"
    }
  ],
  "totalCount": 0
}
```

#### Obeležavanje kao pročitano
```
POST /api/notifications/{notificationId}/read
Authorization: Bearer {accessToken}

Response: 200 OK
```

#### Broj nepročitanih
```
GET /api/notifications/unread-count
Authorization: Bearer {accessToken}

Response: 200 OK
0 // integer
```

#### Podešavanja notifikacija
```
GET /api/notifications/preferences
Authorization: Bearer {accessToken}

PUT /api/notifications/preferences
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "emailEnabled": boolean,
  "inAppEnabled": boolean,
  "types": {
    "BOOK_PUBLISHED": boolean,
    "ORDER_PAID": boolean,
    // ...
  }
}
```

---

## Autentifikacija i Autorizacija

### JWT Tokeni

Sistem koristi **JWT (JSON Web Tokens)** za autentifikaciju. Nakon uspešnog login-a, dobijate:

- **Access Token**: Kratkotrajni token (15 minuta) za API pozive
- **Refresh Token**: Dugotrajni token (7 dana) za obnavljanje access tokena

### Header format

```
Authorization: Bearer {accessToken}
```

### Uloge (Roles)

1. **User** (Default): Osnovni korisnik, može kupovati i čitati knjige
2. **Librarian**: Može upravljati katalogom knjiga, cenama, promocijama
3. **Admin**: Puna kontrola sistema, uključujući brisanje knjiga i upravljanje korisnicima

### Autorizacione politike (Gateway)

- `RequireAuthenticated`: Zahtevan login (bilo koja uloga)
- `RequireLibrarian`: Zahtevana Librarian uloga
- `RequireAdmin`: Zahtevana Admin uloga

---

## Modeli podataka (DTO)

### Auth DTOs

```typescript
// Login Request
interface LoginRequestDto {
  email: string;
  password: string;
}

// Login Response
interface AuthTokensDto {
  accessToken: string;
  refreshToken: string;
  expiresAt: string; // ISO 8601
}

// Register Request
interface RegisterRequestDto {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phone?: string;
  dateOfBirth?: string; // YYYY-MM-DD
}
```

### Book DTOs

```typescript
interface BookDto {
  id: string;
  title: string;
  description?: string;
  authors: string[];
  categories: string[];
  tags: string[];
  isbn?: string;
  publisher?: string;
  publicationDate?: string;
  language: string;
  price: {
    amount: number;
    currency: string;
    vatRate: number;
  };
  promoPrice?: {
    amount: number;
    currency: string;
    vatRate: number;
  };
  status: "Draft" | "Published" | "Unlisted";
  coverUrl?: string;
}
```

### Order DTOs

```typescript
interface OrderDto {
  id: string;
  userId: string;
  status: "Pending" | "Paid" | "Cancelled" | "Refunded";
  totalAmount: number;
  currency: string;
  items: OrderItemDto[];
  payments: PaymentDto[];
  createdAt: string;
}

interface OrderItemDto {
  bookId: string;
  title: string;
  price: number;
  quantity: number;
}
```

### Error Response

```typescript
interface ErrorResponse {
  code: string;
  message: string;
  details?: any;
}
```

---

## Konfiguracija

### Environment Variables

#### Gateway
- `JWT__SECRET_KEY`: JWT signing key
- `JWT__ISSUER`: Token issuer (default: "LibraHub.Identity")
- `JWT__AUDIENCE`: Token audience (default: "LibraHub")

#### Database Connection Strings
Format: `Host={host};Port=5432;Database={db_name};Username=librahub_admin;Password={password}`

#### RabbitMQ Connection
Format: `amqp://{username}:{password}@{host}:5672/`

### Portovi

| Servis | Port |
|--------|------|
| Gateway | 5000 (HTTP), 5001 (HTTPS) |
| Identity | 60950 |
| Catalog | 60960 |
| Content | 60970 |
| Orders | 60980 |
| Library | 60990 |
| Notifications | 61000 |
| PostgreSQL | 5432 |
| RabbitMQ | 5672 (AMQP), 15672 (Management) |
| Redis | 6379 |
| pgAdmin | 5050 |

---

## Pokretanje projekta

### Preuslovi

- Docker Desktop
- .NET 8 SDK (za lokalni development)

### Pokretanje infrastrukture

```bash
# Windows PowerShell
.\infra\scripts\init-local.ps1

# Linux/Mac
./infra\scripts\init-local.sh
```

Ovo će:
1. Pokrenuti PostgreSQL, RabbitMQ, Redis, pgAdmin
2. Pokrenuti sve mikroservise
3. Sačekati da se servisi spreme

### Zaustavljanje

```bash
# Windows PowerShell
.\infra\scripts\stop-local.ps1

# Linux/Mac
./infra\scripts\stop-local.sh
```

### Swagger dokumentacija

Svi servisi imaju Swagger UI dostupan na:
- Gateway: `http://localhost:5000/swagger` (Development)
- Identity: `http://localhost:60950/swagger`
- Catalog: `http://localhost:60960/swagger`
- Content: `http://localhost:60970/swagger`
- Orders: `http://localhost:60980/swagger`
- Library: `http://localhost:60990/swagger`
- Notifications: `http://localhost:61000/swagger`

---

## Frontend Integration Tips

### 1. API Base URL

```typescript
const API_BASE_URL = 'http://localhost:5000/api';
```

### 2. Auth Flow

1. Korisnik se registruje/loguje
2. Sačuvati `accessToken` i `refreshToken` u localStorage/sessionStorage
3. Dodati `Authorization: Bearer {accessToken}` header na sve zahteve
4. Kada access token istekne, koristiti refresh token da dobijete novi

### 3. Error Handling

Svi errori dolaze u formatu:
```typescript
{
  code: string;
  message: string;
}
```

Status kodovi:
- `400`: Bad Request (validacija)
- `401`: Unauthorized (nije ulogovan)
- `403`: Forbidden (nema dozvole)
- `404`: Not Found
- `500`: Server Error

### 4. Real-time Notifications

Notifications servis koristi **SignalR** za real-time notifikacije. Endpoint: `/hubs/notifications`

### 5. File Uploads

Za upload cover slika i e-book fajlova koristiti `multipart/form-data` format.

### 6. Streaming Content

Za streaming e-book sadržaja:
1. Dobiti read token preko `/api/books/{bookId}/read-token`
2. Koristiti token za streaming: `/api/stream?token={token}`
3. Token ima ograničeno vreme trajanja (60 minuta)

---

## Dodatne napomene

- Sve datume prosleđivati u ISO 8601 formatu
- Sve GUID vrednosti su UUID stringovi
- Paginacija koristi `page` i `pageSize` parametre (default: page=1, pageSize=20)
- Valuta se koristi 3-znakovni ISO kod (USD, EUR, RSD, itd.)
- JWT tokeni se čuvaju u cookies ili localStorage (preporuka: httpOnly cookies za production)

---

**Verzija dokumentacije**: 2.0  
**Poslednja ažuriranje**: 2025-12-25