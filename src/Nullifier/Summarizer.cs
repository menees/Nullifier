namespace Nullifier;

#region Using Directives

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Menees;

#endregion

internal sealed class Summarizer
{
	#region Private Data Members

	private readonly IReadOnlyList<Problem> problems;
	private readonly Arguments arguments;

	#endregion

	#region Constructors

	public Summarizer(IReadOnlyList<Problem> problems, Arguments arguments)
	{
		this.problems = problems;
		this.arguments = arguments;
	}

	#endregion

	#region Public Methods

	public void Summarize()
	{
		foreach (var folderGroup in this.problems.GroupBy(problem => Path.GetDirectoryName(problem.File)).OrderBy(g => g.Count()).ThenBy(g => g.Key))
		{
			string folder = this.TrimProjectDirectory(folderGroup.Key ?? string.Empty);
			Console.WriteLine($"{(folder.IsWhiteSpace() ? "<Root>" : folder)}: {folderGroup.Count()}");

			foreach (var fileGroup in folderGroup.GroupBy(problem => problem.File).OrderBy(g => g.Count()).ThenBy(g => g.Key))
			{
				string name = this.TrimProjectDirectory(fileGroup.Key);
				Console.Write($"  {name}: {fileGroup.Count():N0} [");

				bool first = true;
				foreach (var codeGroup in fileGroup.GroupBy(problem => problem.Code).OrderBy(g => g.Count()).ThenBy(g => g.Key))
				{
					if (!first)
					{
						Console.Write("; ");
					}

					Console.Write($"{codeGroup.Key}={codeGroup.Count():N0}");
					first = false;
				}

				Console.WriteLine("]");
			}
		}

		if (this.problems.Count > 0)
		{
			Console.WriteLine("-----------------------------------------------");
		}
	}

	#endregion

	#region Private Methods

	private string TrimProjectDirectory(string path)
	{
		string result = this.arguments.ProjectDirectory.IsNotEmpty() && path.StartsWith(this.arguments.ProjectDirectory, StringComparison.OrdinalIgnoreCase)
			? path[(this.arguments.ProjectDirectory?.Length ?? 0)..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
			: path;
		return result;
	}

	#endregion
}
