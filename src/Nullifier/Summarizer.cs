namespace Nullifier;

#region Using Directives

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

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
		foreach (var fileGroup in this.problems.GroupBy(problem => problem.File).OrderBy(g => g.Count()).ThenBy(g => g.Key))
		{
			string name = fileGroup.Key[(this.arguments.ProjectDirectory?.Length ?? 0)..]
				.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

			Console.Write($"{name}: {fileGroup.Count():N0} [");

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

		if (this.problems.Count > 0)
		{
			Console.WriteLine("-----------------------------------------------");
		}
	}

	#endregion
}
