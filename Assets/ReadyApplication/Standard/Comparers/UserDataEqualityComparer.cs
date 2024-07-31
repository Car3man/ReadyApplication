using RGN.Modules.UserProfile;
using System.Collections.Generic;

public sealed class UserDataEqualityComparer : IEqualityComparer<UserData>
{
	public bool Equals(UserData x, UserData y)
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

		return x.userId == y.userId &&
		       x.email == y.email &&
		       x.displayName == y.displayName &&
		       x.bio == y.bio;
	}

	public int GetHashCode(UserData obj)
	{
		unchecked
		{
			int hashCode = (obj.userId != null ? obj.userId.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ (obj.email != null ? obj.email.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ (obj.displayName != null ? obj.displayName.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ (obj.bio != null ? obj.bio.GetHashCode() : 0);
			return hashCode;
		}
	}
}