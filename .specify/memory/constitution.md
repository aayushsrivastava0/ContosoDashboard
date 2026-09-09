<!-- Sync Impact Report
- Version change: template placeholders → 1.0.0
- Modified principles: template placeholders → Security & Training Boundaries; User-Centered Workflow & Role Awareness; Test-First Validation; Maintainable Separation; Simplicity & Explicit Ownership
- Added sections: Additional Constraints; Development Workflow; Governance
- Removed sections: all placeholder content from the generic template scaffold
- Follow-up TODOs: TODO(RATIFICATION_DATE): confirm the original ratification date for the project before formal governance sign-off
-->

# ContosoDashboard Constitution

## Core Principles

### I. Security & Training Boundaries
All work on ContosoDashboard MUST remain consistent with its training-only scope. The application MUST use mock authentication and local-only dependencies, and features MUST not assume production-grade identity, cloud deployment, or external service availability. Any security-related change MUST document the training context, the mock behavior being used, and the production controls that would be required outside this training environment.

This principle exists because the repository is explicitly designed for learning and demonstrates secure-by-practice patterns without claiming production readiness. The purpose is to teach proper access control and risk awareness without creating a false sense of operational security.

### II. User-Centered Workflow & Role Awareness
Every feature MUST support the real task flow of a dashboard used by project members, team leads, and administrators. The application MUST model ownership, visibility, and role-based responsibilities clearly across tasks, projects, notifications, and user profiles. Access to data MUST be limited to the user’s authorized scope, and any view or mutation MUST be checked against the active user and role context.

This principle preserves the product’s core value: each user sees only the data and actions relevant to their responsibilities. It also prevents unauthorized access and makes governance, UX, and authorization logic consistent across features.

### III. Test-First Validation
Any change to authentication, authorization, task filtering, project visibility, or business logic MUST be validated before it is accepted as complete. The project MUST prefer a failing check or explicit validation step first, then implement the fix and confirm the behavior with a focused run or repeatable verification. Manual validation is acceptable where automated tests are not yet configured, but the validation steps MUST be recorded and repeatable.

This principle ensures the project remains teachable and reliable. It prevents regressions in access control and keeps changes grounded in observable behavior rather than assumptions.

### IV. Maintainable Separation of Concerns
The project MUST keep its boundaries explicit: data access, domain models, business services, and UI concerns MUST remain separate. Authentication logic, access checks, and service-layer rules MUST remain in the application architecture rather than being embedded only in the UI. Infrastructure choices such as local persistence, mock identity, and dependency injection MUST remain replaceable without rewriting business behavior.

This principle exists to support the training objective and to preserve the learning path from simple mock implementations to production-ready abstraction patterns.

### V. Simplicity & Explicit Ownership
The application MUST favor clear, minimal, and understandable implementation patterns. Conventions MUST be explicit: naming, ownership, access conditions, and UI behavior MUST be easy to follow without hidden side effects. Complexity MUST be justified by a clear requirement, and any exception to the project’s simplified training model MUST be documented.

This principle keeps the codebase approachable for students and preserves the discipline needed to reason about security and behavior in a training environment.

## Additional Constraints

The project MUST operate within the following technical and governance constraints:

- The application uses ASP.NET Core with Blazor Server, Entity Framework Core, and LocalDB for training purposes.
- Authentication is mock-based and cookie-driven; it MUST remain clearly labeled as a training implementation and not treated as production identity.
- Role-based authorization MUST be enforced consistently for project, task, and user data access.
- Local-only deployment and offline operation are required unless the project explicitly introduces a migration path with justification.
- File-based or uploaded artifacts MUST use unique, non-conflicting identifiers to avoid collisions and orphaned records.
- Security controls MUST be documented clearly, including the difference between training patterns and production standards.

## Development Workflow

The project MUST follow a disciplined delivery workflow:

- Requirements and design changes MUST be recorded in the repository and remain traceable to the training objectives.
- Code changes MUST preserve the user-role model, security boundaries, and offline-first constraints.
- Before completion, the relevant build or validation steps MUST run successfully and any changed security or role behavior MUST be checked.
- Documentation MUST be kept current when behavior, access rules, or architecture change.
- Any exception to this constitution MUST be called out with rationale and reviewed before merge.

## Governance

This Constitution supersedes informal practices and local exceptions whenever a project decision conflicts with it. Amendments MUST be documented, justified, and reviewed before becoming binding. Any change affecting security model, authorization logic, data access requirements, or architecture boundaries MUST include a migration or compatibility note.

Compliance review MUST verify that changes preserve the product’s training scope, role-based access expectations, and security-by-practice intent. The project lead or designated reviewer MUST confirm that altered behavior remains consistent with this document before merge or release.

**Version**: 1.0.0 | **Ratified**: 2026-09-10 : confirm the original project adoption date before formal sign-off | **Last Amended**: 2026-09-10
