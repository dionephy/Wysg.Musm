using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Wysg.Musm.Radium.ViewModels
{
    /// <summary>
    /// Partial: Automation data acquisition methods.
    /// Contains methods for acquiring study remarks, patient remarks, and related deduplication logic.
    /// </summary>
    public partial class MainViewModel
    {
        // New automation helpers (return Task for proper sequencing)
        private async Task AcquireStudyRemarkAsync()
        {
            const int maxRetries = 3;
            const int delayMs = 200;
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Debug.WriteLine($"[Automation][GetStudyRemark] Attempt {attempt}/{maxRetries} - Starting acquisition");
                    
                    // Small delay before attempt to allow PACS UI to stabilize
                    if (attempt > 1)
                    {
                        Debug.WriteLine($"[Automation][GetStudyRemark] Waiting {delayMs * attempt}ms before retry...");
                        await Task.Delay(delayMs * attempt);
                    }
                    
                    var s = await _pacs.GetCurrentStudyRemarkAsync();
                    Debug.WriteLine($"[Automation][GetStudyRemark] Raw result from PACS: '{s}'");
                    Debug.WriteLine($"[Automation][GetStudyRemark] Result length: {s?.Length ?? 0} characters");
                    
                    // Success check: if we got non-empty content, accept it
                    if (!string.IsNullOrEmpty(s))
                    {
                        Debug.WriteLine($"[Automation][GetStudyRemark] SUCCESS on attempt {attempt}");
                        StudyRemark = s;
                        Debug.WriteLine($"[Automation][GetStudyRemark] Set StudyRemark property: '{StudyRemark}'");
                        
                        // NEW: Check copy toggle - only fill ChiefComplaint if toggle is ON
                        if (CopyStudyRemarkToChiefComplaint)
                        {
                            ChiefComplaint = s;
                            Debug.WriteLine($"[Automation][GetStudyRemark] Copy toggle ON - Set ChiefComplaint property: '{ChiefComplaint}'");
                        }
                        else
                        {
                            Debug.WriteLine($"[Automation][GetStudyRemark] Copy toggle OFF - ChiefComplaint not updated");
                        }
                        
                        SetStatus($"Study remark captured ({s.Length} chars)");
                        return; // Success - exit retry loop
                    }
                    else
                    {
                        Debug.WriteLine($"[Automation][GetStudyRemark] Attempt {attempt} returned empty/null");
                        if (attempt < maxRetries)
                        {
                            Debug.WriteLine($"[Automation][GetStudyRemark] Will retry...");
                            continue; // Try again
                        }
                        else
                        {
                            Debug.WriteLine($"[Automation][GetStudyRemark] Max retries reached - accepting empty result");
                            StudyRemark = string.Empty;
                            
                            // Clear ChiefComplaint only if copy toggle is ON
                            if (CopyStudyRemarkToChiefComplaint)
                            {
                                ChiefComplaint = string.Empty;
                            }
                            
                            SetStatus("Study remark empty after retries");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Automation][GetStudyRemark] Attempt {attempt} EXCEPTION: {ex.GetType().Name} - {ex.Message}");
                    Debug.WriteLine($"[Automation][GetStudyRemark] StackTrace: {ex.StackTrace}");
                    
                    if (attempt < maxRetries)
                    {
                        Debug.WriteLine($"[Automation][GetStudyRemark] Will retry after exception...");
                        continue; // Try again
                    }
                    else
                    {
                        Debug.WriteLine($"[Automation][GetStudyRemark] Max retries reached after exception");
                        SetStatus("Study remark capture failed after retries", true);
                        StudyRemark = string.Empty; // Set empty to prevent stale data
                        
                        // Clear ChiefComplaint only if copy toggle is ON
                        if (CopyStudyRemarkToChiefComplaint)
                        {
                            ChiefComplaint = string.Empty;
                        }
                        
                        return;
                    }
                }
            }
        }
        
        private async Task AcquirePatientRemarkAsync()
        {
            const int maxRetries = 3;
            const int delayMs = 200;
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Debug.WriteLine($"[Automation][GetPatientRemark] Attempt {attempt}/{maxRetries} - Starting acquisition");
                    
                    // Small delay before attempt to allow PACS UI to stabilize
                    if (attempt > 1)
                    {
                        Debug.WriteLine($"[Automation][GetPatientRemark] Waiting {delayMs * attempt}ms before retry...");
                        await Task.Delay(delayMs * attempt);
                    }
                    
                    var s = await _pacs.GetCurrentPatientRemarkAsync();
                    Debug.WriteLine($"[Automation][GetPatientRemark] Raw result from PACS: '{s}'");
                    Debug.WriteLine($"[Automation][GetPatientRemark] Result length: {s?.Length ?? 0} characters");
                    
                    // Success check: if we got non-empty content, accept it
                    if (!string.IsNullOrEmpty(s))
                    {
                        Debug.WriteLine($"[Automation][GetPatientRemark] SUCCESS on attempt {attempt}");
                        
                        // Remove duplicate lines based on text between < and >
                        var originalLength = s.Length;
                        s = RemoveDuplicateLinesInPatientRemark(s);
                        Debug.WriteLine($"[Automation][GetPatientRemark] After deduplication: length {originalLength} -> {s.Length}");
                        
                        PatientRemark = s;
                        Debug.WriteLine($"[Automation][GetPatientRemark] Set PatientRemark property: '{PatientRemark}'");
                        SetStatus($"Patient remark captured ({s.Length} chars)");
                        return; // Success - exit retry loop
                    }
                    else
                    {
                        Debug.WriteLine($"[Automation][GetPatientRemark] Attempt {attempt} returned empty/null");
                        if (attempt < maxRetries)
                        {
                            Debug.WriteLine($"[Automation][GetPatientRemark] Will retry...");
                            continue; // Try again
                        }
                        else
                        {
                            Debug.WriteLine($"[Automation][GetPatientRemark] Max retries reached - accepting empty result");
                            PatientRemark = string.Empty;
                            SetStatus("Patient remark empty after retries");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Automation][GetPatientRemark] Attempt {attempt} EXCEPTION: {ex.GetType().Name} - {ex.Message}");
                    Debug.WriteLine($"[Automation][GetPatientRemark] StackTrace: {ex.StackTrace}");
                    
                    if (attempt < maxRetries)
                    {
                        Debug.WriteLine($"[Automation][GetPatientRemark] Will retry after exception...");
                        continue; // Try again
                    }
                    else
                    {
                        Debug.WriteLine($"[Automation][GetPatientRemark] Max retries reached after exception");
                        SetStatus("Patient remark capture failed after retries", true);
                        PatientRemark = string.Empty; // Set empty to prevent stale data
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Removes duplicate lines from patient remark text.
        /// Lines are considered duplicate if the text wrapped within angle brackets (&lt; and &gt;) is the same.
        /// </summary>
        private static string RemoveDuplicateLinesInPatientRemark(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            var lines = input.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();

            foreach (var line in lines)
            {
                // Extract text between < and >
                var key = ExtractAngleBracketContent(line);
                
                // If we haven't seen this key before, or if line has no angle bracket content, keep it
                if (string.IsNullOrEmpty(key) || seen.Add(key))
                {
                    result.Add(line);
                }
            }

            return string.Join("\n", result);
        }

        /// <summary>
        /// Extracts the text content wrapped within angle brackets (&lt; and &gt;).
        /// Returns empty string if no angle bracket content is found.
        /// </summary>
        private static string ExtractAngleBracketContent(string line)
        {
            if (string.IsNullOrEmpty(line)) return string.Empty;

            int startIndex = line.IndexOf('<');
            int endIndex = line.IndexOf('>');

            // Both brackets must exist and be in correct order
            if (startIndex >= 0 && endIndex > startIndex)
            {
                return line.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
            }

            return string.Empty;
        }
    }
}
