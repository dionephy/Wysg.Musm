using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Wysg.Musm.SnomedTools.Abstractions;

namespace Wysg.Musm.SnomedTools.Services
{
    /// <summary>
    /// Real Azure SQL phrase service implementation for standalone app.
    /// </summary>
    public sealed class RealPhraseService : IPhraseService
  {
        private readonly SnomedToolsLocalSettings _settings;

    public RealPhraseService(SnomedToolsLocalSettings settings)
{
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

 private string Cs => _settings.AzureSqlConnectionString 
            ?? throw new InvalidOperationException("Azure SQL connection string not configured");

        private SqlConnection CreateConnection() => new SqlConnection(Cs);

        public async Task<IReadOnlyList<PhraseInfo>> GetAllGlobalPhraseMetaAsync()
     {
var list = new List<PhraseInfo>();
    const string sql = @"SELECT id, account_id, text, active, updated_at, rev, tags, tags_source, tags_semantic_tag
  FROM radium.phrase
          WHERE account_id IS NULL
       ORDER BY updated_at DESC";

 await using var con = CreateConnection();
   await con.OpenAsync().ConfigureAwait(false);
 await using var cmd = new SqlCommand(sql, con) { CommandTimeout = 30 };
      await using var rd = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess).ConfigureAwait(false);

      while (await rd.ReadAsync().ConfigureAwait(false))
     {
     list.Add(new PhraseInfo(
  rd.GetInt64(0),
           rd.IsDBNull(1) ? null : rd.GetInt64(1),
        rd.GetString(2),
     rd.GetBoolean(3),
  rd.GetDateTime(4),
       rd.GetInt64(5),
        rd.IsDBNull(6) ? null : rd.GetString(6),
   rd.IsDBNull(7) ? null : rd.GetString(7),
    rd.IsDBNull(8) ? null : rd.GetString(8)
  ));
  }

  return list;
        }

   public async Task<PhraseInfo> UpsertPhraseAsync(long? accountId, string text, bool active = true)
    {
if (string.IsNullOrWhiteSpace(text))
   throw new ArgumentException("Phrase text cannot be empty", nameof(text));

     await using var con = CreateConnection();
     await con.OpenAsync().ConfigureAwait(false);

   // Check if phrase already exists
    string selectSql = accountId.HasValue
  ? @"SELECT id, account_id, text, active, updated_at, rev, tags, tags_source, tags_semantic_tag
      FROM radium.phrase
      WHERE account_id=@aid AND text=@text"
       : @"SELECT id, account_id, text, active, updated_at, rev, tags, tags_source, tags_semantic_tag
        FROM radium.phrase
       WHERE account_id IS NULL AND text=@text";

   PhraseInfo? existing = null;
 await using (var selCmd = new SqlCommand(selectSql, con) { CommandTimeout = 30 })
      {
     if (accountId.HasValue)
          selCmd.Parameters.AddWithValue("@aid", accountId.Value);
       selCmd.Parameters.AddWithValue("@text", text.Trim());

       await using var rd = await selCmd.ExecuteReaderAsync().ConfigureAwait(false);
         if (await rd.ReadAsync().ConfigureAwait(false))
{
  existing = new PhraseInfo(
         rd.GetInt64(0),
rd.IsDBNull(1) ? null : rd.GetInt64(1),
rd.GetString(2),
         rd.GetBoolean(3),
   rd.GetDateTime(4),
 rd.GetInt64(5),
    rd.IsDBNull(6) ? null : rd.GetString(6),
    rd.IsDBNull(7) ? null : rd.GetString(7),
 rd.IsDBNull(8) ? null : rd.GetString(8)
    );
      }
            }

      if (existing != null && existing.Active == active)
    return existing; // No-op

   PhraseInfo result;
 if (existing == null)
   {
        // Insert new phrase
       string insertSql = accountId.HasValue
    ? @"INSERT INTO radium.phrase (account_id, text, active) VALUES (@aid, @text, @active);
      SELECT id, account_id, text, active, updated_at, rev, tags, tags_source, tags_semantic_tag
       FROM radium.phrase WHERE id = SCOPE_IDENTITY();"
   : @"INSERT INTO radium.phrase (account_id, text, active) VALUES (NULL, @text, @active);
                SELECT id, account_id, text, active, updated_at, rev, tags, tags_source, tags_semantic_tag
FROM radium.phrase WHERE id = SCOPE_IDENTITY();";

       await using var ins = new SqlCommand(insertSql, con) { CommandTimeout = 30 };
  if (accountId.HasValue)
       ins.Parameters.AddWithValue("@aid", accountId.Value);
  ins.Parameters.AddWithValue("@text", text.Trim());
   ins.Parameters.AddWithValue("@active", active);

    await using var rd = await ins.ExecuteReaderAsync().ConfigureAwait(false);
    if (!await rd.ReadAsync().ConfigureAwait(false))
    throw new InvalidOperationException("Insert failed");

     result = new PhraseInfo(
rd.GetInt64(0),
    rd.IsDBNull(1) ? null : rd.GetInt64(1),
 rd.GetString(2),
  rd.GetBoolean(3),
      rd.GetDateTime(4),
         rd.GetInt64(5),
      rd.IsDBNull(6) ? null : rd.GetString(6),
      rd.IsDBNull(7) ? null : rd.GetString(7),
    rd.IsDBNull(8) ? null : rd.GetString(8)
 );
            }
  else
   {
        // Update existing phrase
     string updateSql = accountId.HasValue
   ? @"UPDATE radium.phrase SET active=@active
      WHERE account_id=@aid AND text=@text;
     SELECT id, account_id, text, active, updated_at, rev, tags, tags_source, tags_semantic_tag
   FROM radium.phrase WHERE id=@existingId;"
      : @"UPDATE radium.phrase SET active=@active
    WHERE account_id IS NULL AND text=@text;
       SELECT id, account_id, text, active, updated_at, rev, tags, tags_source, tags_semantic_tag
  FROM radium.phrase WHERE id=@existingId;";

           await using var upd = new SqlCommand(updateSql, con) { CommandTimeout = 30 };
  if (accountId.HasValue)
    upd.Parameters.AddWithValue("@aid", accountId.Value);
                upd.Parameters.AddWithValue("@text", text.Trim());
       upd.Parameters.AddWithValue("@active", active);
    upd.Parameters.AddWithValue("@existingId", existing.Id);

    await using var rd = await upd.ExecuteReaderAsync().ConfigureAwait(false);
         if (!await rd.ReadAsync().ConfigureAwait(false))
       throw new InvalidOperationException("Update failed");

   result = new PhraseInfo(
       rd.GetInt64(0),
rd.IsDBNull(1) ? null : rd.GetInt64(1),
   rd.GetString(2),
  rd.GetBoolean(3),
    rd.GetDateTime(4),
      rd.GetInt64(5),
   rd.IsDBNull(6) ? null : rd.GetString(6),
               rd.IsDBNull(7) ? null : rd.GetString(7),
 rd.IsDBNull(8) ? null : rd.GetString(8)
   );
 }

     return result;
   }

        public async Task RefreshGlobalPhrasesAsync()
        {
   // No-op for standalone app (no caching layer)
         await Task.CompletedTask;
     }
    }
}
