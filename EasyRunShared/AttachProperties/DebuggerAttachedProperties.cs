using System.Windows;

namespace EasyRun.AttachProperties
{
    public static class DebuggerAttachedProperties
    {
        public static readonly DependencyProperty DebuggerAttachedProperty
                            = DependencyProperty.RegisterAttached("DebuggerAttached", typeof(bool), typeof(DebuggerAttachedProperties),
                                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        public static bool GetDebuggerAttached(DependencyObject element)
            => (bool)element.GetValue(DebuggerAttachedProperty);

        public static void SetDebuggerAttached(DependencyObject element, bool value)
            => element.SetValue(DebuggerAttachedProperty, value);
    }
}
