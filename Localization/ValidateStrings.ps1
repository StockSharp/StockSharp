[CmdletBinding()]
param(
	[string]$BaseFile,
	[string]$LanguagesRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if ([string]::IsNullOrWhiteSpace($BaseFile)) {
	$BaseFile = Join-Path $scriptRoot 'strings.json'
}

if ([string]::IsNullOrWhiteSpace($LanguagesRoot)) {
	$LanguagesRoot = Join-Path $scriptRoot '..\Localization.Langs'
}

function Get-StringCount {
	param([Parameter(Mandatory = $true)][string]$Path)

	if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
		throw "File not found: $Path"
	}

	$json = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
	return @($json.PSObject.Properties).Count
}

$baseCount = Get-StringCount -Path $BaseFile
$hasErrors = $false

Write-Host "Base: $BaseFile ($baseCount strings)"

Get-ChildItem -LiteralPath $LanguagesRoot -Directory |
	Where-Object { $_.Name.Length -eq 2 } |
	Sort-Object Name |
	ForEach-Object {
		$file = Join-Path $_.FullName 'strings.json'

		if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
			Write-Host "MISSING $($_.Name): $file" -ForegroundColor Red
			$script:hasErrors = $true
			return
		}

		$count = Get-StringCount -Path $file

		if ($count -ne $baseCount) {
			Write-Host "MISMATCH $($_.Name): $count strings, expected $baseCount" -ForegroundColor Red
			$script:hasErrors = $true
		}
		else {
			Write-Host "OK $($_.Name): $count strings"
		}
	}

if ($hasErrors) {
	exit 1
}

Write-Host 'Localization string counts match.'
