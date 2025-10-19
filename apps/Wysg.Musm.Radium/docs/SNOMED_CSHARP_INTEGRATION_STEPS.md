# ?? SNOMED C# Integration - Remaining Steps

**Status:** ? Database migration complete  
**Progress:** 3/10 code updates complete

---

## ? Completed Steps

1. **PhraseRow** - Added SNOMED columns (`Tags`, `TagsSource`, `TagsSemanticTag`)
2. **PhraseInfo** - Updated record with optional SNOMED properties
3. **Load methods** - Updated `LoadPageAsync`, `LoadPageOnConnectionAsync`, `LoadSmallSetAsync` to SELECT SNOMED columns

---

## ?? Remaining SQL Query Updates

### **File: `PhraseService.cs`**

Update these methods to include SNOMED columns (columns 7, 8, 9):

#### **4. `GetAllNonGlobalPhraseMetaAsync`** (Line ~220)
```csharp
// Change FROM:
const string sql = @"SELECT id, account_id, text, active, created_at, updated_at, rev
                       FROM radium.phrase...";

// Change TO:
const string sql = @"SELECT id, account_id, text, active, created_at, updated_at, rev, tags, tags_source, tags_semantic_tag
                       FROM radium.phrase...";

// Update reader:
list.Add(new PhraseInfo(
    rd.GetInt64(0),
    rd.GetInt64(1),
    rd.GetString(2),
    rd.GetBoolean(3),
    rd.GetDateTime(5),
    rd.GetInt64(6),
    rd.IsDBNull(7) ? null : rd.GetString(7),
    rd.IsDBNull(8) ? null : rd.GetString(8),
    rd.IsDBNull(9) ? null : rd.GetString(9)
));
```

#### **5. `LoadGlobalPhrasesAsync`** (Line ~350)
```csharp
// Change FROM:
const string sql = @"SELECT id, account_id, text, active, created_at, updated_at, rev
                       FROM radium.phrase...";

// Change TO:
const string sql = @"SELECT id, account_id, text, active, created_at, updated_at, rev, tags, tags_source, tags_semantic_tag
                       FROM radium.phrase...";

// Update reader:
var row = new PhraseRow
{
    Id = rd.GetInt64(0),
    AccountId = rd.IsDBNull(1) ? null : rd.GetInt64(1),
    Text = rd.GetString(2),
    Active = rd.GetBoolean(3),
    CreatedAt = rd.GetDateTime(4),
    UpdatedAt = rd.GetDateTime(5),
    Rev = rd.GetInt64(6),
    Tags = rd.IsDBNull(7) ? null : rd.GetString(7),
    TagsSource = rd.IsDBNull(8) ? null : rd.GetString(8),
    TagsSemanticTag = rd.IsDBNull(9) ? null : rd.GetString(9)
};
```

#### **6. `GetAllPhraseMetaAsync` and `GetAllGlobalPhraseMetaAsync`** (Lines ~180, ~370)
Update the `.Select()` lambda to include SNOMED properties:

```csharp
// Change FROM:
.Select(r => new PhraseInfo(r.Id, r.AccountId, r.Text, r.Active, r.UpdatedAt, r.Rev)).ToList();

// Change TO:
.Select(r => new PhraseInfo(r.Id, r.AccountId, r.Text, r.Active, r.UpdatedAt, r.Rev, 
    r.Tags, r.TagsSource, r.TagsSemanticTag)).ToList();
```

