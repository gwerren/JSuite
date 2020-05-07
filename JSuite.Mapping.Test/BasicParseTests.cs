namespace JSuite.Mapping.Test
{
    using System;
    using JSuite.Mapping.Parser;
    using JSuite.Mapping.Parser.Exceptions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class BasicParseTests
    {
        [TestMethod]
        public void UnexpectedTokenTest()
        {
            AssertEx.ThrowsException<UnexpectedTokenWithPositionContextException>(
                () => Mapper.ParseScript("=A[B$(applicantId)]"),
                ex =>
                {
                    Assert.AreEqual(1, ex.Line);
                    Assert.AreEqual(4, ex.Column);
                    Assert.AreEqual("B", ex.TokenValue);
                });

            AssertEx.ThrowsException<UnexpectedTokenWithPositionContextException>(
                () => Mapper.ParseScript("=A[$(applicantId),B$(applicantId)]"),
                ex =>
                {
                    Assert.AreEqual(1, ex.Line);
                    Assert.AreEqual(19, ex.Column);
                    Assert.AreEqual("B", ex.TokenValue);
                });

            AssertEx.ThrowsException<UnexpectedTokenWithPositionContextException>(
                () => Mapper.ParseScript($"=A{Environment.NewLine}  [B$(applicantId)]"),
                ex =>
                {
                    Assert.AreEqual(2, ex.Line);
                    Assert.AreEqual(4, ex.Column);
                    Assert.AreEqual("B", ex.TokenValue);
                });
        }
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
