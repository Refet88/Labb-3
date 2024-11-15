using Labb_3.Command;
using Labb_3.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Labb_3.ViewModel
{
    internal class PlayerViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel? mainWindowViewModel;
        private readonly ConfigurationViewModel? configurationViewModel;
        private DispatcherTimer _timer;
        private int _timeLeft;
        private string _selectedAnswer;
        private bool _isAnswerCorrect;
        private Brush _buttonAColor;
        private Brush _buttonBColor;
        private Brush _buttonCColor;
        private Brush _buttonDColor;
        private List<string> _answerOptions;
        private Random _random;

        private bool _canAnswer = true;
        public bool CanAnswer
        {
            get => _canAnswer;
            set
            {
                _canAnswer = value;
                RaisePropertyChanged(nameof(CanAnswer));
            }
        }

        private int _correctAnswers;
        public int CorrectAnswers
        {
            get => _correctAnswers;
            set
            {
                _correctAnswers = value;
                RaisePropertyChanged(nameof(CorrectAnswers));
            }
        }

        public int TimeLeft
        {
            get => _timeLeft;
            set
            {
                _timeLeft = value;
                RaisePropertyChanged(nameof(TimeLeft));
            }
        }

        public List<string> AnswerOptions
        {
            get => _answerOptions;
            set
            {
                _answerOptions = value;
                RaisePropertyChanged(nameof(AnswerOptions));
            }
        }

        public string AnswerA { get; set; }
        public string AnswerB { get; set; }
        public string AnswerC { get; set; }
        public string AnswerD { get; set; }

        public PlayerViewModel(MainWindowViewModel? mainWindowViewModel)
        {
            this.mainWindowViewModel = mainWindowViewModel;
            this.configurationViewModel = mainWindowViewModel.ConfigurationViewModel;

            if (mainWindowViewModel.ActivePack != null)
            {
                TimeLeft = mainWindowViewModel.ActivePack.TimeLimitInSeconds;
                RaisePropertyChanged(nameof(mainWindowViewModel.ActivePack.TimeLimitInSeconds));
                RaisePropertyChanged(nameof(TimeLeft));
            }

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;

            OnAnswerSelectedCommand = new DelegateCommand(OnAnswerSelected);
            _answerOptions = new List<string>();
            _random = new Random();

            if (configurationViewModel?.SelectedQuestion != null)
            {
                UpdateAnswerOptions(configurationViewModel.SelectedQuestion);
            }

            ButtonAColor = Brushes.LightGray;
            ButtonBColor = Brushes.LightGray;
            ButtonCColor = Brushes.LightGray;
            ButtonDColor = Brushes.LightGray;

            CanAnswer = true;
        }

        public void UpdateTime(int newTime)
        {
            TimeLeft = newTime;
        }

        public DelegateCommand OnAnswerSelectedCommand { get; }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_timeLeft > 0)
            {
                _timeLeft--;
                RaisePropertyChanged(nameof(TimeLeft));
            }
            else if (_timeLeft == 0)
            {
                _timer.Stop();
                ShowCorrectAnswer();
                TimesUp();


            }
        }

        private async void TimesUp()
        {
            await Task.Delay(2000);
            ShowEndWindow();
        }

        public string SelectedAnswer
        {
            get => _selectedAnswer;
            set
            {
                _selectedAnswer = value;
                RaisePropertyChanged();
            }
        }

        public bool IsAnswerCorrect
        {
            get => _isAnswerCorrect;
            set
            {
                _isAnswerCorrect = value;
                RaisePropertyChanged();
            }
        }

        public Brush ButtonAColor
        {
            get => _buttonAColor;
            set
            {
                _buttonAColor = value;
                RaisePropertyChanged();
            }
        }

        public Brush ButtonBColor
        {
            get => _buttonBColor;
            set
            {
                if (_buttonBColor != value)
                {
                    _buttonBColor = value;
                    RaisePropertyChanged(nameof(ButtonBColor));
                }
            }
        }

        public Brush ButtonCColor
        {
            get => _buttonCColor;
            set
            {
                _buttonCColor = value;
                RaisePropertyChanged();
            }
        }

        public Brush ButtonDColor
        {
            get => _buttonDColor;
            set
            {
                _buttonDColor = value;
                RaisePropertyChanged();
            }
        }

        public async void OnAnswerSelected(object parameter)
        {
            if (CanAnswer && parameter is string answer)
            {
                StopTimer();
                CanAnswer = false;
                SelectedAnswer = answer;
                await CheckAnswer();
            }
        }

        private async Task CheckAnswer()
        {
            if (configurationViewModel?.SelectedQuestion != null)
            {
                var correctAnswer = configurationViewModel.SelectedQuestion.CorrectAnswer;

                Debug.WriteLine($"Selected Answer: {SelectedAnswer}, Correct Answer: {correctAnswer}");

                if (SelectedAnswer == correctAnswer)
                {
                    IsAnswerCorrect = true;
                    CorrectAnswers++;
                    SetButtonColor(SelectedAnswer, Brushes.Green);
                    RaisePropertyChanged(nameof(SetButtonColor));
                }
                else
                {
                    IsAnswerCorrect = false;
                    SetButtonColor(SelectedAnswer, Brushes.Red);
                    await Task.Delay(600);
                    SetButtonColor(correctAnswer, Brushes.Green);
                    RaisePropertyChanged();
                }
                CanAnswer = true;
                StartTimer();

            }
            await LoadNextQuestion();

        }

        private void UpdateButtonColors()
        {
            if (IsAnswerCorrect)
            {
                SetButtonColor(SelectedAnswer, Brushes.Green);
            }
            else
            {
                SetButtonColor(SelectedAnswer, Brushes.Red);
            }
        }

        private void SetButtonColor(string answer, Brush color)
        {
            if (answer == AnswerA)
            {
                ButtonAColor = color;
                RaisePropertyChanged(nameof(ButtonAColor));
            }
            else if (answer == AnswerB)
            {
                ButtonBColor = color;
                RaisePropertyChanged(nameof(ButtonBColor));
            }
            else if (answer == AnswerC)
            {
                ButtonCColor = color;
                RaisePropertyChanged(nameof(ButtonCColor));
            }
            else if (answer == AnswerD)
            {
                ButtonDColor = color;
                RaisePropertyChanged(nameof(ButtonDColor));
            }
        }

        public async Task LoadNextQuestion()
        {
            Debug.WriteLine("LoadNextQuestion: Called");
            var nextQuestion = configurationViewModel?.GetNextQuestion();
            if (nextQuestion != null)
            {
                await Task.Delay(1000);
                Debug.WriteLine("LoadNextQuestion: Next question found");
                configurationViewModel.SelectedQuestion = nextQuestion;
                ResetButtonColors();
                RaisePropertyChanged(nameof(configurationViewModel.SelectedQuestion));


                UpdateAnswerOptions(nextQuestion);

                Debug.WriteLine($"New Selected Question Index: {configurationViewModel.ActivePack.Questions.IndexOf(configurationViewModel.SelectedQuestion)}");                
            }
            else
            {
                StopTimer();
                ShowEndWindow();
            }
        }

        private void ShowEndWindow()
        {
            var result = MessageBox.Show($"You got {CorrectAnswers} out of {mainWindowViewModel.ActivePack.Questions.Count} answers correct! Do you want to restart the game?", "Complete!", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                RestartGame();
            }
            else
            {
                mainWindowViewModel.CurrentView = mainWindowViewModel.ConfigurationViewModel;
                ResetButtonColors();
                mainWindowViewModel.ActivePack.ShuffleQuestions();
                RaisePropertyChanged(nameof(mainWindowViewModel.ActivePack.ShuffleQuestions));
                RaisePropertyChanged(nameof(UpdateAnswerOptions));
                RaisePropertyChanged(nameof(ResetButtonColors));
            }
        }

        private void ShowCorrectAnswer()
        {
            if (configurationViewModel?.SelectedQuestion != null)
            {
                var correctAnswer = configurationViewModel.SelectedQuestion.CorrectAnswer;
                SetButtonColor(correctAnswer, Brushes.Green);
                RaisePropertyChanged(nameof(SetButtonColor));
            }
        }
        public void RestartGame()
        {
            // Återställ poängräkningen och timern
            CorrectAnswers = 0;
            TimeLeft = mainWindowViewModel?.ActivePack?.TimeLimitInSeconds ?? 0;

            // Återställ färgerna på knapparna
            ResetButtonColors();

            // Starta om spelet genom att ladda första frågan
            if (mainWindowViewModel?.ActivePack != null)
            {
                mainWindowViewModel.ActivePack.ShuffleQuestions(); // Blanda om frågorna
                configurationViewModel.SelectedQuestion = mainWindowViewModel.ActivePack.Questions.FirstOrDefault();

                if (configurationViewModel.SelectedQuestion != null)
                {
                    UpdateAnswerOptions(configurationViewModel.SelectedQuestion);
                }
            }

            // Starta om timern
            StartTimer();
            RaisePropertyChanged(nameof(configurationViewModel.SelectedQuestion));
        }

        public void UpdateAnswerOptions(Question? question)
        {
            if (question == null)
            {
                // Om questions är null, rensa svarsalternativen och återställ knappfärgerna
                AnswerOptions = new List<string>();
                AnswerA = string.Empty;
                AnswerB = string.Empty;
                AnswerC = string.Empty;
                AnswerD = string.Empty;
                RaisePropertyChanged(nameof(AnswerA));
                RaisePropertyChanged(nameof(AnswerB));
                RaisePropertyChanged(nameof(AnswerC));
                RaisePropertyChanged(nameof(AnswerD));
                RaisePropertyChanged(nameof(AnswerOptions));
                ResetButtonColors();
                return;
            }

            AnswerOptions = new List<string>
    {
        question.CorrectAnswer,
        question.IncorrectAnswers[0],
        question.IncorrectAnswers[1],
        question.IncorrectAnswers[2]
    };

            // Blanda svarsalternativen
            var randomizedAnswers = AnswerOptions.OrderBy(x => _random.Next()).ToList();

            // Tilldela de blandade svarsalternativen till bokstäver
            if (randomizedAnswers.Count >= 4)
            {
                AnswerA = randomizedAnswers[0];
                AnswerB = randomizedAnswers[1];
                AnswerC = randomizedAnswers[2];
                AnswerD = randomizedAnswers[3];
                RaisePropertyChanged(nameof(AnswerA));
                RaisePropertyChanged(nameof(AnswerB));
                RaisePropertyChanged(nameof(AnswerC));
                RaisePropertyChanged(nameof(AnswerD));
            }

            RaisePropertyChanged(nameof(AnswerOptions));
        }

        public void ResetButtonColors()
        {
            ButtonAColor = Brushes.LightGray;
            ButtonBColor = Brushes.LightGray;
            ButtonCColor = Brushes.LightGray;
            ButtonDColor = Brushes.LightGray;
            RaisePropertyChanged(nameof(ButtonAColor));
            RaisePropertyChanged(nameof(ButtonBColor));
            RaisePropertyChanged(nameof(ButtonCColor));
            RaisePropertyChanged(nameof(ButtonDColor));
            if (mainWindowViewModel.CurrentView == mainWindowViewModel.ConfigurationViewModel)
            {
                ButtonAColor = Brushes.LightGray;
                ButtonBColor = Brushes.LightGray;
                ButtonCColor = Brushes.LightGray;
                ButtonDColor = Brushes.LightGray;
                RaisePropertyChanged(nameof(ButtonAColor));
                RaisePropertyChanged(nameof(ButtonBColor));
                RaisePropertyChanged(nameof(ButtonCColor));
                RaisePropertyChanged(nameof(ButtonDColor));
            }
        }

        public void StartTimer()
        {
            _timer.Start();
        }

        public void StopTimer()
        {
            _timer.Stop();
        }
    }
}