# Snippet Feature Implementation Summary

## Overview

This document summarizes the design and SQL migration for the **Snippet** feature in the Radium application. Snippets are advanced text expansion templates with placeholder navigation support, enabling radiologists to insert structured report sections with tab-stop navigation between editable regions.

---

## Feature Goals

The snippet feature provides:

1. **Template-based text expansion** with placeholder support for structured data entry
2. **Pre-parsed AST storage** for fast runtime insertion without regex overhead
3. **Tab navigation** between placeholders (Tab forward, Shift+Tab backward)
4. **Choice placeholders** for selection from predefined options
5. **Text placeholders** with default values for free-form input
6. **Account-scoped storage** with management UI in Settings window
7. **Synchronous database ⊥ snapshot ⊥ UI flow** consistent with phrases/hotkeys

---

## Database Schema

### Table: `radium.snippet`

```sql
CREATE TABLE [radium].[snippet](
	[snippet_id] [bigint] IDENTITY(1,1) NOT NULL,
	[account_id] [bigint] NOT NULL,
	[trigger_text] [nvarchar](64) NOT NULL,
	[snippet_text] [nvarchar](4000) NOT NULL,
	[snippet_ast] [nvarchar](max) NOT NULL,
	[description] [nvarchar](256) NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](3) NOT NULL,
	[updated_at] [datetime2](3) NOT NULL,
	[rev] [bigint] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
```

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `snippet_id` | BIGINT IDENTITY | NOT NULL | Primary key |
| `account_id` | BIGINT | NOT NULL | Foreign key to `app.account`, scopes snippet to user |
| `trigger_text` | NVARCHAR(64) | NOT NULL | Trigger keyword to activate snippet (e.g., `abdct`) |
| `snippet_text` | NVARCHAR(4000) | NOT NULL | Template text with placeholder syntax |
| `snippet_ast` | NVARCHAR(MAX) | NOT NULL | Pre-parsed JSON structure of placeholders |
| `description` | NVARCHAR(256) | NULL | Optional human-readable description |
| `is_active` | BIT | NOT NULL | Whether snippet is enabled (default: 1) |
| `created_at` | DATETIME2(3) | NOT NULL | Creation timestamp (UTC) |
| `updated_at` | DATETIME2(3) | NOT NULL | Last update timestamp (UTC) |
| `rev` | BIGINT | NOT NULL | Revision number for optimistic concurrency |

### Constraints & Indexes

```sql
-- Primary key
CONSTRAINT [PK_snippet] PRIMARY KEY CLUSTERED ([snippet_id] ASC)

-- Unique constraint on account_id + trigger_text
CONSTRAINT [UQ_snippet_account_trigger] UNIQUE NONCLUSTERED (
    [account_id] ASC,
    [trigger_text] ASC
)

-- Foreign key to account table (cascade delete)
CONSTRAINT [FK_snippet_account] FOREIGN KEY([account_id])
    REFERENCES [app].[account] ([account_id])
    ON DELETE CASCADE

-- Check constraints
CONSTRAINT [CK_snippet_trigger_not_blank] 
    CHECK ((len(ltrim(rtrim([trigger_text])))>(0)))
CONSTRAINT [CK_snippet_text_not_blank] 
    CHECK ((len(ltrim(rtrim([snippet_text])))>(0)))
CONSTRAINT [CK_snippet_ast_not_blank] 
    CHECK ((len(ltrim(rtrim([snippet_ast])))>(0)))

-- Indexes for query performance
INDEX [IX_snippet_account_active] ([account_id] ASC, [is_active] ASC)
INDEX [IX_snippet_account_rev] ([account_id] ASC, [rev] ASC)
INDEX [IX_snippet_account_trigger_active] ([account_id] ASC, [trigger_text] ASC, [is_active] ASC)
    INCLUDE([snippet_text],[snippet_ast],[description])
```

### Default Constraints

```sql
CONSTRAINT [DF_snippet_active] DEFAULT ((1)) FOR [is_active]
CONSTRAINT [DF_snippet_created] DEFAULT (sysutcdatetime()) FOR [created_at]
CONSTRAINT [DF_snippet_updated] DEFAULT (sysutcdatetime()) FOR [updated_at]
CONSTRAINT [DF_snippet_rev] DEFAULT ((1)) FOR [rev]
```

### Trigger: `trg_snippet_touch`

Auto-updates `updated_at` and increments `rev` when meaningful changes occur:

