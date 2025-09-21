I’m GitHub Copilot.

Implemented what is feasible now:
	1.	Repository (Npgsql)
		•	Added contracts and implementation:
			•	Services/IStudynameLoincRepository.cs
			•	GetStudynamesAsync, EnsureStudynameAsync
			•	GetPartsAsync (from loinc.part)
			•	GetMappingsAsync, SaveMappingsAsync (to med.rad_studyname_loinc_part)
		•	Services/StudynameLoincRepository.cs
			•	Uses IRadiumLocalSettings.LocalConnectionString (fallback to CentralConnectionString)
			•	Save is delete+insert in a transaction
		•	Registered in DI.
	2.	Wire the window VM
		•	ViewModels/StudynameLoincViewModel.cs
			•	Now injected with repository
			•	Loads Studynames on start
			•	Preselect(studyname) ensures row in med.rad_studyname and selects it
			•	Reloads parts from loinc.part and current mappings from med.rad_studyname_loinc_part
			•	Tracks selection and PartSequenceOrder (default “A”)
			•	Save maps SelectedParts to repository
		•	Views/StudynameLoincWindow.xaml
			•	Shows studynames list, parts grouped, with a ComboBox for PartSequenceOrder next to each checkbox
		•	Views/StudynameLoincWindow.xaml.cs
			•	Uses DI to resolve StudynameLoincViewModel so repo is injected
			•	Open(preselectStudyname) keeps preselect behavior
	3.	Hook entry points
		•	The “Manage studyname” button already opens the window.
		•	You can call StudynameLoincWindow.Open(studyname) from New Study/Add Study flow after you ensure the studyname is inserted (left to your PACS wiring).
	4.	Finish UI details
		•	Added PartSequenceOrder editing (A/B/C/D) per selected part.
		•	Mapping preview shows selected items.

Docs updated
	•	Spec.md: Added PartSequenceOrder column and exclusive tab behavior notes.
	•	Plan.md: Marked repository/SQL work for mapping table and Manage studyname done; listed remaining wiring.
	•	Tasks.md: Marked repo integration and UI pieces done; left DB wiring tasks and tests.

What you need to configure
	•	Set IRadiumLocalSettings.LocalConnectionString (or CentralConnectionString) to a DB that contains:
		•	med.rad_studyname
		•	loinc.part
		•	med.rad_studyname_loinc_part
	•	Run apps/Wysg.Musm.Radium/Data/med.studyname_loinc_part.sql to create the mapping table.

Next optional steps
	•	Wire New Study/Add Study to call StudynameLoincWindow.Open with the ensured studyname.
	•	Optionally add search/filter for parts and larger sequence set.
	•	Add repository unit tests under tests/Wysg.Musm.Tests once a test DB is available.