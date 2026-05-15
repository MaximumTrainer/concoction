using Concoction.Application.Abstractions;
using Concoction.Domain.Enums;
using Concoction.Domain.Models;

namespace Concoction.Application.Compliance;

public sealed class DefaultSensitiveFieldPolicy : ISensitiveFieldPolicy
{
    private static readonly (string Pattern, string Classification, SensitiveFieldStrategy Strategy)[] PatternRules =
    [
        ("email", "PII.Email", SensitiveFieldStrategy.Pseudonymize),
        ("phone", "PII.Phone", SensitiveFieldStrategy.Tokenize),
        ("ssn", "PII.SSN", SensitiveFieldStrategy.Redact),
        ("mrn", "PHI.MRN", SensitiveFieldStrategy.Redact),
        ("dob", "PHI.DOB", SensitiveFieldStrategy.Synthesize)
    ];

    public ComplianceDecision Evaluate(string table, ColumnSchema column)
    {
        var name = column.Name.ToLowerInvariant();

        foreach (var rule in PatternRules)
        {
            if (name.Contains(rule.Pattern, StringComparison.Ordinal))
            {
                return new ComplianceDecision(table, column.Name, rule.Strategy, rule.Classification, $"Matched pattern '{rule.Pattern}'.");
            }
        }

        return new ComplianceDecision(table, column.Name, SensitiveFieldStrategy.None, "None", "No sensitive pattern matched.");
    }
}
