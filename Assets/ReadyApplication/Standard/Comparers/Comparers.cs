namespace ReadyApplication.Standard
{
	public static class Comparers
	{
		public static CurrencyEqualityComparer Currency => new();
		public static UserAchievementEqualityComparer UserAchievement => new();
		public static UserDataEqualityComparer UserData => new();
	}
}