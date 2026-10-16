namespace Ate.Domain.Common;

public readonly record struct Error
{
    public string Code { get; }
    public string Description { get; }
    public ErrorType Type { get; }
    public IReadOnlyList<string>? Details { get; }

    private Error(string code, string description, ErrorType type, IReadOnlyList<string>? details = null)
    {
        Code = code;
        Description = description;
        Type = type;
        Details = details;
    }

    public static Error None => new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);

    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static Error NotFound(string entityName, object id) =>
        new($"{entityName}.NotFound", $"{entityName} with identifier '{id}' was not found.", ErrorType.NotFound);

    public static Error NotFound<TEntity>(object id) =>
        NotFound(typeof(TEntity).Name, id);

    public static Error NotFound(string entityName) =>
        new($"{entityName}.NotFound", $"The requested {entityName} was not found.", ErrorType.NotFound);

    public static Error NotFound<TEntity>() =>
        NotFound(typeof(TEntity).Name);

    public static Error Validation(string code, string description, IReadOnlyList<string>? details = null) =>
        new(code, description, ErrorType.Validation, details);

    public static Error Validation(string entityName, string description) =>
        new($"{entityName}.Validation", description, ErrorType.Validation);

    public static Error Validation<TEntity>(string description) =>
        Validation(typeof(TEntity).Name, description);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    public static Error Conflict(string entityName, string fieldName, object value) =>
        new($"{entityName}.Conflict", $"{entityName} with {fieldName} '{value}' already exists.", ErrorType.Conflict);

    public static Error Conflict<TEntity>(string fieldName, object value) =>
        Conflict(typeof(TEntity).Name, fieldName, value);

    public static Error Unauthorized(string code, string description) =>
        new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string description) =>
        new(code, description, ErrorType.Forbidden);
}
