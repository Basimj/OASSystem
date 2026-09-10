param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

$projectRootPath = (Resolve-Path $ProjectRoot).Path
$solutionPath = Join-Path $projectRootPath 'OASSystem.sln'
if (-not (Test-Path $solutionPath)) {
    throw "OASSystem.sln was not found under: $projectRootPath"
}

Write-Host "Employees legacy cleanup root: $projectRootPath"

$obsoleteFiles = @(
    'OAS.Client/Features/Employees/Components/EmployeeEditor.razor',
    'OAS.Client/Features/Employees/Workspace/EmployeeEditorFormState.cs',
    'OAS.Client/Features/Employees/Workspace/EmployeeEditorWorkspaceState.cs',
    'Shared/UiLib/Components/Surfaces/EmployeeCard.razor',
    'Shared/UiLib/Components/Surfaces/EmployeeCard.razor.css',
    'Shared/UiLib/wwwroot/css/features/employees.css',
    'Shared/UiLib/Components/Employees/UiEmployeeBasicSection.razor',
    'Shared/UiLib/Components/Employees/UiEmployeeBasicSection.razor.cs',
    'Shared/UiLib/Components/Employees/UiEmployeeBasicSection.razor.css',
    'Shared/UiLib/Components/Employees/UiEmployeeJobSection.razor',
    'Shared/UiLib/Components/Employees/UiEmployeeJobSection.razor.cs',
    'Shared/UiLib/Components/Employees/UiEmployeeJobSection.razor.css',
    'Shared/UiLib/Components/Employees/UiEmployeeContactSection.razor',
    'Shared/UiLib/Components/Employees/UiEmployeeContactSection.razor.cs',
    'Shared/UiLib/Components/Employees/UiEmployeeContactSection.razor.css',
    'Shared/UiLib/Components/Employees/UiEmployeeFilesSection.razor',
    'Shared/UiLib/Components/Employees/UiEmployeeFilesSection.razor.css',
    'Shared/UiLib/Components/Employees/UiEmployeeNotesSection.razor',
    'Shared/UiLib/Components/Employees/UiEmployeeNotesSection.razor.cs',
    'Shared/UiLib/Components/Employees/UiEmployeeNotesSection.razor.css',
    'Shared/UiLib/Components/Employees/UiEmployeeEditorShell.razor',
    'Shared/UiLib/Components/Employees/UiEmployeeEditorShell.razor.cs',
    'Shared/UiLib/Components/Employees/UiEmployeeEditorShell.razor.css',
    'Shared/UiLib/Components/Inputs/UiTextArea.razor',
    'Shared/UiLib/Components/Inputs/UiTextArea.razor.cs',
    'Shared/UiLib/Components/Inputs/UiTextArea.razor.css',
    'OAS.Application/Features/Employees/Queries/GetNextEmployeeCode/GetNextEmployeeCodeQuery.cs',
    'OAS.Application/Features/Employees/Queries/GetNextEmployeeCode/GetNextEmployeeCodeQueryHandler.cs',
    'OAS.Application/Features/Employees/Specifications/EmployeeCodeSpecification.cs'
)

foreach ($relativePath in $obsoleteFiles) {
    $fullPath = Join-Path $projectRootPath $relativePath
    if (Test-Path $fullPath) {
        Remove-Item -LiteralPath $fullPath -Force
        Write-Host "Removed: $relativePath"
    }
    else {
        Write-Host "Already absent: $relativePath"
    }
}

$bundlePath = Join-Path $projectRootPath 'Shared/UiLib/wwwroot/css/oas-ui-bundle.css'
if (Test-Path $bundlePath) {
    $content = Get-Content -LiteralPath $bundlePath -Raw
    $updated = $content -replace '(?m)^\s*@import\s+["'']\.\/features\/employees\.css["''];\s*\r?\n?', ''
    if ($updated -ne $content) {
        Set-Content -LiteralPath $bundlePath -Value $updated -Encoding utf8
        Write-Host 'Removed employees.css import from oas-ui-bundle.css.'
    }
    else {
        Write-Host 'No legacy employees.css import found in oas-ui-bundle.css.'
    }
}

$obsoleteDirectories = @(
    'Shared/UiLib/wwwroot/css/features',
    'OAS.Application/Features/Employees/Queries/GetNextEmployeeCode'
)

foreach ($relativeDirectory in $obsoleteDirectories) {
    $directory = Join-Path $projectRootPath $relativeDirectory
    if (Test-Path $directory) {
        $remaining = Get-ChildItem -LiteralPath $directory -Force
        if ($remaining.Count -eq 0) {
            Remove-Item -LiteralPath $directory -Force
            Write-Host "Removed empty directory: $relativeDirectory"
        }
    }
}


$generatedDirectories = @(
    'Shared/UiLib/obj',
    'Shared/UiLib/bin',
    'OAS.Client/obj',
    'OAS.Client/bin',
    'OAS.API/obj',
    'OAS.API/bin'
)

foreach ($relativeDirectory in $generatedDirectories) {
    $directory = Join-Path $projectRootPath $relativeDirectory
    if (Test-Path $directory) {
        Remove-Item -LiteralPath $directory -Recurse -Force
        Write-Host "Removed stale generated artifacts: $relativeDirectory"
    }
}

Write-Host 'Employees legacy cleanup completed.'
