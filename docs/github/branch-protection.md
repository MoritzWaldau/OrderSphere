# Branch Protection & Required Checks (master)

These rules enforce the strategy described in [branching.md](branching.md) § "Branch Protection".
They are **repository settings** and cannot be enforced through committed files — therefore they are
documented here as a versioned ruleset (`branch-protection.json`) plus the command to apply it.

## Rules

- Pull request required for every merge, ≥ 1 approval, code-owner review (see `.github/CODEOWNERS`).
- Linear history (squash merge), no force pushes, no deletion of `master`.
- Stale reviews are dismissed on a new push; open review threads must be resolved.
- Required status checks (must be green, branch must be up to date):

  | Check (Context)               | Source |
  |-------------------------------|--------|
  | `Format`                      | ci.yml |
  | `Build & Test`                | ci.yml |
  | `Vulnerable Packages`         | ci.yml |
  | `Analyze (csharp)`            | codeql.yml |
  | `Dependency Review`           | dependency-review.yml |
  | `SonarCloud Code Analysis`    | SonarCloud app |

  > Context names are verified against the checks actually produced for the PR head commit.
  >
  > **Do not add as required:** `Validate PR title` (pr-title-lint.yml) runs via
  > `pull_request_target` and does not appear in the head commit's check runs — as a required check it
  > would stay permanently "pending" and block the merge. Likewise deliberately not merge-blocking:
  > `Trivy Filesystem Scan`, `Secret Scan`, `OpenSSF Scorecard` (external tool downloads can be flaky;
  > they still report into the Security tab).
  >
  > Remove the stale check: an earlier required check `build` (from the deleted build.yml) must be
  > removed from branch protection, otherwise it stays permanently "pending".

## Applying

Prerequisite: `gh` authenticated, admin rights on the repo.

```bash
gh api \
  --method POST \
  -H "Accept: application/vnd.github+json" \
  /repos/MoritzWaldau/OrderSphere/rulesets \
  --input docs/github/branch-protection.json
```

Updating an existing ruleset (determine the ID beforehand via `gh api /repos/MoritzWaldau/OrderSphere/rulesets`):

```bash
gh api --method PUT \
  -H "Accept: application/vnd.github+json" \
  /repos/MoritzWaldau/OrderSphere/rulesets/<RULESET_ID> \
  --input docs/github/branch-protection.json
```

## Environments

- `dev` — no approval gate (automatic/manual dev deploy).
- `staging`, `production` — when building out Track D, add **Required Reviewers** and possibly a
  wait timer (Settings → Environments). Production deploys run only after manual approval.
