namespace Durak.Architecture.Shared.Domain
{
    public sealed class TableContext
    {
        public int AttackCardsCount { get; private set; }
        public int DefendedCardsCount { get; private set; }

        public bool HasCards => AttackCardsCount > 0;
        public bool AllCardsDefended => HasCards && DefendedCardsCount >= AttackCardsCount;

        public void SetCounts(int attackCardsCount, int defendedCardsCount)
        {
            AttackCardsCount = attackCardsCount < 0 ? 0 : attackCardsCount;
            DefendedCardsCount = defendedCardsCount < 0 ? 0 : defendedCardsCount;
        }
    }
}
