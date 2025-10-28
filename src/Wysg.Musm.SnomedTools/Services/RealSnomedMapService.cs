using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Wysg.Musm.SnomedTools.Abstractions;

namespace Wysg.Musm.SnomedTools.Services
{
    /// <summary>
    /// Real Azure SQL SNOMED mapping service implementation for standalone app.
    /// </summary>
    public sealed class RealSnomedMapService : ISnomedMapService
    {
    private readonly SnomedToolsLocalSettings _settings;

        public RealSnomedMapService(SnomedToolsLocalSettings settings)
        {
      _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    private string Cs => _settings.AzureSqlConnectionString 
 ?? throw new InvalidOperationException("Azure SQL connection string not configured");

        private SqlConnection CreateConnection() => new SqlConnection(Cs);

        public async Task CacheConceptAsync(SnomedConcept concept)
        {
         if (concept == null)
                throw new ArgumentNullException(nameof(concept));

            const string sql = @"EXEC snomed.upsert_concept @concept_id, @concept_id_str, @fsn, @pt, @module_id, @active, @expires_at";

   await using var con = CreateConnection();
            await con.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqlCommand(sql, con) { CommandType = CommandType.StoredProcedure };
            cmd.CommandText = "snomed.upsert_concept";
       cmd.Parameters.AddWithValue("@concept_id", concept.ConceptId);
          cmd.Parameters.AddWithValue("@concept_id_str", concept.ConceptIdStr);
      cmd.Parameters.AddWithValue("@fsn", concept.Fsn);
     cmd.Parameters.AddWithValue("@pt", (object?)concept.Pt ?? DBNull.Value);
      cmd.Parameters.AddWithValue("@module_id", DBNull.Value);
         cmd.Parameters.AddWithValue("@active", concept.Active);
            cmd.Parameters.AddWithValue("@expires_at", DBNull.Value);
     await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        public async Task<bool> MapPhraseAsync(
            long phraseId,
     long? accountId,
     long conceptId,
            string mappingType = "exact",
            decimal? confidence = null,
            string? notes = null,
            long? mappedBy = null)
      {
    await using var con = CreateConnection();
  await con.OpenAsync().ConfigureAwait(false);

       // Verify concept exists in cache
         await using (var chk = new SqlCommand("SELECT 1 FROM snomed.concept_cache WHERE concept_id=@id", con))
          {
   chk.Parameters.AddWithValue("@id", conceptId);
    var ok = await chk.ExecuteScalarAsync().ConfigureAwait(false) != null;
                if (!ok)
    throw new InvalidOperationException("SNOMED concept not in cache. Cache concept first.");
            }

       if (accountId == null)
  {
       // Global phrase mapping
        await using (var v = new SqlCommand("SELECT 1 FROM radium.phrase WHERE id=@pid AND account_id IS NULL", con))
       {
    v.Parameters.AddWithValue("@pid", phraseId);
           if (await v.ExecuteScalarAsync().ConfigureAwait(false) == null)
    throw new InvalidOperationException("Phrase must be global (account_id IS NULL)");
     }

    await using var cmd = new SqlCommand("radium.map_global_phrase_to_snomed", con) 
     { 
       CommandType = CommandType.StoredProcedure 
  };
    cmd.Parameters.AddWithValue("@phrase_id", phraseId);
                cmd.Parameters.AddWithValue("@concept_id", conceptId);
          cmd.Parameters.AddWithValue("@mapping_type", mappingType);
   cmd.Parameters.AddWithValue("@confidence", (object?)confidence ?? DBNull.Value);
 cmd.Parameters.AddWithValue("@notes", (object?)notes ?? DBNull.Value);
     cmd.Parameters.AddWithValue("@mapped_by", (object?)mappedBy ?? DBNull.Value);
     await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                return true;
     }
 else
       {
         // Account-specific phrase mapping
     await using (var v = new SqlCommand("SELECT 1 FROM radium.phrase WHERE id=@pid AND account_id=@aid", con))
    {
              v.Parameters.AddWithValue("@pid", phraseId);
         v.Parameters.AddWithValue("@aid", accountId.Value);
          if (await v.ExecuteScalarAsync().ConfigureAwait(false) == null)
      throw new InvalidOperationException("Phrase must be account-specific for accountId");
        }

     await using var cmd = new SqlCommand("radium.map_phrase_to_snomed", con) 
      { 
 CommandType = CommandType.StoredProcedure 
    };
       cmd.Parameters.AddWithValue("@phrase_id", phraseId);
    cmd.Parameters.AddWithValue("@concept_id", conceptId);
    cmd.Parameters.AddWithValue("@mapping_type", mappingType);
   cmd.Parameters.AddWithValue("@confidence", (object?)confidence ?? DBNull.Value);
              cmd.Parameters.AddWithValue("@notes", (object?)notes ?? DBNull.Value);
          await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
     return true;
      }
        }
  }
}
