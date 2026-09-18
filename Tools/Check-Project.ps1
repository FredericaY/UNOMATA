param(
    [switch]$RunCoreTests,
    [switch]$ValidateSpecs,
    [string]$DotnetPath,
    [string]$OpenSpecPath,
    [string]$NodePath
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$problems = 0

function Find-Tool([string]$Provided, [string]$Name, [string]$Fallback) {
    if ($Provided) {
        if (Test-Path -LiteralPath $Provided -PathType Leaf) { return (Resolve-Path -LiteralPath $Provided).Path }
        throw "Explicit tool path does not exist: $Provided"
    }
    $found = Get-Command $Name -ErrorAction SilentlyContinue
    if ($found) { return $found.Source }
    if ($Fallback -and (Test-Path -LiteralPath $Fallback -PathType Leaf)) { return $Fallback }
    return $null
}

Write-Output "Project: $projectRoot"
$versionFile = Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt'
$editorVersion = ((Get-Content -LiteralPath $versionFile | Select-String '^m_EditorVersion:').Line -split ': ', 2)[1]
Write-Output "[INFO] Required Unity Editor: $editorVersion (runtime acceptance is separate)."

foreach ($relative in @('Assets/_Project/Scenes/SampleScene.unity', 'CardChainCore/CardChainCore.sln', 'Docs/PROJECT_REVIEW.md', 'Docs/TODO.md')) {
    if (Test-Path -LiteralPath (Join-Path $projectRoot $relative)) { Write-Output "[PASS] $relative" }
    else { Write-Output "[BLOCKED] Missing: $relative"; $problems++ }
}

$documents = @(Get-Item -LiteralPath (Join-Path $projectRoot 'README.md')) + @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Docs') -Filter '*.md' -Recurse)
$brokenLinks = 0
foreach ($document in $documents) {
    $content = [IO.File]::ReadAllText($document.FullName)
    foreach ($match in [regex]::Matches($content, '(?<!!)\[[^\]]+\]\(([^)\r\n]+)\)')) {
        $target = $match.Groups[1].Value.Trim().Trim('<', '>')
        if ($target -match '^(https?://|mailto:|#|codex:)') { continue }
        $target = [Uri]::UnescapeDataString(($target -split '#', 2)[0])
        if (-not $target) { continue }
        if (-not (Test-Path -LiteralPath (Join-Path $document.DirectoryName $target))) {
            Write-Output "[FAIL] Broken local link: $($document.Name) -> $target"
            $brokenLinks++
        }
    }
}
$problems += $brokenLinks
if ($brokenLinks -eq 0) { Write-Output "[PASS] Local file links in $($documents.Count) Markdown documents (anchors not checked)." }

$dotnet = $null
$sdkReady = $false
$candidates = @()
if ($DotnetPath) {
    $candidates = @(Find-Tool $DotnetPath 'dotnet' '')
} else {
    $onPath = Get-Command 'dotnet' -ErrorAction SilentlyContinue
    if ($onPath) { $candidates += $onPath.Source }
    $candidates += (Join-Path $env:LOCALAPPDATA 'Microsoft/dotnet/dotnet.exe')
    $candidates += (Join-Path $env:ProgramFiles 'dotnet/dotnet.exe')
}
foreach ($candidate in ($candidates | Select-Object -Unique)) {
    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) { continue }
    $sdks = @(& $candidate --list-sdks)
    if ($LASTEXITCODE -eq 0 -and ($sdks | Where-Object { $_ -match '^8\.' })) {
        $dotnet = $candidate
        $sdkReady = $true
        Write-Output "[PASS] .NET 8 SDK is discoverable: $dotnet"
        break
    }
}
if (-not $sdkReady) { Write-Output '[BLOCKED] .NET 8 SDK is not discoverable; Core build/test cannot be verified.'; $problems++ }

Push-Location $projectRoot
try {
    if ($RunCoreTests) {
        if ($sdkReady) {
            & $dotnet test 'CardChainCore/CardChainCore.sln'
            if ($LASTEXITCODE -ne 0) { $problems++; Write-Output '[FAIL] Core test command did not succeed.' }
        } else { Write-Output '[SKIPPED] Core tests: missing SDK, no test result produced.' }
    }
    if ($ValidateSpecs) {
        $openSpec = Find-Tool $OpenSpecPath 'openspec.cmd' (Join-Path $env:APPDATA 'npm/openspec.cmd')
        if (-not $openSpec) { Write-Output '[BLOCKED] OpenSpec CLI is not discoverable.'; $problems++ }
        else {
            $node = Find-Tool $NodePath 'node' (Join-Path $env:ProgramFiles 'nodejs/node.exe')
            $savedPath = $env:PATH
            $savedTelemetry = $env:OPENSPEC_TELEMETRY
            try {
                if ($node) { $env:PATH = (Split-Path -Parent $node) + [IO.Path]::PathSeparator + $env:PATH }
                $env:OPENSPEC_TELEMETRY = '0'
                & $openSpec validate --all --strict --no-interactive
                if ($LASTEXITCODE -ne 0) { Write-Output '[FAIL] OpenSpec validation.'; $problems++ }
            } finally {
                $env:PATH = $savedPath
                $env:OPENSPEC_TELEMETRY = $savedTelemetry
            }
        }
    }
} finally { Pop-Location }

Write-Output "Checks finished: $problems blocking issue(s). Unity Play Mode was not run."
if ($problems -gt 0) { exit 1 }
exit 0
