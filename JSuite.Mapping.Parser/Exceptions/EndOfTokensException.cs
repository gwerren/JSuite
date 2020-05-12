namespace JSuite.Mapping.Parser.Exceptions
{
    public class EndOfTokensException : ParsingException
    {
        public EndOfTokensException() : base("Unexpected end of tokens encountered.") { }
    }
}