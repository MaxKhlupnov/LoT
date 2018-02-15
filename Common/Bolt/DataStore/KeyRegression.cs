using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;

namespace HomeOS.Hub.Common.Bolt.DataStore
{
    
    [DataContract]
    public class KeyRegression
    {
        private string FQFilename;

        [DataMember]
        byte[][] keys;

        [DataMember]
        uint MW;

        [DataMember]
        uint current;

        public KeyRegression(uint max_winds, string target_dir)
        {
            FQFilename = target_dir + "/" + ".kr";
            MW = max_winds;
            current = 0;
            keys = new Byte[MW][];
            setup();
        }

        public KeyRegression(string target_dir)
        {
            FQFilename = target_dir + "/" + ".kr";
            Load();
        }

        protected void setup()
        {
            keys[MW-1] = new Byte[20];
            RandomNumberGenerator rng = new RNGCryptoServiceProvider();
            rng.GetBytes(keys[MW-1]);

            for (uint i = MW - 1; i >= 1; --i)
            {
                keys[i-1] = unwind(keys[i]);
            }

            current = 0;
        }

        protected byte[] unwind(byte[] stm)
        {
            HashAlgorithm hasher = new SHA1Managed();
            return hasher.ComputeHash(stm);
        }

        protected byte[] wind()
        {
            current = current + 1;
            return keys[current - 1];
        }

        public byte[] GetKey()
        {
            byte[] ret = wind();
            Flush();
            return ret;
        }
        
        public uint GetKeyVersion()
        {
            return current;
        }

        public static Byte[] KeyDer(Byte[] key)
        {
            if (key.Length == 16)
                return key;
            else
                return key.Take(16).ToArray();
        }

        public static byte[] GetSpecific(byte[] key, uint num_hashes)
        {
            HashAlgorithm hasher = new SHA1Managed();
            for (int i = 0; i < num_hashes; ++i)
            {
                key = hasher.ComputeHash(key);
            }
            return key;
        }

        public bool Load()
        {
            try
            {
                TextReader tr = new StreamReader(FQFilename);
                string json = tr.ReadToEnd();
                tr.Close();

                MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(KeyRegression));
                KeyRegression kr = (KeyRegression)ser.ReadObject(ms);
                ms.Close();

                MW = kr.MW;
                current = kr.current;
                keys = kr.keys;
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to load keyregression file: " + FQFilename);
                Console.WriteLine("{0} Exception caught.", e);
                return false;
            }
        }

        public void Flush()
        {
            TextWriter tw = new StreamWriter(FQFilename, false);

            MemoryStream ms = new MemoryStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(KeyRegression));
            ser.WriteObject(ms, this);
            byte[] json = ms.ToArray();
            ms.Close();

            tw.Write(Encoding.UTF8.GetString(json, 0, json.Length));
            tw.Flush();
            tw.Close();
        }
    }
}
