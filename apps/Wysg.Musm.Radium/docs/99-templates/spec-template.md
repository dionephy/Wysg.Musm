# Feature Specification Template

**Date**: 2025-11-11  
**Type**: Template  
**Category**: Documentation  
**Status**: ? Active Template

---

## Summary

This template provides a standardized structure for writing feature specifications. It focuses on **WHAT** users need and **WHY**, avoiding implementation details. The template includes mandatory sections, optional sections, and AI-generation guidelines.

---

## Purpose

### Why Use This Template?
- **Consistency** - Ensures all feature specs have the same structure
- **Clarity** - Separates business requirements from technical implementation
- **Completeness** - Includes checklists to prevent missing critical information
- **AI-Friendly** - Contains guidelines for AI-assisted specification generation

### When to Use
- Creating new feature specifications
- Documenting feature requests from stakeholders
- Planning major enhancements or new capabilities

---

## Template Structure

### Metadata Block
```markdown
# Feature Specification: [FEATURE NAME]

**Feature Branch**: `[###-feature-name]`  
**Created**: [DATE]  
**Status**: Draft | In Review | Approved | Implemented  
**Input**: User description: "$ARGUMENTS"
```

### Execution Flow (main)
```
1. Parse user description from Input
   �� If empty: ERROR "No feature description provided"
2. Extract key concepts from description
   �� Identify: actors, actions, data, constraints
3. For each unclear aspect:
   �� Mark with [NEEDS CLARIFICATION: specific question]
4. Fill User Scenarios & Testing section
   �� If no clear user flow: ERROR "Cannot determine user scenarios"
5. Generate Functional Requirements
   �� Each requirement must be testable
   �� Mark ambiguous requirements
6. Identify Key Entities (if data involved)
7. Run Review Checklist
   �� If any [NEEDS CLARIFICATION]: WARN "Spec has uncertainties"
   �� If implementation details found: ERROR "Remove tech details"
8. Return: SUCCESS (spec ready for planning)
```

---

## Guidelines

### Quick Guidelines
- ? **Focus on WHAT** - What users need and why
- ? **Avoid HOW** - No tech stack, APIs, code structure
- ?? **Audience** - Written for business stakeholders, not developers

### Section Requirements
- **Mandatory sections**: Must be completed for every feature
- **Optional sections**: Include only when relevant to the feature
- **When a section doesn't apply**: Remove it entirely (don't leave as "N/A")

### For AI Generation
When creating specifications from user prompts:

1. **Mark all ambiguities** - Use `[NEEDS CLARIFICATION: specific question]` for any assumption
2. **Don't guess** - If prompt doesn't specify something, mark it
3. **Think like a tester** - Every vague requirement should fail the "testable" checklist
4. **Common underspecified areas**:
   - User types and permissions
   - Data retention/deletion policies
   - Performance targets and scale
   - Error handling behaviors
   - Integration requirements
   - Security/compliance needs

---

## Mandatory Sections

### User Scenarios & Testing

#### Primary User Story
[Describe the main user journey in plain language]

**Example**:
> "As a radiologist, I want to quickly access previous study reports so I can compare findings without leaving the current report editor."

#### Acceptance Scenarios
1. **Given** [initial state], **When** [action], **Then** [expected outcome]
2. **Given** [initial state], **When** [action], **Then** [expected outcome]

**Example**:
> 1. **Given** a patient with 3 previous studies, **When** user clicks "Load Previous Studies", **Then** system displays a list of all 3 studies ordered by date (newest first)

#### Edge Cases
- What happens when [boundary condition]?
- How does system handle [error scenario]?

**Example**:
> - What happens when patient has no previous studies?
> - How does system handle network timeout when fetching studies?

---

### Requirements

#### Functional Requirements
- **FR-001**: System MUST [specific capability]
- **FR-002**: System MUST [specific capability]
- **FR-003**: Users MUST be able to [key interaction]
- **FR-004**: System MUST [data requirement]
- **FR-005**: System MUST [behavior]

**Example of marking unclear requirements**:
- **FR-006**: System MUST authenticate users via [NEEDS CLARIFICATION: auth method not specified - email/password, SSO, OAuth?]
- **FR-007**: System MUST retain user data for [NEEDS CLARIFICATION: retention period not specified]

#### Non-Functional Requirements (Optional)
- **NFR-001**: System MUST respond within [performance target]
- **NFR-002**: System MUST support [scalability target]
- **NFR-003**: System MUST comply with [regulatory requirement]

---

## Optional Sections

### Key Entities (if feature involves data)
- **[Entity 1]**: [What it represents, key attributes without implementation]
- **[Entity 2]**: [What it represents, relationships to other entities]

**Example**:
> - **PreviousStudy**: Represents a historical radiology study for the same patient (attributes: study date, modality, findings text, conclusion text, study ID)
> - **ComparisonField**: Represents a comparison statement between current and previous study (relationship: references one PreviousStudy)

### Dependencies
- **Depends on**: [Other features, services, or systems]
- **Blocks**: [Features that can't proceed without this]

### Assumptions
- [Assumption 1 about system state, user behavior, or environment]
- [Assumption 2]

---

## Review & Acceptance Checklist

### Content Quality
- [ ] No implementation details (languages, frameworks, APIs)
- [ ] Focused on user value and business needs
- [ ] Written for non-technical stakeholders
- [ ] All mandatory sections completed

### Requirement Completeness
- [ ] No [NEEDS CLARIFICATION] markers remain
- [ ] Requirements are testable and unambiguous
- [ ] Success criteria are measurable
- [ ] Scope is clearly bounded
- [ ] Dependencies and assumptions identified

---

## Execution Status

*Updated during processing*

- [ ] User description parsed
- [ ] Key concepts extracted
- [ ] Ambiguities marked
- [ ] User scenarios defined
- [ ] Requirements generated
- [ ] Entities identified
- [ ] Review checklist passed

---

## Usage Examples

### Good Specification
```markdown
**FR-001**: System MUST display a list of previous studies when user clicks "Previous Studies" button
**FR-002**: Each study in the list MUST show: study date, modality, and first 50 characters of findings
**FR-003**: User MUST be able to select a study from the list to view full report
```

### Bad Specification (Too Technical)
```markdown
? FR-001: System must use REST API to fetch previous studies from Azure SQL database
? FR-002: Frontend must implement lazy loading with React hooks for performance
```

### Ambiguous Specification (Needs Clarification)
```markdown
?? FR-001: System must display recent studies [NEEDS CLARIFICATION: how many? how "recent"?]
?? FR-002: System must load quickly [NEEDS CLARIFICATION: define "quickly" - 1s? 5s?]
```

---

## Related Documents

- [Plan Template](plan-template.md) - For implementation planning
- [Tasks Template](tasks-template.md) - For task tracking
- [Architecture Documentation](../11-architecture/) - For technical design

---

## Tips & Best Practices

### Do's ?
- ? Use concrete, measurable acceptance criteria
- ? Include examples for clarity
- ? Mark all ambiguities explicitly
- ? Write in user-centric language
- ? Keep requirements atomic (one testable thing per requirement)

### Don'ts ?
- ? Don't specify technology choices
- ? Don't include implementation details
- ? Don't use vague terms like "fast", "easy", "intuitive" without definition
- ? Don't assume unstated requirements
- ? Don't mix multiple requirements into one

---

## Changelog

### 2025-11-11 - Standardization
- Added metadata block
- Added comprehensive sections
- Added usage examples
- Added tips and best practices
- Improved AI generation guidelines

---

**Last Updated**: 2025-11-25  
**Template Version**: 2.0  
**Maintained By**: Documentation Team
