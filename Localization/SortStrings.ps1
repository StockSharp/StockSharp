[CmdletBinding()]
param(
	[string]$BaseFile,
	[string]$LanguagesRoot,
	[switch]$Check
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

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function ConvertTo-SortedJson {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Content,

		[Parameter(Mandatory = $true)]
		[string]$Path
	)

	$json = $Content | ConvertFrom-Json
	$properties = @($json.PSObject.Properties)

	if ($properties.Count -eq 0) {
		throw "No localization strings found: $Path"
	}

	$names = [string[]]@($properties.Name)
	[Array]::Sort($names, [StringComparer]::Ordinal)

	$sorted = [ordered]@{}

	foreach ($name in $names) {
		$value = $json.PSObject.Properties[$name].Value

		if ($value -isnot [string]) {
			throw "Localization value '$name' is not a string: $Path"
		}

		$sorted[$name] = $value
	}

	$lines = @(([pscustomobject]$sorted | ConvertTo-Json -Depth 2) -split "`r?`n")

	# Windows PowerShell emits four-space indentation and two spaces after the
	# property separator. Keep the repository's existing two-space JSON style.
	for ($i = 0; $i -lt $lines.Count; $i++) {
		$line = $lines[$i]

		if (-not $line.StartsWith('    "', [StringComparison]::Ordinal)) {
			continue
		}

		$line = '  ' + $line.Substring(4)
		$separatorIndex = $line.IndexOf('":  "', [StringComparison]::Ordinal)

		if ($separatorIndex -ge 0) {
			$line = $line.Remove($separatorIndex + 3, 1)
		}

		$lines[$i] = $line
	}

	$newLine = if ($Content.Contains("`r`n")) { "`r`n" } else { "`n" }
	return ($lines -join $newLine) + $newLine
}

$files = @($BaseFile)

$files += Get-ChildItem -LiteralPath $LanguagesRoot -Directory |
	Where-Object { $_.Name.Length -eq 2 } |
	Sort-Object Name |
	ForEach-Object { Join-Path $_.FullName 'strings.json' }

$changed = 0

foreach ($file in $files) {
	if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
		throw "File not found: $file"
	}

	$resolvedPath = (Resolve-Path -LiteralPath $file).Path
	$content = [IO.File]::ReadAllText($resolvedPath, [Text.Encoding]::UTF8)
	$sortedContent = ConvertTo-SortedJson -Content $content -Path $resolvedPath

	if ($content -ceq $sortedContent) {
		Write-Host "OK $resolvedPath"
		continue
	}

	$changed++

	if ($Check) {
		Write-Host "UNSORTED $resolvedPath" -ForegroundColor Red
		continue
	}

	[IO.File]::WriteAllText($resolvedPath, $sortedContent, $utf8NoBom)
	Write-Host "SORTED $resolvedPath"
}

if ($Check -and $changed -gt 0) {
	Write-Host "$changed localization files are not sorted." -ForegroundColor Red
	exit 1
}

if ($Check) {
	Write-Host 'Localization strings are sorted.'
}
else {
	Write-Host "$changed localization files sorted."
}
