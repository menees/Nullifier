![windows build](https://github.com/menees/Nullifier/workflows/windows%20build/badge.svg)

# Nullifier

This program tries to fix simple nullability problems that are reported when enabling 
[Nullable Reference Types](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/nullable-reference-types) 
on an existing C# project. I didn't have good luck with the [NullabilityInference](https://github.com/icsharpcode/NullabilityInference) project, 
and Microsoft hasn't (yet) shipped any nullability code fixers of their own. So, I made this little program that:
* Runs MSBuild on a C# project,
* Looks through any C# compiler errors,
* Tries to fix simple problems on the lines indicated by the compiler errors,
* Optionally repeats the process if any code fixes were applied.

For example, when you enable nullable checking, the compiler produces a
[CS8600 error](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/nullable-warnings#possible-null-assigned-to-a-nonnullable-reference)
for the following code:

``` C#
// Error CS8600 Converting null literal or possible null value to non-nullable type.
string text = null;
```

Nullifier will apply the obvious fix here, which is adding the `?` type suffix to `string`:

``` C#
string? text = null;
```

Nullifier is a console app. You can run it with a `/?` parameter to see the current help.
Nullifier is intentionally very simplistic. It doesn't do any reflection over assemblies, and
it doesn't use Roslyn to parse the syntax trees or semantic models. It just looks at the
lines reported in the C# compiler errors, and it tries to fix a few problems that have
"obvious" solutions. In my experience, it fixes 30-40% of the initial nullability errors I
run into when adding `<Nullable>enable</Nullable>` to an existing project.
[YMMV](https://dictionary.cambridge.org/us/dictionary/english/ymmv).

Here's an example command line to run Nullifier on a project:

``` text
Nullifier.exe Libraries\MyProject /verbose+ /summarize+ /fixDataMembers+ /whatif-
```

## Gradual Approach
To gradually introduce nullability to an existing project use the [SetNullableDisable.ps1](src/Scripts/SetNullableDisable.ps1)
script. It must be run in [PowerShell "Core" 6.0 or later](https://github.com/PowerShell/PowerShell/releases)
(not Windows PowerShell) to correctly preserve each .cs file's encoding and byte-order marks.

The general process as [originally outlined by Gérald Barré on Meziantou's blog](https://www.meziantou.net/csharp-8-nullable-reference-types.htm#adding-nullable-anno) is:
1. Manually change the .csproj file to use `<Nullable>enable</Nullable>`.
2. Add `#nullable disable` at the top of each .cs file using the [SetNullableDisable.ps1](src/Scripts/SetNullableDisable.ps1) script.
3. Remove any `#nullable enable` lines at the top of files (after running the script) since nullability is on at the project-level now.
4. For each file (one at a time), remove the #nullable disable directive, run Nullifier, and fix warnings. Start with files that have the fewest dependencies so new warnings should
only appear in the files where you've removed the directive.

You're finished when there are no more "#nullable disable" lines in your code.

## CharityWare
This software is charityware.  If you like it and use it, I ask that you donate something to the charity of your choice.
I'll never know if you follow this policy, but the good karma from following it will be well worth your investment.
