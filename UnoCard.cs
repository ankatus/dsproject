using System;

namespace dsproject
{
    internal class UnoCard
    {
        public CardType Type { get; }
        public CardColor? Color { get; }
        public int? Number { get; }

        public UnoCard(CardType type) : this(type, null, null)
        {
        }

        public UnoCard(CardType type, CardColor? color, int? number)
        {
            if (number is (not null) and (< 0 or > 9)) throw new ArgumentException(null, nameof(number));

            Type = type;
            Color = color;
            Number = number;
        }

        public char[][] GetGraphic()
        {
            switch (Type)
            {
                case CardType.Wild:
                    throw new NotImplementedException();
                    break;
                case CardType.WildDrawFour:
                    throw new NotImplementedException();
                    break;
                case CardType.Skip:
                    throw new NotImplementedException();
                    break;
                case CardType.DrawTwo:
                    throw new NotImplementedException();
                    break;
                case CardType.Reverse:
                    throw new NotImplementedException();
                    break;
                case CardType.Number:
                    if (Number != null)
                    {
                        return CardGraphics.NumberCards[(int) Number];
                    }
                    else
                    {
                        throw new InvalidOperationException("Number card with null number");
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
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