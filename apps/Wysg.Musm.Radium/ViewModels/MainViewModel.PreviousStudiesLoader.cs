using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Data loading of previous studies from repository (isolated from presentation logic).
    /// </summary>
    public partial class MainViewModel
    {
        private async Task LoadPreviousStudiesForPatientAsync(string patientId)
        {
            if (_studyRepo == null) return;
            try
            {
                var rows = await _studyRepo.GetReportsForPatientAsync(patientId);
                PreviousStudies.Clear();
                var groups = rows.GroupBy(r => new { r.StudyId, r.StudyDateTime, r.Studyname });
                foreach (var g in groups.OrderByDescending(g => g.Key.StudyDateTime))
                {
                    string modality = ExtractModality(StudyName);
                    if (PreviousStudies.Any(t => t.StudyDateTime == g.Key.StudyDateTime && string.Equals(t.Modality, modality, StringComparison.OrdinalIgnoreCase)))
                        continue; // enforce uniqueness by datetime + modality
                    var tab = new PreviousStudyTab
                    {
                        Id = Guid.NewGuid(),
                        StudyDateTime = g.Key.StudyDateTime,
                        Modality = modality,
                        Title = $"{g.Key.StudyDateTime:yyyy-MM-dd} {modality}",
                        OriginalHeader = string.Empty
                    };
                    foreach (var row in g.OrderByDescending(r => r.ReportDateTime))
                    {
                        string findings = string.Empty; string conclusion = string.Empty; string headerFind = string.Empty;
                        try
                        {
                            using var doc = JsonDocument.Parse(row.ReportJson);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("header_and_findings", out var hf)) headerFind = hf.GetString() ?? string.Empty;
                            if (root.TryGetProperty("findings", out var ff)) findings = ff.GetString() ?? headerFind; else findings = headerFind;
                            if (root.TryGetProperty("conclusion", out var cc)) conclusion = cc.GetString() ?? string.Empty;
                        }
                        catch (Exception ex) { Debug.WriteLine("[PrevLoad] JSON parse error: " + ex.Message); }
                        var choice = new PreviousReportChoice
                        {
                            ReportDateTime = row.ReportDateTime,
                            CreatedBy = row.CreatedBy ?? string.Empty,
                            Studyname = row.Studyname,
                            Findings = string.IsNullOrWhiteSpace(findings) ? headerFind : findings,
                            Conclusion = conclusion,
                            _studyDateTime = row.StudyDateTime
                        };
                        tab.Reports.Add(choice);
                    }
                    tab.SelectedReport = tab.Reports.FirstOrDefault();
                    if (tab.SelectedReport != null)
                    {
                        tab.OriginalFindings = tab.SelectedReport.Findings;
                        tab.OriginalConclusion = tab.SelectedReport.Conclusion;
                        tab.Findings = tab.SelectedReport.Findings;
                        tab.Conclusion = tab.SelectedReport.Conclusion;
                    }
                    PreviousStudies.Add(tab);
                }
                if (PreviousStudies.Count > 0)
                {
                    SelectedPreviousStudy = PreviousStudies.First();
                    PreviousReportified = true; // default ON
                }
            }
            catch (Exception ex) { Debug.WriteLine("[PrevLoad] error: " + ex.Message); }
        }
    }
}
