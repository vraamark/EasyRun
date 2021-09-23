namespace EasyRun.Dialogs
{
    public class DialogProfileParameter : DialogTextParameter
    {
        private bool useTye;

        public bool UseTye
        {
            get { return useTye; }
            set { SetProperty(ref useTye, value); }
        }
    }
}
