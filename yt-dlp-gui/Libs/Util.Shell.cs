using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Libs {
    public partial class Util {
        /* 送出 Ctrl+C -_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_ */
        private enum CtrlTypes : uint {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }
        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate? HandlerRoutine, bool Add);
        private delegate bool ConsoleCtrlDelegate(CtrlTypes CtrlType);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GenerateConsoleCtrlEvent(CtrlTypes dwCtrlEvent, uint dwProcessGroupId);

        public static void SendCtrlC(Process process) {
            if (AttachConsole((uint)process.Id)) {
                SetConsoleCtrlHandler(null, true);

                //GenerateConsoleCtrlEvent(CtrlTypes.CTRL_C_EVENT, (uint)process.Id);
                GenerateConsoleCtrlEvent(CtrlTypes.CTRL_C_EVENT, (uint)0);

                FreeConsole();
                process.WaitForExit();

                SetConsoleCtrlHandler(null, false);
            }
        }
        /* -_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_ */
        public static async Task Explorer(string path) {
            await Task.Run(() => {
                if (File.Exists(path)) {
                    Process.Start("explorer.exe", string.Format("/select,\"{0}\"", path));
                } else if (Directory.Exists(path)) {
                    Process.Start("explorer.exe", string.Format("/e,\"{0}\"", path));
                }
            });
        }
    }
}
