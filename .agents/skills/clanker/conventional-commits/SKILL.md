---
name: conventional-commits
description: Formats commit messages following the Conventional Commits specification (v1.0.0) without the optional scope. Use this to write clear, structured commit messages that can be parsed by automated tools.
---

# Conventional Commits (No Scope)

This skill guides the formatting of commit messages according to the [Conventional Commits 1.0.0](https://www.conventionalcommits.org/) specification, with scope omitted for simplicity.

## Commit Message Format

```
<type>: <description>

[optional body]

[optional footer(s)]
```

## Commit Types

- **feat**: A new feature
- **fix**: A bug fix
- **docs**: Documentation changes (e.g., README, comments)
- **style**: Code style changes that don't affect functionality (e.g., formatting, missing semicolons)
- **refactor**: Code refactoring without feature changes or bug fixes
- **perf**: Performance improvements
- **test**: Adding or updating tests
- **chore**: Build process, dependencies, tooling changes (e.g., version bumps)
- **ci**: CI/CD configuration changes
- **revert**: Reverting a previous commit

## Examples

### Simple feature
```
feat: add user authentication
```

### Bug fix
```
fix: prevent race condition in request handling
```

### Documentation
```
docs: update installation instructions
```

### Breaking change (use `!` before colon)
```
feat!: remove legacy API endpoints

BREAKING CHANGE: The /api/v1/users endpoint has been removed. Use /api/v2/users instead.
```

### With body and footer
```
fix: correct spacing in email validation

The validation regex was incorrectly rejecting valid email addresses
with multiple dots in the domain name.

Fixes: #456
```

## Best Practices

- **Type**: Use lowercase, standard types from the list above. Stick to predefined types for consistency.
- **Description**: Write in imperative mood ("add" not "added"), keep it concise (50 chars max for title).
- **No Scope**: Omit the `(scope)` part entirely to keep messages clean.
- **Breaking Changes**: Use `!` after the type (e.g., `feat!:`) for breaking changes, optionally with a `BREAKING CHANGE:` footer.
- **Body**: Explain the "why" and provide context if needed. Separate from title with a blank line.
- **Footers**: Use trailers like `Fixes:`, `Closes:`, `Co-authored-by:` when relevant.

## Workflow

When committing:
1. Choose the appropriate type from the list above
2. Write a concise, imperative description (50 chars or less)
3. If needed, add a blank line and then a detailed body
4. If needed, add footers with references to issues or co-authors
5. Commit using the formatted message
