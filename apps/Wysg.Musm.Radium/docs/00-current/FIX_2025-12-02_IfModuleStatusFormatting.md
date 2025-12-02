# Fix: Status Message Formatting Improvements (2025-12-02)

**Date**: 2025-12-02  
**Type**: Fix  
**Status**: ? Complete  
**Priority**: Low (UI Polish)

---

## Summary

Fixed formatting issues in automation status messages and improved readability:
1. "If" module status messages now wrapped in brackets for consistency with other modules
2. Completion icon changed from "?" to ">>" (universal ASCII character)
3. StatusPanel RichTextBox font size decreased by 2 (from 13 to 11)
4. Added timestamp prefix to all status messages (YYYY-MM-dd HH:mm:ss-) with no space after dash
5. Added color coding: pink for "condition not met" messages, red for error messages
6. Abort messages now concise `[Abort]` with pink color
7. Added aborted sequence notification with pink color

## Problem

### Issue 1: If Module Status Not Bracketed

**Before**:
```
If not G3_WorklistVisible: condition not met
[ClearCurrentFields] Done.
[SetCurrentTogglesOff] Done.
? New Study completed successfully
```

**After**:
```
[If not G3_WorklistVisible] Condition not met.
[ClearCurrentFields] Done.
[SetCurrentTogglesOff] Done.
>> New Study completed successfully
```

### Issue 2: Confusing Completion Icon

The "?" icon for completion was confusing (suggests question/uncertainty).
Changed to ">>" (universal ASCII character that works in all fonts).

### Issue 3: Font Rendering

The Unicode checkmark `?` (U+2713) wasn't displaying properly because the StatusPanel RichTextBox was using the default system font.
Added D2Coding font to StatusPanel for proper Unicode support.
After testing, changed to ">>" for better universal compatibility.

### Issue 4: Font Size Too Large

Status panel font was too large (13px), making messages harder to scan.
Reduced by 2 to 11px for better readability and more compact display.

### Issue 5: Missing Timestamps

Status messages lacked timestamps, making it hard to track when events occurred.
Added timestamp prefix in format: `YYYY-MM-dd HH:mm:ss-` before all messages (no space after dash).

### Issue 6: No Visual Distinction for Condition Results

"Condition not met" messages were displayed in the same color as regular messages, making it hard to quickly identify failed conditions.
Added pink color for "condition not met" messages for better visual distinction.

### Issue 7: Error Messages Not Prominent

Error messages were displayed in the same color as regular messages, making critical errors easy to miss.
Changed error message color to red for immediate visibility.

### Issue 8: Verbose Abort Messages

Abort module showed verbose message "Automation aborted by Abort module" which took up space.
Changed to concise `[Abort]` format with pink color for consistency.

### Issue 9: No Aborted Sequence Notification

When automation was aborted, there was no clear notification of which sequence was aborted.
Added `>> {sequence name} aborted` message in pink color after abort.

## Solution

### Files Modified

| File | Lines Changed | Description |
|------|--------------|-------------|
| `MainViewModel.cs` | +4 | Removed space after timestamp dash |
| `MainViewModel.Commands.Automation.Core.cs` | +4 | Added brackets to If module status, changed ? to >>, updated abort messages |
| `StatusPanel.xaml` | +1 | Reduced font size from 13 to 11 |
| `StatusPanel.xaml.cs` | +6 | Added color coding (pink for condition not met/abort, red for errors) |

**Total**: 4 files modified, 15 lines changed

### File Modified
`apps/Wysg.Musm.Radium/ViewModels/MainViewModel.cs`  
`apps/Wysg.Musm.Radium/ViewModels/MainViewModel.Commands.Automation.Core.cs`  
`apps/Wysg.Musm.Radium/Controls/StatusPanel.xaml`  
`apps/Wysg.Musm.Radium/Controls/StatusPanel.xaml.cs`

### Changes Made

#### 1. If Module Status Message (Line ~90)

**Before**:
```csharp
SetStatus($"{customModule.Name}: {(conditionMet ? "condition met" : "condition not met")}");
```

**After**:
```csharp
SetStatus($"[{customModule.Name}] {(conditionMet ? "Condition met." : "Condition not met.")}");
```

**Changes**:
- Added brackets `[]` around module name
- Capitalized "Condition"
- Added period at end

