using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HomeOS.Hub.Common.Bolt.DataStore
{
    /// <summary>
    /// Read and write data to a Windows Azure blob storage account.
    /// </summary>
    public interface IStream
    {
        /* Put */
        /// <summary>
        /// Appends a new value to the specified key.
        /// </summary>
        void Append(IKey key, IValue value, long timestamp = -1);


        /// <summary>
        /// Appends a list of key value pairs to the stream. Each key value pair is stored with the current timestamp.
        /// </summary>
        void Append(List<Tuple<IKey,IValue>> listOfKeyValuePairs);


        /// <summary>
        /// Appends a value to all the keys provided as a list
        /// </summary>
        void Append(List<IKey> listOfKeys, IValue value);

        /// <summary>
        /// Modifies the newest value for the specified key.
        /// </summary>
        void Update(IKey key, IValue value);

        /* Get, including Range queries */
        /// <summary>
        /// Gets the newest value from the specified key.
        /// </summary>
        IValue Get(IKey key);

        /// <summary>
        /// Gets the newest [key, value, timestamp] tuple inserted.
        /// </summary>
        Tuple<IKey, IValue> GetLatest();

        /// <summary>
        /// Gets the newest [key, value, timestamp] tuple inserted for the given key.
        /// </summary>
        Tuple<IValue, long> GetLatest(IKey key);
        
        /// <summary>
        /// Get all the [key, value, ts] tuples corresponding to the specified key.
        /// </summary>
        IEnumerable<IDataItem> GetAll(IKey key);

        /// <summary>
        /// Get all the [key, value, timestamp] tuples in the given time range corresponding to the specified key.
        /// </summary>
        IEnumerable<IDataItem> GetAll(IKey key, long startTimeStamp, long endTimeStamp);



        /// <summary>
        /// Get values for given key at startTimeStamp, startTimeStamp+skip, startTimeStamp+2*skip ..... endTimeStamps
        /// </summary>
        IEnumerable<IDataItem> GetAllWithSkip(IKey key, long startTimeStamp, long endTimeStamp, long skip);



        /// <summary>
        /// Get a list of all keys in the specified key range.
        /// </summary>
        HashSet<IKey> GetKeys(IKey startKey, IKey endKey);

        /*
        /// <summary>
        /// Deletes the current stream.
        /// </summary>
        void DeleteStream();
        */

        /* ACL calls */
        /// <summary>
        /// Grants read access to the app at the specified AppId.
        /// </summary>
        bool GrantReadAccess(string AppId);

        /// <summary>
        /// Grants read access to the app at the specified HomeId and AppId.
        /// </summary>
        bool GrantReadAccess(string HomeId, string AppId);

        /// <summary>
        /// Revokes read access from the app at the specified AppId.
        /// </summary>
        bool RevokeReadAccess(string AppId);

        /// <summary>
        /// Revokes read access from the app at the specified HomeId and AppId.
        /// </summary>
        bool RevokeReadAccess(string HomeId, string AppId);

        /*
        /// <summary>
        /// Flushes the current stream from memory.
        /// </summary>
        // void Flush();
        */

        /// <summary>
        /// Closes the current stream.
        /// </summary>
        /// <returns>A boolean indicating success or failure.</returns>
        bool Close();

        void DumpLogs(string file);
        void Seal(bool checkMemPressure);
    }
}
