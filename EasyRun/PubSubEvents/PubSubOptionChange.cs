namespace EasyRun.PubSubEvents
{
    public class PubSubOptionChange
    {
        public PubSubOptionChange(string optionId, string oldValue, string newValue)
        {
            OptionId = optionId;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public string OptionId { get; set; }
        public string NewValue { get; set; }
        public string OldValue { get; set; }
    }
}
