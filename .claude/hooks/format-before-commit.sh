#!/bin/sh
# Claude Code PreToolUse hook: auto-formats staged C# files before any `git commit`
# Bash call, so the .githooks/pre-commit format gate cannot fail Claude-driven commits.
#
# Receives Claude Code's PreToolUse JSON payload on stdin. Only acts when the Bash
# command being run contains "git commit"; otherwise no-ops immediately.

set -e

payload=$(cat)

command=$(printf '%s' "$payload" | grep -o '"command"[[:space:]]*:[[:space:]]*"[^"]*"' | head -n1 | sed 's/^"command"[[:space:]]*:[[:space:]]*"//; s/"$//')

case "$command" in
    *"git commit"*) ;;
    *) exit 0 ;;
esac

if ! command -v dotnet >/dev/null 2>&1; then
    exit 0
fi

staged_cs=$(git diff --cached --name-only --diff-filter=ACMR -- '*.cs')

if [ -z "$staged_cs" ]; then
    exit 0
fi

dotnet format OrderSphere.slnx --no-restore --severity warn --exclude "**/Migrations/**" --include $staged_cs >&2 || true

git add -u

exit 0
