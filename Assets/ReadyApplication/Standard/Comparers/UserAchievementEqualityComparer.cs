using RGN.Modules.Achievement;
using System.Collections.Generic;

namespace ReadyApplication.Standard
{
	public sealed class UserAchievementEqualityComparer : IEqualityComparer<UserAchievement>
	{
		public bool Equals(UserAchievement x, UserAchievement y)
		{
			if (ReferenceEquals(x, y))
			{
				return true;
			}

			if (ReferenceEquals(x, null))
			{
				return false;
			}

			if (ReferenceEquals(y, null))
			{
				return false;
			}

			if (x.GetType() != y.GetType())
			{
				return false;
			}

			return x.id == y.id &&
			       x.value == y.value &&
			       x.valueToReach == y.valueToReach &&
			       x.isCompleted == y.isCompleted &&
			       x.isClaimed == y.isClaimed &&
			       x.lastCompleteTime == y.lastCompleteTime;
		}

		public int GetHashCode(UserAchievement obj)
		{
			unchecked
			{
				int hashCode = (obj.id != null ? obj.id.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ obj.value;
				hashCode = (hashCode * 397) ^ obj.valueToReach;
				hashCode = (hashCode * 397) ^ obj.isCompleted.GetHashCode();
				hashCode = (hashCode * 397) ^ obj.isClaimed.GetHashCode();
				hashCode = (hashCode * 397) ^ obj.lastCompleteTime.GetHashCode();
				return hashCode;
			}
		}
	}
}