using Labb_3.Command;
using Labb_3.Dialogs;
using Labb_3.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;


namespace Labb_3.ViewModel
{
    internal class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<QuestionPackViewModel> Packs { get; set; }
        private readonly ImportOpenTriviaDatabase importOpenTriviaDatabase;
        public ObservableCollection<ImportOpenTriviaDatabase.Category> Categories { get; set; } = new ObservableCollection<ImportOpenTriviaDatabase.Category>();
        public ObservableCollection<Question> ImportedQuestions { get; set; } = new ObservableCollection<Question>();
        public IEnumerable<Difficulty> Difficulties => Enum.GetValues(typeof(Difficulty)).Cast<Difficulty>();
        public ConfigurationViewModel ConfigurationViewModel { get; }
        public PlayerViewModel PlayerViewModel { get; }

        private ImportOpenTriviaDatabase.Category _selectedCategory;

        public string filePath;

        private int _numberOfQuestions = 1;
        public int NumberOfQuestions
        {
            get => _numberOfQuestions;
            set
            {
                _numberOfQuestions = value;
                RaisePropertyChanged();
            }
        }

        public ImportOpenTriviaDatabase.Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                RaisePropertyChanged();
            }
        }


        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                RaisePropertyChanged();
                ShowConfigurationViewCommand.RaiseCanExecuteChanged();
            }
        }


        private QuestionPackViewModel? _activePack;

        public QuestionPackViewModel? ActivePack
        {
            get => _activePack;
            set
            {
                _activePack = value;
                RaisePropertyChanged(nameof(ActivePack));
                ConfigurationViewModel.RaisePropertyChanged(nameof(ActivePack));
            }
        }

        private QuestionPackViewModel? _newPack;

        public QuestionPackViewModel? NewPack
        {
            get => _newPack;
            set
            {
                _newPack = value;
                RaisePropertyChanged();
            }
        }

        public MainWindowViewModel()
        {
            importOpenTriviaDatabase = new ImportOpenTriviaDatabase();

            var directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Labb_3 Quiz");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }


            filePath = Path.Combine(directoryPath, "packs.json");

            LoadPacksFromFile(filePath);

            ConfigurationViewModel = new ConfigurationViewModel(this);

            if (Packs.Any())
            {
                ActivePack = Packs.First();
                RaisePropertyChanged(nameof(ActivePack));
            }
            else
            {
                ActivePack = new QuestionPackViewModel(new QuestionPack("Question Pack# 1", Difficulty.Medium, 30));
            }

            NewPack = new QuestionPackViewModel(new QuestionPack("<New Question Pack>", Difficulty.Medium, 30));

            PlayerViewModel = new PlayerViewModel(this);


            Packs = new ObservableCollection<QuestionPackViewModel>();
            OpenCreateNewPackDialogCommand = new DelegateCommand(OpenCreateNewPackDialog);

            ToggleFullScreenCommand = new DelegateCommand(ToggleFullScreen);
            ExitGameCommand = new DelegateCommand(ExitGame);

            CreateNewPackCommand = new DelegateCommand(CreateNewPack);
            RemoveQuestionPackCommand = new DelegateCommand(RemoveQuestionPack, CanRemoveQuestionPack);
            SelectPackCommand = new DelegateCommand(SelectedPack);

            ShowPlayerViewCommand = new DelegateCommand(ShowPlayerView, CanShowPlayerView);
            ShowConfigurationViewCommand = new DelegateCommand(ShowConfigurationView, CanShowConfigurationView);

            LoadCategoriesCommand = new DelegateCommand(async _ => await LoadCategories());
            ImportQuestionsCommand = new DelegateCommand(async (obj) => await ImportQuestions(obj));
            OpenTriviaDialogCommand = new DelegateCommand(_ => OpenTriviaDialog());

            Packs.Add(ActivePack);

            CurrentView = ConfigurationViewModel;
        }


        public async Task LoadCategories()
        {
            try
            {
                var categories = await importOpenTriviaDatabase.GetCategoriesAsync();
                Categories.Clear();
                foreach (var category in categories)
                {
                    Categories.Add(category);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading categories: {ex.Message}");
            }
        }

        private Question ConvertToQuestion(ImportOpenTriviaDatabase.TriviaQuestion triviaQuestion)
        {
            // Kontrollera om IncorrectAnswers är null och skapa en tom lista om så är fallet
            var incorrectAnswers = triviaQuestion.IncorrectAnswers ?? new List<string>();


            return new Question(
                triviaQuestion.Question,
                triviaQuestion.CorrectAnswer,
                incorrectAnswers.ElementAtOrDefault(0),
                incorrectAnswers.ElementAtOrDefault(1),
                incorrectAnswers.ElementAtOrDefault(2)
            );
        }

        public async Task ImportQuestions(object obj)
        {
            if (SelectedCategory != null)
            {
                try
                {

                    var triviaQuestions = await importOpenTriviaDatabase.GetQuestionsAsync(SelectedCategory.Id.ToString(), "medium", NumberOfQuestions);
                    ImportedQuestions.Clear();
                    foreach (var triviaQuestion in triviaQuestions)
                    {
                        var question = ConvertToQuestion(triviaQuestion);
                        ImportedQuestions.Add(question);
                    }

                    Debug.WriteLine($"Imported {ImportedQuestions.Count} questions."); // Logga antalet importerade frågor

                    // Lägg till importerade frågor till ditt aktiva paket
                    if (ActivePack != null)
                    {
                        foreach (var question in ImportedQuestions)
                        {
                            ActivePack.Questions.Add(question);
                        }
                        ActivePack.Difficulty = Difficulty.Medium;
                        RaisePropertyChanged(nameof(ActivePack));
                    }

                    // Visa en ruta om importen är lyckad
                    MessageBox.Show($"Successfully imported {ImportedQuestions.Count} questions.", "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                    if (obj is Window window)
                    {
                        window.Close();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error importing questions: {ex.Message}");
                    MessageBox.Show($"Error importing questions: {ex.Message}", "Import Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                Debug.WriteLine("No category selected.");
                MessageBox.Show("No category selected.", "Import Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void OpenTriviaDialog()
        {
            await LoadCategories();

            var dialog = new TriviaDialog
            {
                DataContext = this
            };
            dialog.ShowDialog();
        }


        public DelegateCommand ToggleFullScreenCommand { get; }
        public DelegateCommand ExitGameCommand { get; }
        public DelegateCommand OpenCreateNewPackDialogCommand { get; }
        public DelegateCommand CreateNewPackCommand { get; }
        public DelegateCommand RemoveQuestionPackCommand { get; }
        public DelegateCommand ShowPlayerViewCommand { get; }
        public DelegateCommand ShowConfigurationViewCommand { get; }
        public DelegateCommand SelectPackCommand { get; }
        public DelegateCommand LoadCategoriesCommand { get; private set; }
        public DelegateCommand ImportQuestionsCommand { get; private set; }
        public DelegateCommand OpenTriviaDialogCommand { get; private set; }
        public void ToggleFullScreen(object obj)
        {
            var window = Application.Current.MainWindow;
            if (window.WindowState == WindowState.Normal)
            {
                window.WindowState = WindowState.Maximized;
                window.WindowStyle = WindowStyle.None;
            }
            else
            {
                window.WindowState = WindowState.Normal;
                window.WindowStyle = WindowStyle.SingleBorderWindow;
            }
        }

        public void ExitGame(object obj)
        {

            SavePacksToFile(filePath);
            Application.Current.Shutdown();
        }

        private void ShowPlayerView(object obj)
        {
            CurrentView = PlayerViewModel;
            if (ActivePack?.Questions?.Any() == true)
            {
                var firstQuestion = ActivePack.Questions.First();
                ConfigurationViewModel.SelectedQuestion = firstQuestion;
                RaisePropertyChanged(nameof(ConfigurationViewModel.SelectedQuestion));
                RaisePropertyChanged(nameof(ConfigurationViewModel));
            }
            PlayerViewModel.UpdateTime(ActivePack.TimeLimitInSeconds);
            PlayerViewModel.StartTimer();
        }

        private bool CanShowConfigurationView(object obj)
        {
            return CurrentView == PlayerViewModel;

        }

        private bool CanShowPlayerView(object obj)
        {
            return ActivePack != null && ActivePack.Questions.Any();
        }
        private void ShowConfigurationView(object obj)
        {
            ActivePack.ShuffleQuestions();
            RaisePropertyChanged(nameof(ActivePack.ShuffleQuestions));
            CurrentView = ConfigurationViewModel;
            PlayerViewModel.StopTimer();
        }

        public void OpenCreateNewPackDialog(object obj)
        {
            var dialog = new CreateNewPackDialog
            {
                DataContext = this
            };
            dialog.ShowDialog();
        }

        public void CreateNewPack(object obj)
        {
            if (NewPack != null)
            {
                Packs.Add(NewPack);
                ActivePack = NewPack;

                RaisePropertyChanged(nameof(Packs));
                RaisePropertyChanged(nameof(ActivePack));

                // Återställ NewPack till ett nytt tomt paket
                NewPack = new QuestionPackViewModel(new QuestionPack("<New Question Pack>", Difficulty.Medium, 30));
            }

            if (obj is Window window)
            {
                window.Close();
            }
        }

        public void SelectedPack(object obj)
        {

            if (obj is QuestionPackViewModel selectedPack)
            {
                ActivePack = selectedPack;
                RaisePropertyChanged(nameof(ActivePack));
                RaisePropertyChanged(nameof(selectedPack));
            }
        }
        public void RemoveQuestionPack(object obj)
        {
            if (obj is QuestionPackViewModel pack && Packs.Count > 1)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Are you sure you want to delete this question pack?",
                    "Delete Question Pack",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    Packs.Remove(pack);

                    ActivePack = Packs.FirstOrDefault();
                    ConfigurationViewModel.IsQuestionVisible = false;
                    RaisePropertyChanged(nameof(Packs));
                    RaisePropertyChanged(nameof(ActivePack));
                    RemoveQuestionPackCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private bool CanRemoveQuestionPack(object obj)
        {
            return Packs.Count > 1;
        }

        public void LoadPacksFromFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    Debug.WriteLine($"JSON Content: {json}");
                    var settings = new JsonSerializerSettings();
                    settings.Converters.Add(new StringEnumConverter());


                    var packs = JsonConvert.DeserializeObject<List<QuestionPack>>(json, settings);

                    if (packs != null)
                    {
                        Packs = new ObservableCollection<QuestionPackViewModel>(packs.Select(p => new QuestionPackViewModel(p)));
                        RaisePropertyChanged(nameof(Packs));
                        Debug.WriteLine($"Loaded {Packs.Count} packs.");
                        foreach (var pack in Packs)
                        {
                            Debug.WriteLine($"Pack: {pack.Name}, Questions: {pack.Questions.Count}");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Deserialization returned null.");
                    }
                }
                else
                {
                    Debug.WriteLine("File does not exist.");
                    Packs = new ObservableCollection<QuestionPackViewModel>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading packs: {ex.Message}");
                Packs = new ObservableCollection<QuestionPackViewModel>();
            }
        }

        public void SavePacksToFile(string filePath)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented
                };
                settings.Converters.Add(new StringEnumConverter());

                // Kontrollera om Packs innehåller data
                if (Packs == null || !Packs.Any())
                {
                    Debug.WriteLine("No packs to save.");
                    return;
                }

                // Serialisera till JSON
                var json = JsonConvert.SerializeObject(Packs.ToList(), settings);
                File.WriteAllText(filePath, json);
                Debug.WriteLine($"File saved to {filePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving packs: {ex.Message}");
            }
        }
    }
}
