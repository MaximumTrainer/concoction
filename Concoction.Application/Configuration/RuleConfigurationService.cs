using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Concoction.Application.Abstractions;
using Concoction.Domain.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Concoction.Application.Configuration;

public sealed class RuleConfigurationService : IRuleConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public RuleConfiguration Load(string path)
    {
        var text = File.ReadAllText(path);
        var extension = Path.GetExtension(path);

        return extension.ToLowerInvariant() switch
        {
            ".yaml" or ".yml" => LoadYaml(text),
            _ => JsonSerializer.Deserialize<RuleConfiguration>(text, JsonOptions)
                 ?? throw new InvalidOperationException("Failed to deserialize JSON rule configuration.")
        };
    }

    public IReadOnlyList<string> Validate(RuleConfiguration configuration)
    {
        var validationContext = new ValidationContext(configuration);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(configuration, validationContext, results, true);

        foreach (var table in configuration.Tables)
        {
            Validator.TryValidateObject(table, new ValidationContext(table), results, true);
            foreach (var column in table.Columns)
            {
                Validator.TryValidateObject(column, new ValidationContext(column), results, true);
            }
        }

        return results
            .Select(static result => result.ErrorMessage)
            .Where(static message => !string.IsNullOrWhiteSpace(message))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public RuleConfiguration Merge(RuleConfiguration defaults, RuleConfiguration schemaDerived, RuleConfiguration user)
    {
        var map = new Dictionary<string, Dictionary<string, ColumnRule>>(StringComparer.OrdinalIgnoreCase);

        Apply(defaults);
        Apply(schemaDerived);
        Apply(user);

        var tables = map
            .OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static table => new TableRule
            {
                Table = table.Key,
                Columns = table.Value.OrderBy(static c => c.Key, StringComparer.OrdinalIgnoreCase).Select(static c => c.Value).ToArray()
            })
            .ToArray();

        return new RuleConfiguration { Version = user.Version, Tables = tables };

        void Apply(RuleConfiguration config)
        {
            foreach (var table in config.Tables)
            {
                if (!map.TryGetValue(table.Table, out var columns))
                {
                    columns = new Dictionary<string, ColumnRule>(StringComparer.OrdinalIgnoreCase);
                    map[table.Table] = columns;
                }

                foreach (var column in table.Columns)
                {
                    columns[column.Column] = column;
                }
            }
        }
    }

    private static RuleConfiguration LoadYaml(string text)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<RuleConfiguration>(text)
               ?? throw new InvalidOperationException("Failed to deserialize YAML rule configuration.");
    }
}
