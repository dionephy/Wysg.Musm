# Comprehensive API Implementation Plan

## ?? Executive Summary

Based on the complete database schema analysis, here's what needs to be done:

### Current Status
- ? **User Settings API** - Fully implemented
- ? **Hotkeys API** - Fully implemented  
- ? **Snippets API** - Fully implemented
- ?? **Phrases API** - Partially implemented (needs enhancement for global phrases & tags)

### Missing (Critical for Security)
- ?? **Global Phrases API** - Not implemented
- ?? **SNOMED Concept Cache API** - Not implemented
- ?? **Phrase-SNOMED Mapping API** - Not implemented
- ?? **Exported Reports API** - Not implemented

---

## ?? Complete Table Coverage

| Table | Purpose | Current Status | Priority |
|-------|---------|----------------|----------|
| `app.account` | User accounts | Via Firebase Auth | ? Complete |
| `radium.user_setting` | User settings (JSON) | API Complete | ? Complete |
| `radium.hotkey` | Text expansion hotkeys | API Complete | ? Complete |
| `radium.snippet` | Template snippets | API Complete | ? Complete |
| `radium.phrase` | Medical phrases | Partial (account only) | ?? Needs Enhancement |
| `radium.phrase_snomed` | Account phrase mappings | Missing | ?? High Priority |
| `radium.global_phrase_snomed` | Global phrase mappings | Missing | ?? High Priority |
| `snomed.concept_cache` | SNOMED concepts | Missing | ?? High Priority |
| `radium.exported_report` | Report tracking | Missing | ?? Medium Priority |

---

## ?? Required Enhancements

### 1. Phrases API Enhancement

**Current Implementation Issues**:
- ? Only handles account-specific phrases (`account_id NOT NULL`)
- ? Doesn't support global phrases (`account_id IS NULL`)
- ? Missing `tags`, `tags_source`, `tags_semantic_tag` fields
- ? No combined endpoint for account + global phrases
- ? No sync endpoint for revision-based updates

**Required Changes**:

#### A. Update `PhraseDto.cs`
```csharp
public sealed class PhraseDto
{
    public long Id { get; set; }
    public long? AccountId { get; set; } // ? Changed to nullable
    public string Text { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long Rev { get; set; }
    public string? Tags { get; set; } // ? Added
    public string? TagsSource { get; set; } // ? Added (computed)
    public string? TagsSemanticTag { get; set; } // ? Added (computed)
}
```

#### B. Update `PhraseRepository.cs`

**Add Methods**:
```csharp
// Global phrases (read-only for regular users)
Task<List<PhraseDto>> GetAllGlobalAsync(bool activeOnly = false);
Task<PhraseDto?> GetGlobalByIdAsync(long phraseId);
Task<List<PhraseDto>> SearchGlobalAsync(string? query, bool activeOnly, int maxResults);

// Combined (account + global)
Task<List<PhraseDto>> GetCombinedAsync(long accountId, bool activeOnly = false);

// Sync support
Task<List<PhraseDto>> GetChangesSinceRevisionAsync(long accountId, long minRev);
Task<List<PhraseDto>> GetGlobalChangesSinceRevisionAsync(long minRev);

// Admin only - global phrase management
Task<PhraseDto> CreateGlobalPhraseAsync(string text, bool active, string? tags);
Task<PhraseDto> UpdateGlobalPhraseAsync(long phraseId, string text, bool active, string? tags);
Task<bool> DeleteGlobalPhraseAsync(long phraseId);
```

**Update SQL queries** to include:
```sql
SELECT id, account_id, text, active, created_at, updated_at, rev,
       tags, tags_source, tags_semantic_tag
FROM radium.phrase
```

#### C. Create `PhrasesController.cs` Endpoints

