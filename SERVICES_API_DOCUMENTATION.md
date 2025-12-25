# LibraHub - Detaljna API Dokumentacija po Servisima

## 📋 Sadržaj

1. [Identity Service](#1-identity-service)
2. [Catalog Service](#2-catalog-service)
3. [Content Service](#3-content-service)
4. [Orders Service](#4-orders-service)
5. [Library Service](#5-library-service)
6. [Gateway Service](#6-gateway-service)
7. [Notifications Service](#7-notifications-service)

---

## 1. Identity Service

**Base URL**: `http://localhost:5000/api` (kroz Gateway)  
**Direktan URL**: `http://localhost:60950`  
**Baza podataka**: `librahub_identity`

### 1.1. Autentifikacija (`/api/auth`)

#### 1.1.1. Registracija
```http
POST /api/auth/register
Content-Type: application/json
```

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe",
  "phone": "+381601234567",
  "dateOfBirth": "1990-01-15"
}
```

**Response:** `204 No Content`
Body: Empty

**Napomene:**
- Email mora biti validan format
- Password mora zadovoljiti sigurnosne kriterijume
- `phone` i `dateOfBirth` su opcioni
- Nakon registracije, korisnik dobija email sa verification link-om
- Link format: `http://localhost:3000/verify-email?token={token}`
- Token ističe za 7 dana
- response je prazan

---

#### 1.1.2. Login
```http
POST /api/auth/login
Content-Type: application/json
```

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response:** `200 OK`
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "a1b2c3d4e5f6...",
  "expiresAt": "2024-12-23T15:30:00Z"
}
```

**Napomene:**
- Access token traje 15 minuta
- Refresh token traje 7 dana
- Nakon 5 neuspešnih pokušaja, nalog se blokira na 15 minuta

---

#### 1.1.3. Refresh Token
```http
POST /api/auth/refresh
Content-Type: application/json
```

**Request Body:**
```json
{
  "refreshToken": "a1b2c3d4e5f6..."
}
```

**Response:** `200 OK`
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "new_refresh_token...",
  "expiresAt": "2024-12-23T16:00:00Z"
}
```

---

#### 1.1.4. Email Verifikacija
```http
POST /api/auth/verify-email
Content-Type: application/json
```

**Request Body:**
```json
{
  "token": "verification_token_from_email"
}
```

**Response:** `200 OK`
Body: Empty

**Napomene:**
- Token se dobija iz email-a (query parameter u verification link-u)
- Token može biti korišćen samo jednom
- Ako je email već verifikovan, vraća 200 OK (idempotent operacija)
- Nakon verifikacije, `emailVerified` postaje `true`

---

#### 1.1.5. Forgot Password
```http
POST /api/auth/forgot-password
Content-Type: application/json
```

**Request Body:**
```json
{
  "email": "user@example.com"
}
```

**Response:** `200 OK`

**Napomene:**
- Šalje email sa tokenom za reset lozinke
- Token važi 24 sata

---

#### 1.1.6. Reset Password
```http
POST /api/auth/reset-password
Content-Type: application/json
```

**Request Body:**
```json
{
  "token": "reset_token_from_email",
  "newPassword": "NewSecurePassword123!",
  "confirmPassword": "NewSecurePassword123!"
}
```

**Response:** `200 OK`

---

### 1.2. Korisnički Profil (`/api/me`)

**Zahtevana autentifikacija**: Da (Bearer token)

#### 1.2.1. Dohvatanje Profila
```http
GET /api/me
Authorization: Bearer {accessToken}
```

**Response:** `200 OK`
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "roles": ["User"],
  "emailVerified": true,
  "status": "Active"
}
```

**Status vrednosti:**
- `Active`: Korisnik je aktivan
- `Disabled`: Korisnik je onemogućen
- `Pending`: Čeka verifikaciju emaila

---

### 1.3. Upravljanje Korisnicima (`/api/users`)

**Zahtevana autentifikacija**: Da (Admin role)

#### 1.3.1. Kreiranje Korisnika
```http
POST /api/users
Authorization: Bearer {accessToken}
Content-Type: application/json
```

**Request Body:**
```json
{
  "email": "newuser@example.com",
  "role": "Librarian"
}
```

**Response:** `200 OK`
```json
{
  "value": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Dostupne uloge:**
- `User`
- `Librarian`
- `Admin`

---

#### 1.3.2. Dodeljivanje Uloge
```http
POST /api/users/{userId}/roles
Authorization: Bearer {accessToken}
Content-Type: application/json
```

**Request Body:**
```json
{
  "role": "Librarian"
}
```

**Response:** `200 OK`

---

#### 1.3.3. Uklanjanje Uloge
```http
DELETE /api/users/{userId}/roles/{role}
Authorization: Bearer {accessToken}
```

**Response:** `200 OK`

**Primer:**
```http
DELETE /api/users/3fa85f64-5717-4562-b3fc-2c963f66afa6/roles/Librarian
```

---

#### 1.3.4. Onemogućavanje Korisnika
```http
POST /api/users/{userId}/disable
Authorization: Bearer {accessToken}
Content-Type: application/json
```

**Request Body:**
```json
{
  "reason": "Violation of terms of service"
}
```

**Response:** `200 OK`

---

#### 1.3.5. Omogućavanje Korisnika
```http
POST /api/users/{userId}/enable
Authorization: Bearer {accessToken}
```

**Response:** `200 OK`

---

#### 1.3.6. Upload Avatara
```http
POST /api/users/{userId}/avatar
Authorization: Bearer {accessToken}
Content-Type: multipart/form-data
```

**Request:**
- `file`: Image file (JPG, PNG)

**Response:** `200 OK`
```json
{
  "value": "https://api.librahub.local/avatars/user_avatar.jpg"
}
```

---

#### 1.3.7. Završetak Registracije
```http
POST /api/users/complete-registration
Content-Type: application/json
```

**Request Body:**
```json
{
  "token": "registration_completion_token",
  "firstName": "John",
  "lastName": "Doe",
  "phone": "+381601234567",
  "dateOfBirth": "1990-01-15"
}
```

**Response:** `200 OK`

**Napomene:**
- Koristi se kada admin kreira korisnika
- Token se šalje emailom
- Token važi 72 sata

---

## 2. Catalog Service

**Base URL**: `http://localhost:5000/api` (kroz Gateway)  
**Direktan URL**: `http://localhost:60960`  
**Baza podataka**: `librahub_catalog`

### 2.1. Knjige (`/api/books`)

#### 2.1.1. Pretraga Knjiga
```http
GET /api/books?searchTerm={term}&page={page}&pageSize={size}
```

**Query Parameters:**
- `searchTerm` (optional): Tekst za pretragu
- `page` (default: 1): Broj stranice
- `pageSize` (default: 20): Broj rezultata po stranici

**Response:** `200 OK`
```json
{
  "books": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "The Great Gatsby",
      "description": "A classic American novel...",
      "status": "Published",
      "authors": ["F. Scott Fitzgerald"]
    }
  ],
  "totalCount": 150,
  "page": 1,
  "pageSize": 20,
  "totalPages": 8
}
```

**Status vrednosti:**
- `Draft`: Knjiga u izradi
- `Published`: Objavljena i dostupna
- `Unlisted`: Uklonjena sa liste, ali još uvek postoji

---

#### 2.1.2. Dohvatanje Knjige
```http
GET /api/books/{id}
```

**Response:** `200 OK`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "The Great Gatsby",
  "description": "A classic American novel...",
  "language": "en",
  "publisher": "Scribner",
  "publicationDate": "1925-04-10T00:00:00Z",
  "isbn": "978-0-7432-7356-5",
  "status": "Published",
  "authors": ["F. Scott Fitzgerald"],
  "categories": ["Fiction", "Classic"],
  "tags": ["American Literature", "1920s"],
  "pricing": {
    "price": 9.99,
    "currency": "USD",
    "vatRate": 0.20,
    "promoPrice": 7.99,
    "promoStartDate": "2024-12-01T00:00:00Z",
    "promoEndDate": "2024-12-31T23:59:59Z"
  }
}
```

---

#### 2.1.3. Kreiranje Knjige
```http
POST /api/books
Authorization: Bearer {accessToken} (Librarian role)
Content-Type: application/json
```

**Request Body:**
```json
{
  "title": "New Book Title"
}
```

**Response:** `201 Created`
```json
{
  "value": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

---

#### 2.1.4. Ažuriranje Knjige
```http
PUT /api/books/{id}
Authorization: Bearer {accessToken} (Librarian role)
Content-Type: application/json
```

**Request Body:**
```json
{
  "description": "Updated description",
  "language": "en",
  "publisher": "New Publisher",
  "publicationDate": "2024-01-01T00:00:00Z",
  "isbn": "978-0-1234-5678-9",
  "authors": ["Author One", "Author Two"],
  "categories": ["Fiction", "Science Fiction"],
  "tags": ["Tag1", "Tag2"]
}
```

**Response:** `204 No Content`

**Napomene:**
- Svi polja su opciona
- Samo prosleđena polja će biti ažurirana

---

#### 2.1.5. Postavljanje Cene
```http
POST /api/books/{id}/pricing
Authorization: Bearer {accessToken} (Librarian role)
Content-Type: application/json
```

**Request Body:**
```json
{
  "price": 19.99,
  "currency": "USD",
  "vatRate": 0.20,
  "promoPrice": 14.99,
  "promoStartDate": "2024-12-01T00:00:00Z",
  "promoEndDate": "2024-12-31T23:59:59Z"
}
```

**Response:** `204 No Content`

**Napomene:**
- `vatRate` je decimalna vrednost (0.20 = 20%)
- `promoPrice`, `promoStartDate`, `promoEndDate` su opcioni

---

#### 2.1.6. Objava Knjige
```http
POST /api/books/{id}/publish
Authorization: Bearer {accessToken} (Librarian role)
```

**Response:** `204 No Content`

**Napomene:**
- Menja status sa `Draft` na `Published`
- Knjiga postaje vidljiva u pretrazi

---

#### 2.1.7. Uklanjanje sa Liste
```http
POST /api/books/{id}/unlist
Authorization: Bearer {accessToken} (Librarian role)
```

**Response:** `204 No Content`

**Napomene:**
- Menja status na `Unlisted`
- Knjiga više nije vidljiva u pretrazi, ali još uvek postoji

---

#### 2.1.8. Brisanje Knjige
```http
POST /api/books/{id}/remove
Authorization: Bearer {accessToken} (Admin role)
Content-Type: application/json
```

**Request Body:**
```json
{
  "reason": "Copyright violation"
}
```

**Response:** `204 No Content`

**Napomene:**
- Trajno briše knjigu iz sistema
- Samo Admin može obrisati knjigu

---

### 2.2. Obaveštenja (`/api/announcements`)

#### 2.2.1. Lista Obaveštenja
```http
GET /api/announcements?bookId={bookId}&page={page}&pageSize={size}
```

**Query Parameters:**
- `bookId` (optional): Filter po knjizi
- `page` (default: 1)
- `pageSize` (default: 20)

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "bookId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "New Book Release",
      "content": "We're excited to announce...",
      "publishedAt": "2024-12-23T10:00:00Z"
    }
  ],
  "totalCount": 10,
  "page": 1,
  "pageSize": 20
}
```

---

#### 2.2.2. Kreiranje Obaveštenja
```http
POST /api/announcements
Authorization: Bearer {accessToken} (Librarian role)
Content-Type: application/json
```

**Request Body:**
```json
{
  "bookId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "New Book Release",
  "content": "We're excited to announce the release of..."
}
```

**Response:** `201 Created`
```json
{
  "value": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

---

#### 2.2.3. Objava Obaveštenja
```http
POST /api/announcements/{id}/publish
Authorization: Bearer {accessToken} (Librarian role)
```

**Response:** `204 No Content`

---

### 2.3. Promocije (`/api/promotions`)

**Zahtevana autentifikacija**: Librarian ili Admin

#### 2.3.1. Lista Kampanja
```http
GET /api/promotions/campaigns?page={page}&pageSize={size}
Authorization: Bearer {accessToken}
```

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Holiday Sale",
      "description": "Special holiday discounts",
      "status": "Active",
      "startsAtUtc": "2024-12-01T00:00:00Z",
      "endsAtUtc": "2024-12-31T23:59:59Z",
      "stackingPolicy": "None",
      "priority": 1,
      "rules": [...]
    }
  ],
  "totalCount": 5,
  "page": 1,
  "pageSize": 20
}
```

**Status vrednosti:**
- `Draft`: Kampanja u izradi
- `Active`: Aktivna kampanja
- `Paused`: Privremeno pauzirana
- `Ended`: Završena
- `Cancelled`: Otkazana

**Stacking Policy:**
- `None`: Ne može se kombinovati sa drugim promocijama
- `Allow`: Može se kombinovati

---

#### 2.3.2. Kreiranje Kampanje
```http
POST /api/promotions/campaigns
Authorization: Bearer {accessToken}
Content-Type: application/json
```

**Request Body:**
```json
{
  "name": "Holiday Sale",
  "description": "Special holiday discounts",
  "startsAtUtc": "2024-12-01T00:00:00Z",
  "endsAtUtc": "2024-12-31T23:59:59Z",
  "stackingPolicy": "None",
  "priority": 1
}
```

**Response:** `201 Created`
```json
{
  "value": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

---

#### 2.3.3. Dohvatanje Kampanje
```http
GET /api/promotions/campaigns/{id}
Authorization: Bearer {accessToken}
```

**Response:** `200 OK`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Holiday Sale",
  "description": "Special holiday discounts",
  "status": "Active",
  "startsAtUtc": "2024-12-01T00:00:00Z",
  "endsAtUtc": "2024-12-31T23:59:59Z",
  "stackingPolicy": "None",
  "priority": 1,
  "createdBy": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "rules": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "discountType": "Percentage",
      "discountValue": 20.0,
      "currency": "USD",
      "appliesToScope": "Category",
      "scopeValues": ["Fiction"]
    }
  ]
}
```

---

#### 2.3.4. Dodavanje Pravila
```http
POST /api/promotions/campaigns/{id}/rules
Authorization: Bearer {accessToken}
Content-Type: application/json
```

**Request Body:**
```json
{
  "discountType": "Percentage",
  "discountValue": 20.0,
  "currency": "USD",
  "minPriceAfterDiscount": 5.0,
  "maxDiscountAmount": 50.0,
  "appliesToScope": "Category",
  "scopeValues": ["Fiction", "Science Fiction"],
  "exclusions": ["3fa85f64-5717-4562-b3fc-2c963f66afa6"]
}
```

**Response:** `201 Created`
```json
{
  "value": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Discount Type:**
- `Percentage`: Procenat popusta (npr. 20.0 = 20%)
- `FixedAmount`: Fiksni iznos popusta

**Applies To Scope:**
- `All`: Sve knjige
- `Category`: Po kategorijama
- `Book`: Specifične knjige
- `Author`: Po autorima

---

#### 2.3.5. Uklanjanje Pravila
```http
DELETE /api/promotions/rules/{ruleId}?campaignId={campaignId}
Authorization: Bearer {accessToken}
```

**Response:** `204 No Content`

---

#### 2.3.6. Aktiviranje Kampanje
```http
POST /api/promotions/campaigns/{id}/activate
Authorization: Bearer {accessToken}
```

**Response:** `204 No Content`

---

#### 2.3.7. Pauziranje Kampanje
```http
POST /api/promotions/campaigns/{id}/pause
Authorization: Bearer {accessToken}
```

**Response:** `204 No Content`

---

#### 2.3.8. Završetak Kampanje
```http
POST /api/promotions/campaigns/{id}/end
Authorization: Bearer {accessToken}
```

**Response:** `204 No Content`

---

#### 2.3.9. Otkazivanje Kampanje
```http
POST /api/promotions/campaigns/{id}/cancel
Authorization: Bearer {accessToken}
```

**Response:** `204 No Content`

---

### 2.4. Admin Statistics (`/admin/statistics`)

**Zahtevana autentifikacija**: Admin role

#### 2.4.1. Book Statistics
```http
GET /admin/statistics/books
Authorization: Bearer {accessToken}
```

**Response:** `200 OK`
```json
{
  "total": 500,
  "published": 450,
  "draft": 30,
  "unlisted": 20,
  "newLast30Days": 15
}
```

**Napomene:**
- Statistika je keširana (Redis cache, 5 minuta TTL)
- `newLast30Days`: Knjige kreirane u poslednjih 30 dana

---

### 2.5. Cene (`/api/pricing`)

#### 2.4.1. Quote (Kalkulacija Cene)
```http
POST /api/pricing/quote
Content-Type: application/json
```

**Request Body:**
```json
{
  "currency": "USD",
  "items": [
    {
      "bookId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "quantity": 1
    },
    {
      "bookId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "quantity": 2
    }
  ],
  "atUtc": "2024-12-23T12:00:00Z"
}
```

**Response:** `200 OK`
```json
{
  "items": [
    {
      "bookId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "quantity": 1,
      "unitPrice": 19.99,
      "vatAmount": 3.998,
      "totalPrice": 23.988,
      "discountAmount": 5.0,
      "finalPrice": 18.988
    }
  ],
  "subtotal": 59.97,
  "vatTotal": 11.994,
  "discountTotal": 15.0,
  "total": 56.964,
  "currency": "USD"
}
```

**Napomene:**
- Automatski primenjuje aktivne promocije
- `atUtc` određuje trenutak za koji se računa cena (za promocije)

---

## 3. Content Service

**Base URL**: `http://localhost:5000/api` (kroz Gateway)  
**Direktan URL**: `http://localhost:60970`  
**Baza podataka**: `librahub_content`  
**Storage**: MinIO (S3-compatible)

### 3.1. Read Token (`/api/books/{bookId}/read-token`)

#### 3.1.1. Generisanje Read Tokena
```http
POST /api/books/{bookId}/read-token?format={format}&version={version}
Authorization: Bearer {accessToken}
```

**Query Parameters:**
- `format` (optional): Format fajla (PDF, EPUB, etc.)
- `version` (optional): Verzija izdanja

**Response:** `200 OK`
```json
{
  "token": "read_token_string_here"
}
```

**Napomene:**
- Token važi 60 minuta
- Korisnik mora imati entitlement za knjigu
- Token se koristi za streaming sadržaja

---

### 3.2. Streaming (`/api/stream`)

#### 3.2.1. Streaming Sadržaja
```http
GET /api/stream?token={readToken}
```

**Response:** `200 OK`
- Content-Type: `application/pdf` (ili drugi format)
- File stream sa Range request podrškom

**Napomene:**
- Podržava HTTP Range requests za resumable download
- Token se validira pre svakog zahteva

---

### 3.3. Upload (`/api/books/{bookId}`)

**Zahtevana autentifikacija**: Librarian ili Admin

#### 3.3.1. Upload Cover Slike
```http
POST /api/books/{bookId}/cover
Authorization: Bearer {accessToken}
Content-Type: multipart/form-data
```

**Request:**
- `file`: Image file (JPG, PNG)
  - Max size: 10MB
  - Stored in `covers` bucket

**Response:** `200 OK`
```json
{
  "value": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

---

#### 3.3.2. Upload Edition Fajla
```http
POST /api/books/{bookId}/editions
Authorization: Bearer {accessToken}
Content-Type: multipart/form-data
```

**Request:**
- `file`: E-book file (PDF, EPUB, etc.)
  - Max size: 100MB
  - Stored in `editions` bucket
- `format`: Format string (PDF, EPUB, etc.)
- `version`: Version number (optional)

**Response:** `200 OK`
```json
{
  "value": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Napomene:**
- Moguće je imati više verzija istog formata
- Moguće je imati više formata za istu knjigu

---

## 4. Orders Service

**Base URL**: `http://localhost:5000/api` (kroz Gateway)  
**Direktan URL**: `http://localhost:60980`  
**Baza podataka**: `librahub_orders`

### 4.1. Narudžbine (`/api/orders`)

**Zahtevana autentifikacija**: Da (Bearer token)

#### 4.1.1. Kreiranje Narudžbine
```http
POST /api/orders
Authorization: Bearer {accessToken}
Content-Type: application/json
```

**Request Body:**
```json
{
  "bookIds": [
    "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "3fa85f64-5717-4562-b3fc-2c963f66afa7"
  ]
}
```

**Response:** `201 Created`
```json
{
  "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Napomene:**
- Automatski kalkuliše cene sa aktivnim promocijama
- Kreira se sa statusom `Pending`

---

#### 4.1.2. Početak Plaćanja
```http
POST /api/orders/{orderId}/start-payment
Authorization: Bearer {accessToken}
Content-Type: application/json
```

**Request Body:**
```json
{
  "provider": "MockPayment"
}
```

**Response:** `200 OK`
```json
{
  "value": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Napomene:**
- Vraća Payment ID
- Status narudžbine ostaje `Pending` dok se plaćanje ne završi

---

#### 4.1.3. Potvrda Plaćanja
```http
POST /api/orders/{orderId}/capture-payment
Authorization: Bearer {accessToken}
Content-Type: application/json
```

**Request Body:**
```json
{
  "paymentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "providerReference": "payment_reference_from_gateway"
}
```

**Response:** `200 OK`

**Napomene:**
- Nakon uspešnog plaćanja, status se menja na `Paid`
- Automatski se kreiraju entitlements za korisnika
- Šalje se notifikacija korisniku

---

#### 4.1.4. Otkazivanje Narudžbine
```http
POST /api/orders/{orderId}/cancel
Authorization: Bearer {accessToken}
Content-Type: application/json
```

**Request Body:**
```json
{
  "reason": "Changed my mind"
}
```

**Response:** `200 OK`

**Napomene:**
- Može se otkazati samo ako je status `Pending`
- `reason` je opciono

---

#### 4.1.5. Dohvatanje Narudžbine
```http
GET /api/orders/{orderId}
Authorization: Bearer {accessToken}
```

**Response:** `200 OK`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Paid",
  "totalAmount": 29.98,
  "currency": "USD",
  "items": [
    {
      "bookId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "The Great Gatsby",
      "price": 19.99,
      "quantity": 1
    }
  ],
  "payments": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "status": "Completed",
      "amount": 29.98,
      "provider": "MockPayment",
      "createdAt": "2024-12-23T10:00:00Z"
    }
  ],
  "createdAt": "2024-12-23T10:00:00Z"
}
```

**Status vrednosti:**
- `Pending`: Čeka plaćanje
- `Paid`: Plaćeno
- `Cancelled`: Otkazano
- `Refunded`: Povraćaj novca

---

#### 4.1.6. Moje Narudžbine
```http
GET /api/orders?page={page}&pageSize={size}
Authorization: Bearer {accessToken}
```

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "status": "Paid",
      "totalAmount": 29.98,
      "currency": "USD",
      "itemCount": 2,
      "createdAt": "2024-12-23T10:00:00Z"
    }
  ],
  "totalCount": 10,
  "page": 1,
  "pageSize": 20
}
```

