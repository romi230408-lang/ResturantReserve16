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
            var tempPackage = new CardsSet(true);   // רק בשביל ערבוב
            PackageCards = tempPackage
                .GetAllCards()
                .Select(c => new CardData
                {
                    Type = c.Type,
                    Value = c.Value
                })
                .ToList();

            PackageCardCount = PackageCards.Count;

            myCards = new CardsSet(full: false)
            {
                SingleSelect = true
            };

            HostName = new User().Name;
            Created = DateTime.Now;
            IsHostTurn = true;
        }

        public void Restart()
        {
            if (!IsHostUser)
                return;
            pickedCardsCount = 0;

            // יוצרים קופה חדשה ומעורבבת
            var tempPackage = new CardsSet(true);
            PackageCards = tempPackage.GetAllCards();

            // קלף פתוח ראשון
            var firstData = PackageCards[0];
            PackageCards.RemoveAt(0);
            OpenedCardData = firstData;

            PackageCardCount = PackageCards.Count;

            myCards.Reset(false);

            Move = [Keys.NoMove, Keys.NoMove];
            IsHostTurn = true;

            UpdateFbMove();
        }
        public override void TakePackageCard()
        {
            if (!IsMyTurn || PackageCards == null || PackageCards.Count == 0)
                return;

            // לוקחים תמיד את הקלף הראשון מהקופה המשותפת
            var data = PackageCards[0];
            PackageCards.RemoveAt(0);

            var card = new Card(data.Type, data.Value)
            {
                Index = data.Index
            };

            var addedCard = myCards.Add(card);

            pickedCardsCount++;
            PackageCardCount = PackageCards.Count;

            Move = [Keys.TakeFromPackage, addedCard.Index];
            IsHostTurn = !IsHostTurn;

            UpdateFbMove();
        }



        public Card? TakeCard()
        {
            if(!IsMyTurn || PackageCards == null || PackageCards.Count == 0)
                return null;
            // 1) We remove once random card from player cards.
            //    We thow this card to the garbage.
            myCards.TakeCard();

            // 2) We add the opened card to player cards.
            if (OpenedCardData != null)
            {
                var prevCard = new Card(OpenedCardData.Type, OpenedCardData.Value)
                {
                    Index = OpenedCardData.Index
                };
                myCards.Add(prevCard);
            }

            var data = PackageCards[0];
            PackageCards.RemoveAt(0);

            OpenedCardData = data;

            pickedCardsCount++;
            PackageCardCount = PackageCards.Count;

            Move = [Keys.TakeFromPackage, 0];
            IsHostTurn = !IsHostTurn;

            UpdateFbMove();
            return new Card(data.Type, data.Value)
            {
                Index = data.Index
            };
        }

       

        internal void SelectCard(Card card)
        {
            if(!IsMyTurn)
                return;

            myCards.SelectCard(card);

            Move = [Keys.ThrowCard, card.Index];
   
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
                { nameof(PickedCardsCount), pickedCardsCount },
                { nameof(PackageCards), PackageCards },
                { nameof(PackageCardCount), PackageCards.Count },
                { nameof(OpenedCardData), OpenedCardData }
            };
            fbd.UpdateFields(Keys.GamesCollection, Id, dict, OnComplete);
        }

        public override void Play(bool MyMove)
        {
            if (_status.CurrentStatus == GameStatus.Statuses.Play)
            {
                DisplayMoveArgs args = new(MyMove);
                DisplayChanged?.Invoke(this, args);
                OnGameChanged?.Invoke(this, EventArgs.Empty);               
            }
        }


        private void OnChange(IDocumentSnapshot? snapshot, Exception? error)
        {
            Game? updatedGame = snapshot?.ToObject<Game>();
            if (updatedGame != null)
            {
                PackageCards = updatedGame.PackageCards;
                PackageCardCount = PackageCards.Count;
                IsFull = updatedGame.IsFull;
                GuestName = updatedGame.GuestName;
                Move = updatedGame.Move;
                IsHostTurn = updatedGame.IsHostTurn;
                pickedCardsCount = updatedGame.pickedCardsCount;
                OpenedCardData = updatedGame.OpenedCardData;
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
