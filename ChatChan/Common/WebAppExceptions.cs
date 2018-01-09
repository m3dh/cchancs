namespace ChatChan.Common
{
    using System;

    public class ClientInputException : Exception
    {
        public ClientInputException(string param) : base($"Unexpected parameter input for {param}")
        {
        }

        public ClientInputException(Exception ex) : base("Exception happened while parsing the request.", ex)
        {
        }
    }

    public class NotFoundException : Exception
    {
        public NotFoundException(string message): base(message)
        {
        }
    }

    public class DuplicatedException : Exception
    {
    }
}
