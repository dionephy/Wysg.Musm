# Implementation Plan Template

**Date**: 2025-11-11  
**Type**: Template  
**Category**: Documentation  
**Status**: ? Active Template

---

## Summary

This template provides a standardized structure for creating implementation plans. It guides the transition from feature specifications to executable tasks, following a constitutional development approach with research, design, and validation phases.

---

## Purpose

### Why Use This Template?
- **Systematic Planning** - Structured approach from spec to implementation
- **Research-Driven** - Resolves unknowns before committing to designs
- **Constitutional Compliance** - Built-in checks for simplicity and best practices
- **TDD-Ready** - Generates contract tests before implementation
- **AI-Friendly** - Designed for AI-assisted planning and code generation

### When to Use
- Planning implementation after feature spec approval
- Breaking down large features into tasks
- Documenting technical decisions and research
- Ensuring compliance with project constitution

---

## Template Structure

### Metadata Block
```markdown
# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]`  
**Date**: [DATE]  
**Spec**: [link to feature specification]  
**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`
```

### Execution Flow
```
1. Load feature spec from Input path
   �� If not found: ERROR "No feature spec at {path}"
2. Fill Technical Context (scan for NEEDS CLARIFICATION)
   �� Detect Project Type from context (web=frontend+backend, mobile=app+api)
   �� Set Structure Decision based on project type
3. Fill the Constitution Check section based on the content of the constitution document.
4. Evaluate Constitution Check section below
   �� If violations exist: Document in Complexity Tracking
   �� If no justification possible: ERROR "Simplify approach first"
   �� Update Progress Tracking: Initial Constitution Check
5. Execute Phase 0 �� research.md
   �� If NEEDS CLARIFICATION remain: ERROR "Resolve unknowns"
6. Execute Phase 1 �� contracts, data-model.md, quickstart.md, agent files
7. Re-evaluate Constitution Check section
   �� If new violations: Refactor design, return to Phase 1
   �� Update Progress Tracking: Post-Design Constitution Check
8. Plan Phase 2 �� Describe task generation approach (DO NOT create tasks.md)
9. STOP - Ready for /tasks command
```

**IMPORTANT**: The /plan command STOPS at step 8. Phases 2-4 are executed by other commands:
- Phase 2: /tasks command creates tasks.md
- Phase 3-4: Implementation execution (manual or via tools)

---

## Template Sections

### 1. Summary
[Extract from feature spec: primary requirement + technical approach from research]

**Example**:
> "Implement previous study comparison feature. Users can load, view, and compare findings from past radiology reports. Technical approach: Load studies from Azure SQL, display in sortable list, enable side-by-side comparison view."

---

### 2. Technical Context

