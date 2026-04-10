# Land Registration System (LRS) - Technical Documentation

## Table of Contents
1. [Executive Summary](#executive-summary)
2. [Architecture Overview](#architecture-overview)
3. [Technology Stack](#technology-stack)
4. [Project Structure](#project-structure)
5. [Layer-by-Layer Analysis](#layer-by-layer-analysis)
6. [Data Flow](#data-flow)
7. [Database Schema](#database-schema)
8. [API Endpoints](#api-endpoints)
9. [External Integrations](#external-integrations)
10. [Key Modules and Components](#key-modules-and-components)

---

## Executive Summary

The Land Registration System (LRS) is a modern web application designed to manage land registration documents and sources. The system follows Clean Architecture principles with a layered approach, separating concerns into Domain, Application, Infrastructure, and Presentation layers. The application integrates with Alfresco ECM (Enterprise Content Management) for document storage and uses PostgreSQL as the primary database.

**Key Features:**
- Document scanning and upload functionality
- Document versioning and management
- Source and Administrative Source management
- Integration with Alfresco for file storage
- RESTful API with Swagger documentation
- Angular-based frontend with camera/scanner support

---

## Architecture Overview

### Architecture Pattern: Clean Architecture / Onion Architecture

The system follows Clean Architecture principles, which promote separation of concerns and independence from frameworks, UI, and external agencies. The architecture consists of four main layers:

```
┌─────────────────────────────────────────┐
│         LRS.Client (Angular)            │  ← Presentation Layer
│     (Frontend/User Interface)           │
└──────────────────┬──────────────────────┘
                   │ HTTP/REST
┌──────────────────▼──────────────────────┐
│         LRS.API (ASP.NET Core)          │  ← API/Interface Layer
│     (Controllers, Middleware)           │
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│      LRS.Application (Business Logic)   │  ← Application Layer
│    (Services, DTOs, Interfaces)         │
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│        LRS.Domain (Core Domain)         │  ← Domain Layer
│   (Entities, Domain Interfaces)         │
└──────────────────┬──────────────────────┘
                   │
┌──────────────────▼──────────────────────┐
│   LRS.Infrastructure (Data Access)      │  ← Infrastructure Layer
│ (DbContext, Repositories, External)     │
└─────────────────────────────────────────┘
```

### Dependency Flow

- **Outer layers depend on inner layers**: API → Application → Domain ← Infrastructure
- **Domain layer has no dependencies** on other layers (pure business logic)
- **Infrastructure depends on Domain** (implements Domain interfaces)
- **Application depends on Domain** (uses Domain entities and interfaces)
- **API depends on Application and Infrastructure** (orchestrates the layers)

---

## Technology Stack

### Backend Technologies

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 9.0 | Runtime framework |
| ASP.NET Core | 9.0.0 | Web API framework |
| Entity Framework Core | 9.0.0 | ORM (Object-Relational Mapping) |
| PostgreSQL | - | Primary database |
| Npgsql.EntityFrameworkCore.PostgreSQL | 9.0.0 | PostgreSQL provider for EF Core |
| Swashbuckle (Swagger) | 7.0.0 | API documentation |

### Frontend Technologies

| Technology | Version | Purpose |
|------------|---------|---------|
| Angular | 18.2.0 | Frontend framework |
| TypeScript | 5.5.2 | Programming language |
| RxJS | 7.8.0 | Reactive programming |
| Angular Router | 18.2.0 | Client-side routing |
| Angular Forms | 18.2.0 | Form handling |

### External Services

| Service | Purpose |
|---------|---------|
| Alfresco ECM | Document storage and management (via REST API) |
| PostgreSQL Database | Relational data storage |

### Development Tools

| Tool | Purpose |
|------|---------|
| Visual Studio / VS Code | IDE |
| Angular CLI | Angular project scaffolding and build |
| Entity Framework Core Tools | Database migrations |
| Swagger UI | API testing and documentation |

---

## Project Structure

### Solution Overview

```
LandRegistrationSystem/
│
├── LRS.API/                          # Web API Layer (Presentation/Interface)
│   ├── Controllers/                  # API Controllers (REST endpoints)
│   ├── Middleware/                   # Custom middleware (exception handling)
│   ├── Program.cs                    # Application entry point & DI configuration
│   ├── appsettings.json              # Configuration
│   └── Properties/                   # Launch settings
│
├── LRS.Application/                  # Application Layer (Business Logic)
│   ├── DTOs/                         # Data Transfer Objects
│   ├── Interfaces/                   # Service interfaces
│   └── Services/                     # Application services (business logic)
│
├── LRS.Domain/                       # Domain Layer (Core Business Entities)
│   ├── Entities/                     # Domain entities (business models)
│   ├── Interfaces/                   # Repository and service interfaces
│   └── common/                       # Common domain interfaces
│
├── LRS.Infrastructure/               # Infrastructure Layer (Data Access & External)
│   ├── Persistence/                  # Database context (EF Core)
│   ├── Repositories/                 # Repository implementations
│   ├── Services/                     # External service implementations (Alfresco)
│   └── Migrations/                   # Database migrations (EF Core)
│
└── LRS.Client/                       # Frontend (Angular Application)
    ├── src/
    │   └── app/
    │       ├── components/           # Angular components
    │       ├── services/             # Angular services (HTTP clients)
    │       ├── app.config.ts         # Angular configuration
    │       └── app.routes.ts         # Routing configuration
    └── package.json                  # NPM dependencies
```

---

## Layer-by-Layer Analysis

### 1. LRS.Domain Layer (Core Domain)

**Purpose**: Contains the core business entities and domain logic. This layer has no dependencies on other layers.

#### Folder: `Entities/`

Contains domain entities representing core business concepts:

**Document.cs**
- Represents a document in the system
- Properties:
  - `Id`: Primary key
  - `SourceId`: Foreign key to Source entity
  - `DocumentName`: Name of the document (from AdministrativeSourceType)
  - `SubmissionDate`: When the document was submitted
  - `AlfDocumentId`: Alfresco node ID (external storage reference)
  - `AppRegId`: Application Registration ID (required, user input)
  - `UniqueParcelId`: Unique Parcel ID (optional/deprecated)
  - `IsVoid`: Versioning flag (true if superseded)
  - `ModifiedBy`, `CreatedBy`: Audit fields
  - Navigation property: `Source`

**Source.cs**
- Represents a source record
- Properties:
  - `Id`: Primary key
  - `RecordationDate`: Date the source was recorded
  - `Status`: Active/inactive status
  - `IsCreated`: Flag indicating if source was created
  - Navigation property: `AdministrativeSource`

**AdministrativeSource.cs**
- Links Source to AdministrativeSourceType
- Properties:
  - `Id`: Primary key
  - `SourceId`: Foreign key to Source
  - `AdministrativeSourceTypeId`: Foreign key to AdministrativeSourceType
  - `ApplicationId`, `BaUnitId`: Optional foreign keys
  - Navigation property: `AdministrativeSourceType`

**AdministrativeSourceType.cs**
- Lookup table for document types (multilingual support)
- Properties:
  - `Id`: Primary key
  - `EnglishValue`: English name (required)
  - `AmharicValue`, `OromifaValue`, `TigrinyaValue`, `HarariValue`: Multilingual names
- Seed data includes 7 predefined types (Title Certificate, Tax Receipt, etc.)

#### Folder: `Interfaces/`

**IGenericRepository<T>.cs**
- Generic repository pattern interface
- Methods: `GetByIdAsync`, `GetAllAsync`, `FindAsync`, `AddAsync`, `Update`, `Remove`

**IDocumentRepository.cs**
- Extends `IGenericRepository<Document>`
- Additional method: `GetLatestBySourceAndTypeAsync` (for versioning)

**ISourceRepository.cs**
- Extends `IGenericRepository<Source>`

**IUnitOfWork.cs**
- Unit of Work pattern interface
- Provides access to all repositories
- Methods: `SaveChangesAsync`, `Dispose`
- Properties: `Sources`, `Documents`, `AdministrativeSources`, `AdministrativeSourceTypes`

#### Folder: `common/`

**IAggregateRoot.cs**
- Marker interface for aggregate root entities
- Ensures all domain entities have an `Id` property

---

### 2. LRS.Application Layer (Business Logic)

**Purpose**: Contains application-specific business logic, orchestrates domain operations, and defines DTOs for data transfer.

#### Folder: `DTOs/`

**UploadDocumentDto.cs**
- DTO for document upload requests
- Properties:
  - `SourceId` (optional): Link to existing source
  - `AdministrativeSourceTypeId` (required): Document type
  - `AppRegId` (required): Application Registration ID
  - `File` (required): IFormFile to upload
  - `CreatedBy` (optional): User who created the document
  - `RecordationDate` (optional): Source recordation date
- Includes data annotations for validation

**DocumentResponseDto.cs**
- DTO for document responses
- Properties: All document fields plus `AdminSourceTypeEnglish` for display

#### Folder: `Interfaces/`

**IDocumentService.cs**
- Application service interface for document operations
- Methods:
  - `UploadDocumentAsync`: Upload and process documents
  - `GetDocumentFileAsync`: Retrieve document file stream
  - `GetDocumentsBySourceIdAsync`: Get all documents for a source

**IAlfrescoService.cs**
- External service interface for Alfresco integration
- Methods:
  - `UploadDocumentAsync`: Upload file to Alfresco
  - `GetDocumentStreamAsync`: Retrieve file from Alfresco
  - `TestConnectionAsync`: Test Alfresco connectivity
  - `DeleteNodeAsync`: Delete node in Alfresco (cleanup)

#### Folder: `Services/`

**DocumentService.cs**
- Main business logic for document operations
- **UploadDocumentAsync Flow**:
  1. Validates AdministrativeSourceType exists
  2. Resolves or creates Source entity
  3. Creates AdministrativeSource association
  4. Uploads file to Alfresco (stores in folder hierarchy: AppRegId/Application/BaUnit/SpatialUnit)
  5. Handles versioning (marks old documents as void)
  6. Saves document metadata to database
  7. Returns DocumentResponseDto
  8. Includes error handling with Alfresco cleanup on DB failure

- **GetDocumentFileAsync**: Retrieves file from Alfresco using node ID
- **GetDocumentsBySourceIdAsync**: Returns all documents for a source

---

### 3. LRS.Infrastructure Layer (Data Access & External Services)

**Purpose**: Implements data access, repositories, and external service integrations.

#### Folder: `Persistence/`

**LrsDbContext.cs**
- Entity Framework Core DbContext
- DbSets: `Sources`, `AdministrativeSources`, `AdministrativeSourceTypes`, `Documents`
- `OnModelCreating`: Configures seed data for AdministrativeSourceType (7 predefined types)

#### Folder: `Repositories/`

**GenericRepository<T>.cs**
- Generic repository implementation
- Uses DbContext for all CRUD operations
- Implements `IGenericRepository<T>`

**DocumentRepository.cs**
- Extends `GenericRepository<Document>`
- Implements `IDocumentRepository`
- `GetLatestBySourceAndTypeAsync`: Uses LINQ with Include for eager loading, orders by SubmissionDate descending

**SourceRepository.cs**
- Extends `GenericRepository<Source>`
- Implements `ISourceRepository`

**UnitOfWork.cs**
- Implements `IUnitOfWork`
- Provides centralized access to all repositories
- Manages DbContext lifecycle
- `SaveChangesAsync`: Commits all changes to database

#### Folder: `Services/`

**AlfrescoService.cs**
- Implements `IAlfrescoService`
- HTTP client-based service for Alfresco REST API
- **Key Features**:
  - Basic authentication (username/password from config)
  - Root node resolution (tries multiple methods: -root- alias, Company Home, fallback)
  - Folder hierarchy creation (AppRegId/Application/BaUnit/SpatialUnit)
  - File upload with conflict handling
  - File retrieval with streaming
  - Node deletion for cleanup

#### Folder: `Migrations/`

- Entity Framework Core migrations
- `InitialCreate`: Initial database schema
- `AddSourceDocumentManagement`: Adds Source and Document management
- `MakeUniqueParcelIdNullable`: Schema update
- `LrsDbContextModelSnapshot`: Current model snapshot

---

### 4. LRS.API Layer (Presentation/Interface)

**Purpose**: HTTP API endpoints, request/response handling, middleware.

#### File: `Program.cs`

Application entry point and dependency injection configuration:

**Service Registrations**:
- `LrsDbContext`: PostgreSQL database context
- Repositories: `ISourceRepository`, `IDocumentRepository`, `IUnitOfWork`
- `IAlfrescoService`: HTTP client with base URL configuration
- `IDocumentService`: Application service
- Controllers with validation configuration
- Swagger/OpenAPI
- CORS policy for Angular frontend (localhost:4200)

**Middleware Pipeline**:
1. `GlobalExceptionHandlerMiddleware`: Global exception handling
2. CORS
3. Swagger UI
4. Controllers

#### Folder: `Controllers/`

**DocumentsController.cs**
- `POST /api/documents/upload`: Upload document
  - Accepts `multipart/form-data`
  - Validates using model binding
  - Returns `DocumentResponseDto`
  
- `GET /api/documents/{id}`: Get document file
  - Streams file from Alfresco through API
  - Returns file with appropriate content type
  
- `GET /api/documents/source/{sourceId}`: Get documents by source
  - Returns list of `DocumentResponseDto`

**AdministrativeSourceTypesController.cs**
- `GET /api/administrative-source-types`: Get all types
- `GET /api/administrative-source-types/{id}`: Get type by ID

**AlfrescoController.cs**
- `GET /api/alfresco/test`: Test Alfresco connectivity
  - Returns connection status and root node ID

#### Folder: `Middleware/`

**GlobalExceptionHandlerMiddleware.cs**
- Global exception handler (SRS requirement)
- Catches all unhandled exceptions
- Maps exceptions to HTTP status codes:
  - `FileNotFoundException` → 404 Not Found
  - `ArgumentNullException`, `ArgumentException` → 400 Bad Request
  - `UnauthorizedAccessException` → 401 Unauthorized
  - Default → 500 Internal Server Error
- Returns JSON error response
- Includes detailed error in development environment

#### Configuration Files

**appsettings.json**
- Connection string: PostgreSQL database
- Alfresco configuration:
  - BaseUrl: Alfresco REST API endpoint
  - Username/Password: Authentication
  - RootFolderId: Root folder identifier (-root-)

---

### 5. LRS.Client Layer (Frontend)

**Purpose**: Angular-based user interface for document management.

#### Structure

**app.config.ts**
- Angular application configuration
- Providers: Zone change detection, Router, HttpClient

**app.routes.ts**
- Route configuration:
  - `/` → redirects to `/scan`
  - `/scan` → ScanComponent (lazy loaded)
  - `/documents` → DocumentListComponent (lazy loaded)

#### Folder: `components/`

**scan/ScanComponent**
- Document scanning and upload interface
- Features:
  - Camera/scanner integration (WebRTC getUserMedia)
  - File upload option
  - Form validation
  - Administrative Source Type dropdown (loaded from API)
  - AppRegId input (required)
  - SourceId input (optional, for linking to existing source)
  - Upload progress and status messages
  - Image preview after capture/selection

**document-list/DocumentListComponent**
- Document listing and management interface
- Features: (Implementation details would be in component file)

#### Folder: `services/`

**document.service.ts**
- Angular service for document API calls
- Methods:
  - `uploadDocument`: POST to `/api/documents/upload`
  - `getDocumentsBySource`: GET `/api/documents/source/{sourceId}`
  - `getDocumentFile`: GET `/api/documents/{id}` (blob)
  - `downloadDocument`: Downloads document file

**administrative-source-type.service.ts**
- Service for AdministrativeSourceType API
- Methods: `getAll()`, `getById()`

---

## Data Flow

### Document Upload Flow

```
1. User Action (Frontend)
   └─> ScanComponent.uploadDocument()
       └─> FormData created with file, AppRegId, AdministrativeSourceTypeId, etc.

2. HTTP Request
   └─> POST /api/documents/upload
       └─> DocumentsController.UploadDocument()

3. Application Layer
   └─> DocumentService.UploadDocumentAsync()
       │
       ├─> Validate AdministrativeSourceType (UnitOfWork.AdministrativeSourceTypes)
       │
       ├─> Resolve/Create Source
       │   └─> UnitOfWork.Sources.GetByIdAsync() OR AddAsync()
       │
       ├─> Create/Update AdministrativeSource
       │   └─> UnitOfWork.AdministrativeSources.AddAsync()
       │
       ├─> Upload to Alfresco
       │   └─> AlfrescoService.UploadDocumentAsync()
       │       ├─> Get/Create folder hierarchy: AppRegId/Application/BaUnit/SpatialUnit
       │       └─> Upload file, return nodeId
       │
       ├─> Handle Versioning
       │   └─> UnitOfWork.Documents.GetLatestBySourceAndTypeAsync()
       │       └─> Mark old document as void (IsVoid = true)
       │
       ├─> Create Document Entity
       │   └─> UnitOfWork.Documents.AddAsync()
       │
       └─> Save Changes
           └─> UnitOfWork.SaveChangesAsync()
               └─> LrsDbContext.SaveChanges()
                   └─> PostgreSQL Database

4. Response
   └─> DocumentResponseDto returned to Controller
       └─> JSON response to Frontend
```

### Document Retrieval Flow

```
1. User Action (Frontend)
   └─> DocumentService.getDocumentFile(documentId)

2. HTTP Request
   └─> GET /api/documents/{id}
       └─> DocumentsController.GetDocument()

3. Application Layer
   └─> DocumentService.GetDocumentFileAsync()
       ├─> UnitOfWork.Documents.GetByIdAsync(id)
       │   └─> Database query via Entity Framework
       │
       └─> AlfrescoService.GetDocumentStreamAsync(nodeId)
           └─> HTTP GET to Alfresco REST API
               └─> /nodes/{nodeId}/content

4. Response
   └─> File stream returned
       └─> Controller.File(stream, contentType, fileName)
           └─> Binary response to Frontend
```

### Error Handling Flow

```
Exception Occurs (Any Layer)
   └─> GlobalExceptionHandlerMiddleware.InvokeAsync()
       ├─> Log exception (ILogger)
       └─> HandleExceptionAsync()
           ├─> Map exception type to HTTP status code
           ├─> Create JSON error response
           └─> Return error response
```

---

## Database Schema

### Entity Relationships

```
AdministrativeSourceType (1) ──┐
                                │
                                │ (many-to-one)
                                │
AdministrativeSource (many) ────┘
    │
    │ (one-to-one)
    │
Source (1)
    │
    │ (one-to-many)
    │
Document (many)
```

### Tables

**AdministrativeSourceTypes**
- `Id` (PK, int, identity)
- `AmharicValue` (string, nullable)
- `EnglishValue` (string, required)
- `OromifaValue` (string, nullable)
- `TigrinyaValue` (string, nullable)
- `HarariValue` (string, nullable)

**AdministrativeSources**
- `Id` (PK, int, identity)
- `SourceId` (FK → Sources, int, unique)
- `AdministrativeSourceTypeId` (FK → AdministrativeSourceTypes, int)
- `ApplicationId` (int, nullable)
- `BaUnitId` (int, nullable)

**Sources**
- `Id` (PK, int, identity)
- `RecordationDate` (datetime, nullable)
- `Status` (bool, default: false)
- `IsCreated` (bool)

**Documents**
- `Id` (PK, int, identity)
- `SourceId` (FK → Sources, int, nullable)
- `DocumentName` (string, nullable)
- `SubmissionDate` (datetime, nullable)
- `AlfDocumentId` (string, nullable) - Alfresco node ID
- `AppRegId` (string, required)
- `UniqueParcelId` (string, nullable)
- `IsVoid` (bool, default: false)
- `ModifiedBy` (string, nullable)
- `CreatedBy` (string, nullable)

---

## API Endpoints

### Base URL
- Development: `http://localhost:5000` (or configured port)
- Swagger UI: `http://localhost:5000/swagger`

### Endpoints

#### Documents

| Method | Endpoint | Description | Request Body | Response |
|--------|----------|-------------|--------------|----------|
| POST | `/api/documents/upload` | Upload a document | multipart/form-data (UploadDocumentDto) | DocumentResponseDto |
| GET | `/api/documents/{id}` | Get document file | - | File stream |
| GET | `/api/documents/source/{sourceId}` | Get documents by source | - | DocumentResponseDto[] |

#### Administrative Source Types

| Method | Endpoint | Description | Request Body | Response |
|--------|----------|-------------|--------------|----------|
| GET | `/api/administrative-source-types` | Get all types | - | AdministrativeSourceType[] |
| GET | `/api/administrative-source-types/{id}` | Get type by ID | - | AdministrativeSourceType |

#### Alfresco

| Method | Endpoint | Description | Request Body | Response |
|--------|----------|-------------|--------------|----------|
| GET | `/api/alfresco/test` | Test Alfresco connection | - | { success: bool, nodeId?: string, error?: string } |

---

## External Integrations

### Alfresco ECM Integration

**Purpose**: External document storage and management

**Configuration**:
- Base URL: Configured in `appsettings.json`
- Authentication: Basic Auth (username/password)
- API Version: Alfresco REST API v1

**Folder Structure in Alfresco**:
```
Root (-root-)
└── {AppRegId}/              # Application Registration ID folder
    └── Application/
        └── BaUnit/
            └── SpatialUnit/
                └── {FileName}    # Uploaded document
```

**Operations**:
1. **Upload**: Creates folder hierarchy if needed, uploads file, returns node ID
2. **Retrieve**: Gets file content stream using node ID
3. **Delete**: Deletes node (used for cleanup on error)
4. **Test Connection**: Validates connectivity and credentials

**Error Handling**:
- Upload failures trigger cleanup (delete node if DB save fails)
- Connection errors are logged and returned to client

---

## Key Modules and Components

### Repository Pattern

**Purpose**: Abstracts data access logic, provides testability

**Implementation**:
- `IGenericRepository<T>`: Generic interface for common operations
- Specific repositories extend generic: `IDocumentRepository`, `ISourceRepository`
- All repositories implemented in `LRS.Infrastructure.Repositories`

### Unit of Work Pattern

**Purpose**: Manages transactions, coordinates multiple repositories

**Implementation**:
- `IUnitOfWork` interface in Domain layer
- `UnitOfWork` class in Infrastructure layer
- Provides access to all repositories
- `SaveChangesAsync()` commits all changes in a single transaction

### Service Layer Pattern

**Purpose**: Encapsulates business logic, orchestrates domain operations

**Implementation**:
- Application services in `LRS.Application.Services`
- Services depend on repositories (via UnitOfWork) and external services
- Examples: `DocumentService`, `AlfrescoService`

### Dependency Injection

**Configuration**: `Program.cs`

**Scoped Services** (per HTTP request):
- `LrsDbContext`
- All repositories (`ISourceRepository`, `IDocumentRepository`, `IUnitOfWork`)
- Application services (`IDocumentService`)

**HTTP Client Services**:
- `IAlfrescoService` (registered as HttpClient with BaseAddress)

### Versioning Strategy

**Document Versioning**:
- When a new document is uploaded for the same Source and AdministrativeSourceType, the old document is marked as `IsVoid = true`
- The new document is created with `IsVoid = false`
- This allows historical tracking while maintaining current active documents

### Error Handling Strategy

**Layers**:
1. **Domain/Application**: Throws domain exceptions
2. **Infrastructure**: Throws infrastructure exceptions (DB, HTTP)
3. **API Middleware**: Catches all exceptions, maps to HTTP status codes, returns JSON errors

**Exception Types**:
- `FileNotFoundException` → 404
- `ArgumentException`, `ArgumentNullException` → 400
- `UnauthorizedAccessException` → 401
- Others → 500

### Validation Strategy

**Levels**:
1. **Client-side**: Angular form validation
2. **DTO Validation**: Data annotations (Required, StringLength, Range)
3. **API Model Binding**: ASP.NET Core model validation
4. **Business Logic**: Application service validation (e.g., AdministrativeSourceType exists)

---

## Configuration and Environment

### Backend Configuration (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=LRS;Username=postgres;Password=123"
  },
  "Alfresco": {
    "BaseUrl": "http://localhost:8080/alfresco/api/-default-/public/alfresco/versions/1",
    "Username": "admin",
    "Password": "admin",
    "RootFolderId": "-root-"
  }
}
```

### Frontend Configuration

- **API Proxy**: `proxy.conf.json` (configured for development)
- **Base URL**: `/api` (relative, proxied to backend)
- **CORS**: Configured in backend for `http://localhost:4200`

---

## Development Workflow

### Database Migrations

```bash
# Add migration
dotnet ef migrations add MigrationName --project LRS.Infrastructure --startup-project LRS.API

# Update database
dotnet ef database update --project LRS.Infrastructure --startup-project LRS.API
```

### Running the Application

**Backend**:
```bash
cd LRS.API
dotnet run
```

**Frontend**:
```bash
cd LRS.Client
npm install
ng serve
```

**Access Points**:
- API: `http://localhost:5000` (or configured port)
- Swagger: `http://localhost:5000/swagger`
- Frontend: `http://localhost:4200`

---

## Testing and Quality Assurance

### API Testing
- Swagger UI provides interactive API testing
- Endpoints can be tested directly from Swagger interface

### Error Scenarios Covered
- Invalid file uploads
- Missing required fields
- Database connection failures
- Alfresco connection failures
- Document not found scenarios
- Validation errors

---

## Security Considerations

### Current Implementation
- Basic authentication for Alfresco (credentials in configuration)
- CORS configured for specific origin (localhost:4200)
- Input validation on DTOs
- SQL injection protection via EF Core parameterized queries

### Recommendations for Production
- Implement authentication/authorization (JWT, OAuth2)
- Use HTTPS
- Secure Alfresco credentials (use Azure Key Vault, environment variables, or secrets manager)
- Implement role-based access control
- Add rate limiting
- Implement audit logging
- Use connection string encryption

---

## Future Enhancements

### Potential Improvements
1. Authentication and Authorization
2. Advanced search and filtering
3. Document preview (PDF viewer)
4. Batch document upload
5. Document metadata editing
6. Audit trail/logging
7. Unit and integration tests
8. Docker containerization
9. CI/CD pipeline
10. Multi-language UI support

---

## Conclusion

The Land Registration System demonstrates a well-structured Clean Architecture implementation with clear separation of concerns. The system effectively integrates external services (Alfresco) while maintaining a clean domain model. The layered architecture promotes maintainability, testability, and scalability.

**Key Strengths**:
- Clear architectural boundaries
- Dependency inversion (interfaces in Domain, implementations in Infrastructure)
- Comprehensive error handling
- Document versioning support
- Integration with enterprise content management

**Architecture Compliance**:
- Domain layer has no external dependencies ✓
- Infrastructure implements Domain interfaces ✓
- Application orchestrates business logic ✓
- API layer is thin (controllers delegate to services) ✓

---

*Document Version: 1.0*  
*Last Updated: January 2025*

