# Implementation Plan: Document Upload and Management

**Branch**: `feature/document-upload-management` | **Date**: 2026-09-10 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/feature/document-upload-management/spec.md`

## Summary

This feature adds secure document upload, management, sharing, and discovery to ContosoDashboard while preserving the project’s training/offline architecture. The implementation will extend the existing Blazor Server app with a document domain model, storage abstraction, service-layer authorization checks, project/task integration, and dashboard notification flows. It will maintain the current mock-auth and role-based model while keeping the storage layer ready for future Azure migration without rewriting business logic.

The design also includes an asynchronous virus-scanning background job that processes uploaded files after they are accepted into the workflow. In the production-style Azure extension point, the app will enqueue a file-scan task to Azure Queue Storage and use an Azure Function with a Queue trigger to process the file and update the document status (clean, quarantined, or failed). In the current training app, the same pattern is represented with a queue-friendly service abstraction and local background worker behavior so the business workflow remains consistent.

## Technical Context

**Language/Version**: C# / .NET 8.0 / ASP.NET Core 8  
**Primary Dependencies**: ASP.NET Core, Blazor Server, Entity Framework Core, SQL Server LocalDB, Bootstrap 5, Azure Functions (for future production workflow), Azure Queue Storage  
**Storage**: Local filesystem for uploaded files in a secure application data directory; SQL Server tables for metadata and access records; Azure Queue Storage for async virus-scan jobs in Azure-ready design  
**Testing**: Manual validation flows for upload, access control, search, and dashboard behavior; future xUnit or integration tests may be added if project expands  
**Target Platform**: Windows desktop development environment with local web application deployment  
**Project Type**: Web application (Blazor Server)  
**Performance Goals**: Upload under 30 seconds for 25 MB files, document list under 2 seconds for 500 documents, search under 2 seconds, preview under 3 seconds  
**Constraints**: Offline-first training app, local-only storage, mock auth/cookie model, role-based access control, no production cloud dependencies, Azure-ready abstractions only  
**Scale/Scope**: 5,000 employee users, project/task/document records scoped per role and project membership

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Pass: Security & Training Boundaries — the feature uses the repo’s mock-auth model and local storage while keeping future cloud migration paths behind an abstraction.
- Pass: User-Centered Workflow & Role Awareness — the design keeps sharing, visibility, and access checks tied to project membership and user role.
- Pass: Test-First Validation — plan includes validation of upload, access control, search, and dashboard integration before acceptance.
- Pass: Maintainable Separation of Concerns — document logic will remain in models, services, and controllers/pages rather than UI-only logic.
- Pass: Simplicity & Explicit Ownership — the scope remains focused on upload, search, sharing, and project/task integration without adding unrelated platform features.

No gate violations require a complexity exception.

## Project Structure

### Documentation (this feature)

```text
specs/feature/document-upload-management/
├── spec.md              # Feature requirements and acceptance criteria
├── plan.md              # This file
├── research.md          # Phase 0 design findings
├── data-model.md        # Entity model and validation rules
├── quickstart.md        # Validation steps for upload and access flows
├── contracts/           # Storage and service contracts
└── tasks.md             # Generated later by the tasking workflow
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Data/
│   └── ApplicationDbContext.cs
├── Models/
│   ├── User.cs
│   ├── Project.cs
│   ├── ProjectMember.cs
│   ├── TaskItem.cs
│   ├── Notification.cs
│   ├── Announcement.cs
│   └── Document.cs
├── Services/
│   ├── CustomAuthenticationStateProvider.cs
│   ├── DashboardService.cs
│   ├── NotificationService.cs
│   ├── ProjectService.cs
│   ├── TaskService.cs
│   ├── UserService.cs
│   ├── DocumentService.cs
│   ├── IFileStorageService.cs
│   ├── LocalFileStorageService.cs
│   ├── IScanQueueService.cs
│   └── DocumentScanQueueService.cs
├── Pages/
│   ├── Index.razor
│   ├── Login.cshtml
│   ├── ProjectDetails.razor
│   ├── Projects.razor
│   ├── Tasks.razor
│   ├── Documents.razor
│   └── ...
├── Background/
│   └── DocumentVirusScanFunction.cs
├── Program.cs
├── App.razor
└── appsettings*.json
```

**Structure Decision**: Single web application with service-layer and EF model extensions. Files are stored outside `wwwroot` in a secure local folder and metadata is persisted through the existing `ApplicationDbContext`, while pages and component UI remain responsible only for interaction and rendering. The system also includes a background processing path for document scanning: upload triggers queue submission; the queue message contains document metadata, storage location, and retention status; and an Azure Function with a Queue Storage trigger reads the message and performs scan validation before updating the document state.

## Background Processing Design

### Virus Scan Workflow

1. A user uploads a valid file and the system stores the file to the local or storage-backed repository.
2. The document service creates an audit record and enqueues a virus-scan request containing the document identifier, path, and metadata.
3. The queue message is consumed asynchronously by an Azure Function using a Queue Storage trigger.
4. The scanning function validates the file, calls the chosen malware/AV service or a mock scan implementation for training use, and marks the document as `Clean`, `Quarantined`, or `Failed`.
5. If the document is clean, the user can view/download it and the item is surfaced in normal document lists.
6. If the file fails or is quarantined, the system prevents access, sends a notification to the uploader, and records the outcome in the audit log.

### Azure Functions + Queue Storage Trigger

The production-ready implementation will use Azure Queue Storage as the asynchronous handoff between the application and the scanning worker. The queue-based pattern is intentionally chosen because it decouples upload acceptance from scan processing, reduces upload latency, and allows the system to retry or reprocess items without blocking the user experience.

Example flow:

```text
Upload request -> DocumentService -> Queue message -> Azure Function (Queue trigger) -> Scan result -> Update DocumentStatus -> Notify uploader
```

This is the recommended architecture for the Azure-ready variant even though the training project may use a local mock queue or polling replacement during implementation. The key requirement is that the upload workflow never assumes scan completion before the file is ready for browsing or sharing.

## Complexity Tracking

No complexity exceptions or violations are required for this feature.
