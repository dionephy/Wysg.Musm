# SNOMED Semantic Tag Debugging Guide

## Issue
The phrase "brain" is mapped to a SNOMED concept with "body structure" semantic tag, but it's not displaying in light green color.

## What Was Fixed

### 1. **Dependency Injection** ?
**File**: `apps\Wysg.Musm.Radium\App.xaml.cs`

Added `ISnomedMapService` parameter to MainViewModel factory:
```csharp
services.AddTransient<MainViewModel>(sp => new MainViewModel(
    // ... other parameters ...
    sp.GetService<ISnomedMapService>()  // ¡ç ADDED
));
```

### 2. **Debug Logging Added** ?

**File**: `apps\Wysg.Musm.Radium\ViewModels\MainViewModel.Phrases.cs`
- Added logging when semantic tags are loaded from database
- Shows: phrase text ¡æ semantic tag mapping
- Shows: total count of semantic tags loaded

**File**: `src\Wysg.Musm.Editor\Ui\PhraseColorizer.cs`
- Added logging when phrases are colorized
- Shows: phrase text ¡æ semantic tag ¡æ brush type
- Shows: when phrases have no semantic tag found

## How to Debug

### Step 1: Check if semantic tags are being loaded

1. Run the application
2. Open Visual Studio Output window ¡æ Show output from: Debug
3. Look for log messages like:

```
[SemanticTag] Loaded: 'brain' ¡æ 'body structure'
[SemanticTag] Loaded: 'heart' ¡æ 'body structure'
[SemanticTag] Total semantic tags loaded: 15
```

**Expected**: You should see your "brain" phrase listed with "body structure" tag.

**If not loading**:
- Check database: Is "brain" in `radium.phrase` table?
- Check database: Is there a mapping in `radium.global_phrase_snomed` table?
- Check database: Does the SNOMED concept in `snomed.concept_cache` have correct FSN like "Brain (body structure)"?

### Step 2: Check if phrases are being colorized

Type "brain" in the editor (Findings or Conclusion).

Look for log messages like:
```
[PhraseColor] 'brain' ¡æ semantic tag: 'body structure' ¡æ brush type: SolidColorBrush
```

**Expected**: You should see "brain" being matched with semantic tag "body structure".

**If no color change**:
- Check if semantic tag string exactly matches "body structure" (case-insensitive)
- Verify the FSN in database ends with `(body structure)` not `(Body Structure)` or other variation

### Step 3: Verify database content

Run these SQL queries:

```sql
-- Check if phrase exists
SELECT id, text FROM radium.phrase WHERE text = 'brain';

-- Check if phrase has SNOMED mapping (for global phrases)
SELECT gps.phrase_id, gps.concept_id, cc.fsn, cc.pt
FROM radium.global_phrase_snomed gps
JOIN snomed.concept_cache cc ON cc.concept_id = gps.concept_id
WHERE gps.phrase_id = (SELECT id FROM radium.phrase WHERE text = 'brain');

-- Verify FSN format (should end with parentheses containing semantic tag)
SELECT concept_id, fsn FROM snomed.concept_cache WHERE fsn LIKE '%brain%';
```

Expected FSN format: `"Brain (body structure)"` or `"Brain structure (body structure)"`

### Step 4: Check color brush values

The colors are defined in `PhraseColorizer.cs`:

| Semantic Tag | Color | RGB |
|-------------|-------|-----|
| body structure | Light Green | #90EE90 |
| finding | Light Blue | #ADD8E6 |
| disorder | Light Pink | #FFB3B3 |
| procedure | Light Yellow | #FFFF99 |
| observable entity | Light Purple | #E0C4FF |
| substance | Light Orange | #FFD580 |

**Note**: Light green (#90EE90) might be subtle on dark background (#1E1E1E). Try comparing with other phrases or temporarily change background to verify colors work.

## Common Issues

### Issue 1: Semantic tag not extracted correctly
**Symptom**: Logs show `[SemanticTag] Loaded: 'brain' ¡æ ''` (empty tag)

**Cause**: FSN doesn't have parentheses or has wrong format.

**Fix**: Update database with correct FSN format ending in `(body structure)`.

### Issue 2: Case sensitivity mismatch
**Symptom**: Tag loads but doesn't match in colorizer

**Cause**: Semantic tag matching is case-insensitive, but exact spelling matters.

**Check**: Does `GetBrushForSemanticTag()` use `.ToLowerInvariant()` before switch? ? Yes it does.

### Issue 3: Phrase not found in dictionary
**Symptom**: Log shows `[PhraseColor] 'brain' in snapshot but no semantic tag found (tags count: 15)`

**Cause**: Dictionary lookup is case-insensitive but phrase text doesn't match exactly.

**Check**: 
- Is there whitespace difference? ("brain" vs "brain ")
- Is phrase text in `CurrentPhraseSnapshot` same as key in `PhraseSemanticTags`?

## Expected Flow

1. **App Start** ¡æ `MainViewModel` constructed with `ISnomedMapService` ?
2. **Login Success** ¡æ `LoadPhrasesAsync()` called
3. **Load Phrases** ¡æ For each global phrase, load SNOMED mapping
4. **Extract Tag** ¡æ Call `mapping.GetSemanticTag()` on FSN
5. **Build Dictionary** ¡æ `PhraseSemanticTags[phraseText] = semanticTag`
6. **Editor Loads** ¡æ `EditorControl` binds to `PhraseSemanticTags`
7. **User Types** ¡æ `PhraseColorizer.ColorizeLine()` called
8. **Match Phrase** ¡æ Find "brain" in text
9. **Lookup Tag** ¡æ Get semantic tag from dictionary
10. **Apply Color** ¡æ `GetBrushForSemanticTag("body structure")` ¡æ light green brush

## Quick Test

Add this temporary test phrase to verify the system works:

1. In database, add test phrase: `INSERT INTO radium.phrase (text, account_id) VALUES ('testbody', NULL)`
2. Get the phrase ID
3. Add SNOMED mapping with known concept that has "body structure" tag
4. Restart app
5. Type "testbody" in editor
6. Should appear in light green

If test works but "brain" doesn't, the issue is specific to "brain" phrase or its SNOMED mapping.

## Debug Checklist

- [ ] `ISnomedMapService` is injected to MainViewModel (check App.xaml.cs) ?
- [ ] Debug logs appear in Output window when app starts
- [ ] Semantic tags are loaded (check log count > 0)
- [ ] "brain" appears in semantic tag load logs
- [ ] "brain" phrase appears in `CurrentPhraseSnapshot`
- [ ] "brain" phrase appears in `PhraseSemanticTags` dictionary
- [ ] Typing "brain" triggers colorizer logs
- [ ] Semantic tag lookup succeeds in colorizer
- [ ] Brush type is SolidColorBrush with correct color
- [ ] Text layer invalidates and redraws

## Color Verification

To verify colors are working, try these known semantic tags:

- **disorder** ¡æ Light Pink (#FFB3B3) - very visible
- **procedure** ¡æ Light Yellow (#FFFF99) - very visible

If these show color but "body structure" doesn't, check if there's a typo in the semantic tag string.

## Performance Note

Semantic tags are loaded ONCE at startup (in `LoadPhrasesAsync`). If you update SNOMED mappings in the database:

1. Need to call `RefreshPhrasesAsync()` or
2. Restart the application

This is by design for performance (avoid database hit on every keystroke).
