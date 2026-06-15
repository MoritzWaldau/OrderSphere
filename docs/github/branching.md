# Git Branching & Deployment Strategy

## Goal

This strategy defines the development, test, and release process for OrderSphere. It follows the
GitHub Flow model: one permanent branch, short-lived working branches, environments through
versioned artifacts rather than through branches.

Objectives:

* Isolate development work cleanly
* Provide stable test environments
* Perform safe production releases
* Enable fast rollbacks to previous versions

---

# Branch structure

## Permanent branch

| Branch   | Description                                                  |
| -------- | ------------------------------------------------------------ |
| `master` | The only permanent branch. Deployable at any time, always green. |

`master` is the single integration line. There are no `develop`, `staging`, or `release/*`
branches. Environments are served via CI/CD from `master` and SemVer tags
(see [Environments](#environments)).

## Short-lived branches

| Branch type | Base     | Purpose                     |
| ----------- | -------- | --------------------------- |
| `feature/*` | `master` | Development of new features |
| `bugfix/*`  | `master` | Bug fixes                   |
| `hotfix/*`  | `master` | Critical production defects |

Short-lived branches are branched off `master` and merged back into `master` via pull request after
review. They live only as long as the associated work.

---

# Development process

## Feature development

Create a new feature:

```bash
git switch master
git pull
git switch -c feature/user-management
```

Workflow:

1. Create a feature branch from `master`
2. Carry out the development
3. Pull request into `master`
4. Code review and mandatory CI (build + tests green)
5. Squash merge into `master`
6. Manual deployment to the development environment (workflow "Deploy OrderSphere" via
   `workflow_dispatch`) — deliberately manual for cost control, not automatic on merge

Because `master` is the only integration point, back-merge cascades between several permanent
branches are eliminated entirely.

---

# Environments

Environments are not mapped via branches but via `master` plus versioned, immutable artifacts. A
SemVer tag (`vX.Y.Z`) is the release trigger.

| Trigger               | Action                                                                  |
| --------------------- | ---------------------------------------------------------------------- |
| Merge into `master`   | Build + tests (CI). Dev deployment is manual (see below)               |
| Manual dev deploy     | Workflow "Deploy OrderSphere" (`workflow_dispatch`) deploys master to Development |
| Tag `v*` (SemVer)     | Produce versioned artifact (Docker `ordersphere/*:vX.Y.Z`, ZIP)        |
| Promotion Staging     | Deploy the tagged artifact to Staging                                  |
| Promotion Production  | Deploy the tagged artifact to Production, behind an approval gate      |

Core principle: production deployments never come directly from a branch. Only a previously produced,
versioned artifact is deployed.

```mermaid
gitGraph
    commit id: "master"
    branch feature/x
    commit
    commit
    checkout master
    merge feature/x
    commit id: "v1.5.0" tag: "v1.5.0"
    branch hotfix/login
    commit
    checkout master
    merge hotfix/login
    commit id: "v1.5.1" tag: "v1.5.1"
```

Promotion chain per release tag:

```text
Tag vX.Y.Z
  ↓ produce artifact
Development (from master, manual trigger)
  ↓ promotion
Staging
  ↓ approval gate
Production
```

---

# Release process

A release is a tag on `master`, not a separate branch.

```bash
git switch master
git pull
git tag -a v1.5.0 -m "Release 1.5.0"
git push origin v1.5.0
```

The tag triggers artifact creation. The artifact is then promoted to Staging and — after sign-off
via the approval gate — to Production. Versions follow SemVer (`MAJOR.MINOR.PATCH`).

---

# Deployment strategy

## Core principle

Production deployments never come directly from a branch. A versioned artifact is created for every
release.

### ZIP artifacts

```text
ordersphere-1.5.0.zip
ordersphere-1.5.1.zip
```

### Docker images

```text
ordersphere/<service>:v1.5.0
ordersphere/<service>:v1.5.1
```

Deployment process:

```text
Commit on master
  ↓
Build
  ↓
Tag vX.Y.Z
  ↓
Create artifact
  ↓
Archive artifact
  ↓
Promotion (Staging → Production)
```

This makes every release reproducible at any time.

---

# Rollback strategy

## Goal

On a faulty deployment, it must be possible to revert to the last stable version within a few
minutes.

## Procedure

Currently running version:

```text
v1.6.0
```

If errors occur:

```text
Rollback → Redeploy v1.5.0
```

Important:

* No git revert
* No new build
* No code changes

Only the last successful artifact is deployed again.

---

# Hotfix process

For critical defects in production:

```bash
git switch master
git pull
git switch -c hotfix/login-crash
```

Workflow:

```text
master
 └─ hotfix/login-crash
       ↓ Pull Request + Review
master
       ↓ Patch tag (vX.Y.Z+1)
Artifact → Staging → Production
```

Because `master` is the only permanent branch, the fix automatically becomes part of the next
development and release line on merge into `master`. A separate back-merge cascade into further
branches is not required.

---

# Branch Protection

Rules for `master`:

* Pull request required for every merge
* No direct pushes to `master`
* At least 1 approval
* Required status checks: build and tests must be green
* Linear history via squash merge

---

# CI/CD rules

| Trigger             | Deployment / action                       |
| ------------------- | ----------------------------------------- |
| Merge into `master` | CI (build + tests); no auto-deploy        |
| Manual trigger      | Dev deploy via `workflow_dispatch`        |
| Tag `v*`            | Produce release artifact                  |
| Promotion           | Staging, then Production (gate)           |

The manual DEV path is implemented by the workflow **`Deploy OrderSphere`**
(`.github/workflows/release-deploy.yml`, `workflow_dispatch` with a `version` input). A single run
performs all three release steps together: it writes the SemVer into `<VersionPrefix>`
(`Directory.Build.props`), commits the bump to `master` and creates the tag `vX.Y.Z`, then
provisions and deploys the tagged revision to DEV via `azd`. Staging/Production promotion is not
yet wired (those environments do not exist). Setup prerequisites (GitHub Environment `dev`,
`RELEASE_PAT`, repo variables) are documented in
[../deploy-ordersphere.md](../deploy-ordersphere.md#cicd--deploy-ordersphere-workflow).

---

# Best practices

* Use pull requests for all merges
* Avoid direct commits to `master`
* Merge and delete short-lived branches promptly
* Tag every production version (SemVer)
* Version build artifacts
* Perform rollback via artifacts, not via git revert
* Log production deployments
* Enable the approval gate before Production

---

# Summary

GitHub Flow with one permanent branch:

```text
master   ← single trunk, deployable at any time

feature/*
bugfix/*
hotfix/*
```

Environments via tags and artifacts:

```text
Tag vX.Y.Z → artifact → Development → Staging → Production (approval gate)
```

Benefits:

* One clear integration point, no back-merge cascades
* Controlled, tag-based release process
* Reproducible deployments via versioned artifacts
* Fast rollbacks via the last successful artifact
* Low overhead for a small team and a single solution
