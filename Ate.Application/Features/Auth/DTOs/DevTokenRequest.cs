namespace Ate.Application.Features.Auth.DTOs;

public record DevTokenRequest(
    string? Role = null,
    string? Email = null,
    string? FullName = null,
    string? DocumentNumber = null);
