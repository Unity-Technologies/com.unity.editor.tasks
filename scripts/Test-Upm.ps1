[CmdletBinding()]
Param(
    [string]
    $UnityVersion = "2019.2",
    [switch]
    $Trace = $false
)

Set-StrictMode -Version Latest
if ($Trace) {
    Set-PSDebug -Trace 1
}

. $PSScriptRoot\helpers.ps1 | out-null

$upmDir = Join-Path $rootDirectory 'build\upm'

Get-ChildItem -Directory $upmDir | % {
    Write-Output "Testing $($_.Name)"

    $packageDir = Join-Path $upmDir $_.Name
    Invoke-Command -Fatal { & upm-ci package test --package-path $packageDir -u $UnityVersion }
}
