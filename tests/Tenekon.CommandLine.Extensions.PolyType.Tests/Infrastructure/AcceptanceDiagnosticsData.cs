using System.Collections;

namespace Tenekon.CommandLine.Extensions.PolyType.Tests.Infrastructure;

public sealed class AcceptanceDiagnosticsData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        foreach (var caseResult in AcceptanceFixtureCache.Instance.DiagnosticCases)
            yield return new object[] { caseResult };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
