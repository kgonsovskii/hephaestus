namespace Commons;

/// <summary>Fire-and-forget notify to sites-host after a successful bot upsert.</summary>
public interface ISitesGoalNotifier
{
    void Notify(string ipAddress);
}
