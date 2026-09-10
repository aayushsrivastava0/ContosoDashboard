# Research: Document Upload and Management

## Decision

The feature will be implemented as a secure, training-focused document management capability using the existing Blazor Server architecture, local filesystem storage, and mock role-based access control. It will use an `IFileStorageService` abstraction with a local implementation as the active storage backend and a future Azure-ready replacement as a migration path.

## Rationale

- The repository constitution explicitly requires training/offline boundaries and local-only architecture for this project.
- The current app is built around EF Core + SQL Server LocalDB and a cookie-based mock auth system, which already supports role-driven access and user-specific visibility.
- The feature brief emphasizes secure document access, project visibility, auditability, and offline operation, which fits a service-layer and data-layer extension without introducing a new platform.
- The repository already demonstrates separation between services, data context, and UI, making it the right place to add document management without a major rewrite.

## Alternatives Considered

### 1. Production Azure/Entra implementation
- Pros: production-grade identity, cloud-managed storage, stronger scaling model.
- Cons: conflicts with the repo’s training/offline model and would create unnecessary architecture drift.
- Rejected because: this repo is built to teach security and architecture patterns in a local-first environment, not to model production identity and cloud deployment as the default path.

### 2. Hybrid model with Azure-first implementation
- Pros: supports future production migration with minimal business-logic changes.
- Cons: adds operational and configuration complexity before the feature is validated for the training use case.
- Rejected because: the simpler local-first design is the correct fit for the current app and still preserves future abstraction boundaries.

### 3. UI-only document management without service-layer rules
- Pros: faster initial implementation.
- Cons: weak authorization controls, no reusable storage abstraction, and poor auditability.
- Rejected because: it would violate the project’s maintainable-separation and access-control principles.

## Research Findings

- Storage should be kept outside `wwwroot` to avoid direct browser access and to support authorization checks before file delivery.
- Files should be saved using a unique generated path and database metadata should be written only after the file is successfully saved.
- Document access should be enforced at the service layer, not just in the UI, to protect against IDOR and bypass attempts.
- Notification and audit flows can reuse the existing user and notification patterns already implemented in the dashboard.
- Task and project association should be represented via foreign keys and project membership checks so document visibility is consistent with current role-driven project access.
- Virus scanning should be asynchronous: after a valid upload is stored, the application records a pending scan and submits a queue message containing the document ID and secure storage path. An Azure Function with a Queue Storage trigger is the production-ready worker pattern; local development uses a mock or local queue implementation and does not require Azure credentials.
- Scan outcomes should be persisted as clean, quarantined, or failed. Only clean documents are available for normal viewing, download, sharing, and search; quarantined or failed documents remain blocked and produce audit and notification events.

## Open Decisions to Preserve

- Category values remain text-based strings rather than enum-backed integer columns to keep the feature simpler and more flexible.
- Document IDs remain integer keys to match the project’s existing User/Project/Task identity model.
- Future Azure migration remains an abstraction concern rather than a required current implementation detail. Azure Queue Storage and Azure Functions are optional deployment components; the local implementation must work without cloud keys or services.
