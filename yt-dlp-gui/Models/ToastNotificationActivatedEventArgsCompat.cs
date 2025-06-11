namespace yt_dlp_gui.Models
{
    public class ToastNotificationActivatedEventArgsCompat
    {
        // Minimal placeholder class
        public string Argument { get; set; }
        // public Windows.UI.Notifications.NotificationUserInput UserInput { get; set; }

        public ToastNotificationActivatedEventArgsCompat()
        {
            Argument = string.Empty; // Initialize the property
        }
    }
}
