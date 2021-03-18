namespace ExactReproduction
{
    public class LogPropertyNames
    {
        public string EventId { get; } = "LegacyEventTypeId";
        public string Title { get; } = "MessageTemplate";
        public string Category { get; } = "LegacyEventCategoryName";
        public string Message { get; } = "Message";
        public string ExceptionText { get; } = "Properties";
        public string Computer { get; } = "MachineName";
        public string RegisteredAppId { get; } = "LegacyRegisteredAppId";
        public string Type { get; } = "Level";
    }
}