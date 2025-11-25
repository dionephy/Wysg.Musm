# Documentation Organization - Relative Time Periods

**Created**: 2025-11-11  
**Purpose**: Organize documentation by relative time periods instead of absolute dates

---

## Why Relative Time Periods?

### Problems with Absolute Dates
? **Time Zone Differences** - Date 2025-11-11 in one timezone is 2025-11-10 in another  
? **Maintenance Burden** - Need to constantly update "recent" categories  
? **Unclear Age** - "2025-11-11" doesn't tell you if it's 1 day or 1 year old  
? **Archive Confusion** - When do files move from "recent" to "archive"?  

### Benefits of Relative Periods
? **Self-Organizing** - Files naturally age into correct categories  
? **Consistent View** - Everyone sees the same relative organization  
? **Clear Age** - "Week 0" vs "Month 3" immediately indicates freshness  
? **Automatic Archiving** - Files move categories based on age, not manual effort  

---

## Proposed Time-Based Structure

```
apps/Wysg.Musm.Radium/docs/
������ README.md                          # Main entry point
������ INDEX.md                           # Complete searchable index
��
������ 00-current/                        # Current sprint/week (Week 0)
��   ������ README.md
��   ������ active-features.md             # Features in development
��   ������ active-fixes.md                # Fixes in progress
��   ������ tasks.md                       # Current tasks
��
������ 01-recent/                         # Last 4 weeks (Weeks 1-4)
��   ������ README.md
��   ������ week-1/                        # 1 week old (7-13 days ago)
��   ������ week-2/                        # 2 weeks old (14-20 days ago)
��   ������ week-3/                        # 3 weeks old (21-27 days ago)
��   ������ week-4/                        # 4 weeks old (28-34 days ago)
��
������ 02-this-quarter/                   # Current quarter (Weeks 5-12)
��   ������ README.md
��   ������ month-1/                       # First month of quarter
��   ������ month-2/                       # Second month of quarter
��   ������ month-3/                       # Third month of quarter
��
������ 03-last-quarter/                   # Previous quarter
��   ������ README.md
��   ������ [year-quarter]/                # e.g., 2025-Q1, 2024-Q4
��
������ 04-archive/                        # Older than 6 months
��   ������ README.md
��   ������ 2024/
��   ������ 2023/
��   ������ older/
��
������ 10-features/                       # Feature specifications (by category)
��   ������ ui/
��   ������ automation/
��   ������ phrases/
��   ������ [etc]
��
������ 11-architecture/                   # Architecture docs (timeless)
��   ������ overview/
��   ������ viewmodels/
��   ������ services/
��
������ 12-guides/                         # User & developer guides (timeless)
��   ������ user/
��   ������ developer/
��   ������ diagnostics/
��
������ 99-templates/                      # Document templates (timeless)
```

---

## Time Period Definitions

### Week 0 (Current Sprint)
- **Definition**: Last 7 days (0-6 days ago)
- **Content**: Active development, in-progress features
- **Location**: `00-current/`
- **Auto-Archive**: After 7 days �� `01-recent/week-1/`

### Weeks 1-4 (Recent History)
- **Definition**: 7-34 days ago
- **Content**: Recently completed features, recent fixes
- **Location**: `01-recent/week-[1-4]/`
- **Auto-Archive**: After 35 days �� `02-this-quarter/month-X/`

### Current Quarter (Weeks 5-12)
- **Definition**: 35-90 days ago (current quarter)
- **Content**: Quarter's work, monthly summaries
- **Location**: `02-this-quarter/month-[1-3]/`
- **Auto-Archive**: After 90 days �� `03-last-quarter/[year-quarter]/`

### Last Quarter
- **Definition**: 90-180 days ago (previous quarter)
- **Content**: Previous quarter's work
- **Location**: `03-last-quarter/[year-quarter]/`
- **Auto-Archive**: After 180 days �� `04-archive/[year]/`

### Archive
- **Definition**: Older than 180 days (6+ months)
- **Content**: Historical documentation
- **Location**: `04-archive/[year]/`
- **Never Auto-Archive**: Permanent storage

---

## Category Structure (Timeless)

Some documentation is **timeless** and doesn't age:

### Features (10-features/)
Organized by **feature domain**, not time:
- `ui/` - UI features
- `automation/` - PACS automation
- `phrases/` - Phrase system
- `snomed/` - SNOMED integration
- `reportify/` - Reportify system

### Architecture (11-architecture/)
Organized by **component type**:
- `overview/` - High-level architecture
- `viewmodels/` - ViewModel documentation
- `services/` - Service layer
- `refactorings/` - Major refactorings

### Guides (12-guides/)
Organized by **audience**:
- `user/` - End-user guides
- `developer/` - Developer guides
- `diagnostics/` - Troubleshooting

---

## File Naming Convention

### Time-Based Documents
Format: `[TYPE]_w[WEEK]_[Description].md`

Examples:
- `FEATURE_w0_EditorAutofocus.md` (Current week)
- `BUGFIX_w2_CompletionWindow.md` (2 weeks ago)
- `ENHANCEMENT_w5_HotkeysEdit.md` (5 weeks ago / current quarter)

### Timeless Documents
Format: `[TYPE]_[Component]_[Description].md`

Examples:
- `ARCHITECTURE_MainViewModel.md`
- `GUIDE_PhraseHighlighting.md`
- `SPEC_SnomedIntegration.md`

---

