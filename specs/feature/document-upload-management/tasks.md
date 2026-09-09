---

description: "Implementation tasks for document upload and management"
---

# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/feature/document-upload-management/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, and `quickstart.md`
**Tests**: No automated test tasks are included because the feature specification requests repeatable validation scenarios but does not require an automated test or TDD workflow.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the folders, configuration, and package references needed by the document feature.

- [ ] T001 Create the feature source folders `ContosoDashboard/Background/` and `ContosoDashboard/Models/` entries required by the implementation plan.
- [ ] T002 [P] Add document storage and scan-processing configuration sections with local-safe defaults to `ContosoDashboard/appsettings.json` and `ContosoDashboard/appsettings.Development.json`; do not add Azure keys or required cloud settings.
- [ ] T003 [P] Confirm the feature targets the existing ASP.NET Core 8, Blazor Server, EF Core, and LocalDB dependencies in `ContosoDashboard/ContosoDashboard.csproj` without adding a required Azure SDK dependency for local execution.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Implement shared persistence, storage, authorization, and queue abstractions before any user story work begins.

**Checkpoint**: Local document persistence and the credential-free queue path are available; all story work can proceed.

- [ ] T004 [P] Create the `Document` entity in `ContosoDashboard/Models/Document.cs` with required title/category fields, secure generated storage path metadata, uploader/project/task relationships, sharing flag, and `ScanStatus` values `Pending`, `Clean`, `Quarantined`, and `Failed`.
- [ ] T005 [P] Create the `DocumentShare` entity in `ContosoDashboard/Models/DocumentShare.cs` with document, recipient user, sharing user, timestamp, and active-state fields.
- [ ] T006 [P] Create the `AuditLog` entity in `ContosoDashboard/Models/AuditLog.cs` for upload, download, edit, delete, share, and scan-outcome events.
- [ ] T007 Add `DbSet<Document>`, `DbSet<DocumentShare>`, and `DbSet<AuditLog>` plus relationships, indexes, and delete behaviors to `ContosoDashboard/Data/ApplicationDbContext.cs`.
- [ ] T008 Create the local database migration or schema update for document metadata, shares, audit records, and scan status using the existing EF Core setup in `ContosoDashboard/Data/ApplicationDbContext.cs`.
- [ ] T009 [P] Define `IFileStorageService` in `ContosoDashboard/Services/IFileStorageService.cs` for secure save, read, replace, and delete operations outside `wwwroot`.
- [ ] T010 Implement `LocalFileStorageService` in `ContosoDashboard/Services/LocalFileStorageService.cs` using generated server-side filenames, a configured application-data directory, path traversal protection, and no raw client filename as a storage path.
- [ ] T011 [P] Define `IScanQueueService` and a queue message contract in `ContosoDashboard/Services/IScanQueueService.cs` containing document ID, secure storage path, and metadata needed by the scanner.
- [ ] T012 Implement the credential-free local queue and mock scanner in `ContosoDashboard/Services/DocumentScanQueueService.cs`, preserving the `Clean`, `Quarantined`, and `Failed` outcomes without Azure credentials.
- [ ] T013 [P] Create shared document authorization checks in `ContosoDashboard/Services/DocumentAccessService.cs` for owner, project membership, team-lead, project-manager, and administrator access using the existing mock-auth user and role model.
- [ ] T014 Register document services, local storage, local scan queue, and authorization services in `ContosoDashboard/Program.cs`, including secure local storage path configuration from app settings.

---

## Phase 3: User Story 1 - Upload and organize documents securely (Priority: P1) MVP

**Goal**: Let authenticated users upload valid documents, assign metadata, store files securely, and observe asynchronous local scan outcomes before access is granted.

**Independent Test**: Follow scenarios 1-3 in `specs/feature/document-upload-management/quickstart.md`: a valid upload enters pending scan, a clean result makes it available, invalid input is rejected, and quarantined or failed files remain blocked without Azure keys.

### Implementation for User Story 1

