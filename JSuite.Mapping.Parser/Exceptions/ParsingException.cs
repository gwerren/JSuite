namespace JSuite.Mapping.Parser.Exceptions
{
    using System;

    public abstract class ParsingException : Exception
    {
        protected ParsingException(string message) : base(message) { }

        protected ParsingException(string message, Exception innerException) : base(message, innerException) { }
    }
}