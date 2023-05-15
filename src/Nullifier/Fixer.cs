namespace Nullifier;

#region Using Directives

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Menees;

#endregion

internal sealed partial class Fixer
{
	#region Private Data Members

	private readonly IReadOnlyList<string> errors;
	private readonly Arguments arguments;
	private string[]? currentFileLines;

	#endregion

	#region Constructors

	public Fixer(IReadOnlyList<string> errors, Arguments arguments)
	{
		this.errors = errors;
		this.arguments = arguments;
	}

	#endregion

	#region Public Methods

	public bool Fix(out int updates)
	{
		updates = 0;
		bool result = false;
		Regex regex = CreateErrorRegex();
		List<Problem> problems = new(this.errors.Count);

		foreach (string error in this.errors)
		{
			Match match = regex.Match(error);
			const int ExpectedGroupCount = 8;
			if (!match.Success || match.Groups.Count != ExpectedGroupCount)
			{
				Console.WriteLine($"Unable to parse error: {error}");
				problems.Clear();
				break;
			}
			else
			{
				GroupCollection groups = match.Groups;
				Problem problem = new(
					groups["file"].Value,
					int.Parse(groups["line"].Value, CultureInfo.CurrentCulture),
					int.Parse(groups["column"].Value, CultureInfo.CurrentCulture),
					groups["code"].Value,
					groups["message"].Value);
				problems.Add(problem);
			}
		}

		// Fix the problems in file order so we can cache the current file lines as we make fixes.
		string? currentFile = null;
		foreach (Problem problem in problems.OrderBy(p => p.File).ThenBy(p => p.Line).ThenBy(p => p.Column))
		{
			if (File.Exists(problem.File))
			{
				if (currentFile != problem.File || this.currentFileLines == null)
				{
					currentFile = problem.File;
					this.currentFileLines = File.ReadAllLines(problem.File);
				}

				if (this.Fix(problem))
				{
					updates++;
					if (!this.arguments.WhatIf && !FileUtility.IsReadOnlyFile(problem.File))
					{
						// It's not super efficient to save after every fix, but it makes the implementation simple.
						File.WriteAllLines(problem.File, this.currentFileLines);
						result = true;
					}
				}
			}
		}

		return result;
	}

	#endregion

	#region Generated Regexes
#pragma warning disable MEN002 // Line is too long. It's ok for a GeneratedRegex attribute.

	[GeneratedRegex(@"^(?n)(?<file>.+?)\((?<line>\d+),(?<column>\d+)\): (?<type>error|warning) (?<code>\w+): (?<message>.+?) \[(?<project>.+?)\]$")]
	private static partial Regex CreateErrorRegex();

	[GeneratedRegex(@"(?n)\s'(?<name>\w+)'\s")]
	private static partial Regex CreateParameterNameReferenceRegex();

	// Match a "Type variable = " with a direct null literal, a null from either side of a ternary operator, an "as" operator, or an XxxOrDefault() LINQ method.
	[GeneratedRegex(@"(?n)(^|,|\()\s*(?<type>\w+(<.+?>)?)\s+\w+\s*=\s*(null|.*\?\s*null\s*:.*|.*\?.*:\s*null\s*|.*\s+as\s+.*|.*.(First|Single|Last)OrDefault\(.*\))(;$|,|\))")]
	private static partial Regex CreateNullAssignmentRegex();

	[GeneratedRegex(@"(?n)^\s*(?<type>\w+(<.+?>)?)\s+(?<variable>\w+)\s*=\s*.+;$")]
	private static partial Regex CreateVariableAssignmentRegex();

	[GeneratedRegex(@"(?n)^\s*(?<type>\w+(<.+?>)?)\?\s+(?<variable>\w+)\s*(=\s*.+)?;$")]
	private static partial Regex CreateNullableVariableDeclarationOrAssignmentRegex();

	[GeneratedRegex(@"(?n)^\s*if\s*\(\s*((?<variable>\w+)\s*(==|!=|is|is\s+not)\s*null|null\s*[!=]=\s*(?<variable>\w+)|(\s*!\s*)?string\.IsNullOrEmpty\((?<variable>\w+)\))\s*\)")]
	private static partial Regex CreateIfNullCheckRegex();

	[GeneratedRegex(@"(?n)^Non-nullable (?<type>event|field|property) '(?<member>\w+)' must contain a non-null value when exiting constructor. Consider declaring the \k<type> as nullable.$")]
	private static partial Regex CreateNonNullExitingConstructorRegex();

	[GeneratedRegex(@"(?n)(^|\s+)event\s+(?<type>\w+(<.+?>)?)\s+(?<member>\w+)(;|$|\s)")]
	private static partial Regex CreateEventMemberRegex();