**Language/Version**: [e.g., C# 12 / .NET 8, Python 3.11, Swift 5.9 or NEEDS CLARIFICATION]  
**Primary Dependencies**: [e.g., WPF, FastAPI, UIKit or NEEDS CLARIFICATION]  
**Storage**: [if applicable, e.g., Azure SQL, PostgreSQL, CoreData, files or N/A]  
**Testing**: [e.g., xUnit, pytest, XCTest or NEEDS CLARIFICATION]  
**Target Platform**: [e.g., Windows 10+, Linux server, iOS 15+, WASM or NEEDS CLARIFICATION]  
**Project Type**: [single/web/mobile - determines source structure]  
**Performance Goals**: [domain-specific, e.g., 1000 req/s, <100ms response, 60 fps or NEEDS CLARIFICATION]  
**Constraints**: [domain-specific, e.g., <200ms p95, <100MB memory, offline-capable or NEEDS CLARIFICATION]  
**Scale/Scope**: [domain-specific, e.g., 10k users, 1M LOC, 50 screens or NEEDS CLARIFICATION]

**Example**:
> **Language/Version**: C# 12 / .NET 8  
> **Primary Dependencies**: WPF, AvalonEdit, Azure SDK  
> **Storage**: Azure SQL Database  
> **Testing**: xUnit, Moq  
> **Target Platform**: Windows 10+ (64-bit)  
> **Project Type**: Single (Desktop application)  
> **Performance Goals**: <500ms to load previous studies list, <2s to load full report  
> **Constraints**: Must work offline with cached studies, <50MB memory per study  
> **Scale/Scope**: 100+ studies per patient typical, 10k+ active users

---

### 3. Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Example Checks**:
- [ ] **Simplicity**: No unnecessary abstractions (e.g., avoid Repository pattern unless needed for multiple data sources)
- [ ] **Project Limit**: Maximum 3 projects unless justified
- [ ] **Dependency Management**: Use standard libraries, avoid exotic dependencies
- [ ] **Testing**: Contract tests for all public APIs
- [ ] **Documentation**: Quickstart guide for key user flows

[Gates determined based on constitution file in your project]

**If Violations Exist**: Document in Complexity Tracking section with justification

---

### 4. Project Structure

#### Documentation (this feature)
```
specs/[###-feature]/
������ plan.md              # This file (/plan command output)
������ research.md          # Phase 0 output (/plan command)
������ data-model.md        # Phase 1 output (/plan command)
������ quickstart.md        # Phase 1 output (/plan command)
������ contracts/           # Phase 1 output (/plan command)
������ tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

#### Source Code (repository root)

**Option 1: Single project (DEFAULT)**
```
src/
������ models/          # Data entities
������ services/        # Business logic
������ cli/             # Command-line interface (if applicable)
������ lib/             # Shared utilities

tests/
������ contract/        # API contract tests
������ integration/     # End-to-end tests
������ unit/            # Unit tests
```

**Option 2: Web application** (when "frontend" + "backend" detected)
```
backend/
������ src/
��   ������ models/
��   ������ services/
��   ������ api/
������ tests/

frontend/
������ src/
��   ������ components/
��   ������ pages/
��   ������ services/
������ tests/
```

**Option 3: Mobile + API** (when "iOS/Android" detected)
```
api/
������ [same as backend above]

ios/ or android/
������ [platform-specific structure]
```

**Structure Decision**: [DEFAULT to Option 1 unless Technical Context indicates web/mobile app]

---

### 5. Phase 0: Outline & Research

#### Objectives
1. **Extract unknowns from Technical Context** above
2. **Generate and dispatch research agents**
3. **Consolidate findings** in `research.md`

#### Process
```
For each "NEEDS CLARIFICATION" in Technical Context:
  �� Research task: "Research {unknown} for {feature context}"

For each technology choice:
  �� Research task: "Find best practices for {tech} in {domain}"

For each integration requirement:
  �� Research task: "Identify patterns for {integration} in {context}"
```

#### Output Format (research.md)
For each research item:
- **Decision**: [what was chosen]
- **Rationale**: [why chosen]
- **Alternatives Considered**: [what else evaluated]
- **Trade-offs**: [pros/cons of decision]

**Output**: research.md with all NEEDS CLARIFICATION resolved

---

### 6. Phase 1: Design & Contracts

*Prerequisites: research.md complete*

#### 6.1 Data Model (data-model.md)
Extract entities from feature spec:
- Entity name, fields, relationships
- Validation rules from requirements
- State transitions if applicable

**Example**:
```markdown
### Entity: PreviousStudy
**Fields**:
- StudyId (string, required, unique)
- PatientId (string, required, indexed)
- StudyDate (DateTime, required)
- Modality (string, required)
- FindingsText (string, nullable)
- ConclusionText (string, nullable)

**Relationships**:
- BelongsTo: Patient (many-to-one)
- HasMany: ComparisonFields (one-to-many)

**Validation**:
- StudyDate must be <= current date
- FindingsText max 10,000 characters
```

#### 6.2 API Contracts (contracts/)
Generate API contracts from functional requirements:
- For each user action �� endpoint/method
- Use standard REST/GraphQL/Command patterns
- Output OpenAPI/GraphQL schema or C# interfaces

**Example** (C# interface):
```csharp
public interface IPreviousStudyService
{
    Task<List<PreviousStudy>> GetStudiesForPatientAsync(string patientId);
    Task<PreviousStudy> GetStudyByIdAsync(string studyId);
    Task SaveStudyAsync(PreviousStudy study);
}
```

#### 6.3 Contract Tests
Generate contract tests from contracts:
- One test file per endpoint/interface
- Assert request/response schemas
- **Tests must fail** (no implementation yet)

**Example**:
```csharp
[Fact]
public async Task GetStudiesForPatient_ValidPatientId_ReturnsStudyList()
{
    // Arrange
    var patientId = "12345";
    
    // Act
    var studies = await _service.GetStudiesForPatientAsync(patientId);
    
    // Assert
    Assert.NotNull(studies);
    Assert.All(studies, s => Assert.Equal(patientId, s.PatientId));
}
```

#### 6.4 Quickstart Guide (quickstart.md)
Extract test scenarios from user stories:
- Each story �� integration test scenario
- Quickstart test = story validation steps

**Example**:
```markdown
## Quickstart: Load Previous Studies

1. **Setup**: Create test patient with 3 previous studies
2. **Action**: User clicks "Load Previous Studies" button
3. **Verify**: List displays 3 studies ordered by date (newest first)
4. **Action**: User selects second study
5. **Verify**: Full report appears in comparison view
```

#### 6.5 Agent Context Update
Update agent file incrementally (e.g., `.github/copilot-instructions.md`):
- Add NEW tech from current plan only
- Preserve manual additions between markers
- Update recent changes (keep last 3)
- Keep under 150 lines for token efficiency

**Output**: data-model.md, /contracts/*, failing tests, quickstart.md, agent file

---

### 7. Phase 2: Task Planning Approach

*This section describes what the /tasks command will do - DO NOT execute during /plan*

#### Task Generation Strategy
- Load `.specify/templates/tasks-template.md` as base
- Generate tasks from Phase 1 design docs (contracts, data model, quickstart)
- Each contract �� contract test task [P]
- Each entity �� model creation task [P]
- Each user story �� integration test task
- Implementation tasks to make tests pass

#### Ordering Strategy
- **TDD order**: Tests before implementation
- **Dependency order**: Models before services before UI
- **Mark [P] for parallel execution** (independent files)

#### Estimated Output
25-30 numbered, ordered tasks in tasks.md

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

---

### 8. Phase 3+: Future Implementation

*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)  
**Phase 4**: Implementation (execute tasks.md following constitutional principles)  
**Phase 5**: Validation (run tests, execute quickstart.md, performance validation)

---

## Additional Sections

### Complexity Tracking

*Fill ONLY if Constitution Check has violations that must be justified*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |

**Example**:
| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Repository pattern | Multi-source data (Azure SQL + local cache) | Direct DB access can't handle offline mode |

---

### Progress Tracking

*This checklist is updated during execution flow*

**Phase Status**:
- [ ] Phase 0: Research complete (/plan command)
- [ ] Phase 1: Design complete (/plan command)
- [ ] Phase 2: Task planning complete (/plan command - describe approach only)
- [ ] Phase 3: Tasks generated (/tasks command)
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [ ] Initial Constitution Check: PASS
- [ ] Post-Design Constitution Check: PASS
- [ ] All NEEDS CLARIFICATION resolved
- [ ] Complexity deviations documented

---

## Related Documents

- [Spec Template](spec-template.md) - For feature specifications
- [Tasks Template](tasks-template.md) - For task tracking
- [Constitution](../../memory/constitution.md) - Project principles (if applicable)

---

## Tips & Best Practices

### Do's ?
- ? Resolve all NEEDS CLARIFICATION in Phase 0
- ? Generate contract tests that fail (TDD approach)
- ? Document design decisions in research.md
- ? Keep Phase 1 focused on design, not implementation
- ? Re-run Constitution Check after design

### Don'ts ?
- ? Don't create tasks.md during /plan (that's /tasks command)
- ? Don't skip Phase 0 research
- ? Don't violate constitution without documented justification
- ? Don't write implementation code during planning
- ? Don't assume technology choices without research

---

## Changelog

### 2025-11-11 - Standardization
- Added metadata block and summary
- Added comprehensive examples for each section
- Enhanced Phase 1 details with concrete examples
- Added Tips & Best Practices
- Improved formatting and structure

---

**Last Updated**: 2025-11-25  
**Template Version**: 2.0  
**Maintained By**: Documentation Team  
**Based On**: Constitution v2.1.1
