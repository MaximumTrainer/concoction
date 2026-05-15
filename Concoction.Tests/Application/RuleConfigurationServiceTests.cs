using Concoction.Application.Configuration;
using Concoction.Domain.Models;
using FluentAssertions;

namespace Concoction.Tests.Application;

public sealed class RuleConfigurationServiceTests
{
    [Fact]
    public void Merge_ShouldApplyUserPrecedence()
    {
        var service = new RuleConfigurationService();
        var defaults = new RuleConfiguration
        {
            Version = "1",
            Tables =
            [
                new TableRule
                {
                    Table = "main.users",
                    Columns = [new ColumnRule { Column = "email", Strategy = "pseudonymize" }]
                }
            ]
        };

        var schema = new RuleConfiguration
        {
            Version = "1",
            Tables =
            [
                new TableRule
                {
                    Table = "main.users",
                    Columns = [new ColumnRule { Column = "email", Strategy = "synthesize" }]
                }
            ]
        };

        var user = new RuleConfiguration
        {
            Version = "1",
            Tables =
            [
                new TableRule
                {
                    Table = "main.users",
                    Columns = [new ColumnRule { Column = "email", Strategy = "fixed", FixedValue = "x@x.com" }]
                }
            ]
        };

        var merged = service.Merge(defaults, schema, user);

        merged.Tables.Should().HaveCount(1);
        merged.Tables[0].Columns.Should().ContainSingle();
        merged.Tables[0].Columns[0].FixedValue.Should().Be("x@x.com");
    }
}
