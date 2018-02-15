using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Secpal.Core.ObjectModel;
using HomeOS.Hub.Common;
using HomeOS.Hub.Platform.Views;

namespace HomeOS.Hub.Platform
{
    //public class PolicyRule
    //{
     
    //    public PolicyRule(int ruleId, int userConfId,
    //                      int portId, int moduleId, int groupId, 
    //                      int dayMinFrom, int dayMinTo, int dayOfWeek, 
    //                      AccessMode aMode, int priority)
    //    {
    //        //sanity check the values
    //        // 1. we enforce this currently (for times like 10pm-6am, we'll have two rules).
    //        if (dayMinTo < dayMinFrom)
    //            throw new Exception("Illegal values for dayMin: " + dayMinFrom + " > " + dayMinTo);

    //        if (dayOfWeek < -1 || dayOfWeek > 6)
    //            throw new Exception("Illegal value for dayOfWeek: " + dayOfWeek);

    //        this.RuleId = ruleId;
    //        this.UserConfId = userConfId;

    //        this.PortId = portId;
    //        this.ModuleId = moduleId;
    //        this.GroupId = groupId;

    //        this.DayMinuteFrom = dayMinFrom;
    //        this.DayMinuteTo = dayMinTo;
    //        this.DayOfWeek = dayOfWeek;

    //        this.AccessMode = aMode;
    //        this.Priority = priority;

    //    }

    //    public int UserConfId { get; private set; }
    //    public int RuleId { get; private set; }

    //    public int PortId { get; private set; }
    //    public int ModuleId { get; private set; }
    //    public int GroupId { get; private set; }

    //    public int DayMinuteFrom { get; private set; }
    //    public int DayMinuteTo { get; private set; }
    //    public int DayOfWeek { get; private set; }

    //    public AccessMode AccessMode { get; private set; }
    //    public int Priority { get; private set; }

    //}


    public class PolicyEngine
    {

        List<UserGroupMembershipFact> groupMembershipFacts;
        List<ResourceAccessFact> resourceAccessFacts;

        List<Assertion> policyAssertions;

        LocalAuthorityPrincipal localAuthority;
        VLogger logger;

        public PolicyEngine(VLogger logger)
        {
            this.logger = logger;
            localAuthority = new LocalAuthorityPrincipal();
        }

        //add a new user to the policy database
        internal void AddUser(UserInfo userInfo)
        {
           lock (this)
            {
               //recursively add this user as belonging to all parent groups 
               //we start with the user itself, as each user belongs to its own group

                UserGroupInfo ancestor = userInfo;
                
                while (ancestor != null)
                {
                    UserGroupMembershipFact fact = new UserGroupMembershipFact(new StringPrincipal("usr:" + userInfo.Name),
                                                                       new StringPrincipal("grp:" + ancestor.Name));
                    groupMembershipFacts.Add(fact);
                    policyAssertions.Add(new Assertion(localAuthority, new Claim(fact)));

                    ancestor = ancestor.Parent;
                }
            }
        }

        //add a new user to the policy database
        internal void RemoveUser(UserInfo userInfo)
        {
            lock (this)
            {
                List<Assertion> asserstionsToRemove = new List<Assertion>();
                foreach (var assertion in policyAssertions)
                {
                    if (assertion.Claim.Fact is UserGroupMembershipFact)
                    {
                        UserGroupMembershipFact fact = (UserGroupMembershipFact)assertion.Claim.Fact;

                        if (fact.User.Name.Equals("usr:" + userInfo.Name))
                            asserstionsToRemove.Add(assertion);
                    }
                    else if (assertion.Claim.Fact is ResourceAccessFact)
                    {
                        ResourceAccessFact fact = (ResourceAccessFact)assertion.Claim.Fact;

                        if (fact.Group.Name.Equals("grp:" + userInfo.Name))
                            asserstionsToRemove.Add(assertion);
                    }
                    else
                    {
                        throw new Exception("Unknown fact type!");
                    }
                }

                foreach (var assertion in asserstionsToRemove)
                {
                    policyAssertions.Remove(assertion);
                }
            }

            //PrintPolicies();
        }