	[GeneratedRegex(@"(?n)^\s*((public|private|protected|internal|static|readonly|volatile)\s*)+\s+(?<type>\w+(<.+?>)?)\s+(?<member>\w+)(;|$|\s)")]
	private static partial Regex CreateDataMemberRegex();

	[GeneratedRegex(@"(?n)^\s*Nullability of reference types in type of parameter 'sender' of 'void\s+(?<type>\w+)\.(?<method>\w+\(object\s+sender,\s+\w+\s+\w+\))' doesn't match the target delegate")]
	private static partial Regex CreateEventSenderDeclarationRegex();

	[GeneratedRegex(@"(?n)^\s*return\s*(?<name>\w+)\s*;\s*")]
	private static partial Regex CreateReturnNameRegex();

	[GeneratedRegex(@"(?n)^\s*((public|private|protected|internal|static|partial)\s*)+\s+(?<type>\w+(<.+?>)?\??)\s+(?<member>\w+)(\(|$|\s)")]
	private static partial Regex CreateMethodOrPropertyDeclarationRegex();

	[GeneratedRegex(@"\.TryGetValue\(.*,\s*out\s+(?<type>\w+(<.+?>)?)\s+.+?\)")]
	private static partial Regex CreateTryGetValueOutRegex();

	[GeneratedRegex(@"(?n)(?<function>\w+)(<.+>)?(?<open>\().*?(?<null>null).*?(?<close>\))")]
	private static partial Regex CreateFunctionCallWithNullArgRegex();

	[GeneratedRegex(@"(?<type>\w+(<.+?>)?\??)\s+(?<variable>\w+)")]
	private static partial Regex CreateTypeVariableRegex();

#pragma warning restore MEN002 // Line is too long
	#endregion

	#region Private Methods

	private static Regex CreateDeclarationRegex(string name)
		=> new(@$"(?n)(?<type>\w+(<.+?>)?)\s+{Regex.Escape(name)}[,\)\s]");

	private bool Fix(Problem problem)
	{
		bool result = false;

		switch (problem.Code)
		{
			case "CS8600": // Converting null literal or possible null value to non-nullable type
				result = this.FixAssignNullToVariable(problem)
						|| this.FixAssignFollowedByNullCheck(problem)
						|| this.FixTryGetValueOut(problem);
				break;

			case "CS8603": // Possible null reference return
				result = this.FixNullableReturnType(problem);
				break;

			case "CS8625": // Cannot convert null literal to non-nullable reference type
				result = this.FixAssignNullToVariable(problem)
					|| this.FixNullLiteralPassedToLocalMethod(problem);
				break;

			case "CS8622": // Nullability of reference types in type of parameter 'xxx' of '...method declaration...'
				result = this.FixOverriddenNullableParameter(problem)
					|| this.FixEventSender(problem);
				break;

			case "CS8765": // Nullability of type of parameter 'xxx' doesn't match overridden member
			case "CS8767": // Nullability of reference types in type of parameter 'x' of '...method declaration...'
				result = this.FixOverriddenNullableParameter(problem);
				break;

			case "CS8618": // Non-nullable event|field|property 'xxx' must contain a non-null value when exiting constructor.
				result = this.FixNullableMemberExitingConstructor(problem);
				break;
		}

		return result;
	}

	private bool GetLine(Problem problem, [MaybeNullWhen(false)] out string line)
		=> this.GetLine(problem.Line, out line);

	private bool GetLine(int lineIndex, [MaybeNullWhen(false)] out string line)
	{
		bool result = false;
		line = null;

		if (this.currentFileLines != null && lineIndex < this.currentFileLines.Length)
		{
			line = this.currentFileLines[lineIndex];
			result = true;
		}

		return result;
	}

	private bool MarkTypeNullableAndSetLine(Problem problem, string line, Match match, Group typeGroup, int? lineIndex = null)
	{
		bool result = false;

		if (this.currentFileLines != null && typeGroup.Success)
		{
			int insertIndex = typeGroup.Index + typeGroup.Length;
			line = line.Insert(insertIndex, "?");
			lineIndex ??= problem.Line;
			this.currentFileLines[lineIndex.Value] = line;
			result = true;

			string change = match.Value.Insert(typeGroup.Index - match.Index + typeGroup.Length, "?").Trim();
			this.Report(problem, change, lineIndex);
		}

		return result;
	}

	private void Report(Problem problem, string change, int? lineIndex = null)
	{
		if (this.arguments.Verbose)
		{
			Console.WriteLine($"Fix {problem.Code} {Path.GetFileName(problem.File)}({(lineIndex ?? problem.Line) + 1}): {change}");
		}
	}

