//#define HDS_COMMENT
#if HDS_COMMENT
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace HDS
{
    [DataContract]
    public class OldMetaData
    {
        protected string FQFilename;
        
        [DataMember]
        protected string Owner;

        [DataMember]
        protected Dictionary<string, bool> RACL; // read ACL
        [DataMember]
        protected Dictionary<string, bool> WACL; // write ACL

        public bool load;

        [DataMember]
        protected List<string> Readers;
        [DataMember]
        protected List<string> Writers;

        protected FileStream Lock;

        public OldMetaData(string target_dir, string filename)
        {
            FQFilename = target_dir + "/" + filename;
            Owner = String.Empty;
            RACL = new Dictionary<string, bool>();
            WACL = new Dictionary<string, bool>();
            Readers = new List<string>();
            Writers = new List<string>();

            if (!File.Exists(FQFilename))
            {
                load = false;
            }
            else
            {
                load = true;
            }
            
        }

        public Dictionary<string, bool> GetRACL()
        {
            return RACL;
        }

        public Dictionary<string, bool> GetWACL()
        {
            return WACL;
        }

        public List<string> GetReaders()
        {
            return Readers;
        }

        public List<string> GetWriters()
        {
            return Writers;
        }

        public void ClearReaders()
        {
            Readers.Clear();
        }

        public void ClearWriters()
        {
            Writers.Clear();
        }

        public void PrintMetaData()
        {
            Console.Write("Metadata");
            Console.WriteLine("Owner: " + Owner);
            foreach (KeyValuePair<string, bool> item in RACL)
            {
                Console.WriteLine("Reader ACL");
                Console.WriteLine(item.Key + ":" + item.Value);
            }
            foreach (KeyValuePair<string, bool> item in WACL)
            {
                Console.WriteLine("Writer ACL");
                Console.WriteLine(item.Key + ":" + item.Value);
            }
            foreach (string item in Readers)
            {
                Console.WriteLine("Current Readers");
                Console.WriteLine(item);
            }
            foreach (string item in Writers)
            {
                Console.WriteLine("Current Writers");
                Console.WriteLine(item);
            }
            Console.WriteLine("Lock: " + Lock);
        }

        public void setOwner(string owner)
        {
            Owner = owner;
        }

        public string getOwner()
        {
            return Owner;
        }

        public bool LockMetaData()
        {
            try
            {
                Lock = new FileStream(FQFilename + "lock", FileMode.CreateNew,
                            FileAccess.ReadWrite, FileShare.None, 512, FileOptions.DeleteOnClose);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void UnlockMetaData()
        {
            Lock.Close();
        }

        public void SetReadAccess(string key)
        {
            RACL[key] = true;
        }

        public void SetWriteAccess(string key)
        {
            WACL[key] = true;
        }

        public void RemoveReadAccess(string key)
        {
            if (RACL.ContainsKey(key))
            {
                RACL.Remove(key);
            }
        }

        public void RemoveWriteAccess(string key)
        {
            if (WACL.ContainsKey(key))
            {
                WACL.Remove(key);
            }
        }

        public bool HasReadAccess(string key)
        {
            if ((RACL.ContainsKey(key) && RACL[key] == true))
                    return true;
            else
                return false;
        }

        public bool HasWriteAccess(string key)
        {
            if ((WACL.ContainsKey(key) && WACL[key] == true))
                return true;
            else
                return false;
        }

        public bool IsOwner(string key)
        {
            return (Owner == key);
        }

        public void SetActiveReader(string key)
        {
            Readers.Add(key);
        }

        public void RemoveActiveReader(string key)
        {
            Readers.Remove(key);
        }


        public bool IsActiveReader(string key)
        {
            return Readers.Contains(key);
        }

        public void SetActiveWriter(string key)
        {
            Writers.Add(key);
        }

        public void RemoveActiveWriter(string key)
        {
            Writers.Remove(key);
        }

        public bool IsActiveWriter(string key)
        {
            return Writers.Contains(key);
        }

        public bool HasActiveReaders()
        {
            return Readers.Count > 0;
        }

        public bool HasActiveWriters()
        {
            return Writers.Count > 0;
        }

        /*
        public void FlushManual()
        {
            // flush ACL out to FQFilename
            // write meta-data in JSON format
            mdtw.WriteLine("{\"md\": {");
            mdtw.WriteLine("  \"Owner\": \"" + Owner + "\",");
            mdtw.WriteLine("  \"Sharing\": {");
            foreach (KeyValuePair<string, bool> item in RACL)
            {
                bool read = item.Value;
                bool write = false;

                if (WACL.ContainsKey(item.Key))
                {
                    write = true;
                    WACL.Remove(item.Key);
                }

                string acl = read ? "R" : "";
                acl += write ? "W" : "";

                mdtw.WriteLine("    \"" + item.Key + "\": \"" + acl + "\",");
            }

            foreach (KeyValuePair<string, bool> item in WACL)
            {
                mdtw.WriteLine("    \"" + item.Key + "\": \"W\",");
            }


            mdtw.WriteLine("  }");
            mdtw.WriteLine("}");
            mdtw.Close();
        }
         */

        public void LoadMetaData()
        {
            try
            {
                TextReader mdtr = new StreamReader(FQFilename);
                string json = mdtr.ReadToEnd();
                mdtr.Close();

                MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(OldMetaData));
                OldMetaData md = (OldMetaData)ser.ReadObject(ms);
                ms.Close();

                Owner = md.Owner;
                RACL = md.RACL;
                WACL = md.WACL;
                Readers = md.Readers;
                Writers = md.Writers;
            }
            catch (Exception e)
            {
                Owner = string.Empty;
                RACL.Clear();
                WACL.Clear();
                Readers.Clear();
                Writers.Clear();
                Console.WriteLine("Failed to load metadata file: " + FQFilename);
                Console.WriteLine("{0} Exception caught.", e);
            }
        }

        public void FlushMetaData()
        {
            // TODO(trinabh): We don't want to erase and update each time, rather just update.
            TextWriter mdtw = new StreamWriter(FQFilename, false);
            
            MemoryStream ms = new MemoryStream();
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(OldMetaData));
            ser.WriteObject(ms, this);
            byte[] json = ms.ToArray();
            ms.Close();

            mdtw.Write(Encoding.UTF8.GetString(json, 0, json.Length));
            mdtw.Flush();
            mdtw.Close();
        }

        public void Close()
        {
        }

        public void Dispose() // NOT virtual
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Prevent finalizer from running.
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Call Dispose() on other objects owned by this instance.
                // You can reference other finalizable objects here.
                // ...
                Close();
            }

            // Release unmanaged resources owned by (just) this object.
            // ...
        }

        ~OldMetaData()
        {
            Dispose(false);
        }

    }
}
#endif