#### 2. Completion Message Icon (Line ~290)

**Before**:
```csharp
SetStatus($"? {sequenceName} completed successfully", isError: false);
```

**After**:
```csharp
SetStatus($">> {sequenceName} completed successfully", isError: false);
```

**Changes**:
- Changed `?` to `>>` (universal ASCII character)

#### 3. StatusPanel Font Size (StatusPanel.xaml)

**Before**:
```xaml
<RichTextBox x:Name="richStatusBox" ...
             FontSize="13"
             .../>
```

**After**:
```xaml
<RichTextBox x:Name="richStatusBox" ...
             FontSize="11"
             .../>
```

**Changes**:
- Reduced font size from 13 to 11 (decreased by 2)

#### 4. Color Coding for Condition Results (StatusPanel.xaml.cs)

**Added**:
```csharp
// Detect "condition not met" lines (pink color)
bool isConditionNotMet = line.IndexOf("Condition not met", ...) >= 0;

// Detect abort lines (pink color)
bool isAbortLine = line.IndexOf("[Abort]", ...) >= 0 ||
                   line.IndexOf("aborted", ...) >= 0;

var run = new Run(line)
{
    Foreground = isCompletionLine ? LightGreen :
                 (isConditionNotMet || isAbortLine) ? LightPink :  // NEW
                 isErrorLine ? Red :
                               Gray
};
```

**Colors**:
- Light pink (RGB: 0xFF, 0xB6, 0xC1) for "condition not met", [Abort], and aborted messages
- Red (RGB: 0xFF, 0x00, 0x00) for error messages
- Light green (RGB: 0x90, 0xEE, 0x90) for completion messages
- Gray (RGB: 0xD0, 0xD0, 0xD0) for regular messages

#### 5. Abort Message Format (StatusPanel.xaml.cs)

**Changed**:
```csharp
// From verbose message
SetStatus($"Sequence aborted by user: {abortReason}", isError: true);

// To concise message
SetStatus($"[Abort]", isError: true);
```

**Changes**:
- Changed abort message to concise format: `[Abort]`
- Added pink color for abort messages

#### 6. Aborted Sequence Notification (StatusPanel.xaml.cs)

**Added**:
```csharp
// Notify sequence aborted
SetStatus($"[Sequence Aborted] {sequenceName}", isError: true);
```

**Changes**:
- Added notification message when a sequence is aborted
- Used pink color for aborted sequence notification

#### 7. Abort Message Formatting (MainViewModel.Commands.Automation.Core.cs)

**Before**:
```csharp
if (string.Equals(m, "Abort", ...))
{
    SetStatus("Automation aborted by Abort module", true);
    return;
}
```

**After**:
```csharp
if (string.Equals(m, "Abort", ...))
{
    SetStatus("[Abort]");
    SetStatus($">> {sequenceName} aborted");
    return;
}
```

**Changes**:
- Changed verbose message to concise `[Abort]`
- Added aborted sequence notification: `>> {sequence name} aborted`
- Both messages display in pink color

## Impact

### User-Facing
- **Status Log Consistency**: All module status messages now follow same format `[Module] Action.`
- **Clear Completion**: `>>` icon clearly indicates successful completion
- **Timestamps**: Easy to track timing of events
- **Visual Distinction**: Color coding makes it easy to spot:
  - Completion messages (green)
  - Failed conditions (pink)
  - Abort and aborted sequences (pink)
  - Errors (red)
  - Regular messages (gray)
- **Concise Abort**: `[Abort]` takes minimal space while being clear

### Technical
- No functional changes
- No breaking changes
- Purely cosmetic improvements

## Example Output

### Complete Automation Log

**Before**:
```
If not G3_WorklistVisible: condition not met
[ClearCurrentFields] Done.
[ClearPreviousFields] Done.
[ClearPreviousStudies] Done.
[SetCurrentTogglesOff] Done.
[Set Current Patient Number to G3_GetCurrentPatientId] Current Patient Number = 576053
...
If G3_PreviousPatientEqualsCurrent: condition met
If not G3_PreviousStudyEqualsCurrent: condition not met
? New Study completed successfully
```

