using System;

namespace MenuView.Helpers
{
    /// <summary>
    /// This class is needed to ensure that the ControlzEx library is copy to the resulting solution.
    /// For more details check https://stackoverflow.com/a/39755840/5103334.
    /// </summary>
    public static class CopyDLL
    {
        #region Public Constructors

        static CopyDLL()
        {
            static void dummy(Type _) { }

            var class1 = typeof(Fluent.Ribbon);
            dummy(class1);

            var class2 = typeof(ControlzEx.BadgedEx);
            dummy(class2);
        }

        #endregion Public Constructors
    }
}