	private bool FixAssignNullToVariable(Problem problem)
	{
		bool result = false;

		if (this.GetLine(problem, out string? line))
		{
			// Match "Type variable = ... null ..." and change to "Type? variable = ... null ..."
			Regex assignment = CreateNullAssignmentRegex();
			Match match = assignment.Match(line);
			if (match.Success && match.Groups.Count == 2)
			{
				result = this.MarkTypeNullableAndSetLine(problem, line, match, match.Groups["type"]);
			}
		}

		return result;
	}

	private bool FixAssignFollowedByNullCheck(Problem problem)
	{
		bool result = false;

		if (this.GetLine(problem, out string? line))
		{
			Regex assignment = CreateVariableAssignmentRegex();
			Match match = assignment.Match(line);
			const int ExpectedGroupCount = 3;
			if (match.Success && match.Groups.Count == ExpectedGroupCount && this.GetLine(problem.Line + 1, out string? nextLine))
			{
				Regex isNullCheck = CreateIfNullCheckRegex();
				Match nextMatch = isNullCheck.Match(nextLine);
				if (nextMatch.Success && nextMatch.Groups["variable"].Value == match.Groups["variable"].Value)
				{
					result = this.MarkTypeNullableAndSetLine(problem, line, match, match.Groups["type"]);
				}
			}
		}

		return result;
	}

	private bool FixOverriddenNullableParameter(Problem problem)
	{
		bool result = false;

		// Example message: Nullability of type of parameter 'context' doesn't match overridden member (possibly because of nullability attributes).
		// Example to fix: public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
		Regex nameRef = CreateParameterNameReferenceRegex();
		Match match = nameRef.Match(problem.Message);
		if (match.Success && match.Groups.Count == 2 && this.GetLine(problem, out string? line))
		{
			// Look for "Type variable" and change it to "Type? varible".
			string parameterName = match.Groups["name"].Value;
			Regex declaration = CreateDeclarationRegex(parameterName);
			match = declaration.Match(line, problem.Column);
			if (match.Success && match.Groups.Count == 2)
			{
				result = this.MarkTypeNullableAndSetLine(problem, line, match, match.Groups[1]);
			}
		}

		return result;
	}

	private bool FixNullableMemberExitingConstructor(Problem problem)
	{
		bool result = false;

		Regex messageEx = CreateNonNullExitingConstructorRegex();
		Match messageMatch = messageEx.Match(problem.Message);
		if (messageMatch.Success)
		{
			string matchType = messageMatch.Groups["type"].Value;
			string member = messageMatch.Groups["member"].Value;
			switch (matchType)
			{
				case "event":
					result = this.FixMemberDeclaration(problem, member, CreateEventMemberRegex());
					break;

				case "field":
				case "property":
					// Fixing data members is off by default since the compiler can produce false positives about them.
					// For example, if a constructor calls a private helper method to initialize member fields or properties,
					// the compiler may still complain that they're null when exiting the constructor.
					if (this.arguments.FixDataMembers)
					{
						result = this.FixMemberDeclaration(problem, member, CreateDataMemberRegex());
					}

					break;
			}
		}

		return result;
	}

	private bool FixMemberDeclaration(Problem problem, string member, Regex memberLineRegex)
	{
		bool result = false;

		int lineIndex = 0;
		foreach (string line in this.currentFileLines!)
		{
			Match match = memberLineRegex.Match(line);
			if (match.Success && match.Groups["member"].Value == member)
			{
				result = this.MarkTypeNullableAndSetLine(problem, line, match, match.Groups["type"], lineIndex);
				break;
			}

			lineIndex++;
		}

		return result;
	}

	private bool FixEventSender(Problem problem)
	{
		bool result = false;

		// The message will contain a declaration like: void FormSaver.OnFormLoad(object sender, EventArgs e)
		// We need to look for it without the type name prefix on the method.
		Match messageMatch = CreateEventSenderDeclarationRegex().Match(problem.Message);
		if (messageMatch.Success)
		{
			// Require the type name to match the file name (assumes MEN008 rule is applied).
			string type = messageMatch.Groups["type"].Value;
			if (type == Path.GetFileNameWithoutExtension(problem.File))
			{
				string methodDeclaration = "void " + messageMatch.Groups["method"].Value;

				int lineIndex = 0;
				foreach (string line in this.currentFileLines!)
				{
					if (line.Contains(methodDeclaration, StringComparison.Ordinal))
					{
						string fixedLine = line.Replace("object sender", "object? sender", StringComparison.Ordinal);
						this.currentFileLines[lineIndex] = fixedLine;
						this.Report(problem, fixedLine.Trim(), lineIndex);
						result = true;
						break;
					}

					lineIndex++;
				}
			}
		}

		return result;
	}

