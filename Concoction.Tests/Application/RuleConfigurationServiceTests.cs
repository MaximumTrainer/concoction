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

    [Fact]
    public void Validate_ShouldReturnError_WhenStrategyIsUnknownDataKind()
    {
        var service = new RuleConfigurationService();
        var config = new RuleConfiguration
        {
            Version = "1",
            Tables =
            [
                new TableRule
                {
                    Table = "main.users",
                    Columns = [new ColumnRule { Column = "name", Strategy = "NotARealDataKind" }]
                }
            ]
        };

        var errors = service.Validate(config);

        errors.Should().ContainSingle(e => e.Contains("NotARealDataKind"));
    }

    [Fact]
    public void Validate_ShouldReturnNoErrors_WhenStrategyIsValidDataKind()
    {
        var service = new RuleConfigurationService();
        var config = new RuleConfiguration
        {
            Version = "1",
            Tables =
            [
                new TableRule
                {
                    Table = "main.users",
                    Columns = [new ColumnRule { Column = "contact", Strategy = "Email" }]
                }
            ]
        };

        var errors = service.Validate(config);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldReturnNoErrors_WhenStrategyIsNull()
    {
        var service = new RuleConfigurationService();
        var config = new RuleConfiguration
        {
            Version = "1",
            Tables =
            [
                new TableRule
                {
                    Table = "main.users",
                    Columns = [new ColumnRule { Column = "status" }]
                }
            ]
        };

        var errors = service.Validate(config);

        errors.Should().BeEmpty();
    }
}
