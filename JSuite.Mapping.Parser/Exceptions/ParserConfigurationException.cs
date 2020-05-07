namespace JSuite.Mapping.Parser.Exceptions
{
    using System;

    public class ParserConfigurationException : Exception
    {
        private ParserConfigurationException(string message) : base(message) { }

        public static ParserConfigurationException NotCompleted()
            => new ParserConfigurationException("Parser configuration has not been completed.");

        public static ParserConfigurationException AlreadyCompleted()
            => new ParserConfigurationException("Configuration has been completed, no changes can be made.");

        public static ParserConfigurationException RuleNotDefined(string rule)
            => new ParserConfigurationException($"The required rule '{rule}' has not been defined.");
    }
}