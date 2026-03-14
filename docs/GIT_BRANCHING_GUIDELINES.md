# Git Branching & PR Guidelines (Recommended Company Standard)

Purpose: provide a lightweight, secure branching model that scales for teams and enforces quality controls.

1. Branching model
- Use short-lived feature branches off `main` (trunk-based flow). Keep branches focused and short-lived (hours–days, not weeks).
- Branch name pattern: `{type}/{scope}-{short-desc}` where `type` is one of `feature`, `fix`, `chore`, `hotfix`, `refactor`. Examples: `feature/uploadedfiledto-improvements`, `fix/login-nullref`.
- Avoid long-lived forks/branches; rebase or merge main frequently to reduce conflicts.

2. Protect `main`
- Require pull requests to merge into `main`.
- Enforce CI passing, code review (at least 1-2 reviewers), and passing static analysis checks before merge.
- Enable branch protection rules: disallow direct pushes to `main`, require status checks.

3. Pull requests
- Small, focused PRs with a clear description of intent, test impact, and migration steps if applicable.
- Link relevant issue/ticket and changelog entry.
- Provide reviewer checklist when applicable (security, API changes, migrations).

4. Commits & messages
- Use concise prefix-style commit messages: `type(scope): short description` (e.g., `feat(auth): add token refresh`).
- Squash or rebase to keep history linear if policy requires it. For feature branches, use `--no-ff` merges only if team prefers explicit merge commits.

5. Releases
- Use tags and release notes. Prefer automated release creation via CI/CD after merging to `main`.

6. CI/CD
- All PRs must run unit tests and relevant integration checks.
- Prefer pipelines that build artifacts and run linting, static analysis, and security scanners.

7. Code review & ownership
- Assign reviewers by area ownership. Rotate reviewers to avoid bottlenecks.
- Ensure API-level changes include migration notes and tests.

8. Hotfixes
- Create `hotfix/*` branches off the latest `main` tag or commit. Fast-track security/critical fixes with emergency process and backport to active release branches if needed.

9. Cleaning up
- Delete remote branches after merge to keep repository tidy.
- Periodically prune stale branches; automate alerts for branches older than X days.


Adopt these guidelines and adapt thresholds (reviewers, required checks) to team size and risk profile.
