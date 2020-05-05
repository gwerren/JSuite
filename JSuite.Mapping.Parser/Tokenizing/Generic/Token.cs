namespace JSuite.Mapping.Parser.Tokenizing.Generic
{
    public readonly struct Token<TType>
    {
        public Token(TType type, string value)
        {
            this.Type = type;
            this.Value = value;
        }

        public TType Type { get; }

        public string Value { get; }
    }
}