namespace Application.Helpers
{
    public static class MessageFormatter
    {
        public static string Format(string template, params object[] args)
        {
            return string.Format(template, args);
        }

        public static string FormatError(string message, Exception ex)
        {
            return string.Format(message, ex.Message);
        }

        public static string FormatWithTimeStamp(string message)
        {
            return $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}";
        }
    }
}
