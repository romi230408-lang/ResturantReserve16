using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls;
using Plugin.CloudFirestore;
using ResturantReserve.Models;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;

namespace ResturantReserve.ModelsLogic
{
    public class Game : GameModel
    {
        private readonly CardsSet myCards;     

        public Game() : base()
        {
            PackageCardCount = 52;
            package = new CardsSet(true);
            myCards = new CardsSet(full: false)
            {
                SingleSelect = true
            };

            HostName = new User().Name;
            Created = DateTime.Now;
        }

        public void Restart()
        {
            pickedCardsCount = 0;
            PackageIndex = 0;
            package.Reset(true);
            openedCard = package.TakeCard();
            PackageCardCount = package.Count; 
            myCards.Reset(false);  

            Move = [Keys.NoMove, Keys.NoMove];
            UpdateFbMove();
        }
        public override void TakePackageCard()
        {
            PackageIndex++;
            Card? card = package.TakeCard(PackageIndex);   
            if (card != null)
            {
                var addedCard = myCards.Add(card); 
                pickedCardsCount++;
                PackageCardCount = package.Count;
                Move = [Keys.TakeFromPackage, addedCard.Index];
                IsHostTurn = !IsHostTurn;
                UpdateFbMove();
            }
        }


        public Card? TakeCard()
        {
            // 1) We remove once random card from player cards.
            //    We thow this card to the garbage.
            myCards.TakeCard();

            // 2) We add the opened card to player cards.
            myCards.Add(openedCard);

            var card = package.TakeCard();
            if (card != null)
            {
                openedCard = card;
                pickedCardsCount++;
                PackageCardCount = package.Count; 
                IsHostTurn = !IsHostTurn;
                Move = [Keys.TakeFromPackage, 0];  
                UpdateFbMove(); 
            }
            return openedCard;

        }

       

        internal void SelectCard(Card card)
        {
            myCards.SelectCard(card);

            Move = [Keys.ThrowCard, card.Index];
            UpdateFbMove();

            Move[0] = Keys.ThrowCard;
            Move[1] = card.Index;
            IsHostTurn = !IsHostTurn;
            UpdateFbMove();
        }


        public override string OpponentName => IsHostUser ? GuestName : HostName;

        public override void SetDocument(Action<System.Threading.Tasks.Task> OnComplete)
        {
            Id = fbd.SetDocument(this, Keys.GamesCollection, Id, OnComplete);
        }

        public void UpdateGuestUser(Action<Task> OnComplete)
        {
            IsFull = true;
            GuestName = MyName;
            UpdateFbJoinGame(OnComplete);
        }

        private void UpdateFbJoinGame(Action<Task> OnComplete)
        {
            Dictionary<string, object> dict = new()
            {
                { nameof(IsFull), IsFull },
                { nameof(GuestName), GuestName }
            };
            fbd.UpdateFields(Keys.GamesCollection, Id, dict, OnComplete);
        }

        public override void AddSnapshotListener()
        {
            ilr = fbd.AddSnapshotListener(Keys.GamesCollection, Id, OnChange);
        }

        public override void RemoveSnapshotListener()
        {
            ilr?.Remove();
            DeleteDocument(OnComplete);
        }

        private void OnComplete(Task task)
        {
            if (task.IsCompletedSuccessfully)
                OnGameDeleted?.Invoke(this, EventArgs.Empty);
        }
        protected override void UpdateStatus()
        {
            _status.CurrentStatus = IsHostUser && IsHostTurn || !IsHostUser && !IsHostTurn ?
                GameStatus.Statuses.Play : GameStatus.Statuses.Wait;
        }
        protected override void UpdateFbMove()
        {
            Dictionary<string, object> dict = new()
            {
                { nameof(Move), Move },
                { nameof(IsHostTurn), IsHostTurn },
                { nameof(PackageCardCount), package.Count },
                { nameof(PickedCardsCount), pickedCardsCount },
                { nameof(OpenedCardSource), openedCard?.Source?.ToString() ?? "" },
                { nameof(PackageIndex), PackageIndex }
            };
            fbd.UpdateFields(Keys.GamesCollection, Id, dict, OnComplete);
        }

        public override void Play(bool MyMove)
        {
            if (_status.CurrentStatus == GameStatus.Statuses.Play)
            {
                DisplayMoveArgs args = new(MyMove);
                DisplayChanged?.Invoke(this, args);

                if (MyMove)
                {
                    _status.ChangeStatus();
                    IsHostTurn = !IsHostTurn;
                    UpdateFbMove();       
                }
                else
                {
                    OnGameChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }


        private void OnChange(IDocumentSnapshot? snapshot, Exception? error)
        {
            Game? updatedGame = snapshot?.ToObject<Game>();
            if (updatedGame != null)
            {
                IsFull = updatedGame.IsFull;
                GuestName = updatedGame.GuestName;
                Move = updatedGame.Move;
                IsHostTurn = updatedGame.IsHostTurn;
                PackageCardCount = updatedGame.PackageCardCount;
                pickedCardsCount = updatedGame.pickedCardsCount;
                PackageIndex = updatedGame.PackageIndex;
                openedCard = updatedGame.openedCard;
                UpdateStatus();
                if (_status.CurrentStatus == GameStatus.Statuses.Play)
                {
                    Play(false);
                }
                OnGameChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnGameDeleted?.Invoke(this, EventArgs.Empty);
                    Shell.Current.Navigation.PopAsync();
                });
            }
        }

        public override void DeleteDocument(Action<Task> OnComplete)
        {
            fbd.DeleteDocument(Keys.GamesCollection, Id, OnComplete);
        }
    }
}
