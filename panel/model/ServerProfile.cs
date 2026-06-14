namespace model;

/// <summary>Runtime Hephaestus profile name; wired from <c>profile.txt</c> in Commons at startup.</summary>
public static class ServerProfile
{
    public static Func<string> Current { get; set; } = () => PanelServerIdentity.DefaultKey;
}
