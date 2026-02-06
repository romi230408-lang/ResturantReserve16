using Microsoft.Maui.Controls; 

namespace ResturantReserve.Models
{
    public class CardModel 
    {
        public static readonly string[] CardsImages =
        {
            "startingCard.png","zero.png","one.png","two.png","three.png","four.png",
            "five.png","six.png","seven.png","eight.png","nine.png","peek.png","swap.png", "drawtwo.png",
        };

        public enum CardType { Number, DrawTwo, Swap, Look }

        public static int CardImagesCount => CardsImages.Length;
        public CardType Type { get; set; }   
        public int Value { get; set; }     
        public bool IsSelected { get; set; } 
        public int Index { get; set; }

        public CardModel(CardType type, int value)
        {
            Type = type;
            Value = value;
        }
        public string ImageSource =>
           Type switch
           {
               CardType.Number => $"{Value}.png",
               CardType.Look => "peek.png",
               CardType.Swap => "swap.png",
               CardType.DrawTwo => "drawtwo.png",
               _ => "startingcard.png"
           };

    }
}

