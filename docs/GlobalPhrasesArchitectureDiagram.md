# Global Phrases Architecture Diagram

## Database Schema

```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛                      radium.phrase                          弛
戍式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛 id           弛 bigint (PK, IDENTITY)                        弛
弛 account_id   弛 bigint (NULLABLE, FK ⊥ app.account)          弛 ?式式式 KEY CHANGE
弛 text         弛 nvarchar(400) NOT NULL                       弛
弛 active       弛 bit NOT NULL                                 弛
弛 created_at   弛 datetime2(3) NOT NULL                        弛
弛 updated_at   弛 datetime2(3) NOT NULL                        弛
弛 rev          弛 bigint NOT NULL                              弛
戌式式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎

Indexes:
  ? IX_phrase_account_text_unique (account_id, text) WHERE account_id IS NOT NULL
  ? IX_phrase_global_text_unique (text) WHERE account_id IS NULL
  ? IX_phrase_global_active (active) WHERE account_id IS NULL
```

## Phrase Types

```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛                        PHRASE ECOSYSTEM                           弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
                              ∪
            忙式式式式式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式式式忖
            弛                                   弛
    忙式式式式式式式∪式式式式式式式式忖              忙式式式式式式式式式式∪式式式式式式式式式忖
    弛 GLOBAL PHRASES 弛              弛  ACCOUNT PHRASES   弛
    弛 (account_id    弛              弛  (account_id       弛
    弛    IS NULL)    弛              弛     = specific)    弛
    戌式式式式式式式式式式式式式式式式戎              戌式式式式式式式式式式式式式式式式式式式式戎
            弛                                   弛
            弛  ? Available to all              弛  ? Owned by account
            弛  ? Managed by admins             弛  ? User-created
            弛  ? System-wide library           弛  ? Can override global
            弛                                   弛
            戌式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式戎
                          ∪
                  忙式式式式式式式式式式式式式式式忖
                  弛   COMBINED    弛
                  弛    QUERIES    弛
                  弛               弛
                  弛 Account takes 弛
                  弛  precedence   弛
                  戌式式式式式式式式式式式式式式式戎
```

## Query Flow

```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛                      USER QUERY REQUEST                            弛
弛              "Get phrases starting with 'norm'"                    弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
                               ∪
              忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
              弛  What query type is needed?    弛
              戌式式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式戎
                               弛
        旨收收收收收收收收收收收收收收收收收收收收收收朴收收收收收收收收收收收收收收收收收收收收收收旬
        ∪                      ∪                      ∪
忙式式式式式式式式式式式式式式式忖    忙式式式式式式式式式式式式式式式式式忖    忙式式式式式式式式式式式式式式式式式式忖
弛 ACCOUNT-ONLY  弛    弛   GLOBAL-ONLY   弛    弛    COMBINED      弛
弛               弛    弛                 弛    弛                  弛
弛  Returns only 弛    弛  Returns only   弛    弛  Returns both,   弛
弛  phrases for  弛    弛  system-wide    弛    弛  deduplicated    弛
弛  account 42   弛    弛  phrases        弛    弛                  弛
戌式式式式式式式成式式式式式式式戎    戌式式式式式式式式成式式式式式式式式戎    戌式式式式式式式式成式式式式式式式式式戎
        弛                     弛                      弛
        ∪                     ∪                      ∪
  WHERE account_id      WHERE account_id       MERGE both sets
       = 42                  IS NULL           (account takes
                                                precedence)

    [normal ct]          [normal chest]       [normal ct]
                         [normal mri]         [normal chest]
                                              [normal mri]
```

## Implementation Architecture

