namespace Domain.Exceptions;

public class DuplicateDomainMemberException(string message) : Exception(message);
public class NotFoundException(string message) : Exception(message);
public class UnauthorizedException(string message) : Exception(message);
