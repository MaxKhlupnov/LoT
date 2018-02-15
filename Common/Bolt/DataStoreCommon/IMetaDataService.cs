using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace HomeOS.Hub.Common.Bolt.DataStoreCommon
{
    [ServiceContract]
    public interface IMetaDataService
    {
        [OperationContract]
        bool RegisterPubKey(Principal prin, string key);
        [OperationContract]
        string GetPubKey(Principal prin);

        [OperationContract]
        bool UpdateReaderKey(Principal caller, FQStreamID FQName, ACLEntry entry);
        [OperationContract]
        ACLEntry GetReaderKey(FQStreamID FQName, Principal prin);

        [OperationContract]
        bool AddAccount(FQStreamID FQName, AccountInfo accinfo);

        [OperationContract]
        Dictionary<int, AccountInfo> GetAllAccounts(FQStreamID FQName);

        [OperationContract]
        AccountInfo GetMdAccount(FQStreamID FQName);

        [OperationContract]
        bool AddMdAccount(FQStreamID FQName, AccountInfo accinfo);

        [OperationContract]
        List<Principal> GetAllReaders(FQStreamID FQName);

        [OperationContract]
        void RemoveAllInfo(FQStreamID FQName);
    }

    [Serializable]
    [DataContract]
    public class Principal
    {
        private string _HomeId;
        private string _AppId;
        private Byte[] _Auth;

        [DataMember]
        public string HomeId
        {
            get { return _HomeId; }
            set { _HomeId = value; }
        }

        [DataMember]
        public Byte[] Auth
        {
            get { return _Auth; }
            set { _Auth = value; }
        }

        [DataMember]
        public string AppId
        {
            get { return _AppId; }
            set { _AppId = value; }
        }

        public Principal(string hid, string aid)
        {
            _HomeId = hid;
            _AppId = aid;
        }

        public override string ToString()
        {
            return HomeId + "/" + AppId;
        }

    }

    [Serializable]
    [DataContract]
    public class FQStreamID
    {
        private string _HomeId;
        private string _AppId;
        private string _StreamId;

        [DataMember]
        public string HomeId
        {
            get { return _HomeId; }
            set { _HomeId = value; }
        }

        [DataMember]
        public string AppId
        {
            get { return _AppId; }
            set { _AppId = value; }
        }

        [DataMember]
        public string StreamId
        {
            get { return _StreamId; }
            set { _StreamId = value; }
        }

        public FQStreamID(string hid, string aid, string sid)
        {
            _HomeId = hid;
            _AppId = aid;
            _StreamId = sid;
        }

        public override string ToString()
        {
            return HomeId + "/" + AppId + "/" + StreamId;
        }
    }

    [Serializable]
    [DataContract]
    public class AccountInfo
    {
        private int _num;
        private string _accountName;
        private string _accountKey;
        private string _location;
        private uint _keyVersion;

        [DataMember]
        public int num
        {
            get { return _num; }
            set { _num = value; }
        }

        [DataMember]
        public uint keyVersion
        {
            get { return _keyVersion; }
            set { _keyVersion = value; }
        }

        [DataMember]
        public string accountName
        {
            get { return _accountName; }
            set { _accountName = value; }
        }

        [DataMember]
        public string accountKey
        {
            get { return _accountKey; }
            set { _accountKey = value; }
        }

        [DataMember]
        public string location
        {
            get { return _location; }
            set { _location = value; }
        }

        public AccountInfo(string accName, string accKey, string loc, uint kversion)
        {
            _accountName = accName;
            _accountKey = accKey;
            _location = loc;
            _keyVersion = kversion;
        }
    }

    [Serializable]
    [DataContract]
    public class ACLEntry
    {
        private Principal _readerName;
        private byte[] _encKey;
        private byte[] _IV;
        private uint _keyVersion;

        [DataMember]
        public byte[] encKey
        {
            get { return _encKey; }
            set { _encKey = value; }
        }

        [DataMember]
        public byte[] IV
        {
            get { return _IV; }
            set { _IV = value; }
        }

        [DataMember]
        public uint keyVersion
        {
            get { return _keyVersion; }
            set { _keyVersion = value; }
        }

        [DataMember]
        public Principal readerName
        {
            get { return _readerName; }
            set { _readerName = value; }
        }

        public ACLEntry(Principal rName, byte[] key, byte[] iv, uint kversion)
        {
            _readerName = rName;
            _encKey = key;
            _IV = iv;
            _keyVersion = kversion;
        }

        public Principal GetPrincipal()
        {
            return _readerName;
        }
    }

    [Serializable]
    [DataContract]
    public class StreamInfo
    {

        [DataMember]
        private FQStreamID stream;


        [DataMember]
        private Principal owner;

        [DataMember]
        private Dictionary<int, AccountInfo> accounts;

        [DataMember]
        private AccountInfo md_account;

        [DataMember]
        private Dictionary<string, ACLEntry> readers;

        [DataMember]
        private uint latest_keyversion;

        public StreamInfo(FQStreamID sname)
        {
            stream = sname;
            owner = new Principal(sname.HomeId, sname.AppId);
            accounts = new Dictionary<int, AccountInfo>();
            readers = new Dictionary<string, ACLEntry>();
            latest_keyversion = 0;
        }

        public bool UpdateReader(ACLEntry entry)
        {
            if (entry.keyVersion < latest_keyversion)
                return false;
            readers[entry.GetPrincipal().ToString()] = entry;
            return true;
        }

        public ACLEntry GetReader(Principal prin)
        {
            if (readers.ContainsKey(prin.ToString()))
            {
                return readers[prin.ToString()];
            }
            return null;
        }

        public bool AddAccount(AccountInfo account)
        {
            accounts[account.num] = account;
            return true;
        }

        
        public bool AddMdAccount(AccountInfo account)
        {
            md_account = account;
            return true;
        }

        public Dictionary<int, AccountInfo> GetAllAccounts()
        {
            return accounts;
        }

        public AccountInfo GetMdAccount()
        {
            return md_account;
        }

        public List<Principal> GetAllReaders()
        {
            List<Principal> ret = new List<Principal>();
            foreach (KeyValuePair<string, ACLEntry> entry in readers)
            {
                ret.Add(entry.Value.readerName);
            }
            return ret;
        }

    }
}