namespace Nullifier;

#region Using Directives

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Menees;
using Menees.Windows;

#endregion

internal sealed class Builder
{
	#region Private Data Members

	private readonly Arguments arguments;
	private readonly List<string> errors;

	#endregion

	#region Constructors

	public Builder(Arguments arguments)
	{
		this.arguments = arguments;
		this.errors = new();
	}

	#endregion

	#region Public Properties

	public IReadOnlyList<string> Errors => this.errors;

	#endregion

	#region Public Methods

	public bool Build()
	{
		this.errors.Clear();

		bool result = false;
		string? msBuildPath = this.arguments.MSBuild ?? VisualStudioUtility.ResolvePath(_ => @"MSBuild\Current\Bin\amd64\MSBuild.exe");
		if (msBuildPath.IsEmpty())
		{
			Console.WriteLine("Unable to find MSBuild.");
		}
		else if (this.arguments.ProjectDirectory.IsWhiteSpace())
		{
			Console.WriteLine("No project was specified.");
		}
		else
		{
			this.TryEnableNullable(this.arguments.ProjectDirectory, this.arguments.ProjectFile);
			string logFile = Path.GetTempFileName();
			try
			{
				ProcessStartInfo start = new(msBuildPath) { UseShellExecute = false };
				start.ArgumentList.Add(this.arguments.ProjectFile ?? this.arguments.ProjectDirectory);

				// https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-command-line-reference#switches-for-loggers
				start.ArgumentList.Add("-noConsoleLogger");
				start.ArgumentList.Add(
					$"-fileLoggerParameters:ErrorsOnly;WarningsOnly;NoSummary;NoItemAndPropertyList;ForceNoAlign;DisableConsoleColor;LogFile={logFile}");

				using Process process = new();
				process.StartInfo = start;
				process.Start();
				process.WaitForExit();

				Console.WriteLine($"MSBuild exit code: {process.ExitCode}");

				string[] logLines = File.ReadAllLines(logFile);
				this.errors.AddRange(logLines.Where(line => line.IsNotWhiteSpace()));
				result = true;
			}
			finally
			{
				FileUtility.TryDeleteFile(logFile);
			}
		}

		return result;
	}

	#endregion

	#region Private Methods

	private void TryEnableNullable(string projectDirectory, string? projectFile)
	{
		if (Directory.Exists(projectDirectory) && projectFile.IsEmpty())
		{
			string[] projects = Directory.GetFiles(projectDirectory, "*.csproj");
			if (projects.Length == 1)
			{
				projectFile = projects[0];
			}
		}

		if (File.Exists(projectFile))
		{
			XDocument document = XDocument.Load(projectFile, LoadOptions.PreserveWhitespace);
			XElement? root = document.Root;
			if (root != null)
			{
				bool modified = false;

				// Find the first unconditional property group.
				XElement? propertyGroup = root.Elements("PropertyGroup").FirstOrDefault(element => !element.Attributes().Any());
				if (propertyGroup == null)
				{
					propertyGroup = new("PropertyGroup");
					root.Add(propertyGroup);
					modified = true;
				}

				XElement? nullable = propertyGroup.Element("Nullable");
				if (nullable == null)
				{
					nullable = new("Nullable");
					string indent = propertyGroup.Value.Contains('\t', StringComparison.Ordinal) ? "\t" : "  ";
					propertyGroup.Add(new XText(indent));
					propertyGroup.Add(nullable);
					propertyGroup.Add(new XText(Environment.NewLine + indent));
					modified = true;
				}

				if (nullable.Value != "enable")
				{
					nullable.Value = "enable";
					modified = true;
				}

				if (modified)
				{
					string fileNameOnly = Path.GetFileName(projectFile);
					if (this.arguments.WhatIf || FileUtility.IsReadOnlyFile(projectFile))
					{
						Console.WriteLine($"Project file {fileNameOnly} needs to enable Nullable.");
					}
					else
					{
						// Don't add an XML declaration at the top when saving and preserve input formatting.
						XmlWriterSettings settings = new() { OmitXmlDeclaration = true, Indent = false };
						using XmlWriter writer = XmlWriter.Create(projectFile, settings);
						document.Save(writer);
						Console.WriteLine($"Project file {fileNameOnly} updated to enable Nullable.");
					}
				}
			}
		}
	}

	#endregion
}