#### **7. `UpsertPhraseInternalAsync`** - All RETURNING clauses (~400-450)
```csharp
// In SELECT query:
const string selectSql = accountId.HasValue
    ? @"SELECT id, account_id, text, active, created_at, updated_at, rev, tags, tags_source, tags_semantic_tag
        FROM radium.phrase WHERE account_id=@aid AND text=@text"
    : @"SELECT id, account_id, text, active, created_at, updated_at, rev, tags, tags_source, tags_semantic_tag
        FROM radium.phrase WHERE account_id IS NULL AND text=@text";

// Update reader:
existing = new PhraseInfo(
    rd.GetInt64(0), 
    rd.IsDBNull(1) ? null : rd.GetInt64(1), 
    rd.GetString(2), 
    rd.GetBoolean(3), 
    rd.GetDateTime(5), 
    rd.GetInt64(6),
    rd.IsDBNull(7) ? null : rd.GetString(7),
    rd.IsDBNull(8) ? null : rd.GetString(8),
    rd.IsDBNull(9) ? null : rd.GetString(9)
);

// In INSERT RETURNING:
const string insertSql = @"INSERT INTO radium.phrase(account_id,text,active) VALUES(@aid,@text,@active)
                          RETURNING id, account_id, text, active, created_at, updated_at, rev, tags, tags_source, tags_semantic_tag";

// In UPDATE RETURNING:
string updateSql = accountId.HasValue
    ? @"UPDATE radium.phrase SET active=@active
        WHERE account_id=@aid AND text=@text
        RETURNING id, account_id, text, active, created_at, updated_at, rev, tags, tags_source, tags_semantic_tag"
    : @"UPDATE radium.phrase SET active=@active
        WHERE account_id IS NULL AND text=@text
        RETURNING id, account_id, text, active, created_at, updated_at, rev, tags, tags_source, tags_semantic_tag";

// Update both result readers:
result = new PhraseInfo(
    rd.GetInt64(0), 
    rd.IsDBNull(1) ? null : rd.GetInt64(1), 
    rd.GetString(2), 
    rd.GetBoolean(3), 
    rd.GetDateTime(5), 
    rd.GetInt64(6),
    rd.IsDBNull(7) ? null : rd.GetString(7),
    rd.IsDBNull(8) ? null : rd.GetString(8),
    rd.IsDBNull(9) ? null : rd.GetString(9)
);
```

#### **8. `ToggleActiveInternalAsync`** (~480)
```csharp
// Change FROM:
string sql = accountId.HasValue
    ? @"UPDATE radium.phrase SET active = NOT active
        WHERE account_id=@aid AND id=@pid
        RETURNING id, account_id, text, active, created_at, updated_at, rev"
    : @"UPDATE radium.phrase SET active = NOT active
        WHERE account_id IS NULL AND id=@pid
        RETURNING id, account_id, text, active, created_at, updated_at, rev";

// Change TO:
string sql = accountId.HasValue
    ? @"UPDATE radium.phrase SET active = NOT active
        WHERE account_id=@aid AND id=@pid
        RETURNING id, account_id, text, active, created_at, updated_at, rev, tags, tags_source, tags_semantic_tag"
    : @"UPDATE radium.phrase SET active = NOT active
        WHERE account_id IS NULL AND id=@pid
        RETURNING id, account_id, text, active, created_at, updated_at, rev, tags, tags_source, tags_semantic_tag";

// Update reader:
return new PhraseInfo(
    rd.GetInt64(0), 
    rd.IsDBNull(1) ? null : rd.GetInt64(1), 
    rd.GetString(2), 
    rd.GetBoolean(3), 
    rd.GetDateTime(5), 
    rd.GetInt64(6),
    rd.IsDBNull(7) ? null : rd.GetString(7),
    rd.IsDBNull(8) ? null : rd.GetString(8),
    rd.IsDBNull(9) ? null : rd.GetString(9)
);
```

#### **9. `UpdateSnapshotAfterUpsert` and `UpdateSnapshotAfterToggle`** (~520-560)
Add SNOMED properties when creating `PhraseRow`:

```csharp
// In UpdateSnapshotAfterUpsert:
row = new PhraseRow
{
    Id = info.Id,
    AccountId = info.AccountId,
    Text = info.Text,
    Active = info.Active,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = info.UpdatedAt,
    Rev = info.Rev,
    Tags = info.Tags,
    TagsSource = info.TagsSource,
    TagsSemanticTag = info.TagsSemanticTag
};

// Also update the else block:
row.Text = info.Text;
row.Active = info.Active;
row.UpdatedAt = info.UpdatedAt;
row.Rev = info.Rev;
row.Tags = info.Tags;
row.TagsSource = info.TagsSource;
row.TagsSemanticTag = info.TagsSemanticTag;
```