---

### 4.2. Admin Statistics (`/admin/statistics`)

**Zahtevana autentifikacija**: Admin role

#### 4.2.1. Order Statistics
```http
GET /admin/statistics/orders
Authorization: Bearer {accessToken}
```

**Response:** `200 OK`
```json
{
  "total": 1000,
  "paid": 850,
  "pending": 100,
  "cancelled": 30,
  "refunded": 20,
  "last30Days": {
    "count": 150,
    "revenue": 2995.50
  },
  "last7Days": {
    "count": 45,
    "revenue": 899.25
  },
  "totalRevenue": 16995.75,
  "currency": "USD"
}
```

**Napomene:**
- Statistika je keširana (Redis cache, 5 minuta TTL)
- `last30Days` i `last7Days` prikazuju broj narudžbina i ukupan revenue za period

---

### 4.3. Admin - Narudžbine (`/api/admin/orders`)

**Zahtevana autentifikacija**: Admin role

#### 4.3.1. Povraćaj Novca
```http
POST /api/admin/orders/{orderId}/refund
Authorization: Bearer {accessToken}
Content-Type: application/json
```

**Request Body:**
```json
{
  "reason": "Customer request"
}
```

**Response:** `200 OK`

**Napomene:**
- Može se refundovati samo ako je status `Paid`
- Automatski se uklanjaju entitlements
- Šalje se notifikacija korisniku

