namespace Nullifier;

#region Using Directives

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

#endregion

internal sealed class Problem
{
	#region Constructors

	public Problem(string file, int line, int column, string code, string message)
	{
		this.File = file;

		// Convert line and column to 0-based like C# collections.
		this.Line = line - 1;
		this.Column = column - 1;
		this.Code = code;
		this.Message = message;
	}

	#endregion

	#region Public Properties

	public string File { get; }

	public int Line { get; }

	public int Column { get; }

	public string Code { get; }

	public string Message { get; }

	#endregion

	#region Public Methods

	public override string ToString() => $"{Path.GetFileName(this.File)}({this.Line + 1}): {this.Code}: {this.Message}";

	#endregion
}
