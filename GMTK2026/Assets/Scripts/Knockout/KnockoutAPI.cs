namespace ForgettingBoxer.Knockout
{
    public static class KnockoutAPI
    {
        public static bool AddStar() => AddStars(1);

        public static bool AddStars(int amount)
        {
            if (KnockoutSystem.Instance == null) return false;
            KnockoutSystem.Instance.AddStars(amount);
            return true;
        }

        public static bool TakeDamage(int damage = 1)
        {
            if (KnockoutSystem.Instance == null) return false;
            KnockoutSystem.Instance.TakeDamage(damage);
            return true;
        }
    }
}
