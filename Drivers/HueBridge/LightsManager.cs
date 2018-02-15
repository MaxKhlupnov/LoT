using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO; 
using System.Drawing;
using System.Timers;
using HomeOS.Hub.Platform.Views;
using Newtonsoft.Json;
using Newtonsoft;
using Newtonsoft.Json.Linq;

namespace HomeOS.Hub.Drivers.HueBridge
{

    /// <summary>
    /// Class for handling changes to a group of lights.
    /// </summary>
    public class LightGroup
    {
        /// <summary>
        /// Name of the group.
        /// </summary>
        private int m_iGroup;

        /// <summary>
        /// Name of the group.
        /// </summary>
        private string m_strName;

        /// <summary>
        /// Flag for if a refresh needs to happen next time the light group is given a request (usually because one light from the group was changed).
        /// </summary>
        private bool m_bDirty = false;

        /// <summary>
        /// state of the group set.
        /// </summary>
        private LightState m_state = new LightState();

        /// <summary>
        /// List of all light ids.
        /// </summary>
        private List<int> m_listLights = new List<int>();

        /// <summary>
        /// List of all light ids that belong to this group.
        /// </summary>
        public List<int> LightSet
        {
            get { return m_listLights; }
        }

        /// <summary>
        /// State of the lights.
        /// </summary>
        public LightState State
        {
            get { return m_state; }
        }

        /// <summary>
        /// Id of the group.
        /// </summary>
        public int ID
        {
            get { return m_iGroup; }
        }

        /// <summary>
        /// Name of the group.
        /// </summary>
        public string Name
        {
            get { return m_strName; }
        }

