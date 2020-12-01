using System;

namespace dsproject
{
    internal class UnoCard
    {
        public CardType Type { get; }
        public CardColor Color { get; }
        public int Number { get; }

        public UnoCard(CardType type) : this(type, CardColor.Blue, 0)
        {
        }

        public UnoCard(CardType type, CardColor color, int number)
        {
            if (number is < 0 or > 9) throw new ArgumentException(null, nameof(number));

            Type = type;
            Color = color;
            Number = number;
        }

        public UnoCard(UnoCard source)
        {
            Type = source.Type;
            Color = source.Color;
            Number = source.Number;
        }

        public char[][] GetGraphic()
        {
            return Type switch
            {
                CardType.Wild => throw new NotImplementedException(),
                CardType.WildDrawFour => throw new NotImplementedException(),
                CardType.Skip => throw new NotImplementedException(),
                CardType.DrawTwo => throw new NotImplementedException(),
                CardType.Reverse => throw new NotImplementedException(),
                CardType.Number => CardGraphics.NumberCards[Number],
                _ => throw new ArgumentOutOfRangeException()
            };
        }

#pragma warning disable 659
        public override bool Equals(object obj)
#pragma warning restore 659
        {
            var other = obj as UnoCard;
            if (other is null) return false;
            if (other.Type != Type) return false;
            if (Type == CardType.Number)
            {
                if (other.Color != Color) return false;
                if (other.Number != Number) return false;
            }

            return true;
        }

        public static bool operator ==(UnoCard a, UnoCard b) => a is not null && a.Equals(b);
        public static bool operator !=(UnoCard a, UnoCard b) => a is not null && !a.Equals(b);
    }

    internal enum CardType
    {
        Wild, WildDrawFour, Skip, DrawTwo, Reverse, Number
    }

    internal enum CardColor
    {
        Red, Yellow, Green, Blue
    }

}