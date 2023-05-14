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

	public string? Project { get; private set; }

	public string? MSBuild { get; private set; }

	public bool WhatIf { get; private set; }

	public bool Verbose { get; private set; }

	public bool FixDataMembers { get; private set; }

	#endregion

	#region Public Methods

	public bool Parse(string[] args)
	{
		CommandLine commandLine = new(true);

		commandLine.AddHeader("Makes simple nullablility fixes to a C# project based on compiler errors.");
		commandLine.AddHeader($"Usage: {nameof(Nullifier)} <Path to project folder or file> [options]");

		commandLine.AddValueHandler((value, errors) =>
		{
			if (File.Exists(value) || Directory.Exists(value))
			{
				this.Project = value;
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

		commandLine.AddSwitch("fixDataMembers", "Whether fields and properties should be set to nullable.", value => this.FixDataMembers = value);

		commandLine.AddFinalValidation(errors =>
		{
			if (this.Project.IsWhiteSpace())
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
