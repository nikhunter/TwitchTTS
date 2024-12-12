using ElevenLabs;
using ElevenLabs.Voices;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using NAudio;
using NAudio.Wave;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using Application = System.Windows.Application;
using System.Media;
using System.Net;
using System.Security.Policy;
using static System.Net.Mime.MediaTypeNames;
using System.Xml;
using System.Windows.Media.Animation;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using TwitchLib.Api.Core.Models.Undocumented.Chatters;
using TwitchTTS.Helpers;
using TwitchLib.Api;
using TwitchLib.Client.Interfaces;
using ElevenLabs.VoiceGeneration;
using System.Windows.Data;
using System.Security.AccessControl;

namespace TwitchTTS
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private static TwitchAPI _twitchAPI;
        private TwitchClient _twitchClient;
        private ElevenLabsClient _elClient;
        private VoiceSettings _defaultVoiceSettings;
        private WaveOut waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback());

        public ObservableCollection<Chatter> Chatters { get; set; } = new ObservableCollection<Chatter>();
        public ObservableCollection<Chatter> ActiveChatters { get; set; } = new ObservableCollection<Chatter>();
        public ObservableCollection<TTSMessage> TTSMessages { get; set; } = new ObservableCollection<TTSMessage>();
        public ObservableCollection<Voice> Voices { get; set; } = new ObservableCollection<Voice>{ new Voice("Random", "Random") };

        private bool _updateAvailable = false;
        public bool UpdateAvailable
        {
            get { return _updateAvailable; }
            set
            {
                if (value != _updateAvailable)
                {
                    _updateAvailable = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _ttsEnabled;
        public bool TTSEnabled
        {
            get { return _ttsEnabled; }
            set
            {
                if (value != _ttsEnabled)
                {
                    _ttsEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _ttsProcessing;
        public bool TTSProcessing
        {
            get { return _ttsProcessing; }
            set
            {
                if (value != _ttsProcessing)
                {
                    _ttsProcessing = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _ttsBusy;
        public bool TTSBusy
        {
            get { return _ttsBusy; }
            set
            {
                if (value != _ttsBusy)
                {
                    _ttsBusy = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<ChatMessage> ChatHistoryCollection { get; set; } = new ObservableCollection<ChatMessage>();

        public MainWindow()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-GB");

            InitializeComponent();
            Loaded += OnMainWindowLoaded;

            Chatters = new ObservableCollection<Chatter>
            {
                //new Chatter { UserId = 0, Username = "Delicious_Cake", TTSEnabled = false },
                new Chatter { Username = "Delicious_Cake" },
                new Chatter { Username = "Yomoghadodo" },
                new Chatter { Username = "PatchesAndPieces" },
                new Chatter { Username = "Scarlettyeah" },
                new Chatter { Username = "KatieLouise" },
                new Chatter { Username = "Teosgame" },
                new Chatter { Username = "zzz" },
                new Chatter { Username = "zzz" },
                new Chatter { Username = "zzz" },
                new Chatter { Username = "zzz" },
                new Chatter { Username = "zzz" },
                new Chatter { Username = "zzz" },
                new Chatter { Username = "zzz" },
                new Chatter { Username = "zzz" }
            };
        }

        private void OnMainWindowLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            // Defer the CheckSettings method until after the UI thread has finished rendering
            Dispatcher.BeginInvoke(new Action(async () =>
            {
                CheckSettings();

                InitializeTwitchClient();
                await InitializeELClient();
                DataContext = this;

                File.WriteAllText(@"chat_history.txt", "");
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void CheckSettings()
        {
            // Access all settings in the appSettings section
            var appSettings = ConfigurationManager.AppSettings;

            // Iterate over all keys in appSettings
            foreach (var key in appSettings.AllKeys)
            {
                var value = appSettings[key];

                // Check if the setting value is null or empty
                if (string.IsNullOrEmpty(value))
                {
                    if (MessageBox.Show("Looks like some settings haven't been configured, would you like to change them?\n\nPressing 'No' will close the application.", "TwitchTTS - Missing settings", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        OpenSettings(null, null);
                        return;
                    }
                    else
                    {
                        Environment.Exit(0);
                    }
                }
            }
        }

        private void OpenSettings(object? sender, RoutedEventArgs? e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }

        private void InitializeTwitchClient()
        {
            var credentials = new ConnectionCredentials(
                ConfigurationManager.AppSettings["TwitchLoginUsername"],
                ConfigurationManager.AppSettings["TwitchLoginOAuthToken"]
                );
            _twitchClient = new TwitchClient();
            _twitchClient.Initialize(credentials, ConfigurationManager.AppSettings["TwitchChannelToMonitor"]);

            _twitchClient.OnMessageReceived += Client_OnMessageReceived;
            _twitchClient.Connect();
        }

        private async Task InitializeELClient()
        {
            _elClient = new ElevenLabsClient(ConfigurationManager.AppSettings["ElevenLabsApiKey"]);

            var voiceList = await _elClient.VoicesEndpoint.GetAllVoicesAsync();
            foreach ( var voice in voiceList )
            {
                Voices.Add(voice);
            }
            OnPropertyChanged(nameof(Voices));
            _defaultVoiceSettings = new VoiceSettings {
                SimilarityBoost = float.Parse(ConfigurationManager.AppSettings["SimilarityBoost"]),
                Stability = float.Parse(ConfigurationManager.AppSettings["Stability"]),
                Style = float.Parse(ConfigurationManager.AppSettings["Style"]),
                SpeakerBoost = bool.Parse(ConfigurationManager.AppSettings["SpeakerBoost"])
            };
        }

        private async void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            var chatMessage = e.ChatMessage;
            if (!Chatters.Any(chatter => chatter.Username == chatMessage.DisplayName))
            {
                var userId = int.Parse(chatMessage.UserId);
                var username = chatMessage.DisplayName;
                var voice = Voices[Random.Shared.Next(0, Voices.Count - 1)];
                // Run this code on the UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Chatters.Add(new Chatter { Username = username, Voice = voice });
                });

                OnPropertyChanged(nameof(Chatters));
            }

            if (TTSEnabled && ActiveChatters.Any(chatter => chatter.Username == chatMessage.Username))
            {
                var chatter = ActiveChatters.First(chatter => chatter.Username == chatMessage.Username);
                _ = Task.Run(() => {
                    ProcessTTSMessage(chatter, chatMessage.Message, chatMessage.ColorHex);
                });
            }
        }

        private async void ProcessTTSMessage(Chatter chatter, string message, string colorHex)
        {
            // Add queued message for specific Chatter (Should be ordered 1, 2, 3, etc. for individual users meaning two users can both have a TTSMessage 1st in queue)
            var queueCount = TTSMessages.Count(ttsMessage => ttsMessage.Chatter == chatter);
            TTSMessages.Add(new TTSMessage {
                QueueNumber = queueCount,
                Chatter = chatter,
                HexColour = colorHex,
                Message = message
            });

            // Process message
            var voiceClip = await _elClient.TextToSpeechEndpoint.TextToSpeechAsync(message, chatter.Voice, _defaultVoiceSettings); // TODO Could use Turbo models for faster processing??

            // Set VoiceClip and set TTSProcessing = false
            var ttsMessage = TTSMessages.First(ttsMessage => ttsMessage.Chatter == chatter && ttsMessage.QueueNumber == queueCount);
            ttsMessage.VoiceClip = voiceClip;
            ttsMessage.TTSProcessing = false;
        }

        // TODO MISSING FUNCTION/ROUTINE THAT FREQUENTLY CHECKS TTSMessages LIST AND PLAYS THE LOWEST QueueNumber MESSAGE PER USER

        private async void PlayTTSMessage(TTSMessage ttsMessage)
        {
            // PLAY TTS MESSAGE
            using (Stream ms = new MemoryStream())
            {
                ms.Write(ttsMessage.VoiceClip.ClipData.ToArray());
                ms.Position = 0;

                using (WaveStream blockAlignedStream =
                    new BlockAlignReductionStream(
                        WaveFormatConversionStream.CreatePcmStream(
                            new Mp3FileReader(ms))))
                {
                    waveOut.Init(blockAlignedStream);

                    File.WriteAllText("chat_history.txt", $"{ttsMessage.Chatter.Username}: {ttsMessage.Message}");
                    AddChatMessage(ttsMessage.Chatter.Username, ttsMessage.Message, ttsMessage.HexColour);

                    if (TTSEnabled) // Check the global TTS enabled flag before playing
                    {
                        waveOut.Play();

                        // Continuously check if playback should stop
                        while (waveOut.PlaybackState == PlaybackState.Playing)
                        {
                            if (!TTSEnabled || !ttsMessage.Chatter.TTSActive) // If either flag is false, stop playback
                            {
                                waveOut.Stop(); // Stop the playback if conditions are met
                                break;
                            }

                            await Task.Delay(50); // Use async delay to not block the UI thread
                        }
                    }
                }
            }
        }

        private void AddChatMessage(string username, string message, string colorHex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ChatHistoryCollection.Clear(); // TEMPORARY, TO USE OBS TO DISPLAY THE CHAT LOG
                ChatHistoryCollection.Add(new ChatMessage { UsernameColour = ConvertHexToBrush(colorHex), Username = username, Message = message });
                ChatScroll.ScrollToBottom();
            });
        }

        private void ChatHistoryBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox? textbox = sender as TextBox;
            if (textbox != null)
            {
                textbox.ScrollToEnd();
            }
        }

        private void ToggleTTSButton_Click(object sender, RoutedEventArgs e)
        {
            TTSEnabled = !TTSEnabled;
            ToggleTTSButton.Content = TTSEnabled ? "Stop TTS" : "Start TTS";

            Button? button = sender as Button;
            if (button != null)
            {
                if (TTSEnabled)
                {
                    button.Background = Brushes.Red;

                    File.WriteAllText(@"chat_history.txt", "");
                    ChatHistoryCollection.Clear();
                }
                else
                {
                    button.Background = Brushes.Green;

                    ChatHistoryCollection.Clear(); // TEMPORARY, TO USE OBS TO DISPLAY THE CHAT LOG
                    File.WriteAllText(@"chat_history.txt", "");
                }
            }
        }

        private void ChatterCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Get the Chatter associated with this CheckBox
            if (sender is CheckBox checkBox && checkBox.DataContext is Chatter chatter)
            {
                var viewModel = DataContext as MainWindow;

                // Move Chatter from Chatters to ActiveChatters
                if (viewModel.Chatters.Contains(chatter))
                {
                    viewModel.Chatters.Remove(chatter);
                    viewModel.ActiveChatters.Add(chatter);
                }
            }
        }

        private void ChatterCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // Get the Chatter associated with this CheckBox
            if (sender is CheckBox checkBox && checkBox.DataContext is Chatter chatter)
            {
                var viewModel = DataContext as MainWindow;

                // Move Chatter from ActiveChatters back to Chatters
                if (viewModel.ActiveChatters.Contains(chatter))
                {
                    viewModel.ActiveChatters.Remove(chatter);
                    viewModel.Chatters.Add(chatter);
                }
            }
        }

        private SolidColorBrush ConvertHexToBrush(string hex) => (SolidColorBrush)new BrushConverter().ConvertFromString(hex);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ChatMessage
    {
        public Brush UsernameColour { get; set; }
        public string Username { get; set; }
        public string Message { get; set; }
    }
}
