# Complete Central Database API Design

## Overview

This document outlines the complete API coverage for all `radium.*` and `snomed.*` tables in the central Azure SQL database, following security best practices.

---

## API Endpoint Structure

### Base URL Pattern
```
https://api.example.com/api/accounts/{accountId}/{resource}
```

### Authentication
All endpoints require Firebase JWT authentication in the `Authorization` header:
```
Authorization: Bearer {firebaseToken}
```

---

## 1. ? User Settings API (COMPLETED)

**Table**: `radium.user_setting`

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/accounts/{accountId}/settings` | Get user settings |
| PUT | `/api/accounts/{accountId}/settings` | Create/Update settings |
| DELETE | `/api/accounts/{accountId}/settings` | Delete settings |

**Status**: ? Fully implemented

---

## 2. ? Hotkeys API (COMPLETED)

**Table**: `radium.hotkey`

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/accounts/{accountId}/hotkeys` | Get all hotkeys |
| GET | `/api/accounts/{accountId}/hotkeys/{id}` | Get specific hotkey |
| PUT | `/api/accounts/{accountId}/hotkeys` | Create/Update hotkey |
| POST | `/api/accounts/{accountId}/hotkeys/{id}/toggle` | Toggle active status |
| DELETE | `/api/accounts/{accountId}/hotkeys/{id}` | Delete hotkey |

**Status**: ? Fully implemented

---

## 3. ? Snippets API (COMPLETED)

**Table**: `radium.snippet`

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/accounts/{accountId}/snippets` | Get all snippets |
| GET | `/api/accounts/{accountId}/snippets/{id}` | Get specific snippet |
| PUT | `/api/accounts/{accountId}/snippets` | Create/Update snippet |
| POST | `/api/accounts/{accountId}/snippets/{id}/toggle` | Toggle active status |
| DELETE | `/api/accounts/{accountId}/snippets/{id}` | Delete snippet |

**Status**: ? Fully implemented (assumed based on pattern)

---

## 4. ?? Phrases API (TODO - HIGH PRIORITY)

**Table**: `radium.phrase`

### Account-Specific Phrases

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/accounts/{accountId}/phrases` | Get all phrases for account |
| GET | `/api/accounts/{accountId}/phrases/{id}` | Get specific phrase |
| POST | `/api/accounts/{accountId}/phrases` | Create new phrase |
| PUT | `/api/accounts/{accountId}/phrases/{id}` | Update phrase |
| POST | `/api/accounts/{accountId}/phrases/{id}/toggle` | Toggle active status |
| DELETE | `/api/accounts/{accountId}/phrases/{id}` | Delete phrase |
| GET | `/api/accounts/{accountId}/phrases/sync?minRev={rev}` | Get changes since revision |

### Global Phrases

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/phrases/global` | Get all global phrases |
| GET | `/api/phrases/global/{id}` | Get specific global phrase |
| POST | `/api/phrases/global` | Create new global phrase (admin only) |
| PUT | `/api/phrases/global/{id}` | Update global phrase (admin only) |
| DELETE | `/api/phrases/global/{id}` | Delete global phrase (admin only) |

### Combined View

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/accounts/{accountId}/phrases/combined` | Get account + global phrases |

**Features**:
- Support for tags (JSON field)
- Computed columns: `tags_source`, `tags_semantic_tag`
- Revision tracking for sync
- Active/inactive filtering
- Search by text (fuzzy matching)

**DTO Structure**:
```csharp
public class PhraseDto
{
    public long Id { get; set; }
    public long? AccountId { get; set; }
    public string Text { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long Rev { get; set; }
    public string? Tags { get; set; }
    public string? TagsSource { get; set; }
    public string? TagsSemanticTag { get; set; }
}
```

---

## 5. ?? SNOMED Concept Cache API (TODO - HIGH PRIORITY)