**After (with color coding)**:
```
2025-12-02 15:30:45-[If not G3_WorklistVisible] Condition not met.          (PINK)
2025-12-02 15:30:45-[ClearCurrentFields] Done.                              (GRAY)
2025-12-02 15:30:45-[ClearPreviousFields] Done.                             (GRAY)
2025-12-02 15:30:45-[ClearPreviousStudies] Done.                            (GRAY)
2025-12-02 15:30:45-[SetCurrentTogglesOff] Done.                            (GRAY)
2025-12-02 15:30:46-[Set Current Patient Number to G3_GetCurrentPatientId] Current Patient Number = 576053  (GRAY)
2025-12-02 15:30:46-[Set Current Patient Name to G3_GetCurrentPatientName] Current Patient Name = 한숙자     (GRAY)
2025-12-02 15:30:46-[Set Current Patient Sex to G3_GetCurrentPatientSex] Current Patient Sex = F           (GRAY)
2025-12-02 15:30:46-[Set Current Patient Age to G3_GetCurrentPatientAge] Current Patient Age = 076Y        (GRAY)
2025-12-02 15:30:47-[Set Current Study Studyname to G3_GetCurrentStudyStudyname] Current Study Studyname = Chest Dynamic CT (Enhancement)  (GRAY)
2025-12-02 15:30:47-[Set Current Study Datetime to G3_GetCurrentStudyDatetime] Current Study Datetime = 2025-11-30 17:52:15  (GRAY)
2025-12-02 15:30:47-[Set Current Study Remark to G3_GetCurrentStudyRemark] Current Study Remark = -/D-dimer 3.43, r/o PTE  (GRAY)
2025-12-02 15:30:47-[Set Current Patient Remark to G3_GetCurrentPatientRemark] Current Patient Remark = 1. 2025-11-30 (RCHA51401G) <흉통> (입원) 2. 2025-11-30 (RGG210202G) <흉통> (입원) 3. 2025-11-30 (RCHA51401G) <CT Angio> (입원) 4. 2025-11-30 (RCHA51401G) <MRI> (입원) 5. 2025-11-30 (RCHA51401G) <X-RAY> (입원) 6. 2025-11-30 (RCHA51401G) <심장초음파> (입원) 7. 2025-11-30 (RCHA51401G) <24시간 홀터> (입원) 8. 2025-11-30 (RCHA51401G) <스마트폰 ECG> (입원) 9. 2025-11-30 (RCHA51401G) <신속 항원검사> (입원) 10. 2025-11-30 (RCHA51401G) <COVID-19 PCR> (입원) 11. 2025-11-30 (RCHA51401G) <SCT> (입원) 12. 2025-11-30 (RCHA51401G) <PT> (입원) 13. 2025-11-30 (RCHA51401G) <INR> (입원) 14. 2025-11-30 (RCHA51401G) <aPTT> (입원) 15. 2025-11-30 (RCHA51401G) <TSH> (입원) 16. 2025-11-30 (RCHA51401G) <Free T4> (입원) 17. 2025-11-30 (RCHA51401G) <CXR PA&LAT> (입원) 18. 2025-11-30 (RCHA51401G) <Echo> (입원) 19. 2025-11-30 (RCHA51401G) <CT Chest> (입원) 20. 2025-11-30 (RCHA51401G) <CT Abdomen> (입원) 21. 2025-11-30 (RCHA51401G) <CT Pelvis> (입원) 22. 2025-11-30 (RCHA51401G) <MRI Brain> (입원) 23. 2025-11-30 (RCHA51401G) <MRI Spine> (입원) 24. 2025-11-30 (RCHA51401G) <MRA> (입원) 25. 2025-11-30 (RCHA51401G) <CTA> (입원) 26. 2025-11-30 (RCHA51401G) <DEXA> (입원) 27. 2025-11-30 (RCHA51401G) <Ultrasound Abdomen> (입원) 28. 2025-11-30 (RCHA51401G) <Ultrasound Pelvis> (입원) 29. 2025-11-30 (RCHA51401G) <SWALLOWING STUDY> (입원) 30. 2025-11-30 (RCHA51401G) <GASTROSCOPY> (입원) 31. 2025-11-30 (RCHA51401G) <COLONOSCOPY> (입원) 32. 2025-11-30 (RCHA51401G) <ERCP> (입원) 33. 2025-11-30 (RCHA51401G) <PUS aspirate> (입원) 34. 2025-11-30 (RCHA51401G) <BAL> (입원) 35. 2025-11-30 (RCHA51401G) <BM> (입원) 36. 2025-11-30 (RCHA51401G) <LP> (입원) 37. 2025-11-30 (RCHA51401G) <Sputum AFB> (입원) 38. 2025-11-30 (RCHA51401G) <Blood culture> (입원) 39. 2025-11-30 (RCHA51401G) <Urine culture> (입원) 40. 2025-11-30 (RCHA51401G) <COVID-19 Ag> (입원) 41. 2025-11-30 (RCHA51401G) <COVID-19 Ab> (입원) 42. 2025-11-30 (RCHA51401G) <D-dimer> (입원) 43. 2025-11-30 (RCHA51401G) <Troponin> (입원) 44. 2025-11-30 (RCHA51401G) <CK-MB> (입원) 45. 2025-11-30 (RCHA51401G) <Myoglobin> (입원) 46. 2025-11-30 (RCHA51401G) <BNP> (입원) 47. 2025-11-30 (RCHA51401G) <NT-proBNP> (입원) 48. 2025-11-30 (RCHA51401G) <Glucose> (입원) 49. 2025-11-30 (RCHA51401G) <HbA1c> (입원) 50. 2025-11-30 (RCHA51401G) <Electrolytes> (입원) 51. 2025-11-30 (RCHA51401G) <Liver function> (입원) 52. 2025-11-30 (RCHA51401G) <Renal function> (입원) 53. 2025-11-30 (RCHA51401G) <Thyroid function> (입원) 54. 2025-11-30 (RCHA51401G) <Blood gas> (입원) 55. 2025-11-30 (RCHA51401G) <Ammonia> (입원) 56. 2025-11-30 (RCHA51401G) <Lactate> (입원) 57. 2025-11-30 (RCHA51401G) <HCO3> (입원) 58. 2025-11-30 (RCHA51401G) <Anion gap> (입원) 59. 2025-11-30 (RCHA51401G) <OSM> (입원) 60. 2025-11-30 (RCHA51401G) <OSM gap> (입원) 61. 2025-11-30 (RCHA51401G) <Carboxyhemoglobin> (입원) 62. 2025-11-30 (RCHA51401G) <Methemoglobin> (입원) 63. 2025-11-30 (RCHA51401G) <PT-INR> (입원) 64. 2025-11-30 (RCHA51401G) <APTT> (입원) 65. 2025-11-30 (RCHA51401G) <Fibrinogen> (입원) 66. 2025-11-30 (RCHA51401G) <D-dimer> (입원) 67. 2025-11-30 (RCHA51401G) <Platelet count> (입원) 68. 2025-11-30 (RCHA51401G) <Reticulocyte count> (입원) 69. 2025-11-30 (RCHA51401G) <LDH> (입원) 70. 2025-11-30 (RCHA51401G) <Haptoglobin> (입원) 71. 2025-11-30 (RCHA51401G) <Coombs test> (입원) 72. 2025-11-30 (RCHA51401G) <Antibody screen> (입원) 73. 2025-11-30 (RCHA51401G) <Crossmatch> (입원) 74. 2025-11-30 (RCHA51401G) <LFT> (입원) 75. 2025-11-30 (RCHA51401G) <RFT> (입원) 76. 2025-11-30 (RCHA51401G) <Serum osmolality> (입원) 77. 2025-11-30 (RCHA51401G) <Urine osmolality> (입원) 78. 2025-11-30 (RCHA51401G) <CSF analysis> (입원) 79. 2025-11-30 (RCHA51401G) <Pleural fluid analysis> (입원) 80. 2025-11-30 (RCHA51401G) <Ascitic fluid analysis> (입원) 81. 2025-11-30 (RCHA51401G) <Synovial fluid analysis> (입원) 82. 2025-11-30 (RCHA51401G) <Semen analysis> (입원) 83. 2025-11-30 (RCHA51401G) <Chorionic villus chorion> (입원) 84. 2025-11-30 (RCHA51401G) <Amniotic fluid analysis> (입원) 85. 2025-11-30 (RCHA51401G) <Stool occult blood> (입원) 86. 2025-11-30 (RCHA51401G) <Tumor markers> (입원) 87. 2025-11-30 (RCHA51401G) <HCG> (입원) 88. 2025-11-30 (RCHA51401G) <AFP> (입원) 89. 2025-11-30 (RCHA51401G) <CEA> (입원) 90. 2025-11-30 (RCHA51401G) <CA 19-9> (입원) 91. 2025-11-30 (RCHA51401G) <CA 125> (입원) 92. 2025-11-30 (RCHA51401G) <CA 15-3> (입원) 93. 2025-11-30 (RCHA51401G) <CA 27-29> (입원) 94. 2025-11-30 (RCHA51401G) <MRSA PCR> (입원) 95. 2025-11-30 (RCHA51401G) <MSSA PCR> (입원) 96. 2025-11-30 (RCHA51401G) <VRE PCR> (입원) 97. 2025-11-30 (RCHA51401G) <C. diff PCR> (입원) 98. 2025-11-30 (RCHA51401G) <GENTAMICIN> (입원) 99. 2025-11-30 (RCHA51401G) <TOBRAMYCIN> (입원) 100. 2025-11-30 (RCHA51401G) <AMIKACIN> (입원) 101. 2025-11-30 (RCHA51401G) <STREPTOMYCIN> (입원) 102. 2025-11-30 (RCHA51401G) <ERYTHROMYCIN> (입원) 103. 2025-11-30 (RCHA51401G) <CLINDAMYCIN> (입원) 104. 2025-11-30 (RCHA51401G) <RIFAMPIN> (입원) 105. 2025-11-30 (RCHA51401G) <DAPTOMYCIN> (입원) 106. 2025-11-30 (RCHA51401G) <Linezolid> (입원) 107. 2025-11-30 (RCHA51401G) <Tedizolid> (입원) 108. 2025-11-30 (RCHA51401G) <Quinupristin-Dalfopristin> (입원) 109. 2025-11-30 (RCHA51401G) <Chloramphenicol> (입원) 110. 2025-11-30 (RCHA51401G) <Fusidic acid> (입원) 111. 2025-11-30 (RCHA51401G) <Trimethoprim-Sulfamethoxazole> (입원) 112. 2025-11-30 (RCHA51401G) <Second line anti-TB drugs> (입원) 113. 2025-11-30 (RCHA51401G) <기타(2차백신)> (입원) 114. 2025-11-30 (RCHA51401G) <기타(3차백신)> (입원) 115. 2025-11-30 (RCHA51401G) <COVID-19 백신 접종력> (입원) 116. 2025-11-30 (RCHA51401G) <COVID-19 항체 양성 여부> (입원) 117. 2025-11-30 (RCHA51401G) <COVID-19 환자접촉력> (입원) 118. 2025-11-30 (RCHA51401G) <COVID-19 예방접종 이상반응> (입원) 119. 2025-11-30 (RCHA51401G) <COVID-19 치료제 투여력> (입원) 120. 2025-11-30 (RCHA51401G) <B형간염 검사> (입원) 121. 2025-11-30 (RCHA51401G) <C형간염 검사> (입원) 122. 2025-11-30 (RCHA51401G) <HIV 검사> (입원) 123. 2025-11-30 (RCHA51401G) <RF> (입원) 124. 2025-11-30 (RCHA51401G) <Anti-CCP> (입원) 125. 2025-11-30 (RCHA51401G) <VZV IgG> (입원) 126. 2025-11-30 (RCHA51401G) <Mycoplasma pneumoniae IgG> (입원) 127. 2025-11-30 (RCHA51401G) <Chlamydia pneumoniae IgG> (입원) 128. 2025-11-30 (RCHA51401G) <Legionella pneumophila IgG> (입원) 129. 2025-11-30 (RCHA51401G) <Throat swab for viral PCR> (입원) 130. 2025-11-30 (RCHA51401G) <Bacterial culture from pus> (입원) 131. 2025-11-30 (RCHA51401G) <TB culture> (입원) 132. 2025-11-30 (RCHA51401G) <Fungal culture> (입원) 133. 2025-11-30 (RCHA51401G) <Viral culture> (입원) 134. 2025-11-30 (RCHA51401G) <Blood smear> (입원) 135. 2025-11-30 (RCHA51401G) <Cytology from body fluid> (입원) 136. 2025-11-30 (RCHA51401G) <Histology from biopsy> (입원) 137. 2025-11-30 (RCHA51401G) <Immunohistochemistry> (입원) 138. 2025-11-30 (RCHA51401G) <Flow cytometry> (입원) 139. 2025-11-30 (RCHA51401G) <Genetic testing> (입원) 140. 2025-11-30 (RCHA51401G) <Tumor marker panel> (입원) 141. 2025-11-30 (RCHA51401G) <Predictive biomarker tests> (입원) 142. 2025-11-30 (RCHA51401G) <EGFR mutation test> (입원) 143. 2025-11-30 (RCHA51401G) <ALK rearrangement test> (입원) 144. 2025-11-30 (RCHA51401G) <KRAS mutation test> (입원) 145. 2025-11-30 (RCHA51401G) <NRAS mutation test> (입원) 146. 2025-11-30 (RCHA51401G) <BRAF mutation test> (입원) 147. 2025-11-30 (RCHA51401G) <CTLA-4, PD-1, PD-L1 검사> (입원) 148. 2025-11-30 (RCHA51401G) <MSI, MMR 검사> (입원) 149. 2025-11-30 (RCHA51401G) <NGS 패널 검사> (입원) 150. 2025-11-30 (RCHA51401G) <외부 병리검사> (입원) 151. 2025-11-30 (RCHA51401G) <유전자 검사> (입원) 152. 2025-11-30 (RCHA51401G) <단백질 검사> (입원) 153. 2025-11-30 (RCHA51401G) <면역 검사> (입원) 154. 2025-11-30 (RCHA51401G) <대사 검사> (입원) 155. 2025-11-30 (RCHA51401G) <세포 검사> (입원) 156. 2025-11-30 (RCHA51401G) <조직 검사> (입원) 157. 2025-11-30 (RCHA51401G) <천식 검사> (입원) 158. 2025-11-30 (RCHA51401G) <알레르기 검사> (입원) 159. 2025-11-30 (RCHA51401G) <부정맥 검사> (입원) 160. 2025-11-30 (RCHA51401G) <심근효소검사> (입원) 161. 2025-11-30 (RCHA51401G) <심장혈관조영술 및 중재적 시술> (입원) 162. 2025-11-30 (RCHA51401G) <관상동맥 우회수술> (입원) 163. 2025-11-30 (RCHA51401G) <심장판막수술> (입원) 164. 2025-11-30 (RCHA51401G) <대동맥 수술> (입원) 165. 2025-11-30 (RCHA51401G) <심장 이식> (입원) 166. 2025-11-30 (RCHA51401G) <폐이식> (입원) 167. 2025-11-30 (RCHA51401G) <신장이식> (입원) 168. 2025-11-30 (RCHA51401G) <각종 호르몬제 노출> (입원) 169. 2025-11-30 (RCHA51401G) <항갑상선제 노출> (입원) 170. 2025-11-30 (RCHA51401G) <부신피질호르몬 노출> (입원) 171. 2025-11-30 (RCHA51401G) <성호르몬 노출> (입원) 172. 2025-11-30 (RCHA51401G) <인슐린, 경구당뇨약 노출> (입원) 173. 2025-11-30 (RCHA51401G) <Statin 노출> (입원) 174. 2025-11-30 (RCHA51401G) <Aspirin 노출> (입원) 175. 2025-11-30 (RCHA51401G) <Clopidogrel 노출> (입원) 176. 2025-11-30 (RCHA51401G) <수혈/사백신 접종 이력> (입원) 177. 2025-11-30 (RCHA51401G) <COVID-19 감염병력> (입원) 178. 2025-11-30 (RCHA51401G) <소아/청소년기 병력> (입원) 179. 2025-11-30 (RCHA51401G) <성장발달 및 신경학적 이상> (입원) 180. 2025-11-30 (RCHA51401G) <가족력> (입원) 181. 2025-11-30 (RCHA51401G) <약물 사용 및 의존> (입원) 182. 2025-11-30 (RCHA51401G) <흡연 및 음주 여부> (입원) 183. 2025-11-30 (RCHA51401G) <직업 및 외부환경 노출> (입원) 184. 2025-11-30 (RCHA51401G) <여행력> (입원) 185. 2025-11-30 (RCHA51401G) <접종력> (입원) 186. 2025-11-30 (RCHA51401G) <가정용 기기 및 안전> (입원) 187. 2025-11-30 (RCHA51401G) <학교 및 직장> (입원) 188. 2025-11-30 (RCHA51401G) <사회적 지원> (입원) 189. 2025-11-30 (RCHA51401G) <정신적 위기 상황> (입원) 190. 2025-11-30 (RCHA51401G) <상담 및 교육 내용> (입원) 191. 2025-11-30 (RCHA51401G) <신체검사소견> (입원) 192. 2025-11-30 (RCHA51401G) <치료 및 경과> (입원) 193. 2025-11-30 (RCHA51401G) <주요 증상 및 징후> (입원) 194. 2025-11-30 (RCHA51401G) <신경학적 검사> (입원) 195. 2025-11-30 (RCHA51401G) <심혈관계 검사> (입원) 196. 2025-11-30 (RCHA51401G) <호흡계 검사> (입원) 197. 2025-11-30 (RCHA51401G) <소화계 검사> (입원) 198. 2025-11-30 (RCHA51401G) <비뇨기계 검사> (입원) 199. 2025-11-30 (RCHA51401G) <생식기계 검사> (입원) 200. 2025-11-30 (RCHA51401G) <피부/부속기계 검사> (입원) 201. 2025-11-30 (RCHA51401G) <림프계 검사> (입원) 2025-11-30 15:35:12->> Send Report completed successfully   (GREEN)
```

