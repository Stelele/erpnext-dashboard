# User-Company Association Design

**Date:** 2026-06-06
**Status:** Approved

## Overview

Add RESTful endpoints to manage the many-to-many relationship between Users and Companies.

## Endpoints

### Add User to Company
- **Route:** `POST /users/{userId}/companies/{companyId}`
- **Auth:** `update:users`
- **Response:** `204 No Content` on success

### Remove User from Company
- **Route:** `DELETE /users/{userId}/companies/{companyId}`
- **Auth:** `update:users`
- **Response:** `204 No Content` on success

## Error Responses

| Status | Condition |
|--------|-----------|
| 404 | User not found |
| 404 | Company not found |
| 409 | User already linked to company (POST only) |
| 404 | Link not found (DELETE only) |

## Components

### Application Layer
- `AddUserToCompanyCommand` - record with UserId and CompanyId
- `AddUserToCompanyCommandHandler` - validates existence, checks for duplicates, adds to junction table
- `RemoveUserFromCompanyCommand` - record with UserId and CompanyId
- `RemoveUserFromCompanyCommandHandler` - validates link exists, removes from junction table

### Endpoint Layer
- Two new routes added to `UsersEndpoints.cs` following existing MediatR pattern

### Permissions
- Uses existing `update:users` permission (consistent with CreateUser endpoint)

## Data Flow

1. Request received with userId and companyId in path
2. MediatR command dispatched
3. Handler queries DB for user and company
4. If either missing → 404
5. For POST: check junction table for existing link → 409 if exists
6. For DELETE: check junction table for link → 404 if not exists
7. Perform add/remove operation
8. Save changes
9. Return 204

## Testing

- Unit tests for handlers covering success and all error paths
- Integration tests for endpoints verifying HTTP status codes and auth
