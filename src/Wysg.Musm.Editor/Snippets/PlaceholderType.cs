namespace Wysg.Musm.Editor.Snippets;

public enum PlaceholderType
{
    /// <summary>Free text field.</summary>
    FreeText = 0,

    /// <summary>Single choice from options.</summary>
    SingleChoice = 1,

    /// <summary>Multiple selections from options.</summary>
    MultiSelect = 2,

    /// <summary>Replacement field (used when expanding existing text).</summary>
    Replacement = 3
}
