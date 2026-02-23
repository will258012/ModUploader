using HarmonyLib;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using NLog;
using System.Reflection;
using WinRT.Interop;
using Path = System.IO.Path;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ModUploader
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public static Logger Logger { get; } = LogManager.GetLogger("ModUploader");
        public static Assembly AssemblyCSharp { get; private set; }
        public static Assembly ColossalManaged { get; private set; }

        public static MainWindow MainWindow { get; private set; }
        internal static AppWindow appWindow;
        private static bool hasPatched;
        private static Mutex mutex;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>

        public App()
        {
            try
            {
                mutex = new Mutex(true, "Global\\ModUploader", out var createdNew);
                if (!createdNew)
                {
                    Utils.ActivateExistingWindow();
                    Environment.Exit(0);
                    return;
                }

                Logger.Info($"Mod Uploader started. v{Assembly.GetExecutingAssembly().GetName().Version}");

                InitializeComponent();
            }
            catch (Exception e)
            {
                Logger.Fatal(e);
            }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            try
            {
                MainWindow = new MainWindow();
                MainWindow.Activate();
                appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(WindowNative.GetWindowHandle(MainWindow)));
            }
            catch (Exception e)
            {
                Logger.Fatal(e);
            }
        }
        internal static void ApplyHarmonyPatches()
        {
            if (hasPatched) return;
            var harmony = new Harmony("ModUploader.Patches");
            harmony.PatchAll();
            hasPatched = true;
            Logger.Info("Successfully patched");
        }
        internal static void LoadAssembly()
        {
            Logger.Info("Loading assemblies of CSL");
            try
            {
                var cslPath = UploadHelper.GetCSLPath();
                var assembliesPath = Path.Combine(cslPath, "Cities_Data", "Managed");
                var assemblyCSharpPath = Path.Combine(assembliesPath, "Assembly-CSharp.dll");
                var ColossalManagedPath = Path.Combine(assembliesPath, "ColossalManaged.dll");
                var unityEnginePath = Path.Combine(assembliesPath, "UnityEngine.dll");

                if (!File.Exists(assemblyCSharpPath) || !File.Exists(ColossalManagedPath) || !File.Exists(unityEnginePath))
                {
                    throw new FileNotFoundException($"Some of necessary assemblies not found.");
                }

                AssemblyCSharp = Assembly.LoadFrom(assemblyCSharpPath);
                ColossalManaged = Assembly.LoadFrom(ColossalManagedPath);
                _ = Assembly.LoadFrom(unityEnginePath);
                _ = Assembly.LoadFrom(Path.Combine(assembliesPath, "mscorlib.dll"));

            }
            catch (Exception e)
            {
                throw new Exception("Failed to load assemblies.", e);
            }
        }
    }
}
