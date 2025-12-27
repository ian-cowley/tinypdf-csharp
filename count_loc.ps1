$files = Get-ChildItem -Path "src\TinyPdf" -Filter "*.cs" -Recurse | Where-Object { $_.FullName -notmatch "(\\bin\\|\\obj\\)" }
$totalCount = 0

foreach ($file in $files) {
    if (Test-Path $file.FullName) {
        $lines = Get-Content $file.FullName
        $fileCount = 0
        foreach ($line in $lines) {
            $trimmed = $line.Trim()
            # Skip empty lines and comment lines
            if ($trimmed.Length -gt 0 -and -not $trimmed.StartsWith('//')) {
                $fileCount++
            }
        }
        $totalCount += $fileCount
        # Write-Output "File: $($file.Name) - Lines: $fileCount" # Silenced for silent return
    }
}

return $totalCount
