# This script started based on "Adding nullable annotations to an existing code base"
# https://www.meziantou.net/csharp-8-nullable-reference-types.htm#adding-nullable-anno.
# The general process is:
# 1. Change the .csproj file to use <Nullable>enable</Nullable>.
# 2. Add #nullable disable at the top of each .cs file (using this script).
# 3. Remove any #nullable enable at the top of files (after running this script).
# 4. For each file remove the #nullable disable directive, add the nullable annotations, and fix warnings.
# Start with the files that have the least dependencies. This way the new warnings should
# only appear in the file where you remove the directive.
# You have finished when there are no more "#nullable disable" lines in your code!

param(
    [string] $baseFolder,
    [bool] $recursive = $true,
    [bool] $addTodoComment = $true,
    [string] $context = "disable",  # Options: enable|disable|enable warnings|enable annotations|disable warnings|disable annotations.
                                    # Docs: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives#nullable-context
                                    # Related: https://github.com/dotnet/msbuild/issues/4391#issuecomment-545073077

	[string[]] $files = $null       # If $null or empty, this will search for .cs files under $baseFolder.
									# If non-empty, then only these files will be updated.
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function GetCurrentEncoding([string]$filePath)
{
    $bytes =[System.IO.File]::ReadAllBytes($filePath)

    # Return encoding names compatible with Set-Content. Support for utf8NoBOM only exists in PowerShell Core.
    # https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.management/set-content?view=powershell-7.3#-encoding
    $result = 'utf8NoBOM'
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
    {
        $result = 'utf8BOM'
    }

    return $result
}

if ($PSVersionTable.PSEdition -ine 'Core')
{
    throw "PowerShell $($PSVersionTable.PSVersion) is not supported. PowerShell `"Core`" must be used to correctly preserve UTF-8 byte order marks."
}

if (!$baseFolder)
{
    throw "A base folder is required."
}
elseif (!(Test-Path $baseFolder -Type Container))
{
    throw "The specified base folder does not exist or is inaccessible."
}

$contextLine = "#nullable $context"
if ($addTodoComment)
{
    $contextLine += " // TODO: Remove #nullable $context. [$([Environment]::UserName), $([DateTime]::Now.ToShortDateString())]"
}
$contextLine += "`r`n"

if ($files)
{
	$csFiles = @($files | Get-ChildItem)
}
else
{
	$csFiles = @(Get-ChildItem -Path $baseFolder -Recurse -Filter *.cs)
}

$updateCount = 0
foreach ($csFile in $csFiles)
{
    $csFilePath = $csFile.FullName

    # Skip generated files and files in the bin and obj folders.
    # See comments at the bottom of this page's "Nullable contexts" section for files that #nullable considers generated:
    # https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references#nullable-contexts
    if ($csFile.Name -imatch '.+\.(Generated|Designer|g|g\.i)\.cs$' -or $csFile.Name -ieq 'GeneratedCode.cs' -or $csFilePath -imatch '\\(bin|obj|GeneratedCode)\\')
    {
        continue
    }

    $encodingName = GetCurrentEncoding $csFilePath
    $content = Get-Content $csFilePath -Raw -Encoding $encodingName

    # Skip files that already start with a #nullable context.
    if ($content -and !$content.StartsWith('#nullable'))
    {
        $content = $contextLine + $content
        $shortCsFilePath = $csFilePath.Replace($baseFolder, '').TrimStart([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
        Write-Host "Updating $shortCsFilePath to use #nullable $context. Encoding is $encodingName."
        Set-Content -Path $csFilePath -Value $content -NoNewline -Encoding $encodingName
        $updateCount++
    }
}

Write-Host "Updated $updateCount file$(if ($updateCount -eq 1) {''} else {'s'})."