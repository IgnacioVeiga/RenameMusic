# Template Token Replacement Strategy

## Why this document exists
This note explains a specific bug that can silently generate wrong filenames:

- sequential token replacement (`Replace` in a loop) can cause cascading replacements
- metadata values that contain token-like text (for example `<Album>`) can be modified unintentionally

The current implementation in `TemplateRuleService` avoids that behavior.

## Problem summary
Given a template with multiple tokens, the old approach did this:

1. Start with the template string.
2. Replace one token.
3. Replace the next token on the already modified string.
4. Continue until all tokens are processed.

This is unsafe because replacement output from step `n` becomes input for step `n+1`.

## Concrete failure example
Inputs:

- Template: `"<Title> - <Album>"`
- Title: `"Live <Album>"`
- Album: `"Greatest"`

Old sequential algorithm:

1. Replace `<Title>` -> `"Live <Album> - <Album>"`
2. Replace `<Album>` -> `"Live Greatest - Greatest"`

Expected intent:

- Keep title text as-is.
- Only replace actual template tokens.

Expected final name before filename normalization:

- `"Live <Album> - Greatest"`

The old algorithm produced the wrong semantic result.

## Current solution
The fix uses a two-phase strategy:

1. Build a token-to-value map once, based on rename rules and missing-tag policy.
2. Apply replacements in a single pass over the original template using a token regex:
   - `<TrackNum>|<Title>|<Album>|<AlbumArtist>|<Artist>|<Year>`
   - each regex match is replaced from the map
   - inserted values are not scanned again as template tokens

Because replacements are resolved from original token matches only, no cascading occurs.

## Behavior with missing tags
The replacement map is built after rule checks:

- `Strict` mode:
  - required missing tokens are collected
  - evaluation returns `CanRename = false` before producing a final name
  - replacement value for missing tokens is still defined internally as empty to keep map logic simple
- `UsePlaceholder` mode:
  - missing required token value becomes placeholder text (for example `Unknown`)

This means the strategy preserves existing rename rules while fixing token replacement correctness.

## End-to-end example with current behavior
Inputs:

- Template: `"<Title> - <Album>"`
- Title: `"Live <Album>"`
- Album: `"Greatest"`

Single-pass token replacement result:

- `"Live <Album> - Greatest"`

After `FilenameFunctions.NormalizeFileName` (Windows invalid chars):

- `"Live _Album_ - Greatest"`

This final output is expected and tested.

## Reference in code
- Core implementation:
  - `RenameMusic.Core/Services/TemplateRuleService.cs`
- Regression test:
  - `RenameMusic.Tests/TemplateRuleServiceTests.cs`
  - test: `Evaluate_ShouldNotReplaceTagLikeTextInsideMetadataValue`
