using FluentAssertions;
using Git;

namespace Tests;

[TestClass]
public sealed class HephaestusDataGitAuthTests
{
    [TestMethod]
    [Timeout(120_000)]
    public void Diagnose_github_pat_read_and_push()
    {
        var report = HephaestusDataGitAuthDiagnostics.Diagnose();
        Console.WriteLine(report);
        if (!string.IsNullOrEmpty(report.FailureHint))
            Console.WriteLine(report.FailureHint);

        report.CanRead.Should().BeTrue(
            because: $"PAT must read hephaestus_data. {report.ReadRemote.Detail}");
        report.CanPush.Should().BeTrue(
            because: $"PAT must push hephaestus_data. {report.FailureHint} {report.PushDryRun.Detail}");
    }
}
