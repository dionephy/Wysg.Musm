1. get current study
- patient information, study information -> title bar
- study information -> studyname
- studyname -> if no loinc mapping then open "StudynameLoincWindow" -> if modality == "MR" and no tech mapping then open "StudynameTechWindow" -> (RBM:tech parser) -> fill in Report.technique
- study remark + studyname + patient information -> (LLM:study remark parser) -> fill in Report.chief_complaint, Report.history_preview
- patient remark + studyname + patient information + Report.history_preview -> (LLM:patient remark parser) -> fill in Report.history


2. get prev study
- study information -> title bar
- study information -> studyname
- studyname -> if no loinc mapping then open "StudynameLoincWindow"
- header and findings -> fill PrevReport.header_and_findings
- conclusion -> fill PrevReport.conclusion
- header_and_findings -> (LLM:header findings splitter) -> split header (get PrevReport.split_index)
- splitted header -> (LLM:header parser) -> fill in PrevReport.chief_complaint, PrevReport.history, PrevReport.technique, PrevReport.comparison
- PrevReport.chief_complaint -> (LLM:proofreader) -> fill in PrevReport.chief_complaint_proofread
- PrevReport.history -> (LLM:proofreader) -> fill in PrevReport.history_proofread
- PrevReport.technique -> (LLM:proofreader) -> fill in PrevReport.technique_proofread
- PrevReport.comparison -> (LLM:proofreader) -> fill in PrevReport.comparison_proofread
- splitted findings -> (LLM:proofreader) -> fill in PrevReport.findings_proofread
- PrevReport.conclusion -> (LLM:proofreader) -> fill in PrevReport.conclusion_proofread
- study information -> edit Report.comparison for current study


3. edit current study header
- (optional) add technique for current study -> edit Report.technique
- (optional) select comparisons among prev studies -> edit Report.comparison
- (optional) manually edit chief_complaint and history -> edit Report.chief_complaint, Report.history


4. type in findings
- findings text box -> fill in Report.findings

Editor spec
1. Completion window: phrase, hotkey, snippet
2. Preform ghost: normal form, template, most common form (multiple, LLM)
3. Line ghost: single suggestion (LLM) (1 second after idle) : report whole을 training하면 가능할까? 그런데 그럴꺼면 refined report를 training하는게 더 낫지 않을까?
4. Import and change (LLM) from prev study findings (mouse right click)


5. postprocess
- Report.findings -> (LLM:conclusion generator) -> fill in Report.conclusion_preview
- Report.findings -> (LLM:proofreader) -> fill in Report.findings_proofread
- Report.conclusion_preview -> (LLM:proofreader) -> fill in Report.conclusion_proofread
- Report.chief_complaint -> (LLM:proofreader) -> Report.chief_complaint_proofread
- Report.history -> (LLM:proofreader) -> Report.history_proofread
- Report.technique -> (LLM:proofreader) -> Report.technique_proofread
- Report.comparison -> (LLM:proofreader) -> Report.comparison_proofread
- header -> (RBM:reportifier) -> header_reportified
- findings (Report.findings_proofread) -> (RBM:reportifier) -> findings_reportified
- conclusion (Report.conclusion_proofread) -> (RBM:reportifier) -> conclusion_reportified
- conclusion_reportified -> (RBM:numberer) -> conclusion_reportified_numbered
- open PACS worklist


6. send to PACS
- check if the banner info match current study info
- send header_reportified + findings_reportified to "Findings" field of PACS
- send conclusion_reportified_numbered to "Conclusion" field of PACS
- press "approve" button of PACS
- wait for PACS to confirm the report is sent
- after sent, get Report.header_findings, Report.conclusion, and study info
- save to local DB



Report json components:
- technique (string)
- chief_complaint (string)
- history_preview (string)
- chief_complaint_proofread (string)
- history (string)
- history_proofread (string)
- header_and_findings (string)
- conclusion (string)
- split_index (int)
- comparison (string)
- technique_proofread (string)
- comparison_proofread (string)
- findings_proofread (string)
- conclusion_proofread (string)
- findings (string)
- conclusion_preview (string)

to add
- study_remark (string)_


Abbreviations
RBM: rule-based model
LLM: large language model