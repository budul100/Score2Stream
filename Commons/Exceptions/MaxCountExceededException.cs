using System;

namespace Score2Stream.Commons.Exceptions
{
    public class MaxCountExceededException
        : Exception
    {
        #region Public Constructors

        public MaxCountExceededException()
            : base()
        { }

        public MaxCountExceededException(string message)
            : base(message)
        { }

        public MaxCountExceededException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public MaxCountExceededException(Type type, int maxCount)
            : base($"The maximum number {maxCount} of {type.Name} has been exceeded.")
        { }

        #endregion Public Constructors
    }
}