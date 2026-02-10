using Plugin.CloudFirestore.Attributes;
using ResturantReserve.Models;

namespace ResturantReserve.ModelsLogic
{
    public class CardData
    {
        public CardModel.CardType Type { get; set; }
        public int Value { get; set; }
        public int Index { get; set; }
        [Ignored]
        public string ImageSource =>
           CardModel.GetImageSource(Type, Value);

    }
}