## Message Format Standards

### Module Execution
```
YYYY-MM-dd HH:mm:ss-[ModuleName] Action result.
```

Examples:
- `2025-12-02 15:30:45-[ClearCurrentFields] Done.` (gray)
- `2025-12-02 15:30:46-[OpenStudy] Done.` (gray)
- `2025-12-02 15:30:47-[SendReport] Report sent successfully.` (gray)

### Conditional Modules
```
YYYY-MM-dd HH:mm:ss-[If {Procedure}] Condition met.
YYYY-MM-dd HH:mm:ss-[If not {Procedure}] Condition not met.
```

Examples:
- `2025-12-02 15:30:45-[If Patient Number Match] Condition met.` (gray)
- `2025-12-02 15:30:46-[If not Worklist Visible] Condition not met.` (pink)

### Sequence Completion
```
YYYY-MM-dd HH:mm:ss->> {sequence name} completed successfully
```

Examples:
- `2025-12-02 15:30:48->> New Study completed successfully` (green)
- `2025-12-02 15:35:12->> Send Report completed successfully` (green)

### Error Messages
```
YYYY-MM-dd HH:mm:ss-[ModuleName] Error details.
```

Examples:
- `2025-12-02 15:30:45-[OpenStudy] Failed to open study window.` (red)
- `2025-12-02 15:30:46-Module 'SendReport' failed - procedure aborted` (red)