```csharp
// Account-specific endpoints
[HttpGet("/api/accounts/{accountId}/phrases")]
Task<ActionResult<List<PhraseDto>>> GetAccountPhrases(long accountId, [FromQuery] bool activeOnly = false);

[HttpGet("/api/accounts/{accountId}/phrases/{id}")]
Task<ActionResult<PhraseDto>> GetAccountPhrase(long accountId, long id);

[HttpPost("/api/accounts/{accountId}/phrases")]
Task<ActionResult<PhraseDto>> CreateAccountPhrase(long accountId, [FromBody] UpsertPhraseRequest request);

[HttpPut("/api/accounts/{accountId}/phrases/{id}")]
Task<ActionResult<PhraseDto>> UpdateAccountPhrase(long accountId, long id, [FromBody] UpsertPhraseRequest request);

[HttpPost("/api/accounts/{accountId}/phrases/{id}/toggle")]
Task<ActionResult<PhraseDto>> ToggleAccountPhrase(long accountId, long id);

[HttpDelete("/api/accounts/{accountId}/phrases/{id}")]
Task<IActionResult> DeleteAccountPhrase(long accountId, long id);

// Combined endpoint (account + global)
[HttpGet("/api/accounts/{accountId}/phrases/combined")]
Task<ActionResult<List<PhraseDto>>> GetCombinedPhrases(long accountId, [FromQuery] bool activeOnly = false);

// Sync endpoint (efficient updates)
[HttpGet("/api/accounts/{accountId}/phrases/sync")]
Task<ActionResult<PhraseSyncResponse>> SyncPhrases(long accountId, [FromQuery] long minRev);

// Global phrases (read-only for users)
[HttpGet("/api/phrases/global")]
Task<ActionResult<List<PhraseDto>>> GetGlobalPhrases([FromQuery] bool activeOnly = false);

[HttpGet("/api/phrases/global/{id}")]
Task<ActionResult<PhraseDto>> GetGlobalPhrase(long id);

[HttpGet("/api/phrases/global/search")]
Task<ActionResult<List<PhraseDto>>> SearchGlobalPhrases([FromQuery] SearchPhrasesRequest request);

// Admin endpoints for global phrases
[HttpPost("/api/phrases/global")]
[Authorize(Policy = "AdminOnly")]
Task<ActionResult<PhraseDto>> CreateGlobalPhrase([FromBody] UpsertPhraseRequest request);

[HttpPut("/api/phrases/global/{id}")]
[Authorize(Policy = "AdminOnly")]
Task<ActionResult<PhraseDto>> UpdateGlobalPhrase(long id, [FromBody] UpsertPhraseRequest request);

[HttpDelete("/api/phrases/global/{id}")]
[Authorize(Policy = "AdminOnly")]
Task<IActionResult> DeleteGlobalPhrase(long id);
```

---

### 2. SNOMED Concept Cache API (NEW)

**Files to Create**:

#### `Models/Dtos/SnomedConceptDto.cs`
```csharp
public sealed class SnomedConceptDto
{
    public long ConceptId { get; set; }
    public string ConceptIdStr { get; set; } = string.Empty;
    public string Fsn { get; set; } = string.Empty;
    public string? Pt { get; set; }
    public string? SemanticTag { get; set; }
    public string? ModuleId { get; set; }
    public bool Active { get; set; }
    public DateTime CachedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public sealed class SearchSnomedRequest
{
    public string? Query { get; set; }
    public string? SemanticTag { get; set; }
    public bool? ActiveOnly { get; set; } = true;
    public int MaxResults { get; set; } = 50;
}

public sealed class UpsertSnomedConceptRequest
{
    public required long ConceptId { get; set; }
    public required string ConceptIdStr { get; set; }
    public required string Fsn { get; set; }
    public string? Pt { get; set; }
    public string? SemanticTag { get; set; }
    public string? ModuleId { get; set; }
    public bool Active { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
}
```

#### `Repositories/ISnomedRepository.cs`
```csharp
public interface ISnomedRepository
{
    Task<SnomedConceptDto?> GetByIdAsync(long conceptId);
    Task<SnomedConceptDto?> GetByConceptIdStrAsync(string conceptIdStr);
    Task<List<SnomedConceptDto>> SearchAsync(string? query, string? semanticTag, bool activeOnly, int maxResults);
    Task<List<string>> GetSemanticTagsAsync();
    Task<SnomedConceptDto> UpsertAsync(UpsertSnomedConceptRequest request);
    Task<int> BatchUpsertAsync(List<UpsertSnomedConceptRequest> concepts);
}
```

#### `Repositories/SnomedRepository.cs`
- Implement SQL queries for concept cache
- Use full-text search on FSN and PT
- Support semantic tag filtering

#### `Controllers/SnomedController.cs`
```csharp
[HttpGet("/api/snomed/concepts/{conceptId}")]
Task<ActionResult<SnomedConceptDto>> GetConcept(long conceptId);

[HttpGet("/api/snomed/concepts/search")]
Task<ActionResult<List<SnomedConceptDto>>> SearchConcepts([FromQuery] SearchSnomedRequest request);

[HttpGet("/api/snomed/semantic-tags")]
Task<ActionResult<List<string>>> GetSemanticTags();

[HttpPost("/api/snomed/concepts")]
[Authorize(Policy = "AdminOnly")]
Task<ActionResult<SnomedConceptDto>> UpsertConcept([FromBody] UpsertSnomedConceptRequest request);

[HttpPost("/api/snomed/concepts/batch")]
[Authorize(Policy = "AdminOnly")]
Task<ActionResult<int>> BatchUpsertConcepts([FromBody] List<UpsertSnomedConceptRequest> requests);
```

---

### 3. Phrase-SNOMED Mapping API (NEW)

**Files to Create**:

