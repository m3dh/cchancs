namespace ChatChan.Common
{
    using System;

    public class BadRequest : Exception
    {
        public BadRequest(string param) : base($"Unexpected parameter input for {param}")
        {
        }

        public BadRequest(string param, string value) : base($"Unexpected parameter input for {param} : {value}")
        {
        }

        public BadRequest(Exception ex) : base("Exception happened while parsing the request.", ex)
        {
        }
    }

    public class NotAllowed : Exception
    {
        public NotAllowed(string message) : base(message)
        {
        }
    }

    public class NotFound : Exception
    {
        public NotFound(string message): base(message)
        {
        }
    }

    public class Conflict : Exception
    {
        public enum Code
        {
            Duplication = 0,
            RaceCondition = 1,
        }

        public Code ErrorCode { get; set; }

        public Conflict(Code code, string message) : base(message)
        {
        }
    }

    public class ServiceUnavailable : Exception
    {
        public ServiceUnavailable(string message) : base(message)
        {
        }
    }

    public class NotModified : Exception
    {
        public NotModified(string message) : base(message)
        {
        }
    }
}
