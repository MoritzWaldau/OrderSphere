# Git Branching & Deployment Strategy

## Ziel

Diese Strategie definiert den Entwicklungs-, Test- und Release-Prozess für OrderSphere. Sie folgt
dem GitHub-Flow-Modell: ein dauerhafter Branch, kurzlebige Arbeitsbranches, Umgebungen über
versionierte Artefakte statt über Branches.

Ziele:

* Entwicklungsarbeiten sauber isolieren
* Stabile Testumgebungen bereitstellen
* Sichere Produktiv-Releases durchführen
* Schnelle Rollbacks auf vorherige Versionen ermöglichen

---

# Branch-Struktur

## Dauerhafter Branch

| Branch   | Beschreibung                                                  |
| -------- | ------------------------------------------------------------ |
| `master` | Einziger dauerhafter Branch. Jederzeit deploybar, immer grün. |

`master` ist die einzige Integrationslinie. Es gibt keine `develop`-, `staging`- oder
`release/*`-Branches. Umgebungen werden über CI/CD aus `master` und SemVer-Tags bedient
(siehe [Umgebungen](#umgebungen)).

## Kurzlebige Branches

| Branch-Typ  | Basis    | Zweck                       |
| ----------- | -------- | --------------------------- |
| `feature/*` | `master` | Entwicklung neuer Features  |
| `bugfix/*`  | `master` | Fehlerbehebungen            |
| `hotfix/*`  | `master` | Kritische Produktionsfehler |

Kurzlebige Branches werden von `master` abgezweigt und nach Review per Pull Request wieder nach
`master` zusammengeführt. Sie leben nur so lange wie die zugehörige Arbeit.

---

# Entwicklungsprozess

## Feature-Entwicklung

Neues Feature erstellen:

```bash
git switch master
git pull
git switch -c feature/user-management
```

Workflow:

1. Feature-Branch von `master` erstellen
2. Entwicklung durchführen
3. Pull Request nach `master`
4. Code Review und Pflicht-CI (Build + Tests grün)
5. Squash-Merge nach `master`
6. Automatisches Deployment auf die Development-Umgebung

Da `master` der einzige Integrationspunkt ist, entfallen Rückmerge-Kaskaden zwischen mehreren
dauerhaften Branches vollständig.

---

# Umgebungen

Umgebungen werden nicht über Branches abgebildet, sondern über `master` plus versionierte,
immutable Artefakte. Ein SemVer-Tag (`vX.Y.Z`) ist der Release-Auslöser.

| Auslöser              | Aktion                                                                  |
| --------------------- | ---------------------------------------------------------------------- |
| Merge nach `master`   | Build und automatisches Deployment nach Development                     |
| Tag `v*` (SemVer)     | Versioniertes Artefakt erzeugen (Docker `ordersphere/*:vX.Y.Z`, ZIP)    |
| Promotion Staging     | Getaggtes Artefakt nach Staging deployen                                |
| Promotion Production   | Getaggtes Artefakt nach Production deployen, hinter einem Approval-Gate |

Grundprinzip: Produktivdeployments erfolgen niemals direkt aus einem Branch. Es wird ausschließlich
ein zuvor erzeugtes, versioniertes Artefakt deployed.

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

Promotion-Kette pro Release-Tag:

```text
Tag vX.Y.Z
  ↓ Artefakt erzeugen
Development (aus master automatisch)
  ↓ Promotion
Staging
  ↓ Approval-Gate
Production
```

---

# Release-Prozess

Ein Release ist ein Tag auf `master`, kein eigener Branch.

```bash
git switch master
git pull
git tag -a v1.5.0 -m "Release 1.5.0"
git push origin v1.5.0
```

Der Tag löst die Artefakt-Erzeugung aus. Das Artefakt wird anschließend nach Staging und – nach
Abnahme über das Approval-Gate – nach Production promotet. Versionen folgen SemVer
(`MAJOR.MINOR.PATCH`).

---

# Deployment-Strategie

## Grundprinzip

Produktivdeployments erfolgen niemals direkt aus einem Branch. Für jeden Release wird ein
versioniertes Artefakt erstellt.

### ZIP-Artefakte

```text
ordersphere-1.5.0.zip
ordersphere-1.5.1.zip
```

### Docker Images

```text
ordersphere/<service>:v1.5.0
ordersphere/<service>:v1.5.1
```

Deployment-Prozess:

```text
Commit auf master
  ↓
Build
  ↓
Tag vX.Y.Z
  ↓
Artefakt erstellen
  ↓
Artefakt archivieren
  ↓
Promotion (Staging → Production)
```

Dadurch ist jedes Release jederzeit reproduzierbar.

---

# Rollback-Strategie

## Ziel

Bei einem fehlerhaften Deployment muss innerhalb weniger Minuten auf die letzte stabile Version
zurückgewechselt werden können.

## Vorgehensweise

Aktuell laufende Version:

```text
v1.6.0
```

Falls Fehler auftreten:

```text
Rollback → Redeploy v1.5.0
```

Wichtig:

* Kein Git Revert
* Kein neuer Build
* Keine Codeänderungen

Es wird lediglich das letzte erfolgreiche Artefakt erneut deployed.

---

# Last Successful Release

Die CI/CD-Pipeline speichert die zuletzt erfolgreiche Produktivversion.

| Version | Commit  | Status  |
| ------- | ------- | ------- |
| v1.6.0  | 9a1bcde | Failed  |
| v1.5.0  | 84ab123 | Success |
| v1.4.2  | 1fa234c | Success |

Im Fehlerfall wird automatisch oder manuell ausgeführt:

```text
Redeploy Last Successful Release
```

---

# Hotfix-Prozess

Für kritische Fehler in Produktion:

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
       ↓ Patch-Tag (vX.Y.Z+1)
Artefakt → Staging → Production
```

Da `master` der einzige dauerhafte Branch ist, wird der Fix mit dem Merge nach `master`
automatisch Teil der nächsten Development- und Release-Linie. Eine separate Rückmerge-Kaskade in
weitere Branches ist nicht erforderlich.

---

# Branch Protection

Regeln für `master`:

* Pull Request für jeden Merge erforderlich
* Keine direkten Pushes auf `master`
* Mindestens 1 Approval
* Required Status Checks: Build und Tests müssen grün sein
* Lineare Historie über Squash-Merge

---

# CI/CD-Regeln

| Auslöser            | Deployment / Aktion              |
| ------------------- | -------------------------------- |
| Merge nach `master` | Automatisch nach Development      |
| Tag `v*`            | Release-Artefakt erzeugen         |
| Promotion           | Staging, dann Production (Gate)   |

---

# Best Practices

* Pull Requests für alle Merges verwenden
* Direkte Commits auf `master` vermeiden
* Kurzlebige Branches zeitnah mergen und löschen
* Jede Produktivversion taggen (SemVer)
* Build-Artefakte versionieren
* Rollback über Artefakte durchführen, nicht über Git Revert
* Produktionsdeployments protokollieren
* Approval-Gate vor Production aktivieren

---

# Zusammenfassung

GitHub Flow mit einem dauerhaften Branch:

```text
master   ← einziger Trunk, jederzeit deploybar

feature/*
bugfix/*
hotfix/*
```

Umgebungen über Tags und Artefakte:

```text
Tag vX.Y.Z → Artefakt → Development → Staging → Production (Approval-Gate)
```

Vorteile:

* Ein klarer Integrationspunkt, keine Rückmerge-Kaskaden
* Kontrollierter, tag-basierter Release-Prozess
* Reproduzierbare Deployments über versionierte Artefakte
* Schnelle Rollbacks über das letzte erfolgreiche Artefakt
* Geringer Overhead bei kleinem Team und einer Solution
