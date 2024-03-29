﻿namespace Nullifier;

using System.Globalization;

internal sealed class Program
{
	private static void Main(string[] args)
	{
		try
		{
			Arguments arguments = new();
			if (arguments.Parse(args))
			{
				bool retry;
				Builder builder = new(arguments);
				do
				{
					retry = false;
					if (!builder.Build())
					{
						Console.WriteLine("Unable to build.");
					}
					else if (builder.Errors.Count == 0)
					{
						Console.WriteLine("Project built successfully.");
					}
					else
					{
						Console.WriteLine($"Analyzing {builder.Errors.Count} build errors.");
						Fixer fixer = new(builder.Errors, arguments);
						if (arguments.ListFiles)
						{
							Console.WriteLine("Distinct .cs files with errors:");
							string[] files = fixer.Problems.Select(p => p.File)
								.Where(f => ".cs".Equals(Path.GetExtension(f), StringComparison.OrdinalIgnoreCase))
								.Distinct(StringComparer.OrdinalIgnoreCase)
								.Order()
								.ToArray();
							for (int index = 0; index < files.Length; index++)
							{
								Console.Write(index == 0 ? "@('" : "'");
								Console.Write(files[index].Replace("'", "''", StringComparison.Ordinal));
								Console.WriteLine((index == files.Length - 1) ? "')" : "',");
							}
						}

						if (arguments.Summarize)
						{
							Console.WriteLine("Summarizing fixes:");
							Summarizer summarizer = new(fixer.Problems, arguments);
							summarizer.Summarize();
						}

						if (!fixer.Fix(out int updates))
						{
							Console.WriteLine($"No fixes were made out of {builder.Errors.Count} errors. {updates} potential fixes were found.");
						}
						else
						{
							Console.Write($"{updates} fixes were made out of {builder.Errors.Count} errors. Do you want to rebuild and try again? [y/n] ");
							if (char.ToLower(Console.ReadKey().KeyChar, CultureInfo.CurrentCulture) == 'y')
							{
								// Eat any other keypresses including Enter.
								while (Console.KeyAvailable)
								{
									Console.ReadKey();
								}

								retry = true;
							}

							Console.WriteLine();
						}
					}
				}
				while (retry);
			}
		}
#pragma warning disable CA1031 // Do not catch general exception types. Top-level Main method needs to catch and report all exceptions.
		catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
		{
			Console.Error.WriteLine("Unhandled exception:");
			Console.Error.WriteLine(ex.ToString());
		}
	}
}
