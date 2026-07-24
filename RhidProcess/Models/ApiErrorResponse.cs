namespace RhidProcess.Models;

public sealed record ApiErrorResponse(
    string ErrorId,
    string Code,
    string Stage,
    string Message);
