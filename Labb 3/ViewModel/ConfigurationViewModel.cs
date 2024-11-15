using Labb_3.Command;
using Labb_3.Dialogs;
using Labb_3.Model;
using System.Diagnostics;
using System.Linq;

namespace Labb_3.ViewModel
{
    internal class ConfigurationViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel? mainWindowViewModel;
        private bool isQuestionVisible;
        private Question? selectedQuestion;


        public ConfigurationViewModel(MainWindowViewModel? mainWindowViewModel)
        {
            this.mainWindowViewModel = mainWindowViewModel;
            OpenPackOptionsCommand = new DelegateCommand(OpenPackOptions);
            AddQuestionCommand = new DelegateCommand(AddQuestion);
            RemoveQuestionCommand = new DelegateCommand(_ => RemoveQuestion(), _ => SelectedQuestion != null);

            IsQuestionVisible = false;

            if (mainWindowViewModel?.ActivePack?.Questions?.Any() == true)
            {
                SelectedQuestion = mainWindowViewModel.ActivePack.Questions.First();
                RaisePropertyChanged(nameof(ActivePack));
            }
            else
            {
                Debug.WriteLine("No questions available in ActivePack during initialization.");
            }
        }

        public QuestionPackViewModel? ActivePack { get => mainWindowViewModel.ActivePack; }

        public DelegateCommand OpenPackOptionsCommand { get; }
        public DelegateCommand AddQuestionCommand { get; }
        public DelegateCommand RemoveQuestionCommand { get; }

        public bool IsQuestionVisible
        {
            get => isQuestionVisible;
            set
            {
                isQuestionVisible = value;
                RaisePropertyChanged(nameof(IsQuestionVisible));
            }
        }

        public void OpenPackOptions(object obj)
        {
            var dialog = new PackOptionsDialog
            {
                DataContext = this
            };
            dialog.ShowDialog();
        }

        public Question? SelectedQuestion
        {
            get => selectedQuestion;
            set
            {
                selectedQuestion = value;
                RaisePropertyChanged(nameof(SelectedQuestion));
                RemoveQuestionCommand.RaiseCanExecuteChanged();
                IsQuestionVisible = selectedQuestion != null;
                RaisePropertyChanged(nameof(QuestionCount));
                RaisePropertyChanged(nameof(QuestionStatus));
                mainWindowViewModel.PlayerViewModel?.UpdateAnswerOptions(selectedQuestion);
            }
        }

        public string QuestionStatus
        {
            get
            {
                if (SelectedQuestion == null)
                    return string.Empty;

                int currentIndex = ActivePack.Questions.IndexOf(SelectedQuestion) + 1;
                int totalQuestions = ActivePack.Questions.Count;
                return $"Question {currentIndex} of {totalQuestions}";
            }
        }

        public int QuestionCount => ActivePack.Questions.Count;

        public void AddQuestion(object obj)
        {
            var newQuestion = new Question("New Question", string.Empty, string.Empty, string.Empty, string.Empty);
            mainWindowViewModel.ActivePack?.Questions.Add(newQuestion);
            SelectedQuestion = newQuestion;
            IsQuestionVisible = true;
            RaisePropertyChanged(nameof(ActivePack));
            RaisePropertyChanged(nameof(SelectedQuestion));
            RaisePropertyChanged(nameof(ActivePack.Questions));
            RaisePropertyChanged(nameof(QuestionCount));
            RaisePropertyChanged(nameof(QuestionStatus));
            RaisePropertyChanged(nameof(mainWindowViewModel.PlayerViewModel.AnswerOptions));
            RaisePropertyChanged(nameof(mainWindowViewModel.PlayerViewModel.LoadNextQuestion));
            RemoveQuestionCommand.RaiseCanExecuteChanged();

        }

        public Question? GetNextQuestion()
        {
            if (ActivePack != null && SelectedQuestion != null)
            {
                int currentIndex = ActivePack.Questions.IndexOf(SelectedQuestion);
                Debug.WriteLine($"Current Index: {currentIndex}, Total Questions: {ActivePack.Questions.Count}");
                if (currentIndex >= 0 && currentIndex < ActivePack.Questions.Count - 1)
                {
                    var nextQuestion = ActivePack.Questions[currentIndex + 1];
                    Debug.WriteLine($"Next Question Index: {currentIndex + 1}");
                    return nextQuestion;
                }
            }
            Debug.WriteLine("GetNextQuestion: Returning null");
            return null;
        }

        public void RemoveQuestion()
        {
            if (SelectedQuestion != null)
            {
                ActivePack.Questions.Remove(SelectedQuestion);
                RaisePropertyChanged(nameof(ActivePack.Questions));
                RaisePropertyChanged(nameof(QuestionCount));
                RaisePropertyChanged(nameof(QuestionStatus));
                mainWindowViewModel?.ShowPlayerViewCommand.RaiseCanExecuteChanged();
            }
        }
    }
}
