param(
    [string]$Editor = 'D:\Unity\6000.3.29f1\Editor\Unity.exe',
    [string]$CacheRoot = 'D:\Unity',
    [string]$Method = 'SpringBuild.CameraApk',
    [switch]$WithGraphics
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $root 'UnityProject'
$log = Join-Path $root ('reports\build-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')
$env:ALLUSERSPROFILE = $env:ProgramData
$env:TEMP = Join-Path $CacheRoot 'AR-InstallTemp'
$env:TMP = $env:TEMP
$env:GRADLE_USER_HOME = Join-Path $CacheRoot 'AR-Gradle'
$env:UPM_CACHE_ROOT = Join-Path $CacheRoot 'AR-UPMCache'
$env:BEE_CACHE_DIRECTORY = Join-Path $CacheRoot 'AR-Bee'
$env:DOTNET_PROCESSOR_COUNT = '2'
foreach ($dir in @($env:TEMP, $env:GRADLE_USER_HOME, $env:UPM_CACHE_ROOT, $env:BEE_CACHE_DIRECTORY)) {
    [IO.Directory]::CreateDirectory($dir) | Out-Null
}
$settings = Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Internet Settings'
if ($settings.ProxyEnable -and $settings.ProxyServer -match '^https?://[^;]+$') {
    $proxy = [uri]$settings.ProxyServer
    $env:HTTP_PROXY = $env:HTTPS_PROXY = $proxy.AbsoluteUri
    $env:NO_PROXY = 'localhost,127.0.0.1'
    $env:JAVA_TOOL_OPTIONS = "-Dhttp.proxyHost=$($proxy.Host) -Dhttp.proxyPort=$($proxy.Port) -Dhttps.proxyHost=$($proxy.Host) -Dhttps.proxyPort=$($proxy.Port)"
}
$arguments = @('-batchmode', '-quit', '-job-worker-count', '2',
    '-projectPath', ('"' + $project + '"'), '-buildTarget', 'Android', '-executeMethod', $Method,
    '-logFile', ('"' + $log + '"'))
if (!$WithGraphics) { $arguments += '-nographics' }
Write-Output $log
$job = Start-Process $Editor -ArgumentList $arguments -WorkingDirectory $project -WindowStyle Hidden -PassThru
$job.WaitForExit()
exit $job.ExitCode
