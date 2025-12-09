using Spectre.Console;
using Spectre.IO;
using Path = Spectre.IO.Path;

namespace Make;

public sealed class BuildRunnerSelector
{
    private readonly IGlobber _globber;
    private readonly IFileSystem _fileSystem;
    private readonly IEnvironment _environment;
    private readonly IAnsiConsole _console;
    private readonly BuildRunners _runners;
    private readonly Dictionary<string, IBuildRunner> _runnerLookup;

    public BuildRunnerSelector(
        IGlobber globber,
        IFileSystem fileSystem,
        IEnvironment environment,
        IAnsiConsole console,
        BuildRunners runners)
    {
        _globber = globber ?? throw new ArgumentNullException(nameof(globber));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _runners = runners ?? throw new ArgumentNullException(nameof(runners));

        _runnerLookup = new Dictionary<string, IBuildRunner>();
        foreach (var runner in _runners.GetBuildRunners())
        {
            foreach (var name in runner.GetKeywords())
            {
                _runnerLookup[name] = runner;
            }
        }
    }

    public (DirectoryPath Root, IBuildRunner Runner, Path[] Candidates)? Find(MakeSettings settings)
    {
        var comparer = new PathComparer(caseSensitive: false);

        var current = GetWorkingDirectory(settings);
        if (current == null)
        {
            return null;
        }

        while (current is { IsRoot: false })
        {
            foreach (var runner in _runners.GetBuildRunners())
            {
                var names = new HashSet<string>(runner.GetKeywords());
                if (settings.Prefer != null)
                {
                    if (!names.Contains(settings.Prefer))
                    {
                        continue;
                    }
                }

                foreach (var glob in runner.GetGlobs(settings))
                {
                    var candidates = _globber
                        .Match(glob, new GlobberSettings
                        {
                            Root = current,
                            Comparer = comparer,
                        });

                    if (candidates?.ToArray() is Path[] { Length: > 0 } foundCandidates)
                    {
                        if (settings.Trace)
                        {
                            _console.MarkupLine(
                                $"[gray]Found root[/] {current.FullPath} [gray]using glob[/] {glob}");
                            _console.MarkupLine(
                                $"[gray]Found candidates:[/] {string.Join<Path>(',', foundCandidates)}");
                        }

                        if (runner.CanRun(settings, current))
                        {
                            if (settings.Trace)
                            {
                                _console.MarkupLine($"[gray]Using runner[/] {runner.Name}");
                            }

                            return (current, runner, foundCandidates);
                        }
                    }
                }
            }

            // Check the parent directory
            current = current.GetParent();
        }

        return null;
    }

    private DirectoryPath? GetWorkingDirectory(MakeSettings settings)
    {
        if (settings.WorkingDirectory != null)
        {
            var workingDirectory =
                new DirectoryPath(settings.WorkingDirectory)
                    .MakeAbsolute(_environment);

            return _fileSystem.Exist(workingDirectory) ? workingDirectory : null;
        }

        return _environment.WorkingDirectory;
    }
}