	private bool FixNullableReturnType(Problem problem)
	{
		bool result = false;

		if (this.GetLine(problem, out string? line))
		{
			Regex returnEx = CreateReturnNameRegex();
			Match returnMatch = returnEx.Match(line);
			if (returnMatch.Success)
			{
				string returnedName = returnMatch.Groups["name"].Value;

				// Search up X lines for: Type? returnedName.
				// Then continue searching up : keywords Type Function(...
				const int MaxLookbackLines = 120; // Based on MEN003 rule.
				int lineIndex = problem.Line - 1;
				int earliestLineIndex = Math.Max(0, lineIndex - MaxLookbackLines);

				Regex? variableDeclarationEx = CreateNullableVariableDeclarationOrAssignmentRegex();
				Regex? methodDeclarationEx = null;
				string? returnType = null;
				for (; lineIndex >= earliestLineIndex; lineIndex--)
				{
					line = this.currentFileLines![lineIndex];
					if (variableDeclarationEx != null)
					{
						Match match = variableDeclarationEx.Match(line);
						if (match.Success && match.Groups["variable"].Value == returnedName)
						{
							variableDeclarationEx = null;
							returnType = match.Groups["type"].Value;
							methodDeclarationEx = CreateMethodOrPropertyDeclarationRegex();
						}
					}
					else if (returnType != null && methodDeclarationEx != null)
					{
						Match match = methodDeclarationEx.Match(line);
						if (match.Success)
						{
							Group typeGroup = match.Groups["type"];
							if (typeGroup.Value == returnType)
							{
								result = this.MarkTypeNullableAndSetLine(problem, line, match, typeGroup, lineIndex);
								break;
							}
						}
					}
				}
			}
		}

		return result;
	}

	private bool FixTryGetValueOut(Problem problem)
	{
		bool result = false;

		if (this.GetLine(problem, out string? line))
		{
			Regex tryGetValueEx = CreateTryGetValueOutRegex();
			Match match = tryGetValueEx.Match(line);
			if (match.Success)
			{
				result = this.MarkTypeNullableAndSetLine(problem, line, match, match.Groups["type"]);
			}
		}

		return result;
	}

	private bool FixNullLiteralPassedToLocalMethod(Problem problem)
	{
		bool result = false;

		if (this.GetLine(problem, out string? invocationLine))
		{
			Regex call = CreateFunctionCallWithNullArgRegex();
			Match match = call.Match(invocationLine);

			// Make sure the null mentioned in the problem is the one we match against.
			// If there are multiple null args, there are probably compiler errors for each of them.
			// Note that problem.Column can be off by one if we previously added a '?' null prefix
			// somewhere on the current line.
			if (match.Success && (match.Groups["null"].Index == problem.Column || match.Groups["null"].Index == problem.Column + 1))
			{
				int openIndex = match.Groups["open"].Index;
				int closeIndex = match.Groups["close"].Index;

				// If function calls are nested (e.g., X(Y(a,b), c)) or multiple template args are used, then this simple split won't work.
				string[] args = invocationLine[(openIndex + 1)..(closeIndex - 1)].Split(',', StringSplitOptions.TrimEntries);
				int[] nullArgs = args.Select((arg, index) => (arg, index)).Where(tuple => tuple.arg == "null").Select(tuple => tuple.index).ToArray();
				string functionName = match.Groups["function"].Value;

				// Try to find a matching function in the current file with at least that many args (so overloads get fixed too).
				Regex declarationEx = CreateMethodOrPropertyDeclarationRegex();
				Regex? argEx = null;
				int lineIndex = 0;
				foreach (string declarationLine in this.currentFileLines!)
				{
					// We'll only try to fix functions where all the parameters fit on one line.
					match = declarationEx.Match(declarationLine);
					if (match.Success
						&& match.Groups["member"].Value == functionName
						&& match.Value.EndsWith("(", StringComparison.Ordinal)
						&& declarationLine.TrimEnd().EndsWith(")", StringComparison.Ordinal))
					{
						int argIndex = 0;
						argEx ??= CreateTypeVariableRegex();
						foreach (Match argMatch in argEx.Matches(declarationLine, match.Groups["member"].Index).Cast<Match>())
						{
							Group typeGroup = argMatch.Groups["type"];
							if (!typeGroup.Value.EndsWith("?", StringComparison.Ordinal)
								&& nullArgs.Contains(argIndex)
								&& this.MarkTypeNullableAndSetLine(problem, declarationLine, argMatch, argMatch.Groups["type"], lineIndex))
							{
								result = true;
							}

							argIndex++;
						}
					}

					lineIndex++;
				}
			}
		}

		return result;
	}

	#endregion
}