        /// <summary>
        /// Whether this light state needs to update next time it is given a change.
        /// </summary>
        public bool Dirty
        {
            get { return m_bDirty; }
            set { m_bDirty = value; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="strName"></param>
        /// <param name="lightSet"></param>
        public LightGroup(int iID, string name, int[] lightSet)
        {
            m_strName = name;
            m_iGroup = iID;
            m_listLights = new List<int>(lightSet);
        }

        /// <summary>
        /// When a single light is changed, the group needs to check if it is a part of the group to flag that it's dirty.
        /// </summary>
        /// <param name="iLightID"></param>
        public void OnSingleLightChange(int iLightID)
        {
            if (m_listLights.Contains(iLightID))
            {
                m_bDirty = true;
            }
        }
    }

    /// <summary>
    /// Class for controlling lights and light groups.
    /// </summary>
    public class LightsManager
    {

        // IP of the bridge for the lights.
        private IPAddress m_strLightsIP;

        /// <summary>
        /// Registered user id for the lights.
        /// </summary>
        private string m_strUser;
        
        /// <summary>
        /// 
        /// </summary>
        private bool m_bMakingtRequest = false; // if the light system is making a request to the lights.

        /// <summary>
        /// Queue for the update order that the lights need to be changed in based on request timing. 
        /// </summary>
        private Queue<KeyValuePair<int, bool>> m_queLightUpdateOrder = new Queue<KeyValuePair<int, bool>>(); // Order the lights should be updated in to prevent one light from blocking another. True means ID represents a group.
        
        /// <summary>
        /// States of all the individual lights.
        /// </summary>
        private List<LightState> m_listLightStates = new List<LightState>();
    
        /// <summary>
        /// Groups of lights.
        /// </summary>
        private List<LightGroup> m_listLightGroup = new List<LightGroup>();


        /// <summary>
        /// State for the light that's been bumped.
        /// </summary>
        private LightState m_bumpState = new LightState();

        /// <summary>
        /// Index of the light that's been bumped.
        /// </summary>
        private int m_iBumpedLightIndex = -1;

        /// <summary>
        /// Timer for throttling how often calls can be made to the lights system.
        /// </summary>
        Timer m_updateTimer = new Timer(1000);

        /// <summary>
        /// Last log returned by the light bridge.
        /// </summary>
        private String m_strLastLog = null;

        private VLogger logger;

        public String LastLog
        {
            get { return m_strLastLog; }
        }

        /// <summary>
        /// Number of bulbs
        /// </summary>
        public int BulbCount
        {
            get { return m_listLightStates.Count; }
        }

        /// <summary>
        /// Get the number of groups.
        /// </summary>
        public int GroupCount
        {
            get { return m_listLightGroup.Count; }
        }

        /// <summary>
        /// Constructor that loads based on user params
        /// </summary>
        /// <param name="strLightsIP"></param>
        /// <param name="strUser"></param>
        public LightsManager(IPAddress strLightsIP, string strUser, VLogger logger)
        {
            m_strLightsIP = strLightsIP;
            m_strUser = strUser;
            this.logger = logger;
        }

        /// <summary>
        /// Loads hub data values.
        /// </summary>
        /// <param name="strLightsIP"></param>
        /// <param name="strUser"></param>
        /// <param name="iBulbCount"></param>
        //private void Load(IPAddress strLightsIP, string strUser, int iBulbCount)
        //{
        //    m_strLightsIP = strLightsIP;
        //    m_strUser = strUser;

        //    List<int> listIDs = new List<int>();
        //    for (int i = 0; i < iBulbCount; ++i)
        //    {
        //        m_listLightStates.Add(new LightState());
        //        listIDs.Add(i);
        //    }

        //    // Add the all set. Don't call the standard method for all lights so there isn't any accidents if there are more lights than the program knows about.
        //    LightGroup group = new LightGroup(0, "All", listIDs.ToArray());
        //    m_listLightGroup.Add(group);

        //    ResetAllBulbs();

        //    m_updateTimer.Elapsed += Update;
        //    m_updateTimer.Start();
        //}

        /// <summary>
        /// Initializes the light states
        /// </summary>
        public void Init()
        {

            string response = MakeLightRequest("/api/" + m_strUser, "GET");

            JObject jobject = JObject.Parse(response);

            foreach (var topChild in jobject.Children())
            {
                if (topChild.Type == JTokenType.Property && ((JProperty)topChild).Name.Equals("lights"))
                {

                    if (topChild.Children().Count() != 1)
                    {
                        logger.Log("I don't understand the Hue Bridge response!. Quitting");
                        return;
                    }

                    var lightList = topChild.Children().First();

                    foreach (var light in lightList.Children())
                    {
                        LightState lstate = new LightState();
                        
                        lstate.Index = int.Parse(((JProperty)light).Name);
                        
                        if (light.Children().Count() != 1)
                        {
                            logger.Log("I don't understand the Hue Bridge response for light object!. Quitting");
                            return;
                        }

                        var lightDetails = light.Children().First();

                        lstate.Name = lightDetails["name"].ToString();

                        lstate.ParseState(lightDetails["state"]);

                        m_listLightStates.Add(lstate);
                    }

                }
            }
        }

        public void SetBridgeIp(IPAddress newBridgeIp)
        {
            m_strLightsIP = newBridgeIp;
        }

        public bool CanReachBridge()
        {
            try
            {
                string response = MakeLightRequest("/api/" + m_strUser + "/lights", "GET");
                return true;
            }
            catch (Exception e)
            {
                logger.Log("Exception while trying to reach hue bridge: " + e.Message);
                return false;
            }

        }


        ///// <summary>
        ///// Adds a light group to the system.
        ///// </summary>
        ///// <param name="iId"></param>
        ///// <param name="arLightIds"></param>
        //public void AddGroup(string strname, int[] arLightIds)
        //{
        //    LightGroup group = new LightGroup(m_listLightGroup.Count, strname,  arLightIds);

        //    m_listLightGroup.Add(group);

        //    // Push over the group to the bridge.
        //    string json = "{ \"name\": \"" + strname + "\", \"lights\":[";
        //    foreach (int i in group.LightSet)
        //    {
        //        json += "\"" + (i+1) + "\",";
        //    }
        //    json += "]}";
        //    MakeLightRequest("/api/" + m_strUser + "/groups/" + group.ID + "/action", "PUT", json);
        //}

        ///// <summary>
        ///// Matches the group name to the string.
        ///// </summary>
        ///// <param name="strName"></param>
        ///// <returns></returns>
        //public LightGroup GetGroup(string strName)
        //{
        //    foreach (LightGroup group in m_listLightGroup)
        //    {
        //        if (group.Name == strName)
        //        {
        //            return group;
        //        }
        //    }

        //    return null;
        //}

        ///// <summary>
        ///// Get the color of a given light.
        ///// </summary>
        ///// <param name="iLight"></param>
        ///// <returns></returns>
        //public Color GetLightColor(int iLight)
        //{
        //    if (iLight < 0 || iLight >= m_listLightStates.Count)
        //    {
        //        return Color.White;
        //    }

        //    return m_listLightStates[iLight].Color;
        //}

        ///// <summary>
        ///// Update the state of lights that were queued up while the requests were blocking.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //public void Update(object source, ElapsedEventArgs e)
        //{
        //    if (!m_bMakingtRequest)
        //    {
        //        // If a light needs to be bumped.
        //        if (m_iBumpedLightIndex >= 0)
        //        {
        //            MakeLightRequest("/api/" + m_strUser + "/lights/" + (m_iBumpedLightIndex + 1) + "/state", "PUT", m_bumpState.ToJSON());
        //            m_iBumpedLightIndex = -1;
        //        }
        //        else if (m_queLightUpdateOrder.Count > 0)
        //        {
        //            KeyValuePair<int, bool> pairUpdateRequest = m_queLightUpdateOrder.Dequeue();

        //            if (pairUpdateRequest.Value)
        //            {
        //                // Update a group of lights.
        //                LightGroup group = m_listLightGroup[pairUpdateRequest.Key];
        //                MakeLightRequest("/api/" + m_strUser + "/groups/" + group.ID + "/action", "PUT", group.State.ToJSON());
        //            }
        //            else
        //            {
        //                // Update a single light.
        //                LightState state = m_listLightStates[(int)pairUpdateRequest.Key];
        //                MakeLightRequest("/api/" + m_strUser + "/lights/" + (pairUpdateRequest.Key + 1) + "/state", "PUT", state.ToJSON());
        //            }
        //        }
        //    }   
        //}

        ///// <summary>
        ///// Set the level at which the lights can interact.
        ///// </summary>
        //public void SetAllLockLevel(int iLockLevel)
        //{
        //    foreach (LightState state in m_listLightStates)
        //    {
        //        state.PriorityLock = iLockLevel;
        //    }
        //}

        ///// <summary>
        ///// Set the lock level for a single light.
        ///// </summary>
        ///// <param name="iIndex"></param>
        ///// <param name="iLockLevel"></param>
        //public void SetSingleLockLevel(int iIndex, int iLockLevel)
        //{
        //    if (iIndex >= m_listLightStates.Count || iIndex < 0)
        //    {
        //        return;
        //    }

        //    m_listLightStates[iIndex].PriorityLock = iLockLevel;
        //}

        ///// <summary>
        ///// Turn on all lights.
        ///// </summary>
        //public void TurnAllLightsOn()
        //{
        //    TurnGroupOn("All");
        //}

        ///// <summary>
        ///// Turn off all lights.
        ///// </summary>
        //public void TurnAllLightsOff()
        //{
        //    TurnGroupOff("All");
        //}

        ///// <summary>
        ///// Turn off all lights.
        ///// </summary>
        //public void ToggleAllLights()
        //{
        //    ToggleGroup("All");
        //}

        ///// <summary>
        ///// Turn on a specific group of lights.
        ///// </summary>
        ///// <param name="strGroup"></param>
        //public void TurnGroupOn(string strGroupName)
        //{
        //    LightGroup group = GetGroup(strGroupName);
        //    if (group == null)
        //    {
        //        return;
        //    }

        //    //If the state is different, or it has been marked that one of the individual lights have changed.
        //    if (!group.State.Enabled || group.Dirty)
        //    {
        //        foreach (int iLightId in group.LightSet)
        //        {
        //            m_listLightStates[iLightId].Enabled = true;
        //            m_listLightStates[iLightId].PriorityLock = 1;
        //        }

        //        group.State.Enabled = true;

        //        UpdateGroupState(group.ID);
        //    }
        //}

        ///// <summary>
        ///// Turn off a specific group of lights.
        ///// </summary>
        ///// <param name="strGroup"></param>
        //public void TurnGroupOff(string strGroupName)
        //{
        //    LightGroup group = GetGroup(strGroupName);
        //    if (group == null)
        //    {
        //        return;
        //    }

        //    //If the state is different, or it has been marked that one of the individual lights have changed.
        //    if (group.State.Enabled || group.Dirty)
        //    {
        //        foreach (int iLightId in group.LightSet)
        //        {
        //            m_listLightStates[iLightId].Enabled = false;
        //            m_listLightStates[iLightId].PriorityLock = 1;
        //        }

        //        group.State.Enabled = false;

        //        UpdateGroupState(group.ID);
        //    }
        //}

        ///// <summary>
        ///// Turn all of a group on or off based on the last setting of the camera group.
        ///// </summary>
        ///// <param name="strGroupName"></param>
        //public void ToggleGroup(string strGroupName)
        //{
        //    LightGroup group = GetGroup(strGroupName);
        //    if (group == null)
        //    {
        //        return;
        //    }

        //    group.State.Enabled = !group.State.Enabled;
        //    foreach (int iLightId in group.LightSet)
        //    {
        //        m_listLightStates[iLightId].Enabled = group.State.Enabled;
        //        m_listLightStates[iLightId].PriorityLock = 1;
        //    }


        //    UpdateGroupState(group.ID);
        //}


        ///// <summary>
        ///// Set a group of lights to a specific color. If the lights are off, this will turn them on.
        ///// </summary>
        ///// <param name="strGroup"></param>
        ///// <param name="color"></param>
        //public void SetGroupColor(string strGroupName, Color color)
        //{
        //    LightGroup group = GetGroup(strGroupName);
        //    if (group == null)
        //    {
        //        return;
        //    }

        //    //If the state is different, or it has been marked that one of the individual lights have changed.
        //    if (!group.State.Enabled || group.State.Color != color || group.Dirty)
        //    {
        //        foreach (int iLightId in group.LightSet)
        //        {
        //            m_listLightStates[iLightId].Enabled = true;
        //            m_listLightStates[iLightId].Color = color;
        //            m_listLightStates[iLightId].PriorityLock = 1;
        //        }

        //        group.State.Enabled = true;
        //        group.State.Color = color;

        //        UpdateGroupState(group.ID);
        //    }
        //}

        ///// <summary>
        ///// Sets the brightness of a light in the white color spectrum.
        ///// </summary>
        ///// <param name="iIndex"></param>
        ///// <param name="fBrightNormalized"></param>
        //public void SetGroupBrightness(string strGroupName, float fBrightNormalized)
        //{
        //    int iColorValue = (int)(255.0f * fBrightNormalized);
        //    Color color = Color.FromArgb(255, iColorValue, iColorValue, iColorValue);
        //    SetGroupColor(strGroupName, color);
        //}

        ///// <summary>
        ///// Turn on a single bulb.
        ///// </summary>
        ///// <param name="iIndex"></param>
        //public void TurnOnBulb(int iIndex, int iLockLevel=0)
        //{
        //    if (iIndex >= m_listLightStates.Count)
        //    {
        //        return;
        //    }

        //    LightState state = m_listLightStates[iIndex];

        //    if (state.PriorityLock <= iLockLevel && !state.Enabled)
        //    {
        //        state.PriorityLock = iLockLevel;

        //        //Notify all groups of the light changing in case it affects them.
        //        foreach (LightGroup group in m_listLightGroup)
        //        {
        //            group.OnSingleLightChange(iIndex);
        //        }

        //        state.Enabled = true;
        //        UpdateLightState(iIndex);
        //    }
        //}

        ///// <summary>
        ///// Turn off a single bulb.
        ///// </summary>
        ///// <param name="iIndex"></param>
        //public void TurnOffBulb(int iIndex, int iLockLevel = 0)
        //{
        //    if (iIndex >= m_listLightStates.Count)
        //    {
        //        return;
        //    }

        //    LightState state = m_listLightStates[iIndex];
        //    if (state.PriorityLock <= iLockLevel && state.Enabled)
        //    {
        //        state.PriorityLock = iLockLevel;

        //        //Notify all groups of the light changing in case it affects them.
        //        foreach (LightGroup group in m_listLightGroup)
        //        {
        //            group.OnSingleLightChange(iIndex);
        //        }

        //        state.Enabled = false;
        //        UpdateLightState(iIndex);
        //    }
        //}

        ///// <summary>
        ///// Change the bulb to on or off depending what it's currently set to.
        ///// </summary>
        ///// <param name="iIndex"></param>
        //public void ToggleBulb(int iIndex, int iLockLevel = 0)
        //{
        //    if (iIndex >= m_listLightStates.Count)
        //    {
        //        return;
        //    }

        //    //Notify all groups of the light changing in case it affects them.
        //    foreach (LightGroup group in m_listLightGroup)
        //    {
        //        group.OnSingleLightChange(iIndex);
        //    }

        //    LightState state = m_listLightStates[iIndex];
        //    if (state.PriorityLock <= iLockLevel)
        //    {
        //        state.PriorityLock = iLockLevel;
        //        state.Enabled = !state.Enabled;
        //        UpdateLightState(iIndex);
        //    }
        //}

        ///// <summary>
        ///// Have the light wink as a given event for focus.
        ///// </summary>
        ///// <param name="iIndex"></param>
        ///// <param name="iLockLevel"></param>
        //public void BumpBulb(int iIndex)
        //{
        //    if (iIndex >= m_listLightStates.Count)
        //    {
        //        return;
        //    }

        //    LightState state = m_listLightStates[iIndex];

        //    m_bumpState.Copy(state);

        //    if (!m_bumpState.Enabled ||
        //        (m_bumpState.Color.R == 0 &&
        //        m_bumpState.Color.G == 0 &&
        //        m_bumpState.Color.B == 0))
        //    {
        //        m_bumpState.Color = Color.FromArgb(10, 10, 10);
        //        m_bumpState.Enabled = true;
        //    }
        //    else
        //    {
        //        const int iOffset = 90;
        //        m_bumpState.Color = Color.FromArgb(Math.Max((int)state.Color.R - iOffset, 1),
        //            Math.Max((int)state.Color.G - iOffset, 1),
        //            Math.Max((int)state.Color.B - iOffset, 1));
        //    }
        //    UpdateLightState(iIndex);


        //    m_iBumpedLightIndex = iIndex;
        //}

        ///// <summary>
        ///// Turn all the bulbs off and reset their color.  Bug: Does not work if lights are already off.
        ///// </summary>
        //public void ResetAllBulbs()
        //{
        //    foreach (LightState state in m_listLightStates)
        //    {
        //        state.Reset();
        //    }

        //    foreach (LightGroup group in m_listLightGroup)
        //    {
        //        group.State.Reset();
        //        group.Dirty = false;
        //    }

        //    UpdateGroupState(0);
        //}

        ///// <summary>
        ///// Reset a bulb to white and turn it off.
        ///// </summary>
        ///// <param name="uiIndex"></param>
        //public void ResetBulb(int iIndex)
        //{
        //    if (iIndex >= m_listLightStates.Count || iIndex < 0)
        //    {
        //        return;
        //    }

        //    //Notify all groups of the light changing in case it affects them.
        //    foreach (LightGroup group in m_listLightGroup)
        //    {
        //        group.OnSingleLightChange(iIndex);
        //    }

        //    LightState state = m_listLightStates[iIndex];
        //    state.Color = Color.White;
        //    state.Enabled = false;
        //    state.PriorityLock = 0;

        //    UpdateLightState(iIndex);
        //}

        ///// <summary>
        ///// Assign a new color to a specific light.
        ///// </summary>
        ///// <param name="uiIndex"></param>
        ///// <param name="color"></param>
        //public void SetLightColor(int iIndex, Color color, int iLockLevel=0)
        //{
        //    if (iIndex >= m_listLightStates.Count || iIndex < 0)
        //    {
        //        return;
        //    }


        //    LightState state = m_listLightStates[iIndex];
        //    if (iLockLevel >= state.PriorityLock && (!state.Enabled || state.Color != color))
        //    {
        //        state.PriorityLock = iLockLevel;

        //        //Notify all groups of the light changing in case it affects them.
        //        foreach (LightGroup group in m_listLightGroup)
        //        {
        //            group.OnSingleLightChange(iIndex);
        //        }

        //        state.Enabled = color.R != 0 && color.B != 0 && color.G != 0 ;
        //        state.Color = color;
        //        UpdateLightState(iIndex);
        //    }

        //}

        ///// <summary>
        ///// Sets the brightness of the light based on a normalized scalar value.  Will turn the light into a gray scale color.
        ///// </summary>
        ///// <param name="uiIndex">Light index</param>
        ///// <param name="fBrightNormalized">Scale of what the brightness should be set to.</param>
        //public void SetLightBrightness(int iIndex, float fBrightNormalized, int iLockLevel = 0)
        //{
        //    int iColorValue = (int)(255.0f * fBrightNormalized);
        //    Color colorLight = Color.FromArgb(255, iColorValue, iColorValue, iColorValue);

        //    SetLightColor(iIndex, colorLight, iLockLevel);
        //}

        /// <summary>
        /// Either push the light state over if not waiting or add to the waiting queue.
        /// </summary>
        /// <param name="uiIndex"></param>
        //private void UpdateLightState(int iIndex)
        //{
        //    KeyValuePair<int, bool> entry = new KeyValuePair<int,bool>(iIndex, false);
        //    // If an already existing request is being made, add the request to the queue.
        //    if (!m_queLightUpdateOrder.Contains(entry))
        //    {
        //        m_queLightUpdateOrder.Enqueue(entry);
        //    }
        //}

        /// <summary>
        /// Either push the light state over if not waiting or add to the waiting queue.
        /// </summary>
        /// <param name="uiIndex"></param>
        //private void UpdateGroupState(int iIndex)
        //{
        //    KeyValuePair<int, bool> entry = new KeyValuePair<int, bool>(iIndex, true);
        //    // If an already existing request is being made, add the request to the queue.
        //    if (!m_queLightUpdateOrder.Contains(entry))
        //    {
        //        m_queLightUpdateOrder.Enqueue(entry);
        //    }
        //}

        /// <summary>
        /// Set a request to the light bridge to update state or give a command.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="method"></param>
        /// <param name="json"></param>
        private string MakeLightRequest(string page, string method, string json = null)
        {

            logger.Log("HueRequest: {0} {1} {2}", page, method, @json);

            var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + m_strLightsIP + page);
            httpWebRequest.ContentType = "text/json";
            httpWebRequest.Method = method;
            m_bMakingtRequest = true;

            //try
            //{
            if (json != null)
            {

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(json);
                }

            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var responseText = streamReader.ReadToEnd();
                //Now you have your response.
                //or false depending on information in the response
                logger.Log("HueResponse: {0}", responseText);

                m_strLastLog = responseText;
                m_bMakingtRequest = false;
                return responseText;
            }
            //}
            //catch (Exception e)
            //{
            //    logger.Log("Got exception in light request: " + e.ToString());
            //    return e.Message;
            //}            
        }

        internal List<LightState> GetAllLights()
        {
            //make a shallow copy and return
            return new List<LightState>(m_listLightStates);
        }


        internal void SetLightBrightness(LightState lstate, byte bValue)
        {

            //change the value first
            lstate.Brightness = bValue;

            MakeLightRequest("/api/" + m_strUser + "/lights/" + (lstate.Index) + "/state", "PUT", lstate.ToJSON());
        }

        internal void SetLightColor(LightState lstate, Color color)
        {
            //change the value first
            lstate.Color = color;

            MakeLightRequest("/api/" + m_strUser + "/lights/" + (lstate.Index) + "/state", "PUT", lstate.ToJSON());
        }
    }
}