- [ ] T015 [US1] Implement document upload validation and metadata creation in `ContosoDashboard/Services/DocumentService.cs`, enforcing required title/category, allowed PDF/Office/text/image types, the 25 MB per-file limit, project membership validation, and pending scan status.
- [ ] T016 [US1] Implement the upload transaction in `ContosoDashboard/Services/DocumentService.cs` so the file is saved outside `wwwroot`, metadata is persisted, an upload audit record is created, and a scan message is queued only after successful file persistence.
- [ ] T017 [US1] Implement local scan processing in `ContosoDashboard/Background/DocumentVirusScanWorker.cs` so queued documents transition from `Pending` to `Clean`, `Quarantined`, or `Failed`, and failed outcomes block access while creating audit and notification records.
- [ ] T018 [US1] Add the Azure-ready Queue Storage trigger adapter contract and implementation shape in `ContosoDashboard/Background/DocumentVirusScanFunction.cs`; keep it disabled from local startup and document that deployment requires Azure configuration outside the training app.
- [ ] T019 [US1] Create the upload and document-list interaction in `ContosoDashboard/Pages/Documents.razor`, including multi-file selection, title/category/project/description/tag fields, upload progress, pending-scan state, and clear validation or completion messages.
- [ ] T020 [US1] Add document navigation to `ContosoDashboard/Shared/NavMenu.razor` and route protection to `ContosoDashboard/Pages/Documents.razor` using the existing authentication and authorization conventions.
- [ ] T021 [US1] Add upload, scan outcome, and validation notifications through `ContosoDashboard/Services/NotificationService.cs` and record lifecycle details through `ContosoDashboard/Services/DocumentService.cs`.

**Checkpoint**: User Story 1 is independently usable with local storage and mock scanning; no Azure keys, queue, or antivirus service is required.

---

## Phase 4: User Story 2 - Search, browse, and filter documents by role and context (Priority: P2)

**Goal**: Let users search and filter only the clean documents within their authorized personal, project, or team scope.

**Independent Test**: Follow scenario 4 in `specs/feature/document-upload-management/quickstart.md`; search by title, tag, uploader, project, category, and date returns only authorized documents within the two-second target.

### Implementation for User Story 2

- [ ] T022 [US2] Add authorized document query methods to `ContosoDashboard/Services/DocumentService.cs` for title, description, tags, uploader, project, category, date, and sorting while excluding non-clean documents from normal results.
- [ ] T023 [US2] Add scoped search, filters, sorting, empty states, and pending/quarantined indicators to `ContosoDashboard/Pages/Documents.razor` without exposing blocked file links.
- [ ] T024 [US2] Add secure preview and download methods to `ContosoDashboard/Services/DocumentService.cs` that re-check authorization and `ScanStatus == Clean` before returning file content.
- [ ] T025 [US2] Add preview and download actions to `ContosoDashboard/Pages/Documents.razor` with clear handling for unauthorized, pending, quarantined, failed, missing, and unsupported files.

**Checkpoint**: User Story 2 is independently testable using documents created by the upload story, with authorization and scan status enforced in the service layer.

---

## Phase 5: User Story 3 - Share, manage, and review document access (Priority: P2)

**Goal**: Let owners and authorized managers share clean documents, update metadata or files, delete documents, and review the resulting notifications and audit trail.

**Independent Test**: Follow scenario 5 in `specs/feature/document-upload-management/quickstart.md`; share a clean document, verify the recipient notification and access, then verify authorized edit/delete actions and audit entries.

### Implementation for User Story 3

- [ ] T026 [US3] Implement share, revoke, and recipient-scope authorization methods in `ContosoDashboard/Services/DocumentService.cs`, allowing only authorized users to share documents with permitted users or project audiences.
- [ ] T027 [US3] Implement metadata edit and secure file replacement in `ContosoDashboard/Services/DocumentService.cs`, resetting replacement files to `Pending` and re-queuing the scan before access resumes.
- [ ] T028 [US3] Implement authorized delete and secure physical file removal in `ContosoDashboard/Services/DocumentService.cs`, including audit logging and cleanup of active share records.
- [ ] T029 [US3] Add share, revoke, edit, replace, and delete controls to `ContosoDashboard/Pages/Documents.razor` with confirmation and blocked-state handling.
- [ ] T030 [US3] Add recipient notifications and document lifecycle audit entries through `ContosoDashboard/Services/NotificationService.cs` and `ContosoDashboard/Services/DocumentService.cs`.
- [ ] T031 [US3] Add an authorized document activity view in `ContosoDashboard/Pages/Notifications.razor` or a dedicated component under `ContosoDashboard/Shared/` for sharing and scan-result notifications.

**Checkpoint**: User Story 3 is independently testable for clean documents and preserves service-layer authorization for every mutation and access action.

---

## Phase 6: User Story 4 - Integrate documents with project tasks and dashboard activity (Priority: P3)

**Goal**: Connect clean, authorized documents to project details, task context, recent activity, and dashboard counts.

**Independent Test**: Follow scenario 6 in `specs/feature/document-upload-management/quickstart.md`; verify recent documents and counts on the dashboard and document context from project/task views.

### Implementation for User Story 4