---

## 5. Library Service

**Base URL**: `http://localhost:5000/api` (kroz Gateway)  
**Direktan URL**: `http://localhost:60990`  
**Baza podataka**: `librahub_library`

### 5.1. Moja Biblioteka (`/api/my`)

**Zahtevana autentifikacija**: Da (Bearer token)

#### 5.1.1. Moje Knjige
```http
GET /api/my/books?skip={skip}&take={take}
Authorization: Bearer {accessToken}
```

**Query Parameters:**
- `skip` (default: 0): Broj preskočenih rezultata
- `take` (default: 20): Broj rezultata

**Response:** `200 OK`
```json
{
  "items": [
    {
      "bookId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "The Great Gatsby",
      "authors": ["F. Scott Fitzgerald"],
      "coverUrl": "https://api.librahub.local/covers/book_cover.jpg",
      "purchasedAt": "2024-12-01T10:00:00Z",
      "progress": {
        "percentage": 45.5,
        "lastPage": 120,
        "lastReadAt": "2024-12-23T08:00:00Z"
      }
    }
  ],
  "totalCount": 15
}
```

---

#### 5.1.2. Ažuriranje Napretka Čitanja
```http
POST /api/my/books/{bookId}/progress
Authorization: Bearer {accessToken}
Content-Type: application/json
```

