# Data Model: Document Upload and Management

## Entities

### User
- UserId: int (primary key)
- Email: string (required, unique)
- DisplayName: string (required)
- Department: string?
- JobTitle: string?
- Role: UserRole (Employee, TeamLead, ProjectManager, Administrator)
- AvailabilityStatus: enum
- CreatedDate: DateTime
- Notifications: relation to user notifications
- Project memberships: relation to project participants

### Project
- ProjectId: int (primary key)
- Name: string (required)
- Description: string?
- ProjectManagerId: int
- Status: ProjectStatus
- StartDate: DateTime
- TargetCompletionDate: DateTime?
- CreatedDate: DateTime
- UpdatedDate: DateTime
- Tasks: collection of task items
- ProjectMembers: collection of members

### TaskItem
- TaskId: int (primary key)
- Title: string (required)
- Description: string?
- Priority: enum
- Status: enum
- DueDate: DateTime?
- AssignedUserId: int
- CreatedByUserId: int
- ProjectId: int?
- CreatedDate: DateTime
- UpdatedDate: DateTime

### Document
- DocumentId: int (primary key)
- Title: string (required)
- Description: string?
- Category: string (required, e.g. Project Documents, Team Resources, Personal Files, Reports, Presentations, Other)
- FileName: string (original or sanitized name used for display only)
- StoredFileName: string (safe generated name, unique and non-user-controlled)
- FilePath: string (relative or secure local path)
- FileType: string (MIME type or content type, up to 255 chars)
- FileSizeBytes: long
- UploadedByUserId: int
- ProjectId: int?
- TaskId: int?
- UploadedDate: DateTime
- UpdatedDate: DateTime
- ScanStatus: enum (Pending, Clean, Quarantined, Failed)
- ScanCompletedDate: DateTime?
- IsShared: bool
- Tags: string? or separate tag mapping if implemented later

### DocumentShare
- DocumentShareId: int (primary key)
- DocumentId: int
- UserId: int
- SharedByUserId: int
- SharedDate: DateTime
- IsActive: bool

### Notification
- NotificationId: int (primary key)
- UserId: int
- Title: string
- Message: string
- Type: enum
- Priority: enum
- IsRead: bool
- CreatedDate: DateTime

### AuditLog
- AuditLogId: int (primary key)
- UserId: int?
- DocumentId: int?
- ActionType: string (Upload, Download, Edit, Delete, Share)
- ActionDate: DateTime
- Details: string

## Relationships

- User 1..N Document
- Project 1..N Document
- Task 1..N Document
- Document 1..N DocumentShare
- User 1..N DocumentShare
- User 1..N Notification
- User 1..N AuditLog
- Document 1..N AuditLog

## Validation Rules

- Document title is required and must be non-empty.
- Category must be one of the allowed values: Project Documents, Team Resources, Personal Files, Reports, Presentations, Other.
- File size must be <= 25 MB.
- Accepted file types must match an allowlist for PDF, Office docs, text, and image types.
- File path must be generated server-side and must not use raw client filenames.
- ScanStatus must begin as Pending after upload and must become Clean, Quarantined, or Failed through the scan workflow.
- Only documents with ScanStatus Clean may appear in normal search, preview, download, or sharing results.
- Only authorized users may view, share, update, or delete a document.
- Project association is optional but must validate against project membership when present.

## State Transitions

- Draft/Upload Pending -> Uploaded with ScanStatus Pending -> Clean -> Available
- ScanStatus Pending -> Quarantined or Failed
- Available -> Shared
- Available -> Updated with ScanStatus Pending when the file is replaced
- Available -> Deleted
- Deleted -> Archived or removed from active lists

The design keeps the model simple and aligns with the repo’s current user/project/task schema while adding only the metadata and access rules needed for document management.
