LLMs
1. proofreader
- input: parsed study remark (chief complaint); output: Report.chief_complaint
- input: parsed study remark (history); output: Report.history
- input: splitted and parsed header (chief complaint); output: Report.chief_complaint
- input: splitted and parsed header (history); output: Report.history
- input: splitted and parsed header (technique); output: Report.technique
- input: splitted and parsed header (comparison); output: Report.comparison
- input: splitted findings; output: Report.proofread_findings
- input: Report.conclusion; output: Report.proofread_conclusion
- input: typed in findings; output: Report.proofread_findings
- input: generated conclusion; output: Report.proofread_conclusion

2. study remark parser
- input: study remark, studyname, patient; output: Report.chief_complaint, Report.history_preview_




- study remark parser
- patient remark parser
- header findings splitter
- header parser
- most common preform
- 


RBMs
- tech parser




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