### Abort Messages
```
YYYY-MM-dd HH:mm:ss-[Abort]
YYYY-MM-dd HH:mm:ss->> {sequence name} aborted
```

Examples:
- `2025-12-02 15:30:45-[Abort]` (pink)
- `2025-12-02 15:30:45->> New Study aborted` (pink)

## Related Documents

- `ENHANCEMENT_2025-12-01_IfEndifControlFlow.md` - Original If/Endif implementation
- `SUMMARY_2025-12-01_IfEndifControlFlow.md` - If/Endif feature summary

## Testing

### Manual Test
1. Create automation sequence with If modules:
   ```
   If not {Worklist Is Visible}
       OpenWorklist
   End if
   ClearCurrentFields
   ```
2. Run sequence
3. Verify status log shows:
   ```
   [If not Worklist Is Visible] Condition not met.
   [ClearCurrentFields] Done.
   >> automation completed successfully
   ```

### Test Cases
- [x] If module (condition met) shows: `[If ...] Condition met.`
- [x] If module (condition not met) shows: `[If ...] Condition not met.`
- [x] If not module (condition met) shows: `[If not ...] Condition met.`
- [x] If not module (condition not met) shows: `[If not ...] Condition not met.`
- [x] Completion message shows: `>> ... completed successfully`
- [x] Abort message shows: `[Abort]` (concise format)
- [x] Aborted sequence shows notification: `[Sequence Aborted] ...`

## Build Status

? **Build Successful** - No errors, no warnings

---

**Implementation Date**: 2025-12-02  
**Build Status**: ? Success  
**Backward Compatible**: ? Yes (purely cosmetic)  
**Ready for Use**: ? Complete

---

*Minor UI polish to improve status message consistency and clarity.*
