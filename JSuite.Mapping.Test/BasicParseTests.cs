namespace JSuite.Mapping.Test
{
    using System;
    using System.Linq;
    using JSuite.Mapping.Parser.Exceptions;
    using JSuite.Mapping.Parser.Parsing;
    using JSuite.Mapping.Parser.Tokenizing;
    using JSuite.Mapping.Parser.Tokenizing.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class BasicParseTests
    {
        [TestMethod]
        public void UnexpectedTokenTest()
        {
            AssertEx.ThrowsException(
                () => ParseScript("=A[B$(applicantId)]"),
                CheckBadTokenException(1, 4, "B"));

            AssertEx.ThrowsException(
                () => ParseScript("=A[$(applicantId),B$(applicantId)]"),
                CheckBadTokenException(1, 19, "B"));

            AssertEx.ThrowsException(
                () => ParseScript($"=A{Environment.NewLine}  [B$(applicantId)]"),
                CheckBadTokenException(2, 4, "B"));
        }

        private static void ParseScript(string script)
        {
            var statements = MappingTokenizer
                .Tokenize(script)
                .ApplyModifications()
                .ToStatements()
                .ApplyPartials()
                .Parse(new TextIndexHelper(script))
                .ToList();
        }

        private static Action<UnexpectedTokenException> CheckBadTokenException(
            int line,
            int column,
            string value)
            => ex =>
            {
                Assert.IsNotNull(ex.Tokens);
                Assert.AreEqual(1, ex.Tokens.Count);

                var badToken = ex.Tokens[0];
                Assert.IsTrue(badToken.Location.HasValue);
                Assert.AreEqual(line, badToken.Location.Value.Line);
                Assert.AreEqual(column, badToken.Location.Value.Column);
                Assert.AreEqual(value, badToken.Value);
            };
    }

    public static class AssertEx
    {
        public static void ThrowsException<TException>(
            Action action,
            Action<TException> checks,
            string message = null)
            where TException : Exception
        {
            try
            {
                action();
                Assert.Fail(message ?? $"Expected exception {typeof(TException).Name} but none was thrown.");
            }
            catch (TException ex)
            {
                checks(ex);
            }
        }
    }
}
