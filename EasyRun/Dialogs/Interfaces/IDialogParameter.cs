namespace EasyRun.Dialogs.Interfaces
{
    public interface IDialogParameter
    {
        string Title { get; set; }

        DialogResponse Response { get; set; }
    }
}
