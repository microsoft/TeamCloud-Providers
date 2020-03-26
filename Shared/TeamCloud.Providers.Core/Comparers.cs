using System.Collections.Generic;
using TeamCloud.Model.Data;

namespace TeamCloud.Providers.Core
{
    internal class UserUpdateComparer : IEqualityComparer<User>
    {
        public bool Equals(User x, User y)
            => object.Equals(x, y) && (x.Role != y.Role);

        public int GetHashCode(User obj)
            => obj.GetHashCode();
    }
}
