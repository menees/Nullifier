namespace Nullifier;

#region Using Directives

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Menees;
using Menees.Shell;

#endregion

internal sealed class Arguments
{
	#region Constructors

	public Arguments()
	{
	}

	#endregion

	#region Public Properties

	public string? ProjectFile { get; private set; }

	public string? ProjectDirectory { get; private set; }

	public string? MSBuild { get; private set; }

	public bool WhatIf { get; private set; }

	public bool Verbose { get; private set; }

	public bool FixDataMembers { get; private set; }

	public bool Summarize { get; private set; }

	public bool ListFiles { get; private set; }

	public bool EnableProject { get; private set; }

	#endregion

	#region Public Methods

	public bool Parse(string[] args)
	{
		CommandLine commandLine = new(true);

		commandLine.AddHeader("Makes simple nullablility fixes to a C# project based on compiler errors.");
		commandLine.AddHeader($"Usage: {nameof(Nullifier)} <Path to project folder or file> [options]");

		commandLine.AddValueHandler((value, errors) =>
		{
			if (Directory.Exists(value))
			{
				this.ProjectDirectory = value;
			}
			else if (File.Exists(value))
			{
				this.ProjectFile = value;
				this.ProjectDirectory = Path.GetDirectoryName(value) ?? Environment.CurrentDirectory;
			}
			else
			{
				errors.Add($"Project {value} was not found.");
			}
		});

		commandLine.AddSwitch("msbuild", "A full path to the MSBuild.exe to use.", (value, errors) =>
		{
			if (File.Exists(value))
			{
				this.MSBuild = value;
			}
			else
			{
				errors.Add("The specified MSBuild file does not exist.");
			}
		});

		commandLine.AddSwitch("whatif", "Set to true to skip writing fixes into files.", value => this.WhatIf = value);

		commandLine.AddSwitch("verbose", "Show output for each fix that's made.", value => this.Verbose = value);

		// Off by default since data members can be fixed in a variety of ways. See comments near FixDataMembers usage in Fixer.
		commandLine.AddSwitch("fixDataMembers", "Whether fields and properties should be set to nullable.", value => this.FixDataMembers = value);

		commandLine.AddSwitch("summarize", "Show summary of errors by file.", value => this.Summarize = value);

		commandLine.AddSwitch("listFiles", "Show PowerShell array of files with build errors.", value => this.ListFiles = value);

		// Off by default since Nullable may be enabled from Directory.Build.props or by individual #nullable enable statements.
		commandLine.AddSwitch("enableProject", "Whether Nullable=enable should be set in the .csproj file.", value => this.EnableProject = value);

		commandLine.AddFinalValidation(errors =>
		{
			if (this.ProjectDirectory.IsWhiteSpace())
			{
				errors.Add("A project must be specified.");
			}
		});

		CommandLineParseResult parseResult = commandLine.Parse(args);
		bool result = parseResult == CommandLineParseResult.Valid;
		if (!result)
		{
			commandLine.WriteMessage();
		}

		return result;
	}

	#endregion
}