**Request Body:**
```json
{
  "percentage": 45.5,
  "lastPage": 120
}
```

**Response:** `200 OK`

**Napomene:**
- `percentage`: 0-100
- `lastPage`: Broj stranice

---

#### 5.1.3. Dohvatanje Napretka
```http
GET /api/my/books/{bookId}/progress
Authorization: Bearer {accessToken}
```

**Response:** `200 OK`
```json
{
  "bookId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "percentage": 45.5,
  "lastPage": 120,
  "lastReadAt": "2024-12-23T08:00:00Z"
}
```

---

### 5.2. Admin Statistics (`/api/admin/statistics`)

**Zahtevana autentifikacija**: Admin role

#### 5.2.1. Entitlement Statistics
```http
GET /api/admin/statistics/entitlements
Authorization: Bearer {accessToken}
```

**Response:** `200 OK`
```json
{
  "total": 2000,
  "active": 1950,
  "revoked": 50,
  "grantedLast30Days": 200
}
```

**Napomene:**
- Statistika je keširana (Redis cache, 5 minuta TTL)
- `grantedLast30Days`: Entitlements dodeljeni u poslednjih 30 dana

---

### 5.3. Admin - Entitlements (`/api/admin/entitlements`)

**Zahtevana autentifikacija**: Admin role