## Migration Script Logic

### Pseudocode
```
For each document with date:
  age = today - document_date
  
  if age <= 7 days:
    move to "00-current/"
  elif age <= 14 days:
    move to "01-recent/week-1/"
  elif age <= 21 days:
    move to "01-recent/week-2/"
  elif age <= 28 days:
    move to "01-recent/week-3/"
  elif age <= 35 days:
    move to "01-recent/week-4/"
  elif age <= 90 days:
    quarter_month = calculate_quarter_month(age)
    move to "02-this-quarter/month-{quarter_month}/"
  elif age <= 180 days:
    quarter = calculate_previous_quarter(age)
    move to "03-last-quarter/{quarter}/"
  else:
    year = document_year
    move to "04-archive/{year}/"
```

---

## README Organization

### Main README.md
```markdown
# Radium Documentation

## Quick Links
- [Current Sprint (Week 0)](00-current/) - Last 7 days
- [Recent (Weeks 1-4)](01-recent/) - Last 4 weeks
- [This Quarter](02-this-quarter/) - Current quarter
- [Last Quarter](03-last-quarter/) - Previous quarter

## By Category
- [Features](10-features/) - Feature specifications
- [Architecture](11-architecture/) - System design
- [Guides](12-guides/) - User & developer guides

## Recent Updates

### Week 0 (Current Sprint)
- [List of current week documents]

### Week 1 (Last Week)
- [List of last week documents]

### Week 2 (2 Weeks Ago)
- [List of 2 weeks ago documents]
```

---

## Automation

### Auto-Archive Script (Weekly)
```powershell
# Run weekly to move aged documents
.\auto-archive-docs.ps1

# Output:
# Moved 15 documents from 00-current/ to 01-recent/week-1/
# Moved 12 documents from 01-recent/week-4/ to 02-this-quarter/month-1/
# Moved 8 documents from 02-this-quarter/month-3/ to 03-last-quarter/2025-Q1/
```

### Date Calculation Logic
```csharp
DateTime now = DateTime.Now;
DateTime docDate = GetDocumentDate(filename);
TimeSpan age = now - docDate;

int weekNumber = (int)(age.TotalDays / 7);
int quarterMonth = ((int)(age.TotalDays / 30)) % 3 + 1;
```

---

## Benefits of This Approach

### For Developers
? **Clear Freshness** - "Week 0" is obviously current, "Month 3" is older  
? **Consistent View** - Everyone sees same relative organization  
? **No Date Confusion** - Relative periods work across timezones  
? **Easy Navigation** - Natural aging from current �� recent �� quarter �� archive  

### For Documentation Maintenance
? **Self-Organizing** - Documents naturally move to correct folders  
? **Automated Archiving** - Script handles aging automatically  
? **No Manual Updates** - No need to change "recent" lists  
? **Scalable** - Works with any document volume  

### For Search
? **Time-Based Search** - "Show me last week's changes"  
? **Category Search** - "Show me all phrase features"  
? **Combined Search** - "Show me recent automation fixes"  

---

## Comparison: Old vs New

### Old Structure (Absolute Dates)
```
02-bugfixes/
������ 2025-02/           �� What's "2025-02"? Is it recent or old?
������ 2025-01/           �� Same date looks different on different days
������ 2024/              �� When do we move to archive?
```

**Problems:**
- Date "2025-11-11" means different things on different days
- No clear indication of age
- Manual decisions about archiving

### New Structure (Relative Periods)
```
00-current/            �� Obviously current (Week 0)
01-recent/
������ week-1/            �� Obviously 1 week old
������ week-2/            �� Obviously 2 weeks old
������ week-3/            �� Obviously 3 weeks old
02-this-quarter/
������ month-1/           �� First month of current quarter
������ month-2/           �� Second month of current quarter
```

**Benefits:**
- "Week 0" always means "current week" regardless of when viewed
- Age is immediately obvious
- Automatic archiving based on age calculation

---

## Implementation Plan

### Phase 1: Create New Structure
1. Create new directory structure
2. Create README files with relative period definitions
3. Create auto-archive script

### Phase 2: Migrate Documents
1. Calculate age of each document
2. Move to appropriate relative period folder
3. Update cross-references

### Phase 3: Update Main README
1. Change from absolute dates to relative periods
2. List "Week 0", "Week 1", etc.
3. Add age indicators

### Phase 4: Automation
1. Set up weekly auto-archive cron job
2. Test aging logic
3. Validate moves

---

## Example: How Documents Age

### Day 0 (Today)
```
BUGFIX_w0_CompletionWindow.md
Location: 00-current/
Age: 0 days
```

### Day 8 (Next Week)
```
BUGFIX_w1_CompletionWindow.md (renamed automatically)
Location: 01-recent/week-1/
Age: 8 days
```

### Day 35 (5 Weeks Later)
```
BUGFIX_q1m1_CompletionWindow.md (renamed automatically)
Location: 02-this-quarter/month-1/
Age: 35 days
```

### Day 91 (After Quarter Ends)
```
BUGFIX_2025Q1_CompletionWindow.md (renamed automatically)
Location: 03-last-quarter/2025-Q1/
Age: 91 days
```

### Day 181 (After 6 Months)
```
BUGFIX_2025_CompletionWindow.md (renamed automatically)
Location: 04-archive/2025/
Age: 181 days
```

---

**Recommendation**: Use relative time periods instead of absolute dates for better maintainability and clarity.