```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛                        APPLICATION LAYER                            弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
                                弛
                                ∪
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛                        IPhraseService                               弛
戍式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛  GetPhrasesForAccountAsync(accountId)                               弛
弛  GetGlobalPhrasesAsync()                               ?式式式 NEW     弛
弛  GetCombinedPhrasesAsync(accountId)                    ?式式式 NEW     弛
弛                                                                     弛
弛  GetPhrasesByPrefixAccountAsync(accountId, prefix)                  弛
弛  GetGlobalPhrasesByPrefixAsync(prefix)                 ?式式式 NEW     弛
弛  GetCombinedPhrasesByPrefixAsync(accountId, prefix)    ?式式式 NEW     弛
弛                                                                     弛
弛  UpsertPhraseAsync(accountId?, text, active)           ?式式式 MODIFIED弛
弛  ToggleActiveAsync(accountId?, phraseId)               ?式式式 MODIFIED弛
弛                                                                     弛
弛  GetAllGlobalPhraseMetaAsync()                         ?式式式 NEW     弛
弛  RefreshGlobalPhrasesAsync()                           ?式式式 NEW     弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
                                弛
                                ∪
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛                        PhraseService                                弛
戍式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛  In-Memory Snapshot Management:                                    弛
弛                                                                     弛
弛  _states[accountId] 式成式? AccountPhraseState (accountId = 42)      弛
弛                      戍式? AccountPhraseState (accountId = 99)      弛
弛                      戌式? AccountPhraseState (accountId = null)    弛
弛                                                    ～                弛
弛                                                    弛                弛
弛                                            GLOBAL_KEY = -1         弛
弛                                                                     弛
弛  Per-Account Locking (FR-261):                                     弛
弛    ? UpdateLock per account                                        弛
弛    ? No global serialization                                       弛
弛    ? Prevents pool starvation                                      弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式成式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
                                弛
                                ∪
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛                    DATABASE (radium.phrase)                         弛
戍式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛  Rows with account_id = NULL    ⊥ Global phrases                   弛
弛  Rows with account_id = 42      ⊥ Account 42's phrases             弛
弛  Rows with account_id = 99      ⊥ Account 99's phrases             弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

## Deduplication Logic (Combined Queries)

```
Step 1: Fetch both sets
忙式式式式式式式式式式式式式式式式式式式式式式忖        忙式式式式式式式式式式式式式式式式式式式式式式忖
弛   Global Phrases     弛        弛  Account 42 Phrases  弛
戍式式式式式式式式式式式式式式式式式式式式式式扣        戍式式式式式式式式式式式式式式式式式式式式式式扣
弛 ? normal chest       弛        弛 ? normal ct          弛
弛 ? normal mri         弛        弛 ? normal chest       弛 ?式 DUPLICATE
弛 ? abnormal finding   弛        弛 ? custom template    弛
戌式式式式式式式式式式式式式式式式式式式式式式戎        戌式式式式式式式式式式式式式式式式式式式式式式戎

Step 2: Create HashSet with account phrases first
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛 HashSet (case-insensitive)     弛
戍式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛 normal ct          ?式 from account (priority)
弛 normal chest       ?式 from account (priority)
弛 custom template    ?式 from account
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎

Step 3: Add global phrases (skip duplicates)
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛 HashSet (final)                弛
戍式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛 normal ct          ?式 account
弛 normal chest       ?式 account (global version ignored)
弛 custom template    ?式 account
弛 normal mri         ?式 global (added, no conflict)
弛 abnormal finding   ?式 global (added, no conflict)
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎

Step 4: Return sorted
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛 Result (ordered by text)       弛
戍式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
弛 abnormal finding               弛
弛 custom template                弛
弛 normal chest                   弛
弛 normal ct                      弛
弛 normal mri                     弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎
```

## Synchronous Flow (FR-258..FR-260)

```
USER ACTION                          DATABASE                    SNAPSHOT
    弛                                    弛                           弛
    弛  Toggle global phrase              弛                           弛
    戍式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式?弛                           弛
    弛                                    弛                           弛
    弛  (account_id=NULL locked)          弛  UPDATE radium.phrase    弛
    弛                                    弛  WHERE id=123             弛
    弛                                    弛  AND account_id IS NULL   弛
    弛                                    戍式式式式式式式式式式式式式式式式式式式式式式式式式式?弛
    弛                                    弛                           弛
    弛                                    弛  RETURNING *              弛
    弛                                    弛?式式式式式式式式式式式式式式式式式式式式式式式式式式扣
    弛                                    弛                           弛
    弛                                    弛  Update in-memory row     弛
    弛                                    弛  state.ById[123]          弛
    弛                                    弛                           弛
    弛                                    弛  Clear cache              弛
    弛                                    弛  (GLOBAL_KEY)             弛
    弛?式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式扛式式式式式式式式式式式式式式式式式式式式式式式式式式式扣
    弛                                                                弛
    弛  UI displays SNAPSHOT state (never optimistic)                弛
    弛                                                                弛
    弛  (lock released)                                              弛
    戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎

Key: User action ⊥ DB update ⊥ Snapshot update ⊥ UI refresh
     Always synchronous, never optimistic
```

## Use Cases

```
忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛                         USE CASE 1                                 弛
弛              System Administrator Populates Library                弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎

Admin Action:
  await phraseService.UpsertPhraseAsync(
    accountId: null,  // Creates global phrase
    text: "normal chest radiograph",
    active: true
  );

Result:
  INSERT INTO radium.phrase(account_id, text, active)
  VALUES (NULL, 'normal chest radiograph', 1);

All accounts can now see this phrase in their autocomplete.

忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛                         USE CASE 2                                 弛
弛                User Overrides Global Phrase                        弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎

Global Phrase: "normal chest radiograph" (account_id = NULL)

User (Account 42) creates:
  await phraseService.UpsertPhraseAsync(
    accountId: 42,
    text: "normal chest radiograph",  // Same text
    active: true
  );

Result:
  ? Database now has TWO rows with same text
  ? Global: account_id = NULL, text = "normal chest radiograph"
  ? Account: account_id = 42, text = "normal chest radiograph"
  ? Filtered indexes prevent duplication within each set
  ? Combined query returns account version only

忙式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式忖
弛                         USE CASE 3                                 弛
弛                  Autocomplete in Editor                            弛
戌式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式戎

User types "norm" in editor:

  var matches = await phraseService.GetCombinedPhrasesByPrefixAsync(
    accountId: 42,
    prefix: "norm",
    limit: 50
  );

Returns:
  ? All active global phrases starting with "norm"
  ? All active account 42 phrases starting with "norm"
  ? Deduplicated (account takes precedence)
  ? Sorted by length, then alphabetically
  ? Limited to 50 results
```

## Migration Safety

```
BEFORE MIGRATION                    AFTER MIGRATION
式式式式式式式式式式式式式式式式式                   式式式式式式式式式式式式式式式

radium.phrase                       radium.phrase
戍式 id                               戍式 id
戍式 account_id (NOT NULL) 式式式式式式式?   戍式 account_id (NULLABLE) ?式 CHANGED
戍式 text                             戍式 text
戍式 active                           戍式 active
戍式 created_at                       戍式 created_at
戍式 updated_at                       戍式 updated_at
戌式 rev                              戌式 rev

Constraints:                        Constraints:
戍式 UQ(account_id, text)             戍式 Filtered IX (account_id, text)
弛                                   弛   WHERE account_id IS NOT NULL
戌式 FK account_id ⊥ account          戍式 Filtered IX (text)
                                    弛   WHERE account_id IS NULL
                                    戌式 FK account_id ⊥ account
                                       (NULL allowed by SQL standard)

Existing Data:                      Existing Data:
? All preserved                     ? All preserved
? All account_id values still       ? All account_id values still valid
  reference valid accounts          ? Ready for NULL inserts
```

## Performance Characteristics

```
Query Type            Index Used                       Complexity
式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
Account-only          IX_phrase_account_active        O(log n + k)
Global-only           IX_phrase_global_active         O(log n + k)
Combined              Both indexes + app merge        O(log n + m + k)
  where n = account rows, m = global rows, k = results

Cache Strategy:
  ? Separate snapshot per account (keyed by accountId)
  ? Separate snapshot for global (keyed by GLOBAL_KEY = -1)
  ? No cross-account locking (FR-261)
  ? Refresh on demand or after mutation
```

## Legend

```
忙式式式式式式忖
弛 Box  弛  = Component or entity
戌式式式式式式戎

式式式式式?  = Data flow or action

?式式式    = Returns or responds

  ∪     = Hierarchy or relationship

[item]  = Result or data item

  ?     = List item or bullet point
```