#### 5.3.1. Dodeljivanje Entitlementa
```http
POST /api/admin/entitlements/grant
Authorization: Bearer {accessToken}
Content-Type: application/json
```

**Request Body:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "bookId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Response:** `201 Created`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Napomene:**
- Admin može direktno dodeliti knjigu korisniku
- Korisnik dobija pristup bez plaćanja

---

#### 5.3.2. Uklanjanje Entitlementa
```http
POST /api/admin/entitlements/{id}/revoke
Authorization: Bearer {accessToken}
Content-Type: application/json
```

**Request Body:**
```json
{
  "reason": "Violation of terms"
}
```

**Response:** `200 OK`

**Napomene:**
- `reason` je opciono
- Korisnik gubi pristup knjizi

---

## 6. Gateway Service

**Base URL**: `http://localhost:5000/api`  
**Direktan URL**: `http://localhost:5000`  
**Svrha**: API Gateway koji rutira zahteve ka backend servisima i agregira podatke

### 6.1. Admin Dashboard (`/api/admin/dashboard`)

**Zahtevana autentifikacija**: Admin role

#### 6.1.1. Dashboard Summary
```http
GET /api/admin/dashboard/summary
Authorization: Bearer {accessToken}
```

**Response:** `200 OK`
```json
{
  "users": {
    "total": 150,
    "active": 140,
    "disabled": 5,
    "pending": 5,
    "newLast30Days": 25,
    "newLast7Days": 8
  },
  "books": {
    "total": 500,
    "published": 450,
    "draft": 30,
    "unlisted": 20,
    "newLast30Days": 15
  },
  "orders": {
    "total": 1000,
    "paid": 850,
    "pending": 100,
    "cancelled": 30,
    "refunded": 20,
    "last30Days": {
      "count": 150,
      "revenue": 2995.50
    },
    "last7Days": {
      "count": 45,
      "revenue": 899.25
    },
    "totalRevenue": 16995.75,
    "currency": "USD"
  },
  "entitlements": {
    "total": 2000,
    "active": 1950,
    "revoked": 50,
    "grantedLast30Days": 200
  },
  "revenue": {
    "total": 16995.75,
    "last30Days": 2995.50,
    "last7Days": 899.25,
    "currency": "USD"
  }
}
```

