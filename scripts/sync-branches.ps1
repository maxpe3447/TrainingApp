<#
.SYNOPSIS
    Propagates changes down the course branch chain: merges every branch into the next one, in order.

.DESCRIPTION
    Run it after committing a fix to the earliest branch where the affected code exists (see CONTRIBUTING.md).
    Works on local branches: pull them first. Stops at the first merge conflict; resolve it, commit, and run again.

.PARAMETER Push
    Push all course branches to origin after a successful run.
#>
param(
    [switch]$Push
)

# Keep in sync with scripts/sync-branches.sh and the course map in README.md.
$branches = @('main', '01-strategy', '02-logging', '03-metrics', '04-tracing', '05-testing')

if (git status --porcelain) {
    Write-Error 'The working tree has uncommitted changes. Commit or stash them first.'
    exit 1
}

$startBranch = git branch --show-current

for ($i = 1; $i -lt $branches.Count; $i++) {
    $previous = $branches[$i - 1]
    $current = $branches[$i]

    Write-Host "==> $previous -> $current" -ForegroundColor Cyan

    git switch --quiet $current
    if ($LASTEXITCODE -ne 0) { exit 1 }

    git merge --no-edit $previous
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Merge conflict while merging '$previous' into '$current'. Resolve it, commit, and run the script again."
        exit 1
    }
}

if ($Push) {
    git push origin @branches
    if ($LASTEXITCODE -ne 0) { exit 1 }
}

git switch --quiet $startBranch
Write-Host 'All course branches are in sync.' -ForegroundColor Green
