using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;
using Swordfish.NET.Collections.Auxiliary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Shell;
using WK.Libraries.SharpClipboardNS;
using yt_dlp_gui.Controls;
using yt_dlp_gui.Models;
using yt_dlp_gui.Wrappers;

namespace yt_dlp_gui.Views
{
    public partial class Main : Window
    {
        // Note: This is a placeholder for the original 'Data' object logic.
        // You may need to replace this with the actual implementation if it was more complex.
        private readonly Config Data = MyApplication.AppConfig;

        private List<DLP> RunningDLP = new();
        public ObservableCollection<DownloadItem> DownloadQueue { get; set; }
        private bool _isProcessingQueue = false;
        private readonly object _queueLock = new object();

        // Constructor and other methods from the original file should be here.
        // This is a simplified reconstruction based on the provided log and code.
        // The following methods are placeholders for the logic you had.
        public Main()
        {
            InitializeComponent();
            DownloadQueue = new ObservableCollection<DownloadItem>();
            DownloadQueueDataGrid.DataContext = this;
            ToastNotificationManagerCompat.OnActivated += ToastNotificationManagerCompat_OnActivated;
        }

        private void ToastNotificationManagerCompat_OnActivated(ToastNotificationActivatedEventArgsCompat e)
        {
            // Your notification logic here
        }

        // Placeholder for the rest of your methods from Main.xaml.cs
        // e.g., Analyze_Start(), Download_Start_Native(), etc.
        // You should paste the rest of your original methods from the file here.
    }
}
