using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HomeOS.Hub.Platform
{
    using Microsoft.Secpal.Core;
    using Microsoft.Secpal.Core.ObjectModel;

    public class ResourceAccessFact : Fact
    {
        public ResourceAccessFact(
            PrincipalIdentifier resource, PrincipalIdentifier module, PrincipalIdentifier group,
            IntegerIdentifier from, IntegerIdentifier to, IntegerIdentifier dayOfWeek,
            VerbIdentifier accessMode, IntegerIdentifier priority
            )
            : base(
                "ResourceAccess",
                resource, module,  group,
                from, to, dayOfWeek,
                accessMode, priority)
        {
            if (resource == null)
            {
                throw new ArgumentNullException("resource");
            }

            if (module == null)
            {
                throw new ArgumentNullException("module");
            }

            if (group == null)
            {
                throw new ArgumentNullException("target");
            }

            if (from == null)
            {
                throw new ArgumentNullException("from");
            }

            if (to == null)
            {
                throw new ArgumentNullException("to");
            }

            if (dayOfWeek == null)
            {
                throw new ArgumentNullException("dayOfWeek");
            }

            if (accessMode == null)
            {
                throw new ArgumentNullException("accessMode");
            }

            if (priority == null)
            {
                throw new ArgumentNullException("priority");
            }

            this.Resource = resource;
            this.Module = module;
            this.Group = group;

            this.From = from;
            this.To = to;
            this.DayOfWeek = dayOfWeek;

            this.AccessMode = accessMode;
            this.Priority = priority;
        }

        public PrincipalIdentifier Resource { get; private set; }
        public PrincipalIdentifier Module { get; private set; }
        public PrincipalIdentifier Group { get; private set; }

        public IntegerIdentifier From { get; private set; }
        public IntegerIdentifier To { get; private set; }
        public IntegerIdentifier DayOfWeek { get; private set; }

        public VerbIdentifier AccessMode { get; private set; }
        public IntegerIdentifier Priority { get; private set; }

        /// <summary>
        /// Returns true if the given object is equal to this one.
        /// </summary>
        /// <param name="obj">
        /// The object to be compared against.
        /// </param>
        /// <returns>
        /// True if the two objects are equal.
        /// </returns>
        public override bool Equals(object obj)
        {
            ResourceAccessFact otherObj = obj as ResourceAccessFact;
            if (otherObj == null)
            {
                return false;
            }

            if (!this.Module.Equals(otherObj.Module) ||
                !this.Resource.Equals(otherObj.Resource) ||
                !this.Group.Equals(otherObj.Group) ||

                !this.From.Equals(otherObj.From) ||
                !this.To.Equals(otherObj.To) ||
                !this.DayOfWeek.Equals(otherObj.DayOfWeek) ||

                !this.AccessMode.Equals(otherObj.AccessMode) ||
                !this.Priority.Equals(otherObj.Priority))
            {
                return false;
            }

            return base.Equals(obj);
        }

        /// <summary>
        /// Returns the hashcode for this object
        /// </summary>
        /// <returns>
        /// The hash code for this object
        /// </returns>
        public override int GetHashCode()
        {
            return
                this.Resource.GetHashCode() ^
                this.Module.GetHashCode() ^
                this.Group.GetHashCode() ^
                
                this.From.GetHashCode() ^
                this.To.GetHashCode() ^
                this.DayOfWeek.GetHashCode() ^

                this.AccessMode.GetHashCode() ^
                this.Priority.GetHashCode();
        }

        /// <summary>
        /// Returns a System.String that represents the given object.
        /// </summary>
        /// <returns>
        /// A System.String that represents the given object.
        /// </returns>
        public override string ToString()
        {
            return String.Format("{0} <- ({1}, {2}) ({3}-{4}, {5}) ({6}, {7})", 
                                 this.Resource, this.Module, this.Group, 
                                 this.From, this.To, this.DayOfWeek,
                                 this.AccessMode, this.Priority);
        }

        /// <summary>
        /// Apply a SyntaxElement.MappingFunction to the syntax tree.
        /// </summary>
        /// <param name="mappingFunction">An aribtrary mapping function to be applied to the syntax tree.</param>
        /// <returns>A new object of the enclosing type with the mapping function applied.</returns>
        public override SyntaxElement ApplyMappingFunction(MappingFunction mappingFunction)
        {
            return new ResourceAccessFact(
                (PrincipalIdentifier)mappingFunction(this.Resource),
                (PrincipalIdentifier)mappingFunction(this.Module),
                (PrincipalIdentifier)mappingFunction(this.Group),

                (IntegerIdentifier)mappingFunction(this.From),
                (IntegerIdentifier)mappingFunction(this.To),
                (IntegerIdentifier)mappingFunction(this.DayOfWeek),
                
                (VerbIdentifier)mappingFunction(this.AccessMode),
                (IntegerIdentifier)mappingFunction(this.Priority));
        }
    }


    public class UserGroupMembershipFact : Fact
    {
        public UserGroupMembershipFact(
            PrincipalIdentifier user,
            PrincipalIdentifier group
            )
            : base(
                "GroupMembership",
                user,
                group)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (group == null)
            {
                throw new ArgumentNullException("group");
            }

            this.User = user;
            this.Group = group;
        }

        public PrincipalIdentifier User { get; private set; }

        public PrincipalIdentifier Group { get; private set; }

        /// <summary>
        /// Returns true if the given object is equal to this one.
        /// </summary>
        /// <param name="obj">
        /// The object to be compared against.
        /// </param>
        /// <returns>
        /// True if the two objects are equal.
        /// </returns>
        public override bool Equals(object obj)
        {
            UserGroupMembershipFact otherObj = obj as UserGroupMembershipFact;
            if (otherObj == null)
            {
                return false;
            }

            if (!this.User.Equals(otherObj.User))
            {
                return false;
            }

            if (!this.Group.Equals(otherObj.Group))
            {
                return false;
            }

            return base.Equals(obj);
        }

        /// <summary>
        /// Returns the hashcode for this object
        /// </summary>
        /// <returns>
        /// The hash code for this object
        /// </returns>
        public override int GetHashCode()
        {
            return this.User.GetHashCode() ^
                    this.Group.GetHashCode();
        }

        /// <summary>
        /// Returns a System.String that represents the given object.
        /// </summary>
        /// <returns>
        /// A System.String that represents the given object.
        /// </returns>
        public override string ToString()
        {
            return this.User + " is in " + this.Group;
        }

        /// <summary>
        /// Apply a SyntaxElement.MappingFunction to the syntax tree.
        /// </summary>
        /// <param name="mappingFunction">An aribtrary mapping function to be applied to the syntax tree.</param>
        /// <returns>A new object of the enclosing type with the mapping function applied.</returns>
        public override SyntaxElement ApplyMappingFunction(MappingFunction mappingFunction)
        {
            return new UserGroupMembershipFact(
                (PrincipalIdentifier)mappingFunction(this.User),
                (PrincipalIdentifier)mappingFunction(this.Group));
        }
    }

    




}