```sql
CREATE TRIGGER [radium].[trg_snippet_touch] ON [radium].[snippet]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    WITH Changed AS (
        SELECT i.snippet_id
        FROM inserted i
        JOIN deleted d ON i.snippet_id = d.snippet_id
        WHERE (i.is_active <> d.is_active) 
           OR (i.trigger_text <> d.trigger_text)
           OR (i.snippet_text <> d.snippet_text)
           OR (i.snippet_ast <> d.snippet_ast)
           OR (ISNULL(i.description,N'') <> ISNULL(d.description,N''))
    )
    UPDATE s
    SET s.updated_at = SYSUTCDATETIME(),
        s.rev = s.rev + 1
    FROM radium.snippet s
    INNER JOIN Changed c ON s.snippet_id = c.snippet_id;
END;
```

---

## Placeholder Syntax Design

### Template Format

Snippet templates use placeholder syntax similar to VS Code/TextMate snippets:

```
Findings: ${1^modality=ct^CT|mr^MRI|us^US|xr^X-ray}
Technique: ${2:Non-contrast ${1} scan}
Indication: ${3}
```

### Placeholder Types

#### 1. **Choice Placeholder** (Radio/Select)
```
${tabstop^label=key1^option1|key2^option2|key3^option3}
```
- `tabstop`: Navigation order (1, 2, 3, ...)
- `label`: Display label for UI
- `key^option` pairs: Selection choices

**Example**:
```
${1^contrast=y^with contrast|n^without contrast}
```

#### 2. **Text Placeholder** (Free-form input)
```
${tabstop:default text}
```
- `tabstop`: Navigation order
- `default text`: Pre-filled value (can be empty)

**Example**:
```
${2:Enter patient history}
```

#### 3. **Variable Reference**
```
${$tabstop}
```
- References the value of another placeholder
- Enables dynamic text based on previous selections

**Example**:
```
Technique: ${1^modality=ct^CT|mr^MRI}
Findings: ${$1} scan shows...
```

### AST JSON Structure

The `snippet_ast` column stores the parsed structure as JSON:

```json
{
  "placeholders": [
    {
      "tabstop": 1,
      "type": "choice",
      "label": "modality",
      "start": 10,
      "end": 45,
      "options": [
        { "key": "ct", "label": "CT" },
        { "key": "mr", "label": "MRI" },
        { "key": "us", "label": "US" }
      ],
      "default": "ct"
    },
    {
      "tabstop": 2,
      "type": "text",
      "start": 58,
      "end": 75,
      "default": "Non-contrast scan"
    }
  ]
}
```

---

## Service Interface

### `ISnippetService`

```csharp
public interface ISnippetService
{
    // Retrieve active snippets for account (from snapshot cache)
    Task<IReadOnlyList<SnippetInfo>> GetActiveSnippetsAsync(long accountId);
    
    // Get snapshot for completion provider integration
    Task<IReadOnlyList<SnippetInfo>> GetSnippetSnapshotAsync(long accountId);
    
    // Upsert snippet (create or update)
    Task<bool> UpsertSnippetAsync(
        long accountId, 
        string triggerText, 
        string snippetText, 
        string snippetAst, 
        string? description = null
    );
    
    // Toggle active state
    Task<bool> ToggleActiveAsync(long accountId, long snippetId);
    
    // Delete snippet
    Task<bool> DeleteSnippetAsync(long accountId, long snippetId);
    
    // Refresh snapshot from database (for consistency recovery)
    Task RefreshSnippetSnapshotAsync(long accountId);
}
```

### `SnippetInfo` Record

```csharp
public sealed record SnippetInfo(
    long SnippetId,
    long AccountId,
    string TriggerText,
    string SnippetText,
    string SnippetAst,
    string? Description,
    bool IsActive,
    DateTime UpdatedAt,
    long Rev
);
```

---

## Implementation Flow

### 1. **Database ⊥ Snapshot ⊥ UI Pattern**

Following the same pattern as phrases/hotkeys:

```
User Action (Add/Toggle/Delete)
  ⊿
Database UPDATE/INSERT/DELETE
  ⊿
Refresh In-Memory Snapshot (per account)
  ⊿
UI Displays Snapshot State
```

### 2. **Snapshot Caching**

```csharp
private readonly ConcurrentDictionary<long, AccountSnippetState> _snapshots = new();

private sealed class AccountSnippetState
{
    public long AccountId { get; }
    public IReadOnlyList<SnippetInfo> Snippets { get; }
    public DateTime LastRefresh { get; }
}
```

### 3. **Cache Invalidation**

```csharp
public async Task<bool> UpsertSnippetAsync(...)
{
    // 1. Database operation
    var success = await ExecuteUpsertAsync(...);
    
    // 2. Invalidate cache (synchronous flow)
    if (success)
    {
        await RefreshSnippetSnapshotAsync(accountId);
    }
    
    return success;
}
```