#### `Models/Dtos/PhraseSnomedMappingDto.cs`
```csharp
public sealed class PhraseSnomedMappingDto
{
    public long Id { get; set; }
    public long PhraseId { get; set; }
    public string PhraseText { get; set; } = string.Empty;
    public long ConceptId { get; set; }
    public string ConceptIdStr { get; set; } = string.Empty;
    public string Fsn { get; set; } = string.Empty;
    public string? Pt { get; set; }
    public string? SemanticTag { get; set; }
    public string MappingType { get; set; } = "exact"; // exact, broader, narrower, related
    public decimal? Confidence { get; set; }
    public string? Notes { get; set; }
    public long? MappedBy { get; set; } // For global mappings only
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string MappingSource { get; set; } = "account"; // "account" or "global"
}

public sealed class CreateMappingRequest
{
    public required long PhraseId { get; set; }
    public required long ConceptId { get; set; }
    public string MappingType { get; set; } = "exact";
    public decimal? Confidence { get; set; }
    public string? Notes { get; set; }
}
```

#### `Repositories/IPhraseSnomedMappingRepository.cs`
```csharp
public interface IPhraseSnomedMappingRepository
{
    // Account mappings
    Task<List<PhraseSnomedMappingDto>> GetAllByAccountAsync(long accountId);
    Task<PhraseSnomedMappingDto?> GetByPhraseIdAsync(long phraseId, long accountId);
    Task<PhraseSnomedMappingDto> UpsertAsync(long accountId, CreateMappingRequest request);
    Task<bool> DeleteAsync(long phraseId, long accountId);
    
    // Global mappings
    Task<List<PhraseSnomedMappingDto>> GetAllGlobalAsync();
    Task<PhraseSnomedMappingDto?> GetGlobalByPhraseIdAsync(long phraseId);
    Task<PhraseSnomedMappingDto> UpsertGlobalAsync(CreateMappingRequest request, long mappedBy);
    Task<bool> DeleteGlobalAsync(long phraseId);
    
    // Combined view (use v_phrase_snomed_combined)
    Task<List<PhraseSnomedMappingDto>> GetCombinedAsync(long accountId);
}
```

#### `Controllers/PhraseSnomedMappingsController.cs`
```csharp
// Account mappings
[HttpGet("/api/accounts/{accountId}/phrase-mappings")]
Task<ActionResult<List<PhraseSnomedMappingDto>>> GetAccountMappings(long accountId);

[HttpGet("/api/accounts/{accountId}/phrase-mappings/{phraseId}")]
Task<ActionResult<PhraseSnomedMappingDto>> GetAccountMapping(long accountId, long phraseId);

[HttpPut("/api/accounts/{accountId}/phrase-mappings")]
Task<ActionResult<PhraseSnomedMappingDto>> UpsertAccountMapping(long accountId, [FromBody] CreateMappingRequest request);

[HttpDelete("/api/accounts/{accountId}/phrase-mappings/{phraseId}")]
Task<IActionResult> DeleteAccountMapping(long accountId, long phraseId);

// Combined view
[HttpGet("/api/accounts/{accountId}/phrase-mappings/combined")]
Task<ActionResult<List<PhraseSnomedMappingDto>>> GetCombinedMappings(long accountId);

// Global mappings (admin only)
[HttpGet("/api/phrase-mappings/global")]
[Authorize(Policy = "AdminOnly")]
Task<ActionResult<List<PhraseSnomedMappingDto>>> GetGlobalMappings();

[HttpPut("/api/phrase-mappings/global")]
[Authorize(Policy = "AdminOnly")]
Task<ActionResult<PhraseSnomedMappingDto>> UpsertGlobalMapping([FromBody] CreateMappingRequest request);

[HttpDelete("/api/phrase-mappings/global/{phraseId}")]
[Authorize(Policy = "AdminOnly")]
Task<IActionResult> DeleteGlobalMapping(long phraseId);
```

---

### 4. Exported Reports API (NEW)

**Files to Create**:

#### `Models/Dtos/ExportedReportDto.cs`
```csharp
public sealed class ExportedReportDto
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public string Report { get; set; } = string.Empty;
    public DateTime ReportDateTime { get; set; }
    public DateTime UploadedAt { get; set; }
    public bool IsResolved { get; set; }
}

public sealed class CreateExportedReportRequest
{
    public required string Report { get; set; }
    public required DateTime ReportDateTime { get; set; }
}
```

#### `Repositories/IExportedReportRepository.cs`
```csharp
public interface IExportedReportRepository
{
    Task<List<ExportedReportDto>> GetAllByAccountAsync(long accountId, bool unresolvedOnly = false);
    Task<ExportedReportDto?> GetByIdAsync(long id, long accountId);
    Task<ExportedReportDto> CreateAsync(long accountId, CreateExportedReportRequest request);
    Task<bool> MarkResolvedAsync(long id, long accountId);
    Task<bool> DeleteAsync(long id, long accountId);
    Task<Dictionary<string, int>> GetStatsAsync(long accountId);
}
```

