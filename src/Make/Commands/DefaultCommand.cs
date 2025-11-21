using System.ComponentModel;
using Spectre.IO;

namespace Make;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class DefaultCommand : AsyncCommand<DefaultCommand.Settings>
{
    private readonly IEnvironment _environment;
    private readonly BuildRunnerSelector _buildRunnerSelector;
    private readonly BuildRunners _runners;

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "[TARGET]")]
        [Description("The target to run")]
        public string? Target { get; set; }

        [CommandOption("--prefer <RUNNER>")]
        [Description("Uses the preferred runner. Available runners are [blue]cake[/], [blue]frosting[/], [blue]project[/], [blue]sln[/], [blue]traversal[/]")]
        public string? Prefer { get; set; }

        [CommandOption("--trace", IsHidden = true)]
        [Description("Outputs trace logging for the make tool")]
        public bool Trace { get; set; }

        [CommandOption("-w|--working", IsHidden = true)]
        [Description("Sets the working directory")]
        public string? WorkingDirectory { get; set; }
    }

    public DefaultCommand(
        IEnvironment environment,
        BuildRunnerSelector buildRunnerSelector,
        BuildRunners runners)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _buildRunnerSelector = buildRunnerSelector ?? throw new ArgumentNullException(nameof(buildRunnerSelector));
        _runners = runners ?? throw new ArgumentNullException(nameof(runners));
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context, Settings settings,
        CancellationToken cancellationToken)
    {
        var options = new MakeSettings
        {
            Trace = settings.Trace,
            Prefer = settings.Prefer,
            WorkingDirectory = settings.WorkingDirectory,
        };

        var result = _buildRunnerSelector.Find(options);
        if (result == null)
        {
            throw new MakeException("Could not find a suitable build tool", null);
        }

        var buildContext = new BuildContext(
            result.Value.Root,
            settings.Target,
            settings.Trace,
            context.Remaining);

        return await result.Value.Runner
            .Run(buildContext);
    }
}