**Napomene:**
- Agregira podatke iz Identity, Catalog, Orders i Library servisa
- Svi podaci se dohvataju paralelno (Task.WhenAll)
- Ako neki servis ne vrati podatke, odgovarajuće polje će biti `null`
- Revenue se izvlači iz OrderStatistics

---

## 7. Notifications Service

**Base URL**: `http://localhost:5000/api` (kroz Gateway)  
**Direktan URL**: `http://localhost:61000`  
**Baza podataka**: `librahub_notifications`  
**SignalR Hub**: `/hubs/notifications`

### 7.1. Notifikacije (`/api/notifications`)

**Zahtevana autentifikacija**: Da (Bearer token)

#### 7.1.1. Moje Notifikacije
```http
GET /api/notifications?skip={skip}&take={take}
Authorization: Bearer {accessToken}
```

**Query Parameters:**
- `skip` (default: 0)
- `take` (default: 20)

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "New Book Available",
      "message": "The book 'The Great Gatsby' is now available",
      "type": "BOOK_PUBLISHED",
      "read": false,
      "createdAt": "2024-12-23T10:00:00Z"
    }
  ],
  "totalCount": 25
}
```

**Tipovi notifikacija:**
- `BOOK_PUBLISHED`: Nova knjiga objavljena
- `ORDER_PAID`: Narudžbina plaćena
- `ORDER_REFUNDED`: Povraćaj novca
- `ENTITLEMENT_GRANTED`: Dodeljen pristup knjizi
- `ANNOUNCEMENT_PUBLISHED`: Novo obaveštenje

---

#### 7.1.2. Obeležavanje kao Pročitano
```http
POST /api/notifications/{notificationId}/read
Authorization: Bearer {accessToken}
```

**Response:** `200 OK`

---

#### 7.1.3. Broj Nepročitanih
```http
GET /api/notifications/unread-count
Authorization: Bearer {accessToken}
```

**Response:** `200 OK`
```json
5
```

---

### 7.2. Podešavanja (`/api/notifications/preferences`)

**Zahtevana autentifikacija**: Da (Bearer token)

#### 7.2.1. Dohvatanje Podešavanja
```http
GET /api/notifications/preferences
Authorization: Bearer {accessToken}
```

**Response:** `200 OK`
```json
{
  "preferences": [
    {
      "type": "BOOK_PUBLISHED",
      "emailEnabled": true,
      "inAppEnabled": true
    },
    {
      "type": "ORDER_PAID",
      "emailEnabled": true,
      "inAppEnabled": true
    }
  ]
}
```

---

#### 7.2.2. Ažuriranje Podešavanja
```http
POST /api/notifications/preferences
Authorization: Bearer {accessToken}
Content-Type: application/json
```

**Request Body:**
```json
{
  "type": "BOOK_PUBLISHED",
  "emailEnabled": false,
  "inAppEnabled": true
}
```

**Response:** `200 OK`

**Napomene:**
- Ažurira podešavanja za specifičan tip notifikacije
- Može se pozvati više puta za različite tipove

---

### 7.3. SignalR Real-time Notifications

**Hub URL**: `ws://localhost:61000/hubs/notifications`

