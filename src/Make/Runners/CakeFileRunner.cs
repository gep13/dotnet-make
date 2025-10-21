using Spectre.IO;

namespace Make;

public sealed class CakeFileRunner : IBuildRunner
{
    private readonly IProcessRunner _processRunner;

    public string Name { get; } = "Cake Runner (file-based)";
    public int Order { get; } = 2;

    public CakeFileRunner(IProcessRunner processRunner)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
    }

    public IEnumerable<string> GetKeywords()
    {
        return ["cakefile"];
    }

    public IEnumerable<string> GetGlobs(MakeSettings settings)
    {
        return ["./build.cs"];
    }

    public bool CanRun(MakeSettings settings, DirectoryPath path)
    {
        // Since we only match on a single thing (with no wildcards),
        // we're sure that we can run it.
        return true;
    }

    public async Task<int> Run(BuildContext context)
    {
        var args = GetArgs(context);

        return await _processRunner.Run(
            "dotnet", args: "run " + args,
            trace: context.Trace,
            workingDirectory: context.Root);
    }

    private static string GetArgs(BuildContext context)
    {
        var args = new List<string>
        {
            "build.cs",
        };

        if (context.Target != null)
        {
            args.Add("--target");
            args.Add($"\"{context.Target}\"");
        }

        context.AddArgs(args);

        return string.Join(" ", args);
    }
}