#### **10. `ConvertToGlobalPhrasesAsync`** (~600)
Update the RETURNING clause and reader:

```csharp
const string updateSql = @"UPDATE radium.phrase SET account_id = NULL 
                           WHERE id=@pid AND account_id=@aid
                           RETURNING id, account_id, text, active, created_at, updated_at, rev, tags, tags_source, tags_semantic_tag";

// Update reader:
var newInfo = new PhraseInfo(
    updRd.GetInt64(0),
    updRd.IsDBNull(1) ? null : updRd.GetInt64(1),
    updRd.GetString(2),
    updRd.GetBoolean(3),
    updRd.GetDateTime(5),
    updRd.GetInt64(6),
    updRd.IsDBNull(7) ? null : updRd.GetString(7),
    updRd.IsDBNull(8) ? null : updRd.GetString(8),
    updRd.IsDBNull(9) ? null : updRd.GetString(9)
);

// And when creating globalRow:
var globalRow = new PhraseRow
{
    Id = newInfo.Id,
    AccountId = null,
    Text = newInfo.Text,
    Active = newInfo.Active,
    CreatedAt = updRd.GetDateTime(4),
    UpdatedAt = newInfo.UpdatedAt,
    Rev = newInfo.Rev,
    Tags = newInfo.Tags,
    TagsSource = newInfo.TagsSource,
    TagsSemanticTag = newInfo.TagsSemanticTag
};
```

---

## ?? UI Integration (GlobalPhrasesViewModel)

The `GlobalPhrasesViewModel` already has semantic tag support! Just ensure it uses the new properties:

```csharp
// This should already work - just verify:
private Brush GetColorForSemanticTag(string? semanticTag)
{
    if (string.IsNullOrEmpty(semanticTag)) return Brushes.Transparent;
    
    return semanticTag switch
    {
        "body structure" => new SolidColorBrush(Color.FromRgb(135, 206, 250)), // Light blue
        "disorder" => new SolidColorBrush(Color.FromRgb(240, 128, 128)),       // Light coral
        "finding" => new SolidColorBrush(Color.FromRgb(255, 255, 224)),        // Light yellow
        "procedure" => new SolidColorBrush(Color.FromRgb(221, 160, 221)),      // Plum
        _ => Brushes.Transparent
    };
}
```

---

## ?? Testing After Updates

1. **Build the solution** - Ensure no compilation errors
2. **Run the app** and open Global Phrases view
3. **Load test data** from your database (the 6 rows you have)
4. **Verify highlighting** - Phrases with `tags_semantic_tag='body structure'` should be light blue

---

## ?? Verification Query

Run this to see your SNOMED data:

```sql
-- See phrases with SNOMED tags
SELECT 
    id,
    text,
    tags_source,
    tags_semantic_tag,
    LEFT(tags, 100) AS tags_preview
FROM radium.phrase
WHERE tags IS NOT NULL
ORDER BY id;
```

---

## ?? Quick Win Test

After making all changes, you can test immediately:

```csharp
// In GlobalPhrasesViewModel or any ViewModel:
var phrases = await _phraseService.GetAllGlobalPhraseMetaAsync();
foreach (var p in phrases)
{
    Debug.WriteLine($"Phrase: {p.Text}, SemanticTag: {p.TagsSemanticTag}");
}
```

---

## ?? Files Modified

- ? `PhraseService.cs` (10 methods to update)
- ? `IPhraseService.cs` (PhraseInfo record - DONE)
- ? `GlobalPhrasesViewModel.cs` (verify semantic tag usage)
- ? `PhraseColorizer.cs` (should work as-is)

---

**Estimated Time:** 30-45 minutes for all SQL query updates  
**Difficulty:** Low (repetitive pattern matching)

Let me know when you've completed the SQL updates and I'll help test the highlighting! ??
