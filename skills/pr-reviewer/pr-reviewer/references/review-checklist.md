# PR Review Checklist: Errors & Edge Cases

## 1. Logic & Edge Cases
- [ ] **Roslyn Nulls**: Is every `GetDeclaredSymbol` or `GetSymbolInfo` result checked for null?
- [ ] **Generic Depth**: Does the return type unwrapping handle nested generics like `Task<ActionResult<IEnumerable<T>>>`?
- [ ] **Partial Classes**: If a controller is split across files, does the analyzer process it correctly without duplicates?
- [ ] **Minimal APIs**: Does the logic assume `ControllerBase`? (TestWire v0.2.0 does, so mark as "unsupported" if encountered).
- [ ] **Record Types**: Are DTOs defined as `record` handled correctly for property extraction?

## 2. Bad Logic & Code Quality
- [ ] **String vs Type**: Are we using string comparisons for types when we should be using `ITypeSymbol` comparisons?
- [ ] **Redundant Loops**: Is the code traversing the entire SyntaxTree multiple times when a single pass or SemanticModel lookup would suffice?
- [ ] **Missing Disposables**: Are `MSBuildWorkspace` or other IDisposable resources leaked?

## 3. Test Integrity
- [ ] **Regression Tests**: Does the PR include a test case that would have failed *before* the fix?
- [ ] **Generator Accuracy**: If the generator changed, did the sample outputs in the tests get updated?

## 4. Error Handling
- [ ] **MSBuild Failures**: Does the CLI report meaningful errors if the project fails to compile or MSBuild isn't found?
- [ ] **Invalid User Input**: Does `GenerateCommand` validate that `--project` actually exists and is a `.csproj`?
