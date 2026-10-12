#!/usr/bin/env bash
# Propagates changes down the course branch chain: merges every branch into the next one, in order.
#
# Run it after committing a fix to the earliest branch where the affected code exists (see CONTRIBUTING.md).
# Works on local branches: pull them first. Stops at the first merge conflict; resolve it, commit, and run again.
#
# Usage: ./scripts/sync-branches.sh [--push]
set -euo pipefail

# Keep in sync with scripts/sync-branches.ps1 and the course map in README.md.
branches=(main 01-strategy 02-logging 03-metrics 04-tracing 05-testing)

push=false
if [[ "${1:-}" == "--push" ]]; then
    push=true
fi

if [[ -n "$(git status --porcelain)" ]]; then
    echo "The working tree has uncommitted changes. Commit or stash them first." >&2
    exit 1
fi

start_branch="$(git branch --show-current)"

for ((i = 1; i < ${#branches[@]}; i++)); do
    previous="${branches[i - 1]}"
    current="${branches[i]}"

    echo "==> $previous -> $current"
    git switch --quiet "$current"

    if ! git merge --no-edit "$previous"; then
        echo "Merge conflict while merging '$previous' into '$current'. Resolve it, commit, and run the script again." >&2
        exit 1
    fi
done

if [[ "$push" == true ]]; then
    git push origin "${branches[@]}"
fi

git switch --quiet "$start_branch"
echo "All course branches are in sync."
