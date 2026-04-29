---
name: gh-fix-ci
description: Fixes CI failures for a given PR by analyzing GitHub Action workflow logs and applying necessary fixes. Use when a PR has failing checks and you need to diagnose and resolve them using the GitHub CLI.
---

# GitHub Fix CI

This skill automates the process of diagnosing and fixing CI failures in GitHub Actions for a specific Pull Request.

## Workflow

1. **Identify Failing Checks**: List the status of checks for the current or specified PR.
   ```bash
   gh pr checks [PR_NUMBER]
   ```
2. **Retrieve Failed Logs**: Get the logs for the failed steps in the most recent workflow run associated with the PR.
   ```bash
   # Find the run ID from the PR checks or run list
   gh run list --branch [BRANCH_NAME] --limit 1 --status failure
   # View failed logs for a specific run
   gh run view [RUN_ID] --log-failed
   ```
3. **Analyze and Reproduce**:
   - Analyze the logs to identify the root cause (e.g., linting error, failing test, compilation error).
   - Reproduce the failure locally using the appropriate project commands (e.g., `npm test`, `dotnet test`).
4. **Apply Fixes**:
   - Apply surgical fixes to resolve the identified issues.
   - Verify the fix locally by running the same commands that failed in CI.
5. **Verify in CI**:
   - Push the changes to the PR branch and monitor the CI status.
   - If failures persist, repeat the diagnosis process.

## Best Practices

- **Surgical Fixes**: Focus exclusively on fixing the CI failure without introducing unrelated changes.
- **Local Verification**: Always attempt to reproduce and verify the fix locally before pushing to GitHub.
- **Log Analysis**: Look for specific error messages or stack traces in the `--log-failed` output.
- **Rerun Jobs**: If a failure seems transient (e.g., network issue), consider rerunning the failed jobs.
  ```bash
  gh run rerun [RUN_ID] --failed
  ```
