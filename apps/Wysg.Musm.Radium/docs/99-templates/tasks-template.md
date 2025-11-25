# Tasks Template

**Date**: 2025-11-11  
**Type**: Template  
**Category**: Documentation  
**Status**: ? Active Template

---

## Summary

This template provides a standardized structure for breaking down implementation plans into executable tasks. It follows Test-Driven Development (TDD) principles, ensuring tests are written before implementation, and supports parallel task execution for efficiency.

---

## Purpose

### Why Use This Template?
- **TDD Enforcement** - Tests written and failing before implementation
- **Parallel Execution** - Identifies tasks that can run simultaneously
- **Clear Dependencies** - Shows which tasks block others
- **Traceable Progress** - Numbered tasks with exact file paths
- **Validation** - Built-in checks for completeness

### When to Use
- After implementation plan (plan.md) is complete
- Before starting implementation
- When breaking down large features into work items
- For coordinating team work on parallel tasks

---

## Template Structure

### Metadata Block
```markdown
# Tasks: [FEATURE NAME]

**Branch**: `[###-feature-name]`  
**Date**: [DATE]  
**Input**: Design documents from `/specs/[###-feature-name]/`  
**Prerequisites**: plan.md (required), research.md, data-model.md, contracts/
```

### Execution Flow
```
1. Load plan.md from feature directory
   �� If not found: ERROR "No implementation plan found"
   �� Extract: tech stack, libraries, structure
2. Load optional design documents:
   �� data-model.md: Extract entities �� model tasks
   �� contracts/: Each file �� contract test task
   �� research.md: Extract decisions �� setup tasks
3. Generate tasks by category:
   �� Setup: project init, dependencies, linting
   �� Tests: contract tests, integration tests
   �� Core: models, services, CLI/UI commands
   �� Integration: DB, middleware, logging
   �� Polish: unit tests, performance, docs
4. Apply task rules:
   �� Different files = mark [P] for parallel
   �� Same file = sequential (no [P])
   �� Tests before implementation (TDD)
5. Number tasks sequentially (T001, T002...)
6. Generate dependency graph
7. Create parallel execution examples
8. Validate task completeness
9. Return: SUCCESS (tasks ready for execution)
```

---

## Task Format

### Standard Format
```
[ID] [P?] Description with exact file path
```

**Components**:
- **[ID]**: Sequential task number (T001, T002, etc.)
- **[P]**: Optional parallel marker (can run simultaneously with other [P] tasks)
- **Description**: What to do and where (include exact file path)

### Examples

**Good Tasks** (Specific, with paths):
```
? T008 [P] Create PreviousStudy model in src/models/PreviousStudy.cs
? T009 [P] Implement IPreviousStudyService in src/services/PreviousStudyService.cs
? T010 Add PreviousStudy repository in src/repositories/PreviousStudyRepository.cs
```

**Bad Tasks** (Vague, no paths):
```
? T008 Create model
? T009 Implement service
? T010 Add database stuff
```

---

## Path Conventions

### Single Project (DEFAULT)
```
src/
������ models/          # Domain entities
������ services/        # Business logic
������ repositories/    # Data access (if applicable)
������ views/           # UI components (WPF/XAML)
������ viewmodels/      # ViewModels (MVVM)
������ controls/        # Custom controls

tests/
������ contract/        # API/interface contract tests
������ integration/     # End-to-end tests
������ unit/            # Unit tests
```

### Web Application
```
backend/src/         # API implementation
frontend/src/        # UI implementation
```

### Mobile + API
```
api/src/             # Backend API
ios/src/ or android/src/  # Platform-specific
```

**Note**: Adjust paths based on Project Structure from plan.md

---

## Phase 3.1: Setup

**Objective**: Initialize project structure and dependencies

```markdown
- [ ] T001 Create project structure per implementation plan
- [ ] T002 Initialize C# project with .NET 8 and required NuGet packages
- [ ] T003 [P] Configure EditorConfig and code analysis rules
- [ ] T004 [P] Set up xUnit test project with Moq
```

**Example** (WPF Application):
```markdown
- [ ] T001 Create solution folders: src/, tests/, docs/
- [ ] T002 Initialize Wysg.Musm.Radium.csproj with .NET 8, WPF, AvalonEdit
- [ ] T003 [P] Configure .editorconfig with C# conventions
- [ ] T004 [P] Create Wysg.Musm.Radium.Tests.csproj with xUnit 2.6, Moq 4.20
```

---

## Phase 3.2: Tests First (TDD)

**?? CRITICAL: These tests MUST be written and MUST FAIL before ANY implementation**

### Contract Tests
Tests for public interfaces/APIs

```markdown
- [ ] T005 [P] Contract test IPreviousStudyService.GetStudiesForPatientAsync in tests/contract/PreviousStudyServiceContractTests.cs
- [ ] T006 [P] Contract test IPreviousStudyService.GetStudyByIdAsync in tests/contract/PreviousStudyServiceContractTests.cs
- [ ] T007 [P] Contract test IPreviousStudyService.SaveStudyAsync in tests/contract/PreviousStudyServiceContractTests.cs
```

### Integration Tests
Tests for complete user scenarios

```markdown
- [ ] T008 [P] Integration test: Load previous studies for patient in tests/integration/LoadPreviousStudiesTests.cs
- [ ] T009 [P] Integration test: Select and display previous study in tests/integration/DisplayPreviousStudyTests.cs
- [ ] T010 [P] Integration test: Compare current with previous study in tests/integration/CompareStudiesTests.cs
```

**Example Test** (Contract):
```csharp
[Fact]
public async Task GetStudiesForPatient_ValidPatientId_ReturnsStudyList()
{
    // Arrange
    var service = new PreviousStudyService(mockRepo.Object);
    var patientId = "12345";
    
    // Act
    var studies = await service.GetStudiesForPatientAsync(patientId);
    
    // Assert
    Assert.NotNull(studies);
    Assert.All(studies, s => Assert.Equal(patientId, s.PatientId));
}
```

---

## Phase 3.3: Core Implementation

**?? ONLY after tests are written and failing**

### Models
```markdown
- [ ] T011 [P] Create PreviousStudy model in src/models/PreviousStudy.cs
- [ ] T012 [P] Create ComparisonField model in src/models/ComparisonField.cs
- [ ] T013 [P] Create PreviousStudyTab ViewModel in src/viewmodels/PreviousStudyTab.cs
```

### Services
```markdown
- [ ] T014 Implement IPreviousStudyService in src/services/PreviousStudyService.cs
- [ ] T015 Add GetStudiesForPatientAsync method (makes T005 pass)
- [ ] T016 Add GetStudyByIdAsync method (makes T006 pass)
- [ ] T017 Add SaveStudyAsync method (makes T007 pass)
```

### UI Components (if applicable)
```markdown
- [ ] T018 Create PreviousStudiesPanel control in src/controls/PreviousStudiesPanel.xaml
- [ ] T019 Implement PreviousStudiesPanel code-behind in src/controls/PreviousStudiesPanel.xaml.cs
- [ ] T020 Add study list ComboBox to PreviousStudiesPanel
- [ ] T021 Add study display TextBox to PreviousStudiesPanel
```

### Validation & Error Handling
```markdown
- [ ] T022 Add input validation to PreviousStudyService
- [ ] T023 Add error handling and logging
- [ ] T024 Add null check guards
```

---

## Phase 3.4: Integration

**Objective**: Wire up components and dependencies

```markdown
- [ ] T025 Connect PreviousStudyService to Azure SQL repository
- [ ] T026 Register services in dependency injection container
- [ ] T027 Wire up PreviousStudiesPanel to MainViewModel
- [ ] T028 Add data binding for study list
- [ ] T029 Add event handlers for study selection
```

---

## Phase 3.5: Polish

**Objective**: Finalize quality and documentation

```markdown
- [ ] T030 [P] Unit tests for validation logic in tests/unit/PreviousStudyValidationTests.cs
- [ ] T031 [P] Performance tests: Load 100 studies <500ms in tests/performance/LoadStudiesPerformanceTests.cs
- [ ] T032 [P] Update quickstart.md with usage examples
- [ ] T033 Remove code duplication (DRY refactoring)
- [ ] T034 Run full test suite and verify all tests pass
- [ ] T035 Execute quickstart.md scenarios manually
```

---

## Dependencies

### Visual Dependency Graph
```
T001 (Setup)
  ��
T002-T004 [P] (Project init)
  ��
T005-T010 [P] (Tests - MUST FAIL)
  ��
T011-T013 [P] (Models)
  ��
T014 (Service interface)
  ��
T015-T017 (Service implementation)
  ��
T018-T021 (UI components)
  ��
T022-T024 (Validation)
  ��
T025-T029 (Integration)
  ��
T030-T035 [P] (Polish)
```

### Blocking Relationships
- **T005-T010** must FAIL before **T011-T024** (TDD enforcement)
- **T011-T013** (Models) must complete before **T014-T017** (Services use models)
- **T014** (Service interface) must exist before **T015-T017** (Implementation)
- **T018-T021** (UI) requires **T014-T017** (Services)
- **T025-T029** (Integration) requires all core components
- **T030-T035** (Polish) is final phase

---

## Parallel Execution Examples

### Example 1: Contract Tests (T005-T007)
```bash
# All test different methods, no conflicts
dotnet test --filter "PreviousStudyServiceContractTests.GetStudiesForPatientAsync" &
dotnet test --filter "PreviousStudyServiceContractTests.GetStudyByIdAsync" &
dotnet test --filter "PreviousStudyServiceContractTests.SaveStudyAsync" &
wait
```

### Example 2: Models (T011-T013)
```bash
# All create different files, no conflicts
code src/models/PreviousStudy.cs &
code src/models/ComparisonField.cs &
code src/viewmodels/PreviousStudyTab.cs &
```

### Example 3: Polish (T030-T032)
```bash
# Different file types, no conflicts
# Developer 1: Unit tests
# Developer 2: Performance tests
# Developer 3: Documentation
```

---

## Task Generation Rules

### From Contracts
```
For each interface method:
  �� Contract test task [P]
  �� Implementation task (sequential)
```

### From Data Model
```
For each entity:
  �� Model creation task [P]
  
For each relationship:
  �� Service layer task (handles relationship)
```

### From User Stories
```
For each user story:
  �� Integration test task [P]
  
For quickstart scenarios:
  �� Validation task (execute scenario)
```

### Ordering Rules
1. **Setup** �� **Tests** �� **Models** �� **Services** �� **UI** �� **Integration** �� **Polish**
2. **Tests before implementation** (TDD)
3. **Dependencies block parallel execution**
4. **Different files** �� Can be [P]
5. **Same file** �� Must be sequential

---

## Validation Checklist

*GATE: Check before starting implementation*

- [ ] All interface methods have contract tests
- [ ] All entities have model creation tasks
- [ ] All tests come BEFORE implementation
- [ ] Parallel tasks ([P]) truly independent (different files)
- [ ] Each task specifies exact file path
- [ ] No [P] task modifies same file as another [P] task
- [ ] Dependencies documented and blocking relationships clear
- [ ] Quickstart scenarios have validation tasks

---

## Related Documents

- [Spec Template](spec-template.md) - For feature specifications
- [Plan Template](plan-template.md) - For implementation plans
- [Implementation Plan](../specs/[feature]/plan.md) - Specific to this feature

---

## Tips & Best Practices

### Do's ?
- ? Write tests FIRST (they must fail)
- ? Use exact file paths in task descriptions
- ? Mark truly independent tasks with [P]
- ? Commit after each task completion
- ? Run tests frequently during implementation
- ? Update this file as tasks complete

### Don'ts ?
- ? Don't implement before tests fail
- ? Don't mark dependent tasks as [P]
- ? Don't use vague descriptions without paths
- ? Don't modify same file in parallel tasks
- ? Don't skip validation checklist

---

## Progress Tracking

### Phase Completion
- [ ] Phase 3.1: Setup (T001-T004)
- [ ] Phase 3.2: Tests First (T005-T010) - **MUST FAIL**
- [ ] Phase 3.3: Core Implementation (T011-T024)
- [ ] Phase 3.4: Integration (T025-T029)
- [ ] Phase 3.5: Polish (T030-T035)

### Quality Gates
- [ ] All contract tests written and failing
- [ ] All integration tests written and failing
- [ ] All tests passing after implementation
- [ ] No code duplication
- [ ] Quickstart scenarios executed successfully
- [ ] Performance targets met

---

## Changelog

### 2025-11-11 - Standardization
- Added metadata block and summary
- Enhanced with C#/.NET examples
- Added visual dependency graph
- Added parallel execution examples
- Added progress tracking section
- Improved formatting and structure

---

**Last Updated**: 2025-11-25  
**Template Version**: 2.0  
**Maintained By**: Documentation Team
