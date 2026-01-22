namespace Umea.se.Toolkit.Images;

public class ImageTooLargeException(string message) : Exception(message)
{
}

public class ImageNotFoundException(string message) : Exception(message)
{
}
