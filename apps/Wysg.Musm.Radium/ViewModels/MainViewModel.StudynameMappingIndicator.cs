using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Tracks whether the current studyname is missing LOINC mappings and surfaces a UI warning flag.
    /// </summary>
    public partial class MainViewModel
    {
        private bool _studynameMappingNeedsAttention;
        public bool StudynameMappingNeedsAttention
        {
            get => _studynameMappingNeedsAttention;
            private set => SetProperty(ref _studynameMappingNeedsAttention, value);
        }

        private int _studynameMappingCheckVersion;

        public void RefreshStudynameMappingStatus() => ScheduleStudynameMappingCheck();

        private void ScheduleStudynameMappingCheck()
        {
            if (_studynameLoincRepo == null)
            {
                StudynameMappingNeedsAttention = false;
                return;
            }

            var trimmed = string.IsNullOrWhiteSpace(StudyName) ? string.Empty : StudyName.Trim();
            var requestId = Interlocked.Increment(ref _studynameMappingCheckVersion);
            _ = EvaluateStudynameMappingAsync(trimmed, requestId);
        }

        private async Task EvaluateStudynameMappingAsync(string? studyname, int requestId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(studyname))
                {
                    if (requestId == _studynameMappingCheckVersion)
                    {
                        StudynameMappingNeedsAttention = false;
                    }
                    return;
                }

                var entries = await _studynameLoincRepo!.GetStudynamesAsync();
                if (requestId != _studynameMappingCheckVersion)
                    return;

                var match = entries.FirstOrDefault(s => string.Equals(s.Studyname, studyname, StringComparison.OrdinalIgnoreCase));
                bool needsAttention;

                if (match == null)
                {
                    needsAttention = true;
                }
                else
                {
                    var mappings = await _studynameLoincRepo.GetMappingsAsync(match.Id);
                    if (requestId != _studynameMappingCheckVersion)
                        return;
                    needsAttention = !mappings.Any();
                }

                StudynameMappingNeedsAttention = needsAttention;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainViewModel] Studyname mapping check failed: {ex.Message}");
            }
        }
    }
}
