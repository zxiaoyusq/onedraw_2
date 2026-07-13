using OneStrokeDemon.ConfigExporter.Model;

namespace OneStrokeDemon.ConfigExporter.Validation;

internal sealed class ConfigValidator
{
    public void Validate(ConfigDocument document, string? schemaPath = null)
    {
        var context = new ConfigValidationContext(document);
        RowConstraintValidator.Validate(context);
        RelationshipValidator.Validate(context);
        SemanticValidator.Validate(context);
        SchemaConstraintMirrorValidator.Validate(context, schemaPath);
    }
}
