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

``` PowerShell
.\Nullifier.exe src\MyProject /verbose+ /summarize+ /whatif-
```

## Gradual Approach
To gradually introduce nullability to an existing project use the [SetNullable.ps1](src/Scripts/SetNullable.ps1)
script with Nullifier. The script must be run in [PowerShell "Core" 6.0 or later](https://github.com/PowerShell/PowerShell/releases)
(not Windows PowerShell) to correctly preserve each .cs file's encoding and byte-order marks.

The general adoption process is based on an [outline by Gérald Barré on Meziantou's blog](https://www.meziantou.net/csharp-8-nullable-reference-types.htm#adding-nullable-anno):
1. Manually change the .csproj file to use `<Nullable>enable</Nullable>`.
2. Run Nullifier and let it fix as many warnings as it can.
3. Run the SetNullable.ps1 script to add `#nullable disable warnings` at the top of each .cs file.

  ``` PowerShell
  .\SetNullable.ps1 -context 'disable warnings' -baseFolder src\MyProject
  ```

4. Remove any `#nullable enable` lines at the top of files (after running the script) since nullability is on at the project level now.
4. For each file (one at a time), remove the `#nullable disable warnings` directive and fix any warnings. Start with files that have the fewest dependencies so new warnings should
only appear in the files where you've removed the directive.

You're finished when there are no more `#nullable disable warnings` lines in your code. The script also adds TODO comments by default,
so you can use the VS Task List or the [Menees VS Tools](https://github.com/menees/VsTools) Tasks tool window to see how many #nullable directives still remain.

## CharityWare
This software is charityware.  If you like it and use it, I ask that you donate something to the charity of your choice.
I'll never know if you follow this policy, but the good karma from following it will be well worth your investment.
