
namespace HomeOS.Hub.Common
{
    using System;
    using System.Collections.Generic;

    public class UserGroupInfo
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public UserGroupInfo Parent { get; private set; }
        public List<UserGroupInfo> Children { get; private set; }

        public UserGroupInfo(int id, string name)
        {
            this.Id = id;
            this.Name = name;
            Parent = null;
            Children = new List<UserGroupInfo>();
        }

        public void SetParent(UserGroupInfo parent)
        {
            this.Parent = parent;
        }

        public void AddChild(UserGroupInfo child)
        {
            lock (Children)
            {
                Children.Add(child);
            }
        }

        public void RemoveChild(UserGroupInfo child)
        {
            lock (Children)
            {
                Children.Remove(child);
            }
        }

        public bool Equals(UserGroupInfo other)
        {
            return this.Name.Equals(other.Name);
        }

        /// <summary>
        /// Is the other group descendant of this one?
        /// </summary>
        public bool IsDescendant(UserGroupInfo other)
        {
            lock (Children)
            {
                foreach (UserGroupInfo child in Children)
                {
                    if (child.Equals(other) || child.IsDescendant(other))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Is the other group an ancestor of this one?
        /// </summary>
        public bool IsAncestor(UserGroupInfo other)
        {
            if ((Parent != null) &&
                 (Parent.Equals(other) || Parent.IsAncestor(other)))
                return true;

            return false;

        }
    }

    public sealed class UserInfo : UserGroupInfo
    {
        public string Password { get; private set; }
        public DateTime ActiveFrom { get; private set; }
        public DateTime ActiveUntil { get; private set; }

        public string LiveId { get; private set; }
        public string LiveIdUniqueUserToken { get; private set; }

        public UserInfo(int id, string name, string password, DateTime activeFrom, DateTime activeUntil, string LiveId, string LiveIdUniqueUserToken="")
            : base(id, name)
        {
            this.Password = password;
            this.ActiveFrom = activeFrom;
            this.ActiveUntil = activeUntil;
            this.LiveId = LiveId;
            this.LiveIdUniqueUserToken = LiveIdUniqueUserToken;
        }    
    }

    //public sealed class GroupMembership
    //{
    //    public int GroupId { get; private set; }
    //    public int UserId { get; private set; }

    //    public GroupMembership(int groupId, int userId)
    //    {
    //        this.GroupId = groupId;
    //        this.UserId = userId;
    //    }
    //}

}