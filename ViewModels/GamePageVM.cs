using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Messaging;
using ResturantReserve.Models;
using ResturantReserve.ModelsLogic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Windows.Input;

namespace ResturantReserve.ViewModels
{
    public partial class GamePageVM : ObservableObject
    {
        private double timeLeft;
        private readonly Game game;
        public string MyName => game.MyName;
        public string OpponentName => game.OpponentName;
        public ICommand ResetGameCommand { get; }
        public ICommand SelectCardCommand { get; }
        public ICommand TakeCardCommand { get; }
        public int PickedCardsCount => game.PickedCardsCount;
        public bool IsHostTurn => game.IsHostTurn;
        public int PackageCardCount => game.PackageCardCount;
        public ObservableCollection<Card> MyCards { get; } = new();
        public string OpenedCardImageSource
        {
            get
            {
                var data = game.OpenedCardData;
                if (data == null)
                    return "startingcard.png";

                return Card.GetImageSource(data.Type, data.Value);
            }
        }

        public ObservableCollection<int> OpponentCards { get; } =  new() { 1, 2, 3, 4 };

        public GamePageVM(Game game)
        {
            game.OnGameChanged += OnGameChanged;
            game.DisplayChanged += OnDisplayChanged;
            this.game = game;
            if (!game.IsHostUser)
                game.UpdateGuestUser(OnComplete);
            SelectCardCommand = new Command<Card>(SelectCard);
            ResetGameCommand = new Command(ResetGame);
            TakeCardCommand = new Command(TakeCard);
            if (game.PackageCards.Count > 0)
            {
                StartNewGame(false);
            }

            WeakReferenceMessenger.Default.Register<AppMessage<long>>(this, (r, m) =>
            {
                OnMessageReceived(m.Value);
            });

            WeakReferenceMessenger.Default.Send(new AppMessage<long>(10000));

        }
        private void OnDisplayChanged(object? sender, DisplayMoveArgs e)
        {
            OnPropertyChanged(nameof(IsHostTurn));
            OnPropertyChanged(nameof(PickedCardsCount)); 
            OnPropertyChanged(nameof(OpenedCardImageSource));
        }
        private void OnMessageReceived(long timeLeft)
        {
            TimeLeft = timeLeft / 1000f;
        }

        public double TimeLeft
        {
            get => Math.Round(timeLeft, 2, MidpointRounding.ToNegativeInfinity);
            set
            {
                if (timeLeft != value)
                {
                    timeLeft = value;
                    OnPropertyChanged();
                }
            }
        }
        private void OnGameChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(OpponentName));
            OnPropertyChanged(nameof(PickedCardsCount)); 
            OnPropertyChanged(nameof(IsHostTurn)); 
            OnPropertyChanged(nameof(OpenedCardImageSource));
            OnPropertyChanged(nameof(PackageCardCount));
        }

        private void OnComplete(Task task)
        {
            if (!task.IsCompletedSuccessfully)
                Toast.Make(Strings.JoinGameErr, CommunityToolkit.Maui.Core.ToastDuration.Long, 14);

        }

        public void AddSnapshotListener()
        {
            game.AddSnapshotListener();
        }

        public void RemoveSnapshotListener()
        {
            game.RemoveSnapshotListener();
        }

        private void TakeCard(object? _)
        {
            TakePackageCard();
        }

        private void SelectCard(Card selectedCard)
        {
            if (selectedCard is null)
                return;

            game.SelectCard(selectedCard);

            OnPropertyChanged(nameof(OpenedCardImageSource));
            OnPropertyChanged(nameof(PickedCardsCount));
        }

        private void StartNewGame(bool restart)
        {
            if (restart)
                game.Restart();

            MyCards.Clear();

            for (int i = 0; i < 4; i++)
            {
                Card? card = game.TakeCard();
                if (card != null)
                    MyCards.Add(card);
            }

            OnPropertyChanged(nameof(OpenedCardImageSource));
        }


        private async void OnWin(object? sender, EventArgs e)
        {
            bool newGame = await Application.Current!.MainPage!.DisplayAlert(
                "Bravo! You Won",
                "Start new game or quit?",
                "Start new Game",
                "Quit");

            if (newGame)
                ResetGame();
            else
                Application.Current?.Quit();
        }

        private void TakePackageCard()
        {
            Card? card = game.TakeCard();
            if (card != null)
            {
                MyCards.Add(card);

                OnPropertyChanged(nameof(PickedCardsCount));
                OnPropertyChanged(nameof(OpenedCardImageSource));
            }
            else
            {
                Toast.Make("No more cards", ToastDuration.Long, 20).Show();
            }
        }


        private void ResetGame()
        {
            StartNewGame(true);
            OnPropertyChanged(nameof(OpenedCardImageSource));
            OnPropertyChanged(nameof(PickedCardsCount));
        }
    }
}
