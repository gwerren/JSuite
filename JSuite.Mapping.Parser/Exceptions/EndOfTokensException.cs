namespace JSuite.Mapping.Parser.Exceptions
{
    using System;

    public class EndOfTokensException : Exception
    {
        public EndOfTokensException() : base("Unexpected end of tokens encountered.") { }
    }
}