**Table**: `snomed.concept_cache`

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/snomed/concepts/{conceptId}` | Get concept by ID |
| GET | `/api/snomed/concepts/search?query={text}` | Search concepts by FSN/PT |
| POST | `/api/snomed/concepts` | Cache a concept (admin only) |
| POST | `/api/snomed/concepts/batch` | Bulk cache concepts |
| GET | `/api/snomed/concepts/semantic-tags` | Get all semantic tags |

**Features**:
- Full-text search on FSN and PT
- Filter by semantic tag
- Filter by active status
- Pagination support
- Expiration handling

**DTO Structure**:
```csharp
public class SnomedConceptDto
{
    public long ConceptId { get; set; }
    public string ConceptIdStr { get; set; }
    public string Fsn { get; set; }
    public string? Pt { get; set; }
    public string? SemanticTag { get; set; }
    public string? ModuleId { get; set; }
    public bool Active { get; set; }
    public DateTime CachedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
```

---

## 6. ?? Phrase-SNOMED Mapping API (TODO - HIGH PRIORITY)

**Tables**: `radium.phrase_snomed`, `radium.global_phrase_snomed`

### Account-Specific Mappings

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/accounts/{accountId}/phrase-mappings` | Get all mappings |
| GET | `/api/accounts/{accountId}/phrase-mappings/{phraseId}` | Get mapping for phrase |
| PUT | `/api/accounts/{accountId}/phrase-mappings` | Create/Update mapping |
| DELETE | `/api/accounts/{accountId}/phrase-mappings/{phraseId}` | Delete mapping |

### Global Mappings

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/phrase-mappings/global` | Get all global mappings |
| GET | `/api/phrase-mappings/global/{phraseId}` | Get mapping for phrase |
| PUT | `/api/phrase-mappings/global` | Create/Update mapping (admin) |
| DELETE | `/api/phrase-mappings/global/{phraseId}` | Delete mapping (admin) |

### Combined View

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/accounts/{accountId}/phrase-mappings/combined` | Get all mappings (account + global) |

**Features**:
- Mapping types: exact, broader, narrower, related
- Confidence scores (0.00 - 1.00)
- Notes field for mapping context
- `mapped_by` tracking for global mappings

**DTO Structure**:
```csharp
public class PhraseSnomedMappingDto
{
    public long Id { get; set; }
    public long PhraseId { get; set; }
    public string PhraseText { get; set; } // Joined
    public long ConceptId { get; set; }
    public string ConceptIdStr { get; set; } // Joined
    public string Fsn { get; set; } // Joined
    public string? Pt { get; set; } // Joined
    public string? SemanticTag { get; set; } // Joined
    public string MappingType { get; set; }
    public decimal? Confidence { get; set; }
    public string? Notes { get; set; }
    public long? MappedBy { get; set; } // Global only
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string MappingSource { get; set; } // "account" or "global"
}
```

---

## 7. ?? Exported Reports API (TODO - MEDIUM PRIORITY)

**Table**: `radium.exported_report`

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/accounts/{accountId}/exported-reports` | Get all reports |
| GET | `/api/accounts/{accountId}/exported-reports/{id}` | Get specific report |
| POST | `/api/accounts/{accountId}/exported-reports` | Upload new report |
| POST | `/api/accounts/{accountId}/exported-reports/{id}/resolve` | Mark as resolved |
| DELETE | `/api/accounts/{accountId}/exported-reports/{id}` | Delete report |
| GET | `/api/accounts/{accountId}/exported-reports/stats` | Get statistics |

**Features**:
- Filter by date range
- Filter by resolved status
- Pagination
- Full-text search in report content

**DTO Structure**:
```csharp
public class ExportedReportDto
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public string Report { get; set; }
    public DateTime ReportDateTime { get; set; }
    public DateTime UploadedAt { get; set; }
    public bool IsResolved { get; set; }
}
```

---

## 8. ?? Account API (READ-ONLY - LOW PRIORITY)

**Table**: `app.account`

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/accounts/me` | Get current user's account |
| GET | `/api/accounts/{accountId}` | Get account by ID (admin only) |
| PUT | `/api/accounts/me/profile` | Update profile (display_name) |

**Note**: Account creation/deletion should be handled by Firebase Authentication flow.

---

## Stored Procedures API

### SNOMED Operations

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/snomed/operations/upsert-concept` | Call `snomed.upsert_concept` |
| POST | `/api/snomed/operations/import-phrases` | Call `radium.import_snomed_phrases` |

### Phrase Mapping Operations

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/operations/map-phrase` | Call `radium.map_phrase_to_snomed` |
| POST | `/api/operations/map-global-phrase` | Call `radium.map_global_phrase_to_snomed` |