---

## Editor Integration

### Trigger Detection

Similar to hotkey detection but with AST parsing:

```csharp
// In EditorControl.OnTextEntered
if (_snippetService != null)
{
    var word = GetCurrentWord();
    var snippet = await _snippetService.GetSnippetByTriggerAsync(accountId, word);
    
    if (snippet != null && IsFollowedBySpace(caretPos))
    {
        ExpandSnippet(snippet);
    }
}
```

### Snippet Expansion

```csharp
private void ExpandSnippet(SnippetInfo snippet)
{
    // 1. Parse AST
    var ast = JsonSerializer.Deserialize<SnippetAst>(snippet.SnippetAst);
    
    // 2. Replace trigger with template
    ReplaceCurrentWord(snippet.SnippetText);
    
    // 3. Initialize placeholder navigation
    _placeholders = ast.Placeholders.OrderBy(p => p.Tabstop).ToList();
    _currentPlaceholderIndex = 0;
    
    // 4. Navigate to first placeholder
    NavigateToPlaceholder(_placeholders[0]);
}
```

### Tab Navigation

```csharp
protected override void OnPreviewKeyDown(KeyEventArgs e)
{
    if (_placeholders != null && _placeholders.Count > 0)
    {
        if (e.Key == Key.Tab && !e.IsShift)
        {
            e.Handled = true;
            NavigateToNextPlaceholder();
        }
        else if (e.Key == Key.Tab && e.IsShift)
        {
            e.Handled = true;
            NavigateToPreviousPlaceholder();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            ClearPlaceholderMode();
        }
    }
    
    base.OnPreviewKeyDown(e);
}
```

---

## Settings UI

### Snippets Tab Layout

```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛 [Add Snippet]                                               弛
弛   Trigger: [________]  Description: [__________________]    弛
弛   Template: [_______________________________________]        弛
弛             [_______________________________________]        弛
弛   [Validate AST]  [Add]                                     弛
戍式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛 [Snippets List]                               [Refresh]     弛
弛 忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖  弛
弛 弛 Id  Trigger  Description      Template    Active Rev  弛  弛
弛 弛 1   abdct    Abdomen CT       ${1^...}     [x]   5    弛  弛
弛 弛 2   chestxr  Chest X-ray      Findings...  [x]   3    弛  弛
弛 弛 3   brain    Brain MRI        ${1:ind...}  [ ]   8    弛  弛
弛 戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎  弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

### ViewModel Commands

```csharp
public class SnippetsViewModel : ViewModelBase
{
    public ObservableCollection<SnippetRow> Snippets { get; }
    
    public ICommand AddSnippetCommand { get; }
    public ICommand DeleteSnippetCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ValidateASTCommand { get; }
    public ICommand ToggleActiveCommand { get; }
    