        internal void AddAccessRule(AccessRule rule)
        {
            lock (this)
            {
                foreach (string portName in rule.DeviceList)
                {
                    foreach (TimeOfWeek timeOfWeek in rule.TimeList)
                    {
                        ResourceAccessFact fact = new ResourceAccessFact(new StringPrincipal("port:" + portName),
                                                                         new StringPrincipal("mod:" + rule.ModuleName),
                                                                         new StringPrincipal("grp:" + rule.UserGroup),

                                                                         new IntegerHolder(timeOfWeek.StartMins),
                                                                         new IntegerHolder(timeOfWeek.EndMins),
                                                                         new IntegerHolder(timeOfWeek.DayOfWeek),

                                                                         new VerbHolder(rule.AccessMode.ToString()),
                                                                         new IntegerHolder(rule.Priority));
                        resourceAccessFacts.Add(fact);
                        policyAssertions.Add(new Assertion(localAuthority, new Claim(fact)));
                    }
                }
            }
        }


        internal bool RemoveAccessRule(string appFriendlyName, string deviceFriendlyName)
        {
            bool removedSomething = false;

            lock (this)
            {
                List<Assertion> asserstionsToRemove = new List<Assertion>();
                foreach (var assertion in policyAssertions)
                {
                    ResourceAccessFact fact = assertion.Claim.Fact as ResourceAccessFact;

                    if (fact != null &&
                        fact.Module.Name.Equals("mod:" + appFriendlyName) &&
                        fact.Resource.Name.Equals("port:" + deviceFriendlyName))
                        asserstionsToRemove.Add(assertion);

                }

                foreach (var assertion in asserstionsToRemove)
                {
                    policyAssertions.Remove(assertion);
                    removedSomething = true;
                }
            }

            //PrintPolicies();

            return removedSomething;
        }

        internal void RemoveAccessRulesForDevice(string deviceFriendlyName)
        {
            lock (this)
            {
                List<Assertion> asserstionsToRemove = new List<Assertion>();
                foreach (var assertion in policyAssertions)
                {
                    ResourceAccessFact fact = assertion.Claim.Fact as ResourceAccessFact;

                    if (fact != null &&
                        fact.Resource.Name.Equals("port:" + deviceFriendlyName))
                        asserstionsToRemove.Add(assertion);
                }

                foreach (var assertion in asserstionsToRemove)
                {
                    policyAssertions.Remove(assertion);
                }
            }

            //PrintPolicies();
        }

        internal void RemoveAccessRulesForModule(string moduleFriendlyName)
        {
            lock (this)
            {
                List<Assertion> asserstionsToRemove = new List<Assertion>();
                foreach (var assertion in policyAssertions)
                {
                    ResourceAccessFact fact = assertion.Claim.Fact as ResourceAccessFact;

                    if (fact != null &&
                        fact.Module.Name.Equals("mod:" + moduleFriendlyName))
                        asserstionsToRemove.Add(assertion);
                }

                foreach (var assertion in asserstionsToRemove)
                {
                    policyAssertions.Remove(assertion);
                }
            }

            //PrintPolicies();
        }


        internal void Init(Configuration config)
        {
            groupMembershipFacts = new List<UserGroupMembershipFact>();
            resourceAccessFacts = new List<ResourceAccessFact>();
            policyAssertions = new List<Assertion>();

            //add the user system to group everyone
            UserGroupMembershipFact fact = new UserGroupMembershipFact(new StringPrincipal("usr:" + Constants.UserSystem.Name),
                                                                      new StringPrincipal("grp:" + "everyone"));


            groupMembershipFacts.Add(fact);
            policyAssertions.Add(new Assertion(localAuthority, new Claim(fact)));

            // Adding AccessRules to allow SystemHigh to access all modules at all times with all devices
            AddSystemHighRules(config);

            //add group membership for other users
            foreach (UserInfo userInfo in config.GetAllUsers())
                AddUser(userInfo);

            //now add the access control rules
            foreach (var rule in config.GetAllPolicies())
                AddAccessRule(rule);

            // ..... now print these policies
            //PrintPolicies();
        }

