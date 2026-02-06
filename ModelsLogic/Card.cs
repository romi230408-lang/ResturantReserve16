using Plugin.CloudFirestore.Attributes;
using ResturantReserve.Models;

namespace ResturantReserve.ModelsLogic
{
    public class Card : CardModel
    {
        private const int OFFSET = 50;

        [Ignored]
        public Thickness Margin { get; set; } = new Thickness(0);

        public Card() : base(CardType.Number, 0) { }
        public Card(CardType type, int value) : base(type, value)
        {
        }


        public void ToggleSelected()
        {
            IsSelected = !IsSelected;
            Margin = new Thickness(Margin.Left, Margin.Top, Margin.Right, IsSelected ? OFFSET : 0);
        }

        public static Card Copy(Card card)
        {
            return new Card(card.Type, card.Value)
            {
                Index = card.Index,
                IsSelected = card.IsSelected,
                Margin = card.Margin
            };
        }
    }
}

