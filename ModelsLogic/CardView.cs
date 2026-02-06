using ResturantReserve.Models;

namespace ResturantReserve.ModelsLogic
{
    public class CardView:ImageButton
    {
        public CardModel Card { get; }

        public CardView(CardModel card)
        {
            Card = card;

            Source = GetSource(card);
            Aspect = Aspect.AspectFit;
            HorizontalOptions = LayoutOptions.Start;
            WidthRequest = 100;
        }

        private string GetSource(CardModel card)
        {
            if (card.Type == CardModel.CardType.Number &&
                card.Value >= 0 && card.Value <= 9)
            {
                return CardModel.CardsImages[card.Value + 1];
            }

            return card.Type switch
            {
                CardModel.CardType.Look => "peek.png",
                CardModel.CardType.Swap => "swap.png",
                CardModel.CardType.DrawTwo => "drawTwo.png",
                _ => "startingCard.png"
            };
        }
    }
}
