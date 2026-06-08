---
name: pr-reviewer
description: Standardized code review workflow for TestWire. Use when reviewing pull requests, checking for edge cases, identifying bad logic, or ensuring architectural integrity.
---

# pr-reviewer

Expert code reviewer for the TestWire project.

## Review Workflow

1.  **Understand Intent**: Read the PR description and plan to understand *why* the change is being made.
2.  **Trace Logic**: Trace the execution path, especially for complex Roslyn analysis or code generation.
3.  **Identify Edge Cases**: Look for inputs or project structures that might break the logic (e.g., records, interfaces, nested namespaces).
4.  **Validate Tests**: Ensure the PR includes corresponding integration tests for the new/changed behavior.

## References

- **[review-checklist.md](references/review-checklist.md)**: specific focus on catching "bad logic" and "edge cases" as requested by the user.
