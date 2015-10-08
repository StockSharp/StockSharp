namespace Terminal
{
    using System;
    using System.Diagnostics;

    public static class Helpers
    {

        public static void ToTraceError(this Exception ex)
        {
            Trace.WriteLine(string.Format("{0} /{1}", ex.Message, ex.StackTrace), DateTime.Now.TimeOfDay.ToString());
        }

        public static void ToTraceInfo(this string message)
        {
            Trace.WriteLine(string.Format("{0}", message), DateTime.Now.TimeOfDay.ToString());
        }

    }
}
