$lines = Get-Content 'src\TinyPdf\TinyPdfCreate.cs'
$count = 0
foreach ($line in $lines) {
    $trimmed = $line.Trim()
    if ($trimmed.Length -gt 0 -and -not $trimmed.StartsWith('//')) {
        $count++
    }
}
Write-Output "Actual lines of code in TinyPdfCreate.cs: $count"
