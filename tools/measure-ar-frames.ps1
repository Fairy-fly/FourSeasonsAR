param(
    [string]$Adb = 'adb',
    [string]$Serial,
    [ValidateRange(15,3600)]
    [int]$DurationSeconds = 660,
    [string]$InputFile,
    [string]$ReportDirectory = (Join-Path $PSScriptRoot '../reports')
)

$ErrorActionPreference = 'Stop'
$pattern = '^Flower AR frames seconds=(?<seconds>[0-9.]+) frames=(?<count>\d+) fps=(?<fps>[0-9.]+) p95_ms=(?<p95>\d+) over50ms=(?<slow>\d+)$'
$events = '^(Flower AR frames |World placed |World swapped |World restored )'
$culture = [System.Globalization.CultureInfo]::InvariantCulture
[IO.Directory]::CreateDirectory($ReportDirectory) | Out-Null
$stem = Join-Path $ReportDirectory ('ar-frames-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))

if ($InputFile) {
    $lines = @(Get-Content -LiteralPath $InputFile | Where-Object { $_ -match $events })
} else {
    $selector = if ($Serial) { @('-s',$Serial) } else { @() }
    $state = & $Adb @selector get-state
    if ($LASTEXITCODE -ne 0 -or $state -ne 'device') {
        throw 'ADB does not report a single authorized device. Select one with -Serial if needed.'
    }

    $since = & $Adb @selector shell 'date "+%m-%d %H:%M:%S.000"'
    if ($LASTEXITCODE -ne 0 -or $since -notmatch '^\d\d-\d\d \d\d:\d\d:\d\d\.\d{3}$') {
        throw 'Could not read the device clock for the start of the recording.'
    }
    $proc = [Diagnostics.Process]::new()
    $proc.StartInfo.FileName = (Get-Command $Adb -CommandType Application).Source
    $proc.StartInfo.UseShellExecute = $false
    $proc.StartInfo.CreateNoWindow = $true
    $proc.StartInfo.RedirectStandardOutput = $true
    foreach ($arg in ($selector + @('logcat','-v','raw','-T',$since,'-e',$events,'-s','Unity:I'))) {
        $proc.StartInfo.ArgumentList.Add($arg)
    }
    $lines = [System.Collections.Generic.List[string]]::new()
    $started = $false
    try {
        $started = $proc.Start()
        if (!$started) { throw 'Unable to start ADB logcat' }
        Write-Host "Capturing the next $DurationSeconds seconds while the device is in world placement."
        $timer = [Diagnostics.Stopwatch]::StartNew()
        $line = $proc.StandardOutput.ReadLineAsync()
        while ($timer.Elapsed.TotalSeconds -lt $DurationSeconds) {
            if (!$line.Wait(500)) { continue }
            if ($null -eq $line.Result) { break }
            if ($line.Result -match $events) { $lines.Add($line.Result) }
            $line = $proc.StandardOutput.ReadLineAsync()
        }
    } finally {
        if ($started -and !$proc.HasExited) { $proc.Kill() }
        $proc.Dispose()
    }
}
$lines | Set-Content -LiteralPath "$stem.log" -Encoding utf8NoBOM
$windows = foreach ($line in $lines) {
    if ($line -match $pattern) {
        [pscustomobject]@{
            seconds = [double]::Parse($Matches.seconds,$culture)
            frames = [int]$Matches.count
            fps = [double]::Parse($Matches.fps,$culture)
            p95_ms = [int]$Matches.p95
            over50ms = [int]$Matches.slow
        }
    }
}
$summary = [ordered]@{
    windows = @($windows | Where-Object { $_ }).Count
    trackedSeconds = 0
    totalFrames = 0
    averageFps = $null
    worstWindowP95_ms = $null
    framesOver50ms = 0
    goalSeconds = 600
    placementEvents = @($lines | Where-Object { $_ -match '^World ' })
}
if ($summary.windows -gt 0) {
    $summary.trackedSeconds = [math]::Round(($windows | Measure-Object -Property seconds -Sum).Sum,2)
    $summary.totalFrames = ($windows | Measure-Object -Property frames -Sum).Sum
    if ($summary.trackedSeconds -gt 0) {
        $summary.averageFps = [math]::Round($summary.totalFrames/$summary.trackedSeconds,2)
    }
    $summary.worstWindowP95_ms = ($windows | Measure-Object -Property p95_ms -Maximum).Maximum
    $summary.framesOver50ms = ($windows | Measure-Object -Property over50ms -Sum).Sum
}
$summary | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath "$stem.json" -Encoding utf8NoBOM
[pscustomobject]$summary
Write-Host "Saved $stem.json and $stem.log"
