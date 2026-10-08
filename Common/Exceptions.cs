namespace LearnAPI.Common;

/// <summary>Domain/application exceptions. GlobalExceptionHandler maps each one to an HTTP status.</summary>
public class AppException(string message) : Exception(message);

public sealed class UnauthorizedException(string message = "Authentication is required.") : AppException(message);   // 401
public sealed class ForbiddenException(string message = "You do not have permission to do this.") : AppException(message); // 403
public sealed class NotFoundException(string message) : AppException(message);                                         // 404
public sealed class ConflictException(string message) : AppException(message);                                         // 409
public sealed class BusinessRuleException(string message) : AppException(message);                                     // 400
