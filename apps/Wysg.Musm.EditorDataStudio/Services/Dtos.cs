using System;

namespace Wysg.Musm.EditorDataStudio.Services
{
    public sealed record TenantDto(long Id, string Code, string Name);
    public sealed record PhraseDto(long Id, string Text, bool CaseSensitive, string Lang, bool Active, DateTime UpdatedAt);
    public sealed record SctConceptDto(string Id, string Term, string ModuleId, string EffectiveTime);
}
