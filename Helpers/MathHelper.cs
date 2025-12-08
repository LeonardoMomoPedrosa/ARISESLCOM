namespace ARISESLCOM.Helpers
{
    public class MathHelper
    {
        public static int GetMaxTerms(decimal amount)
        {
            int possibleTerms = (int)Math.Floor(amount / Consts.MinimumTermAmount);

            // Garante pelo menos 1 parcela, no m�ximo 6
            return Math.Clamp(possibleTerms, 1, Consts.MaxAllowedTerms);
        }

        public static decimal GetTermAmount(decimal totalAmount, short numberOfTerms)
        {
            decimal accum = 0;
            decimal termAmt = totalAmount / numberOfTerms;
            decimal fee = (1 + Consts.InterestFee / 100);

            for (int i = 1; i <= numberOfTerms; ++i)
            {
                accum += termAmt * (decimal)Math.Pow((double)fee, i);
            }

            return accum / numberOfTerms;

        }
    }
}
