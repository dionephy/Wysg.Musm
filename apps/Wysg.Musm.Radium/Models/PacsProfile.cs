namespace Wysg.Musm.Radium.Models
{
    /// <summary>
    /// Represents a PACS profile configuration that can be saved locally.
    /// Each profile has its own automation sequences and UI spy settings.
    /// </summary>
    public sealed class PacsProfile
    {
        public string Name { get; set; } = string.Empty;
        public string ProcessName { get; set; } = "INFINITT";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Automation sequences specific to this PACS
        public string? AutomationNewStudySequence { get; set; }
        public string? AutomationAddStudySequence { get; set; }
        public string? AutomationShortcutOpenNew { get; set; }
        public string? AutomationShortcutOpenAdd { get; set; }
        public string? AutomationShortcutOpenAfterOpen { get; set; }

        // Default constructor for deserialization
        public PacsProfile() { }

        public PacsProfile(string name, string processName = "INFINITT")
        {
            Name = name;
            ProcessName = processName;
        }
    }
}
