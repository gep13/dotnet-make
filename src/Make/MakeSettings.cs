namespace Make;

public sealed class MakeSettings
{
    public bool Trace { get; set; }
    public string? Prefer { get; set; }
    public string? WorkingDirectory { get; set; }
}