---

## Common Query Parameters

### Pagination
```
?page=1&pageSize=50
```

### Filtering
```
?active=true
?startDate=2025-01-01&endDate=2025-01-31
```

### Sorting
```
?sortBy=updated_at&sortOrder=desc
```

### Sync Support
```
?minRev=123  // Get all records with rev > 123
```

---

## Response Format

### Success Response
```json
{
  "data": { ... } or [ ... ],
  "success": true
}
```

### Paginated Response
```json
{
  "data": [ ... ],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalCount": 500,
    "totalPages": 10
  },
  "success": true
}
```

### Error Response
```json
{
  "error": "Error message",
  "code": "ERROR_CODE",
  "success": false
}
```

---

## Security Considerations

### Authorization Levels

1. **User** (default)
   - Access own account resources only
   - Read global resources
   - Cannot modify global resources

2. **Admin** (elevated)
   - Full CRUD on global resources
   - Access to any account (with audit)
   - Can execute stored procedures

### Implementation
```csharp
[Authorize(Policy = "AdminOnly")]
public async Task<IActionResult> CreateGlobalPhrase(...)
```

---

## Database Transaction Patterns

### Read Operations
- Use `CommandTimeout = 30` for normal queries
- Use `CommandTimeout = 60` for search/aggregation

### Write Operations
- Wrap in transactions for multi-table updates
- Use optimistic concurrency with `rev` field
- Return updated entity with new `rev`

### Bulk Operations
- Use `SqlBulkCopy` for large imports
- Batch size: 1000 records
- Use async methods

---

## Implementation Priority

### Phase 1 (Week 1) - Critical
- ? User Settings API
- ? Hotkeys API  
- ? Snippets API
- ?? **Phrases API** (account + global)

### Phase 2 (Week 2) - High Priority
- ?? **SNOMED Concept Cache API**
- ?? **Phrase-SNOMED Mapping API**

### Phase 3 (Week 3) - Medium Priority
- ?? **Exported Reports API**
- Account profile management

### Phase 4 (Week 4) - Polish
- Admin operations
- Statistics endpoints
- Stored procedure wrappers

---

## WPF Client Migration

### Before (Direct Database)
```csharp
// Direct SQL query - INSECURE
var phrases = await _phraseService.GetPhrasesAsync(accountId);
```

### After (API Client)
```csharp
// API call with authentication - SECURE
var phrases = await _apiClient.GetPhrasesAsync(accountId, firebaseToken);
```

### Client Architecture
```
WPF Application
  戍式式 Services/
  弛   戍式式 IAuthService.cs (Firebase token management)
  弛   戌式式 Adapters/
  弛       戍式式 ApiClientBase.cs (HTTP client wrapper)
  弛       戍式式 PhrasesApiClient.cs
  弛       戍式式 HotkeysApiClient.cs
  弛       戍式式 SnippetsApiClient.cs
  弛       戍式式 UserSettingsApiClient.cs
  弛       戍式式 SnomedApiClient.cs
  弛       戌式式 ExportedReportsApiClient.cs
```

---

## Testing Strategy

### Unit Tests
- Repository layer tests (mock SQL)
- Service layer tests (mock repository)

### Integration Tests
- Controller tests (mock service)
- End-to-end API tests

### Load Tests
- Phrase sync with 10,000+ records
- Concurrent user scenarios
- SNOMED search performance

---

## Monitoring & Logging

### Application Insights
- Request/response times
- Failure rates
- Dependency tracking

### Custom Metrics
- Phrases synced per minute
- SNOMED search latency
- Authentication failures

### Alerts
- API response time > 1s
- Error rate > 5%
- Database connection pool exhaustion

---

## Next Steps

1. Review and approve this design
2. Create GitHub issues for each API group
3. Implement Phase 1 (Phrases API)
4. Update WPF client to use Phrases API
5. Repeat for remaining phases

---

**Document Version**: 1.0  
**Last Updated**: 2025-01-22  
**Status**: ?? Design Complete - Awaiting Implementation
