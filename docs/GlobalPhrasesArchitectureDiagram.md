# Global Phrases Architecture Diagram

## Database Schema

```
������������������������������������������������������������������������������������������������������������������������������
��                      radium.phrase                          ��
������������������������������������������������������������������������������������������������������������������������������
�� id           �� bigint (PK, IDENTITY)                        ��
�� account_id   �� bigint (NULLABLE, FK �� app.account)          �� ?������ KEY CHANGE
�� text         �� nvarchar(400) NOT NULL                       ��
�� active       �� bit NOT NULL                                 ��
�� created_at   �� datetime2(3) NOT NULL                        ��
�� updated_at   �� datetime2(3) NOT NULL                        ��
�� rev          �� bigint NOT NULL                              ��
������������������������������������������������������������������������������������������������������������������������������

Indexes:
  ? IX_phrase_account_text_unique (account_id, text) WHERE account_id IS NOT NULL
  ? IX_phrase_global_text_unique (text) WHERE account_id IS NULL
  ? IX_phrase_global_active (active) WHERE account_id IS NULL
```

## Phrase Types

```
������������������������������������������������������������������������������������������������������������������������������������������
��                        PHRASE ECOSYSTEM                           ��
������������������������������������������������������������������������������������������������������������������������������������������
                              ��
            ��������������������������������������������������������������������������
            ��                                   ��
    �����������������妡����������������              �����������������������妡������������������
    �� GLOBAL PHRASES ��              ��  ACCOUNT PHRASES   ��
    �� (account_id    ��              ��  (account_id       ��
    ��    IS NULL)    ��              ��     = specific)    ��
    ������������������������������������              ��������������������������������������������
            ��                                   ��
            ��  ? Available to all              ��  ? Owned by account
            ��  ? Managed by admins             ��  ? User-created
            ��  ? System-wide library           ��  ? Can override global
            ��                                   ��
            ��������������������������������������������������������������������������
                          ��
                  ����������������������������������
                  ��   COMBINED    ��
                  ��    QUERIES    ��
                  ��               ��
                  �� Account takes ��
                  ��  precedence   ��
                  ����������������������������������
```

## Query Flow

```
��������������������������������������������������������������������������������������������������������������������������������������������
��                      USER QUERY REQUEST                            ��
��              "Get phrases starting with 'norm'"                    ��
��������������������������������������������������������������������������������������������������������������������������������������������
                               ��
              ��������������������������������������������������������������������
              ��  What query type is needed?    ��
              ��������������������������������������������������������������������
                               ��
        ����������������������������������������������������������������������������������������������
        ��                      ��                      ��
����������������������������������    ��������������������������������������    ����������������������������������������
�� ACCOUNT-ONLY  ��    ��   GLOBAL-ONLY   ��    ��    COMBINED      ��
��               ��    ��                 ��    ��                  ��
��  Returns only ��    ��  Returns only   ��    ��  Returns both,   ��
��  phrases for  ��    ��  system-wide    ��    ��  deduplicated    ��
��  account 42   ��    ��  phrases        ��    ��                  ��
����������������������������������    ��������������������������������������    ����������������������������������������
        ��                     ��                      ��
        ��                     ��                      ��
  WHERE account_id      WHERE account_id       MERGE both sets
       = 42                  IS NULL           (account takes
                                                precedence)

    [normal ct]          [normal chest]       [normal ct]
                         [normal mri]         [normal chest]
                                              [normal mri]
```

## Implementation Architecture

```
����������������������������������������������������������������������������������������������������������������������������������������������
��                        APPLICATION LAYER                            ��
����������������������������������������������������������������������������������������������������������������������������������������������
                                ��
                                ��
����������������������������������������������������������������������������������������������������������������������������������������������
��                        IPhraseService                               ��
����������������������������������������������������������������������������������������������������������������������������������������������
��  GetPhrasesForAccountAsync(accountId)                               ��
��  GetGlobalPhrasesAsync()                               ?������ NEW     ��
��  GetCombinedPhrasesAsync(accountId)                    ?������ NEW     ��
��                                                                     ��
��  GetPhrasesByPrefixAccountAsync(accountId, prefix)                  ��
��  GetGlobalPhrasesByPrefixAsync(prefix)                 ?������ NEW     ��
��  GetCombinedPhrasesByPrefixAsync(accountId, prefix)    ?������ NEW     ��
��                                                                     ��
��  UpsertPhraseAsync(accountId?, text, active)           ?������ MODIFIED��
��  ToggleActiveAsync(accountId?, phraseId)               ?������ MODIFIED��
��                                                                     ��
��  GetAllGlobalPhraseMetaAsync()                         ?������ NEW     ��
��  RefreshGlobalPhrasesAsync()                           ?������ NEW     ��
����������������������������������������������������������������������������������������������������������������������������������������������
                                ��
                                ��
����������������������������������������������������������������������������������������������������������������������������������������������
��                        PhraseService                                ��
����������������������������������������������������������������������������������������������������������������������������������������������
��  In-Memory Snapshot Management:                                    ��
��                                                                     ��
��  _states[accountId] ������? AccountPhraseState (accountId = 42)      ��
��                      ����? AccountPhraseState (accountId = 99)      ��
��                      ����? AccountPhraseState (accountId = null)    ��
��                                                    ��                ��
��                                                    ��                ��
��                                            GLOBAL_KEY = -1         ��
��                                                                     ��
��  Per-Account Locking (FR-261):                                     ��
��    ? UpdateLock per account                                        ��
��    ? No global serialization                                       ��
��    ? Prevents pool starvation                                      ��
����������������������������������������������������������������������������������������������������������������������������������������������
                                ��
                                ��
����������������������������������������������������������������������������������������������������������������������������������������������
��                    DATABASE (radium.phrase)                         ��
����������������������������������������������������������������������������������������������������������������������������������������������
��  Rows with account_id = NULL    �� Global phrases                   ��
��  Rows with account_id = 42      �� Account 42's phrases             ��
��  Rows with account_id = 99      �� Account 99's phrases             ��
����������������������������������������������������������������������������������������������������������������������������������������������
```

