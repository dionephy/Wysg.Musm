using System;
using System.Collections.Generic;

namespace Wysg.Musm.EditorDataStudio.Services
{
    public sealed record TenantDto(long Id, string Code, string Name);
    public sealed record PhraseDto(long Id, string Text, bool CaseSensitive, string Lang, bool Active, DateTime UpdatedAt);
    public sealed record SctConceptDto(string Id, string Term, string ModuleId, string EffectiveTime);

    public sealed record PhraseSctRoleDto(int RoleGroup, string AttributeId, string AttributeTerm, string ValueConceptId, string ValueTerm);
    public sealed record PhraseSctMappingDto(string RootConceptId, string RootTerm, string? ExpressionCg, string? Edition, string? ModuleId, string? EffectiveTime, List<PhraseSctRoleDto> Roles);
}