#### `Controllers/ExportedReportsController.cs`
```csharp
[HttpGet("/api/accounts/{accountId}/exported-reports")]
Task<ActionResult<List<ExportedReportDto>>> GetReports(long accountId, [FromQuery] bool unresolvedOnly = false);

[HttpGet("/api/accounts/{accountId}/exported-reports/{id}")]
Task<ActionResult<ExportedReportDto>> GetReport(long accountId, long id);

[HttpPost("/api/accounts/{accountId}/exported-reports")]
Task<ActionResult<ExportedReportDto>> CreateReport(long accountId, [FromBody] CreateExportedReportRequest request);

[HttpPost("/api/accounts/{accountId}/exported-reports/{id}/resolve")]
Task<IActionResult> ResolveReport(long accountId, long id);

[HttpDelete("/api/accounts/{accountId}/exported-reports/{id}")]
Task<IActionResult> DeleteReport(long accountId, long id);

[HttpGet("/api/accounts/{accountId}/exported-reports/stats")]
Task<ActionResult<Dictionary<string, int>>> GetStats(long accountId);
```

---

## ?? Security Implementation

### Admin Policy
Add to `Program.cs`:
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("admin", "true"); // Or check Firebase custom claims
    });
});
```

### Account Ownership Verification
All controllers already implement this pattern:
```csharp
var userAccountId = User.GetAccountId();
if (userAccountId.HasValue && userAccountId.Value != accountId)
{
    return Forbid();
}
```

---

## ?? Implementation Timeline

### Week 1 (CRITICAL)
- ? Day 1-2: Enhance Phrases API (global + tags support)
- ? Day 3-4: Implement SNOMED Concept Cache API
- ? Day 5: Testing & documentation

### Week 2 (HIGH PRIORITY)
- ? Day 1-3: Implement Phrase-SNOMED Mapping API
- ? Day 4-5: Implement Exported Reports API

### Week 3 (INTEGRATION)
- ? Update WPF client to use all APIs
- ? Remove direct database access from desktop client
- ? Testing & bug fixes

### Week 4 (POLISH)
- ? Performance optimization
- ? Admin endpoints
- ? Monitoring & logging
- ? Load testing

---

## ?? Testing Strategy

### Unit Tests
```csharp
// Example for Phrases API
[Fact]
public async Task GetCombinedPhrases_ReturnsAccountAndGlobalPhrases()
{
    // Arrange
    var accountId = 1L;
    var mockRepo = new Mock<IPhraseRepository>();
    mockRepo.Setup(r => r.GetCombinedAsync(accountId, false))
        .ReturnsAsync(new List<PhraseDto> { /* test data */ });
    
    // Act
    var result = await _controller.GetCombinedPhrases(accountId);
    
    // Assert
    Assert.NotNull(result.Value);
    Assert.Contains(result.Value, p => p.AccountId == accountId);
    Assert.Contains(result.Value, p => p.AccountId == null); // Global phrase
}
```

### Integration Tests
- Test complete phrase sync workflow
- Test SNOMED search performance
- Test concurrent user access

### Load Tests
- 1000 phrases sync
- 100 concurrent users
- SNOMED search with 100k cached concepts

---

## ?? WPF Client Migration

### Create API Client Wrappers
```
Wysg.Musm.Radium/Services/ApiClients/
戍式式 ApiClientBase.cs (HTTP client + auth)
戍式式 PhrasesApiClient.cs
戍式式 SnomedApiClient.cs
戍式式 PhraseMappingsApiClient.cs
戍式式 ExportedReportsApiClient.cs
戍式式 UserSettingsApiClient.cs (? done)
戍式式 HotkeysApiClient.cs (? done)
戌式式 SnippetsApiClient.cs (? done)
```

### Update DI Registration
```csharp
// Remove direct DB services
// services.AddSingleton<IReportifySettingsService, ReportifySettingsService>(); ?

// Add API clients
services.AddHttpClient<IPhrasesApiClient, PhrasesApiClient>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl));
services.AddHttpClient<ISnomedApiClient, SnomedApiClient>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl));
// etc.
```

---

## ?? Success Criteria

### Phase 1 Complete When:
- ? All API endpoints implemented
- ? Unit tests pass (>80% coverage)
- ? Integration tests pass
- ? Swagger documentation complete
- ? WPF client uses API (no direct DB)

### Production Ready When:
- ? Load tests pass (1000 concurrent users)
- ? Security audit complete
- ? Monitoring & alerts configured
- ? Documentation complete
- ? Deployment guide ready

---

**Status**: ?? Plan Complete - Ready for Implementation  
**Next Step**: Begin Week 1 implementation (Phrases API enhancement)  
**Priority**: ?? CRITICAL - Required for security & scalability