## Deduplication Logic (Combined Queries)

```
Step 1: Fetch both sets
������������������������������������������������        ������������������������������������������������
��   Global Phrases     ��        ��  Account 42 Phrases  ��
������������������������������������������������        ������������������������������������������������
�� ? normal chest       ��        �� ? normal ct          ��
�� ? normal mri         ��        �� ? normal chest       �� ?�� DUPLICATE
�� ? abnormal finding   ��        �� ? custom template    ��
������������������������������������������������        ������������������������������������������������

Step 2: Create HashSet with account phrases first
��������������������������������������������������������������������
�� HashSet (case-insensitive)     ��
��������������������������������������������������������������������
�� normal ct          ?�� from account (priority)
�� normal chest       ?�� from account (priority)
�� custom template    ?�� from account
��������������������������������������������������������������������

Step 3: Add global phrases (skip duplicates)
��������������������������������������������������������������������
�� HashSet (final)                ��
��������������������������������������������������������������������
�� normal ct          ?�� account
�� normal chest       ?�� account (global version ignored)
�� custom template    ?�� account
�� normal mri         ?�� global (added, no conflict)
�� abnormal finding   ?�� global (added, no conflict)
��������������������������������������������������������������������

Step 4: Return sorted
��������������������������������������������������������������������
�� Result (ordered by text)       ��
��������������������������������������������������������������������
�� abnormal finding               ��
�� custom template                ��
�� normal chest                   ��
�� normal ct                      ��
�� normal mri                     ��
��������������������������������������������������������������������
```

## Synchronous Flow (FR-258..FR-260)

```
USER ACTION                          DATABASE                    SNAPSHOT
    ��                                    ��                           ��
    ��  Toggle global phrase              ��                           ��
    ������������������������������������������������������������������������?��                           ��
    ��                                    ��                           ��
    ��  (account_id=NULL locked)          ��  UPDATE radium.phrase    ��
    ��                                    ��  WHERE id=123             ��
    ��                                    ��  AND account_id IS NULL   ��
    ��                                    ������������������������������������������������������?��
    ��                                    ��                           ��
    ��                                    ��  RETURNING *              ��
    ��                                    ��?������������������������������������������������������
    ��                                    ��                           ��
    ��                                    ��  Update in-memory row     ��
    ��                                    ��  state.ById[123]          ��
    ��                                    ��                           ��
    ��                                    ��  Clear cache              ��
    ��                                    ��  (GLOBAL_KEY)             ��
    ��?��������������������������������������������������������������������������������������������������������������������������������
    ��                                                                ��
    ��  UI displays SNAPSHOT state (never optimistic)                ��
    ��                                                                ��
    ��  (lock released)                                              ��
    ������������������������������������������������������������������������������������������������������������������������������������

Key: User action �� DB update �� Snapshot update �� UI refresh
     Always synchronous, never optimistic
```

## Use Cases

```
��������������������������������������������������������������������������������������������������������������������������������������������
��                         USE CASE 1                                 ��
��              System Administrator Populates Library                ��
��������������������������������������������������������������������������������������������������������������������������������������������

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

��������������������������������������������������������������������������������������������������������������������������������������������
��                         USE CASE 2                                 ��
��                User Overrides Global Phrase                        ��
��������������������������������������������������������������������������������������������������������������������������������������������

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

��������������������������������������������������������������������������������������������������������������������������������������������
��                         USE CASE 3                                 ��
��                  Autocomplete in Editor                            ��
��������������������������������������������������������������������������������������������������������������������������������������������

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
����������������������������������                   ������������������������������

radium.phrase                       radium.phrase
���� id                               ���� id
���� account_id (NOT NULL) ��������������?   ���� account_id (NULLABLE) ?�� CHANGED
���� text                             ���� text
���� active                           ���� active
���� created_at                       ���� created_at
���� updated_at                       ���� updated_at
���� rev                              ���� rev

Constraints:                        Constraints:
���� UQ(account_id, text)             ���� Filtered IX (account_id, text)
��                                   ��   WHERE account_id IS NOT NULL
���� FK account_id �� account          ���� Filtered IX (text)
                                    ��   WHERE account_id IS NULL
                                    ���� FK account_id �� account
                                       (NULL allowed by SQL standard)

Existing Data:                      Existing Data:
? All preserved                     ? All preserved
? All account_id values still       ? All account_id values still valid
  reference valid accounts          ? Ready for NULL inserts
```

## Performance Characteristics

```
Query Type            Index Used                       Complexity
������������������������������������������������������������������������������������������������������������������������������������������
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
����������������
�� Box  ��  = Component or entity
����������������

����������?  = Data flow or action

?������    = Returns or responds

  ��     = Hierarchy or relationship

[item]  = Result or data item

  ?     = List item or bullet point
```
