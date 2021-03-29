//using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StandaloneGeneratorV3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Game> gamesList;
        private ObservableCollection<RepoPatch> selectedPatchesList;
        private Logger logger;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize logging
            this.logger = new Logger(this.Dispatcher, this.uiLogWindow);

            gamesList = GamesList.Load();
            this.uiGamesList.ItemsSource = gamesList;

            selectedPatchesList = new ObservableCollection<RepoPatch>();
            uiSelectedPatches.ItemsSource = selectedPatchesList;

            Task.Run(() =>
            {
                List<Repo> repo_list = Repo.Discovery("https://srv.thpatch.net/");
                this.Dispatcher.Invoke(() => this.uiRepos.ItemsSource = repo_list);
                logger.LogLine("Repo discovery finished");
            });
        }

        private void SelectRepo(object sender, MouseButtonEventArgs e)
        {
            var patch = (sender as TextBlock).DataContext as RepoPatch;
            selectedPatchesList.Add(patch);
        }

        private void CreateExe(string game_id, string icon_path)
        {
            File.Copy(AppContext.BaseDirectory + @"res\thcrap\thcrap_loader.exe", game_id + ".exe");

            using (PeResourceUpdater exe = new PeResourceUpdater(game_id + ".exe"))
            {
                exe.ReplaceStringTable(new List<string>()
                {
                    @"thcrap\bin\",
                    string.Format(@"thcrap\bin\thcrap_loader.exe {0}.js {1}", this.uiConfigName.Text, game_id),
                    "thcrap_loader.exe"
                });
                if (icon_path != null)
                    exe.ReplaceIcon(icon_path);
            }
        }

        private async Task CreateStandalonePatchForGame(Game game)
        {
            logger.LogLine("Generating standalone for " + game.Name + "...");
            Directory.CreateDirectory(game.Id);
            Environment.CurrentDirectory = game.Id;

            foreach (RepoPatch patch in selectedPatchesList)
                await Task.Run(() => patch.AddToStack());

            await Task.Run(() => ThcrapUpdateDll.stack_update(
                (string fn, IntPtr filter_data) => (fn.Contains('/') == false) || fn.StartsWith(game.Id + "/") ? 1 : 0, IntPtr.Zero,
                (IntPtr status_, IntPtr param) =>
                {
                    var status = Marshal.PtrToStructure<ThcrapUpdateDll.progress_callback_status_t>(status_);
                    switch (status.status)
                    {
                        case ThcrapUpdateDll.get_status_t.GET_DOWNLOADING:
                        case ThcrapUpdateDll.get_status_t.GET_CANCELLED:
                            break;
                        case ThcrapUpdateDll.get_status_t.GET_OK:
                            var patch = Marshal.PtrToStructure<ThcrapDll.patch_t>(status.patch);
                            string patch_id = Marshal.PtrToStringAnsi(patch.id);
                            logger.LogLine(string.Format("[{0}/{1}] {2}/{3}: OK ({4}b)",
                                status.nb_files_downloaded, status.nb_files_total,
                                patch_id, status.fn, status.file_size));
                            break;
                        case ThcrapUpdateDll.get_status_t.GET_CLIENT_ERROR:
                        case ThcrapUpdateDll.get_status_t.GET_SERVER_ERROR:
                        case ThcrapUpdateDll.get_status_t.GET_SYSTEM_ERROR:
                            logger.LogLine(status.url + " : " + status.error);
                            break;
                        case ThcrapUpdateDll.get_status_t.GET_CRC32_ERROR:
                            logger.LogLine(status.url + " : CRC32 error");
                            break;
                    }
                    return true;
                }, IntPtr.Zero)
            );

            var runconfig = new Runconfig();
            foreach (RepoPatch patch in selectedPatchesList)
                runconfig.patches.Add(new RunconfigPatch(patch.Archive));
            runconfig.Save(uiConfigName.Text);

            var gamesJs = new Dictionary<string, string>()
            {
                { game.Id, "../" + game.Id + ".exe" },
                { game.Id + "_custom", "../" + game.Id + "_custom.exe" },
            };
            string jsonGamesJs = JsonSerializer.Serialize(gamesJs, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("config/games.js", jsonGamesJs);

            CreateExe(game.Id, AppContext.BaseDirectory + @"res\Icon_th18.png");
            CreateExe(game.Id + "_custom", null);

            ThcrapDll.stack_free();
            Environment.CurrentDirectory = "..";
            logger.LogLine("Standalone for " + game.Name + " generated!");
        }
        private async void GenerateStandalonePatch(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists("out"))
                Directory.Delete("out", true);
            Directory.CreateDirectory("out");
            Environment.CurrentDirectory = "out";

            // TODO: download thcrap, then unzip it at some point

            foreach (Game game in gamesList)
                if (game.IsSelected)
                    await CreateStandalonePatchForGame(game);

            Environment.CurrentDirectory = "..";
        }
    }
}
