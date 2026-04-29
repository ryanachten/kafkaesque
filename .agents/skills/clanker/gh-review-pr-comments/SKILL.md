---
name: gh-review-pr-comments
description: Reviews PR comments for validity, addresses valid concerns by making code changes, and returns a summary of invalid comments with explanations for why they should not be addressed.
---

# GitHub Review PR Comments

This skill automates the process of reviewing comments on a Pull Request, determining their validity, and addressing valid concerns while providing feedback on invalid comments.

## Workflow

1. **Fetch PR Comments**: Retrieve all comments for a given PR.
   ```bash
   gh pr view [PR_NUMBER] --comments
   # Alternative: Use the mcp_gitkraken_pull_request_get_comments tool
   ```

2. **Analyze Comment Validity**: For each comment, evaluate whether it:
   - Identifies a legitimate bug, design flaw, or improvement opportunity
   - Suggests changes that align with the codebase's goals and conventions
   - Provides clear, actionable guidance
   - Does not contradict existing requirements or architecture decisions
   - Is not subjective stylistic preference (unless it violates project standards)
   - Can be reasonably addressed within the scope of the PR

3. **Categorize Comments**:
   - **Valid**: Comments that identify real issues to fix or improvements to make
   - **Invalid**: Comments that are:
     - Subjective opinions with no clear standard to enforce
     - Out of scope for the current PR
     - Based on misunderstandings of the implementation
     - Conflicting with project requirements or previous decisions
     - Suggesting premature optimization or over-engineering
     - Already handled by existing code or future work

4. **Address Valid Comments**:
   - For each valid comment, make targeted code changes to resolve the concern
   - Update tests if necessary
   - Add clarifying comments in code if the issue relates to clarity or intent
   - Verify changes locally before committing

5. **Generate Summary Report**:
   - Create a comprehensive summary listing:
     - **Valid Comments Addressed**: Brief description of each fix and which comment prompted it
     - **Invalid Comments**: List each invalid comment with:
       - The comment text or summary
       - The reason it was deemed invalid
       - Any context or clarification provided to the author

6. **Reply to PR**: Optionally add a comment summarizing the work:
   ```bash
   gh pr comment [PR_NUMBER] --body "Summary of addressed and invalid comments..."
   ```

## Best Practices

- **Charitable Interpretation**: Assume good intent when reviewing comments. Interpret ambiguous feedback charitably.
- **Clear Reasoning**: When marking comments as invalid, provide specific, respectful reasons that help the reviewer understand your decision.
- **Scope Awareness**: Distinguish between issues that belong in this PR versus those that should be addressed in future work.
- **Communication**: If uncertain about a comment's validity, seek clarification from the comment author rather than immediately dismissing it.
- **Documentation**: Document your reasoning for complex decisions, especially when rejecting suggestions.
- **Consistency**: Apply the same standards for validity across all comments to maintain fairness.

## Example Invalid Comment Categories

- "Consider adding caching" (out of scope, potential premature optimization)
- "I prefer a different variable name" (subjective, not enforced by standards)
- "This might not work on older Node.js versions" (already addressed by version requirements)
- "Have you considered microservices?" (out of scope for PR)