**Connection:**
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:61000/hubs/notifications", {
        accessTokenFactory: () => accessToken
    })
    .build();

connection.on("ReceiveNotification", (notification) => {
    console.log("New notification:", notification);
});

await connection.start();
```

**Napomene:**
- Zahteva validan JWT token
- Automatski se povezuje na korisnikovu grupu
- Notifikacije se šalju u real-time

---

## Dodatne Napomene

### Error Handling

Svi errori dolaze u formatu:
```json
{
  "code": "ERROR_CODE",
  "message": "Human readable error message",
  "details": {}
}
```

**Česti error kodovi:**
- `VALIDATION_ERROR`: Greška u validaciji
- `NOT_FOUND`: Resurs nije pronađen
- `UNAUTHORIZED`: Nije autentifikovan
- `FORBIDDEN`: Nema dozvole
- `CONFLICT`: Konflikt stanja
- `INTERNAL_ERROR`: Interna greška servera

### Paginacija

Većina lista endpoint-a koristi paginaciju:
- `page`: Broj stranice (počinje od 1)
- `pageSize`: Broj rezultata po stranici (default: 20)
- `skip`/`take`: Alternativni pristup (skip=0, take=20)

### Datumi i Vremena

Svi datumi se prosleđuju i vraćaju u **ISO 8601** formatu:
- Format: `YYYY-MM-DDTHH:mm:ssZ`
- Primer: `2024-12-23T10:30:00Z`

### Valute

Koriste se 3-znakovni ISO kodovi:
- `USD`: US Dollar
- `EUR`: Euro
- `RSD`: Serbian Dinar
- itd.

### File Uploads

Za file uploads koristiti `multipart/form-data`:
- Max cover size: 10MB
- Max edition size: 100MB
- Podržani formati: JPG, PNG (covers), PDF, EPUB (editions)

---

**Verzija dokumentacije**: 3.0  
**Poslednja ažuriranje**: 2025-12-25