    // Add form properties
    public string NewTrigger { get; set; }
    public string NewTemplate { get; set; }
    public string NewDescription { get; set; }
    public string ASTPreview { get; set; }
}
```

---

## Migration Script

**File**: `db/migrations/20251010_add_snippet_table.sql`

```sql
-- Migration: Add snippet table to radium schema
-- Date: 2025-10-10
-- Description: Creates the snippet table for text expansion with AST support

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Create snippet table
CREATE TABLE [radium].[snippet](
	[snippet_id] [bigint] IDENTITY(1,1) NOT NULL,
	[account_id] [bigint] NOT NULL,
	[trigger_text] [nvarchar](64) NOT NULL,
	[snippet_text] [nvarchar](4000) NOT NULL,
	[snippet_ast] [nvarchar](max) NOT NULL,
	[description] [nvarchar](256) NULL,
	[is_active] [bit] NOT NULL,
	[created_at] [datetime2](3) NOT NULL,
	[updated_at] [datetime2](3) NOT NULL,
	[rev] [bigint] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

-- (Constraints, indexes, trigger as shown above...)
```

**Rollback**:
```sql
-- Drop trigger first
DROP TRIGGER IF EXISTS [radium].[trg_snippet_touch];
GO

-- Drop table
DROP TABLE IF EXISTS [radium].[snippet];
GO
```

---

## Functional Requirements

| FR ID | Description |
|-------|-------------|
| FR-311 | System MUST support user-defined snippets with placeholder navigation |
| FR-312 | Snippet storage MUST use `radium.snippet` table with specified schema |
| FR-313 | `snippet_text` MUST contain template with placeholder syntax |
| FR-314 | `snippet_ast` MUST store pre-parsed JSON for fast runtime parsing |
| FR-315 | Snippet service MUST provide CRUD async methods with sync flow |
| FR-316 | Snippet service MUST implement in-memory snapshot caching |
| FR-317 | Editor MUST support snippet expansion with tab navigation |
| FR-318 | Settings MUST include Snippets management tab with CRUD UI |

---

## Tasks

| Task | Status | Description |
|------|--------|-------------|
| T442 | ? Pending | Create `radium.snippet` table with migration |
| T443 | ? Pending | Define `ISnippetService` interface |
| T444 | ? Pending | Implement `SnippetService` or `AzureSqlSnippetService` |
| T445 | ? Pending | Implement snippet snapshot caching |
| T446 | ? Pending | Add cache invalidation logic |
| T447 | ? Pending | Design snippet template syntax grammar |
| T448 | ? Pending | Implement snippet AST parser |
| T449 | ? Pending | Integrate snippet expansion in EditorControl |
| T450 | ? Pending | Implement placeholder tab navigation |
| T451 | ? Pending | Add Snippets tab to SettingsWindow |
| T452 | ? Pending | Create SnippetsViewModel |
| T453 | ? Pending | Wire SnippetsViewModel to SettingsWindow |
| T454 | ? Pending | Apply dark theme styling |
| T455 | ? Pending | Register services in DI container |
| T456 | ? Complete | Update Spec/Plan/Tasks documentation |
| T457 | ? Pending | Add AST validation button |
| T458 | ? Pending | Implement placeholder rendering |
| T459 | ? Pending | Add unit tests |
| T460 | ? Pending | Add integration tests |

---

## Example Use Cases

### 1. Abdomen CT Template

**Trigger**: `abdct`

**Template**:
```
TECHNIQUE: ${1^contrast=wc^with contrast|woc^without contrast} CT abdomen
INDICATION: ${2}
FINDINGS:
  Liver: ${3:Normal size and attenuation}
  Spleen: ${4:Unremarkable}
  Kidneys: ${5:Bilateral kidneys normal}
IMPRESSION: ${6}
```

### 2. Chest X-ray Template

**Trigger**: `chestxr`

**Template**:
```
EXAMINATION: ${1^view=pa^PA|ap^AP|lat^Lateral} chest radiograph
COMPARISON: ${2:None}
FINDINGS:
  Lungs: ${3:Clear bilaterally}
  Heart: ${4:Normal cardiac silhouette}
  Bones: ${5:No acute abnormality}
IMPRESSION: ${6}
```

### 3. Brain MRI Template

**Trigger**: `brainmri`

**Template**:
```
TECHNIQUE: ${1^sequence=t1^T1|t2^T2|flair^FLAIR|dwi^DWI} brain MRI ${2^contrast=wc^with|woc^without} contrast
INDICATION: ${3}
FINDINGS: ${4}
IMPRESSION: ${5}
```

---

## Comparison: Hotkeys vs Snippets

| Feature | Hotkeys | Snippets |
|---------|---------|----------|
| **Purpose** | Simple text replacement | Structured templates with placeholders |
| **Expansion** | Immediate full replacement | Interactive with tab navigation |
| **AST** | No | Yes (pre-parsed JSON) |
| **Placeholders** | No | Yes (choice, text, variable reference) |
| **Navigation** | No | Tab/Shift+Tab between stops |
| **Complexity** | Low | High |
| **Use Case** | Common phrases, abbreviations | Report templates, structured forms |
| **Max Length** | 4000 chars (expansion_text) | 4000 chars (snippet_text) |

---

## Next Steps

1. ? **Schema Design** - Complete
2. ? **Migration Script** - Created in `db/migrations/20251010_add_snippet_table.sql`
3. ? **Documentation** - Updated Spec.md, Plan.md, Tasks.md
4. ? **Service Implementation** - Implement `ISnippetService` and `AzureSqlSnippetService`
5. ? **AST Parser** - Build placeholder syntax parser and JSON generator
6. ? **Editor Integration** - Add expansion logic and tab navigation
7. ? **Settings UI** - Build Snippets management tab
8. ? **Testing** - Unit and integration tests

---

## Summary

? **SQL migration created** - `db/migrations/20251010_add_snippet_table.sql`  
? **Schema documented** - Full table structure with constraints and indexes  
? **Placeholder syntax designed** - Choice, text, and variable reference types  
? **AST structure defined** - JSON format for fast runtime parsing  
? **Service interface specified** - CRUD methods with snapshot caching  
? **Documentation updated** - Spec.md, Plan.md, Tasks.md  
? **Build successful** - No compilation errors  

The snippet feature SQL foundation is complete and ready for implementation!
