namespace HomeOS.Hub.Common
{
    using System;
    using System.Collections.Generic;

    public sealed class RoleList
    {
        List<HomeOS.Hub.Platform.Views.VRole> roles = new List<HomeOS.Hub.Platform.Views.VRole>();

        public bool Optional { get; set; }

        public void AddRole(HomeOS.Hub.Platform.Views.VRole role)
        {
            roles.Add(role);
        }

        public List<HomeOS.Hub.Platform.Views.VRole> GetRoles()
        {
            return roles;
        }

        public bool IsContained(List<HomeOS.Hub.Platform.Views.VRole> rolesInHome)
        {
            if (roles.Count == 0)
            {
                return true;
            }

            foreach (HomeOS.Hub.Platform.Views.VRole role in roles)
            {
                foreach (var homeRole in rolesInHome)
                {
                    if (Role.ContainsRole(homeRole, role))
                        return true;
                }
            }

            return false;
        }

        public bool IsContained(HomeOS.Hub.Platform.Views.VRole role)
        {
            foreach (HomeOS.Hub.Platform.Views.VRole myRole in roles)
            {
                if (Role.ContainsRole(role, myRole))
                    return true;
            }

            return false;
        }

        public override string ToString()
        {
            string ret = "";
            for (int i = 0; i < roles.Count; i++)
            {
                ret += roles[i];
                if (i != roles.Count - 1)
                {
                    ret += " or ";
                }
            }
            return ret;
        }
    }

    public sealed class Manifest
    {
        List<RoleList> roleLists;

        public Manifest()
        {
            roleLists = new List<RoleList>();
        }

        public void AddRoleList(RoleList roleList) 
        {
            roleLists.Add(roleList);
        }

        public List<RoleList> GetRoleLists()
        {
            return roleLists;
        }

        public bool IsCompatibleWithHome(List<HomeOS.Hub.Platform.Views.VRole> rolesInHome)
        {
            //TODO combine with MissingRolesString and MissingRoles
            foreach (RoleList roleList in roleLists)
            {
                if (!roleList.Optional && !roleList.IsContained(rolesInHome))
                    return false;
            }

            return true;
        }

        public string MissingRolesString(List<HomeOS.Hub.Platform.Views.VRole> rolesInHome)
        {
            //TODO combine with IsCompatibleWithHome and MissingRoles
            string ret = "";
            foreach (RoleList roleList in roleLists)
            {
                if (!roleList.Optional && !roleList.IsContained(rolesInHome))
                {
                    ret += "(" + roleList.ToString() + "), ";
                }
            }
            if (ret != "")
            {
                return ret.Substring(0, ret.Length - 2);
            }
            else
            {
                return ret;
            }
        }

        public List<List<HomeOS.Hub.Platform.Views.VRole>> MissingRoles(List<HomeOS.Hub.Platform.Views.VRole> rolesInHome)
        {
            //TODO combine with IsCompatibleWithHome and MissingRolesString
            List<List<HomeOS.Hub.Platform.Views.VRole>> ret = new List<List<HomeOS.Hub.Platform.Views.VRole>>();
            foreach (RoleList roleList in roleLists)
            {
                if (!roleList.Optional && !roleList.IsContained(rolesInHome))
                {
                    ret.Add(roleList.GetRoles());
                }
            }
            return ret;
        }

        public bool IsCompatibleWithRole(HomeOS.Hub.Platform.Views.VRole role)
        {
            foreach (RoleList roleList in roleLists)
            {
                if (roleList.IsContained(role))
                    return true;
            }

            return false;
        }
    }
}
