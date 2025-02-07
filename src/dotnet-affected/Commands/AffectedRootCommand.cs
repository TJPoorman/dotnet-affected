﻿using Affected.Cli.Views;
using System.CommandLine;
using System.CommandLine.Rendering;
using System.Linq;

namespace Affected.Cli.Commands
{
    internal class AffectedRootCommand : RootCommand
    {
        public static readonly FormatOption FormatOption = new();
        public static readonly DryRunOption DryRunOption = new();
        public static readonly OutputDirOption OutputDirOption = new();
        public static readonly OutputNameOption OutputNameOption = new();

        public AffectedRootCommand()
            : base("Determines which projects are affected by a set of changes.\n" +
                   "For examples and detailed descriptions see: " +
                   "https://github.com/leonardochaia/dotnet-affected/blob/main/README.md")
        {
            this.Name = "dotnet-affected";
            this.AddCommand(new DescribeCommand());

            this.AddGlobalOption(AffectedGlobalOptions.RepositoryPathOptions);
            this.AddGlobalOption(AffectedGlobalOptions.SolutionPathOption);
            this.AddGlobalOption(AffectedGlobalOptions.FilterFilePathOption);
            this.AddGlobalOption(AffectedGlobalOptions.VerboseOption);
            this.AddGlobalOption(AffectedGlobalOptions.AssumeChangesOption);
            this.AddGlobalOption(AffectedGlobalOptions.FromOption);
            this.AddGlobalOption(AffectedGlobalOptions.ToOption);
            this.AddGlobalOption(AffectedGlobalOptions.ExclusionRegexOption);
            this.AddGlobalOption(AffectedGlobalOptions.AdditionalProperties);

            this.AddOption(FormatOption);
            this.AddOption(DryRunOption);
            this.AddOption(OutputDirOption);
            this.AddOption(OutputNameOption);

            this.SetHandler(async ctx =>
            {
                var (options, summary) = ctx.ExecuteAffectedExecutor();
                summary.ThrowIfNoChanges();

                var verbose = ctx.ParseResult.GetValueForOption(AffectedGlobalOptions.VerboseOption)!;
                var console = ctx.Console;
                if (verbose)
                {
                    var infoView = new AffectedInfoView(summary);
                    console.Append(infoView);
                }

                var allProjects = summary
                    .ProjectsWithChangedFiles
                    .Concat(summary.AffectedProjects)
                    .Select(p => new ProjectInfo(p, options.AdditionalProperties));

                // Generate output using formatters
                var outputOptions = ctx.GetAffectedCommandOutputOptions(options);

                var formatterExecutor = new OutputFormatterExecutor(console);
                await formatterExecutor.Execute(
                    allProjects,
                    outputOptions.Formatters,
                    outputOptions.OutputDir,
                    outputOptions.OutputName,
                    outputOptions.DryRun,
                    verbose);
            });
        }
    }

    internal sealed class FormatOption : Option<string[]>
    {
        public FormatOption()
            : base(new[]
            {
                "--format", "-f"
            })
        {
            this.Description = "Space-seperated output file formats. Possible values: <traversal, text, json>.";

            this.SetDefaultValue(new[]
            {
                "traversal"
            });
            this.AllowMultipleArgumentsPerToken = true;
        }
    }

    internal sealed class DryRunOption : Option<bool>
    {
        public DryRunOption()
            : base(new[]
            {
                "--dry-run"
            })
        {
            this.Description = "Only output to stdout. No output files will be created.";
            this.SetDefaultValue(false);
        }
    }

    internal sealed class OutputDirOption : Option<string>
    {
        public OutputDirOption()
            : base(new[]
            {
                "--output-dir"
            })
        {
            this.Description = "The directory where the output file(s) will be generated.\n" +
                               "Relative paths will be based on --repository-path.";
        }
    }

    internal sealed class OutputNameOption : Option<string>
    {
        public OutputNameOption()
            : base(new[]
            {
                "--output-name"
            })
        {
            this.Description = "The filename to create.\n" +
                               "Format file extensions will be appended.";
            this.SetDefaultValue("affected");
        }
    }
}
