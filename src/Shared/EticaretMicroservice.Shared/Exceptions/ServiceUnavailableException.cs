namespace EticaretMicroservice.Shared.Exceptions;

public class ServiceUnavailableException : Exception
{
    public ServiceUnavailableException(string message, Exception? innerException = null) : base(message, innerException) { }
}
