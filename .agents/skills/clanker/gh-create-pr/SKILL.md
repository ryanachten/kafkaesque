---
name: gh-create-pr
description: Creates a GitHub Pull Request for the current branch using the `gh` CLI utility. Use this when the user wants to open a PR with a summarized description of their changes.
---

# GitHub Create PR

This skill automates the process of creating a Pull Request (PR) on GitHub using the `gh` CLI.

## Workflow

1. **Summarize Changes**: Gather the current branch's changes by comparing it with the base branch (usually `main` or `master`).
   ```bash
   git diff main...HEAD --stat
   ```
2. **Draft Title & Description**:
   - **Title**: A concise, imperative-style title (e.g., "feat: add user authentication").
   - **Description**: A high-quality summary of "what" changed and "why".
3. **Create PR**: Use `gh pr create` with the drafted title and description.
   ```bash
   gh pr create --title "Your Title" --body "Your Description"
   ```

## Best Practices

- **Base Branch**: Verify the base branch before creating the PR. Default to `main` unless specified otherwise.
- **Labels/Assignees**: Optionally ask the user if they'd like to add labels or assignees.
- **Draft Status**: If the work is still in progress, consider creating a draft PR:
  ```bash
  gh pr create --draft --title "Your Title" --body "Your Description"
  ```
