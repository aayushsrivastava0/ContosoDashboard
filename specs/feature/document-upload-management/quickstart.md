# Quickstart Validation Guide

## Prerequisites

- .NET 8 SDK installed
- SQL Server LocalDB available on the developer machine
- A cloned ContosoDashboard repo on the feature branch

## Setup

1. Open a terminal in the repo root.
2. Restore packages: `dotnet restore`
3. Run the app: `dotnet run --project ContosoDashboard/ContosoDashboard.csproj`
4. Open the application in the browser and log in using a seeded mock user.

## Validation Scenarios

### 1. Upload a valid document
- Log in as an employee or manager.
- Navigate to the document upload area.
- Select a PDF or Office document under 25 MB.
- Enter a title, category, and optional project/tags.
- Submit upload.
- Expected result: success message appears and document appears in the user’s document list.

### 2. Reject invalid input
- Attempt to upload a file over 25 MB or with an unsupported extension.
- Expected result: validation message appears, no document record is created.

### 3. Process the virus-scan workflow locally
- Upload a valid document while running the app locally.
- Expected result: the document is initially pending scan and is not available for download or sharing.
- Run or trigger the local mock scan worker using the implementation's local queue path.
- Expected result: a clean scan makes the document available; a quarantined or failed scan keeps it blocked and records an audit event and notification. No Azure keys or cloud services are required.

### 4. Access control enforcement
- Upload a project document as one user.
- Log out and log in as a different authorized or unauthorized user.
- Expected result: authorized users can see the document; unauthorized users cannot access or download it.

### 5. Search and filter
- Upload several documents with different categories, projects, and tags.
- Search by title, tag, uploader, or project.
- Expected result: only accessible documents appear and results return within 2 seconds.

### 6. Share and notify
- Share a document with another user.
- Expected result: recipient receives an in-app notification and sees the document in the shared view.

### 7. Dashboard integration
- Navigate to the dashboard.
- Expected result: recent documents widget appears and document count reflects the user’s scope.

## Expected Outcomes

- Upload succeeds for valid documents
- Security checks reject invalid files and unauthorized access
- Search and filtering reflect only authorized documents
- Notification and dashboard flows update as expected

This quickstart is a validation guide for the feature and not a full implementation specification.
