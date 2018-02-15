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
    /// Represents a light or group of lights.
    /// </summary>
    public class LightState
    {
        /// <summary>
        /// The index of the light
        /// </summary>
        private int m_index = -1;

        /// <summary>
        /// The name of the light
        /// </summary>
        private string m_name = "unknown";

        /// <summary>
        ///  Color of the light.
        /// </summary>
        private Color m_color = new Color();

        /// <summary>
        /// Whether the light is on.
        /// </summary>
        private bool m_bEnabled = true;

        /// <summary>
        /// level at which the lights can respond to controls.
        /// </summary>
        private int m_iPriorityLock = 0;

        public int Index
        {
            get { return m_index; }
            set { m_index = value; }
        }

        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        /// <summary>
        /// The color of the light.
        /// </summary>
        public Color Color
        {
            get { return m_color; }
            set { m_color = value; }
        }

        /// <summary>
        /// Whether the light is on.
        /// </summary>
        public bool Enabled
        {
            get { return m_bEnabled; }
            set { m_bEnabled = value; }
        }

        /// <summary>
        /// The level at which control calls will be accepted or rejected.
        /// </summary>
        public int PriorityLock
        {
            get { return m_iPriorityLock; }
            set { m_iPriorityLock = value; }
        }

        /// <summary>
        /// Convert to the light state to a JSON struct string.
        /// </summary>
        /// <returns></returns>
        public string ToJSON()
        {
            //return "{" +
            //    "\"on\":" + m_bEnabled.ToString().ToLower() + "," +
            //    "\"sat\":" + (int)(m_color.GetSaturation() * 255) + "," +
            //    "\"bri\":" + (int)(m_color.GetBrightness() * 255) + "," +
            //    "\"hue\":" + (int)(m_color.GetHue() / 360.0f * 65535.0f) +
            //    "}";
            return "{" +
                "\"on\":" + "true" + "," +
                "\"sat\":" + (int)(m_color.GetSaturation() * 255) + "," +
                "\"bri\":" + (int)(m_color.GetBrightness() * 255) + "," +
                "\"hue\":" + (int)(m_color.GetHue() / 360.0f * 65535.0f) +
                "}";
        }

        /// <summary>
        /// Assignment operator.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public LightState Copy(LightState state)
        {
            m_color = state.Color;
            m_bEnabled = state.Enabled;
            m_iPriorityLock = state.PriorityLock;
            return this;
        }

        /// <summary>
        /// Reset the light to defaults.
        /// </summary>
        public void Reset()
        {
            Enabled = false;
            Color = Color.Gray;
            PriorityLock = 0;
        }


        internal void ParseState(JToken state)
        {
            Enabled = (bool)state["on"];

            float hue = ((float)state["hue"]) / 65535.0f;
            float sat = ((float)state["sat"]) / 255.0f;
            float bri = ((float)state["bri"]) / 255.0f;

            //var xy = state["xy"];

            //float x = (float)xy[0];
            //float y = (float)xy[1];

            //double r = 0, g = 0, b = 0;

            //if (y != 0)
            //{
            //    //from: https://github.com/PhilipsHue/PhilipsHueSDKiOS/blob/master/ApplicationDesignNotes/RGB%20to%20xy%20Color%20conversion.md

            //    float z = 1.0f - x - y;

            //    float Y = bri;
            //    float X = (Y / y) * x;
            //    float Z = (Y / y) * z;

            //    r = X * 1.612f - Y * 0.203f - Z * 0.302f;
            //    g = -X * 0.509f + Y * 1.412f + Z * 0.066f;
            //    b = X * 0.026f - Y * 0.072f + Z * 0.962f;

            //    r = r <= 0.0031308f ? 12.92f * r : (1.0f + 0.055f) * Math.Pow(r, (1.0f / 2.4f)) - 0.055f;
            //    g = g <= 0.0031308f ? 12.92f * g : (1.0f + 0.055f) * Math.Pow(g, (1.0f / 2.4f)) - 0.055f;
            //    b = b <= 0.0031308f ? 12.92f * b : (1.0f + 0.055f) * Math.Pow(b, (1.0f / 2.4f)) - 0.055f;
            //}

            //Color = Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));

            Color = FromHSB(hue, sat, bri);
        }

        internal byte Brightness
        {
            get { return (byte)(m_color.GetBrightness() * 255); }

            set
            {
                float hue = Color.GetHue() / 360.0f; 
                float sat = Color.GetSaturation();
                var bri = value;

                Color = FromHSB(hue, sat, bri);
            }
        }

        /// <summary>
        /// Get a color from hue, sat, bri
        /// </summary>
        /// <param name="hue">value between 0 and 1</param>
        /// <param name="sat">value between 0 and 1</param>
        /// <param name="bri">value between 0 and 1</param>
        /// <returns></returns>
        //based on http://www.codeproject.com/Articles/11340/Use-both-RGB-and-HSB-color-schemas-in-your-NET-app
        static Color FromHSB(float hue, float sat, float bri)
        {
            hue = Math.Min(Math.Max(hue, 0), 255);
            sat = Math.Min(Math.Max(sat, 0), 255);
            bri = Math.Min(Math.Max(bri, 0), 255);

            float r = bri;
            float g = bri;
            float b = bri;

            if (sat != 0)
            {
                float max = bri;
                float dif = bri * sat / 255f;
                float min = bri - dif;

                float h = hue * 360f / 255f;

                if (h < 60f)
                {
                    r = max;
                    g = h * dif / 60f + min;
                    b = min;
                }
                else if (h < 120f)
                {
                    r = -(h - 120f) * dif / 60f + min;
                    g = max;
                    b = min;
                }
                else if (h < 180f)
                {
                    r = min;
                    g = max;
                    b = (h - 120f) * dif / 60f + min;
                }
                else if (h < 240f)
                {
                    r = min;
                    g = -(h - 240f) * dif / 60f + min;
                    b = max;
                }
                else if (h < 300f)
                {
                    r = (h - 240f) * dif / 60f + min;
                    g = min;
                    b = max;
                }
                else if (h <= 360f)
                {
                    r = max;
                    g = min;
                    b = -(h - 360f) * dif / 60 + min;
                }
                else
                {
                    r = 0;
                    g = 0;
                    b = 0;
                }
            }

            return Color.FromArgb
                (
                    (int)Math.Round(Math.Min(Math.Max(r, 0), 255)),
                    (int)Math.Round(Math.Min(Math.Max(g, 0), 255)),
                    (int)Math.Round(Math.Min(Math.Max(b, 0), 255))
                    );
        }
    }
}