- [ ] T032 [P] [US4] Add authorized project document queries and project-document counts to `ContosoDashboard/Services/ProjectService.cs`.
- [ ] T033 [P] [US4] Add authorized task-document queries and attachment context to `ContosoDashboard/Services/TaskService.cs`.
- [ ] T034 [US4] Add project document sections and upload/attach entry points to `ContosoDashboard/Pages/ProjectDetails.razor`.
- [ ] T035 [US4] Add related document links and upload/attach entry points to `ContosoDashboard/Pages/Tasks.razor`.
- [ ] T036 [US4] Add recent clean-document queries and an authorized document count to `ContosoDashboard/Services/DashboardService.cs`.
- [ ] T037 [US4] Add the recent documents widget and scoped document count to `ContosoDashboard/Pages/Index.razor`.
- [ ] T038 [US4] Notify authorized project participants when a clean project document becomes available through `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Services/NotificationService.cs`.

**Checkpoint**: User Story 4 is independently testable from project, task, and dashboard entry points while preserving scan and authorization rules.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Complete security review, performance checks, documentation, and end-to-end local validation.

- [ ] T039 [P] Update `ContosoDashboard/README.md` or the repository `README.md` with local storage location, mock scan behavior, pending/clean/quarantined/failed states, and the fact that Azure keys are not required locally.
- [ ] T040 [P] Review `ContosoDashboard/Services/DocumentService.cs`, `ContosoDashboard/Services/DocumentAccessService.cs`, and `ContosoDashboard/Services/LocalFileStorageService.cs` for IDOR, path traversal, unauthorized download, blocked-scan access, and orphaned-file risks.
- [ ] T041 [P] Review document list and search query performance in `ContosoDashboard/Services/DocumentService.cs` against the plan targets of under 2 seconds for 500 documents and search results.
- [ ] T042 Run the complete manual workflow in `specs/feature/document-upload-management/quickstart.md` against the local app, including valid upload, invalid input, local scan outcomes, access control, search, sharing, and dashboard integration.
- [ ] T043 Build the application with `dotnet build ContosoDashboard/ContosoDashboard.csproj` and resolve feature-related compile errors without introducing a required cloud dependency.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; can start immediately.
- **Foundational (Phase 2)**: Depends on Setup and blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational and is the MVP increment.
- **User Story 2 (Phase 4)**: Depends on Foundational and the document data/service surface from US1.
- **User Story 3 (Phase 5)**: Depends on Foundational and the clean-document access surface from US1/US2.
- **User Story 4 (Phase 6)**: Depends on Foundational and the document service surface from US1; it can be developed in parallel with US2 and US3 after those shared contracts are stable.
- **Polish (Phase 7)**: Depends on all desired stories being complete.

### User Story Dependencies

- **US1 (P1)**: Starts after Phase 2; no dependency on another user story.
- **US2 (P2)**: Starts after Phase 2; consumes the document query and clean-status behavior established in US1.
- **US3 (P2)**: Starts after Phase 2; consumes document authorization and clean-file behavior established in US1 and US2.
- **US4 (P3)**: Starts after Phase 2; integrates with the existing project/task/dashboard services and the document service from US1.

### Parallel Opportunities

- T004-T006 can run in parallel because they create separate model files.
- T009 and T011 can run in parallel because they define separate infrastructure abstractions.
- T032 and T033 can run in parallel because they modify separate service files.
- After the foundational checkpoint, separate developers can work on US2, US3, and US4 once the shared US1 service contracts are agreed.
- Documentation and security/performance review tasks T039-T041 can run in parallel after implementation stabilizes.

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 Setup.
2. Complete Phase 2 Foundational, including local storage and credential-free scan queue abstractions.
3. Complete Phase 3 User Story 1.
4. Run the upload, invalid-input, and local scan scenarios from `quickstart.md`.
5. Stop for validation before adding search, sharing, or dashboard integration.

### Incremental Delivery

1. Deliver US1 as the secure upload and local scan MVP.
2. Add US2 for authorized search, filtering, preview, and download.
3. Add US3 for sharing, management, notifications, and audit review.
4. Add US4 for project, task, and dashboard integration.
5. Run the full quickstart and build validation before release.

### Azure-Ready Extension

The local implementation remains the default and requires no Azure keys. The Azure Function and Queue Storage path is an optional deployment adapter described in `ContosoDashboard/Background/DocumentVirusScanFunction.cs`; it consumes the same queue message contract and updates the same document scan statuses when production infrastructure is introduced.

## Notes

- Every task uses the required checklist format: checkbox, sequential ID, optional `[P]`, required story label for story tasks, and an exact file path.
- `[P]` marks only tasks that can work in parallel without depending on incomplete changes in the same file.
- Manual validation is repeatable and documented in `specs/feature/document-upload-management/quickstart.md`.
- Local execution must remain offline-capable and must not require Azure credentials.