        private void AddSystemHighRules(Configuration config)
        {
            AccessRule systemHighaccessRule;
            foreach (string moduleName in config.allModules.Keys)
            {
                systemHighaccessRule = new AccessRule();
                systemHighaccessRule.ModuleName = moduleName;
                systemHighaccessRule.RuleName = Constants.SystemHigh;
                systemHighaccessRule.UserGroup = Constants.SystemHigh;
                systemHighaccessRule.AccessMode = AccessMode.Allow;
                systemHighaccessRule.DeviceList = new List<string> { "*" };
                systemHighaccessRule.TimeList = new List<TimeOfWeek> { new TimeOfWeek(-1, 0, 2400) };
                systemHighaccessRule.Priority = 0;
                AddAccessRule(systemHighaccessRule);
            }

            // Adding systemhigh access rules for "platform-based" modules GuiWeb and GuiWebSec
            systemHighaccessRule = new AccessRule();
            systemHighaccessRule.RuleName = Constants.SystemHigh;
            systemHighaccessRule.UserGroup = Constants.SystemHigh;
            systemHighaccessRule.AccessMode = AccessMode.Allow;
            systemHighaccessRule.DeviceList = new List<string> { "*" };
            systemHighaccessRule.TimeList = new List<TimeOfWeek> { new TimeOfWeek(-1, 0, 2400) };
            systemHighaccessRule.Priority = 0;
            systemHighaccessRule.ModuleName = Constants.GuiServiceSuffixWeb;
            AddAccessRule(systemHighaccessRule);

            systemHighaccessRule.ModuleName = Constants.GuiServiceSuffixWebSec;
            AddAccessRule(systemHighaccessRule);

            // Adding systemhigh access rules for scouts
            systemHighaccessRule = new AccessRule();
            systemHighaccessRule.RuleName = Constants.SystemHigh;
            systemHighaccessRule.UserGroup = Constants.SystemHigh;
            systemHighaccessRule.AccessMode = AccessMode.Allow;
            systemHighaccessRule.DeviceList = new List<string> { "*" };
            systemHighaccessRule.TimeList = new List<TimeOfWeek> { new TimeOfWeek(-1, 0, 2400) };
            systemHighaccessRule.Priority = 0;
            systemHighaccessRule.ModuleName = Constants.ScoutsSuffixWeb;
            AddAccessRule(systemHighaccessRule);

            

        }

        internal void PrintPolicies()
        {
            foreach (var assertion in policyAssertions)
            {
                logger.Log(assertion.ToString());
            }
        }

        internal DateTime AllowAccess(string portName, string moduleName, string username)
        {

            AssertionExpression resourceAccessAssertion = new AssertionExpression(
                                                              new AtomicAssertion(
                                                                  localAuthority,
                                                                  new AtomicClaim(
                                                                      new ResourceAccessFact(
                                                                          new StringPrincipal("port:" + portName),
                                                                          new StringPrincipal("mod:" + moduleName),
                                                                          new PrincipalVariable("$grp"),

                                                                          new IntegerVariable("$from"),
                                                                          new IntegerVariable("$to"),
                                                                          new IntegerVariable("$day"),

                                                                          new VerbVariable("$amode"),
                                                                          new IntegerVariable("prio")))));

            AssertionExpression groupMembershipAssertion = new AssertionExpression(
                                                               new AtomicAssertion(
                                                                    localAuthority,
                                                                    new AtomicClaim(
                                                                        new UserGroupMembershipFact(
                                                                            new StringPrincipal("usr:" + username),
                                                                        new PrincipalVariable("$grp")))));
            DateTime currTime = DateTime.Now;
            
            int currMinute = currTime.Hour * 100 | currTime.Minute;
            
            Expression minutesMoreThanFrom = new ConstraintExpression(new LessThanOrEqualConstraint(new IntegerVariable("$from"), new IntegerHolder(currMinute)));
            Expression minutesLessThanTo = new ConstraintExpression(new LessThanOrEqualConstraint(new IntegerHolder(currMinute), new IntegerVariable("$to")));
            Expression minutesInRange = new AndExpression(minutesMoreThanFrom, minutesLessThanTo);

            int currDayOfWeek = (int) currTime.DayOfWeek;

            Expression noDayOfWeekRestriction = new NotExpression(new ConstraintExpression(new InequalityConstraint(new IntegerVariable("$day"), new IntegerHolder(-1))));
            Expression dayOfWeekMatches = new NotExpression(new ConstraintExpression(new InequalityConstraint(new IntegerVariable("$day"), new IntegerHolder(currDayOfWeek))));
            Expression dayOfWeekAllowed = new OrExpression(noDayOfWeekRestriction, dayOfWeekMatches);

            Query query = new Query(
                              new AndExpression(
                                  resourceAccessAssertion,
                                  groupMembershipAssertion,
                                  minutesInRange,
                                  dayOfWeekAllowed));
                              
            QueryContext context = new QueryContext(localAuthority, policyAssertions, query, 
                                                    DateTime.UtcNow, new PrincipalIdentifier[] { }, new Uri[] { }, 0, false);

            ReadOnlyCollection<Answer> answers = new Microsoft.Secpal.Authorization.QueryEngine().ExecuteQuery(context);

            //logger.Log("\nquery: " + query + "\n");
            //logger.Log("answers: {0}", answers.Count.ToString());
            //foreach (Answer answer in answers)
            //    logger.Log(answer.Substitution.ToString());

            return (answers.Count > 0) ? DateTime.MaxValue : DateTime.MinValue;
                
        }
    }

}