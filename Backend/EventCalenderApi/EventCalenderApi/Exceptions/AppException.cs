using System.Diagnostics;

namespace EventCalenderApi.Exceptions
{
    /// <summary>
    /// Base application exception. All subclasses are handled by ExceptionMiddleware
    /// and return a clean JSON response — they are NOT unhandled crashes.
    /// The [DebuggerNonUserCode] attribute prevents Visual Studio from breaking
    /// on these expected business-logic exceptions.
    /// </summary>
    [DebuggerNonUserCode]
    public abstract class AppException : Exception
    {
        public int StatusCode { get; }

        protected AppException(string message, int statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}