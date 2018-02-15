using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace HomeOS.Hub.Common.SafeTokenHandler
{
    public class SafeTokenHandler
    {
   
        private byte[] cryptKey;
        private byte[] signKey; 

        public SafeTokenHandler(string secretKey)
        {

            this.cryptKey = derive(secretKey, "SIGNATURE");
            this.signKey = derive(secretKey, "ENCRYPTION");
        }


        public SafeTokenUser ProcessToken(string token)
        {
            
            if (string.IsNullOrEmpty(token))
                return null;

            try
            {
                string stoken = DecryptAndValidateToken(token);
                if (string.IsNullOrEmpty(stoken))
                    return null;

                NameValueCollection parsedToken = parse(stoken);
                if (parsedToken == null)
                    return null;

                SafeTokenUser user = new SafeTokenUser(parsedToken["ts"], parsedToken["user"], "", stoken);
                return user;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in Processing Token in SafeTokenHandler " + e);
                return null;
            }
        }

        public string GenerateToken(string user)
        {
            
                if (string.IsNullOrEmpty(user))
                    return null;
                string token = "user=" + user + "&ts=" + getTimestamp();
                try
                {
                    string sign = EncodeandConvertToBase64(SignToken(token, signKey));
                    token = token + "&sign=" + sign;
                    return Encrypt(token, cryptKey);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception in generating Token in SafeTokenHandler "+e);
                    return null;
                }
        }


        private string DecryptAndValidateToken(string token)
        {
            string stoken = Decrypt(token, cryptKey);

            if (!string.IsNullOrEmpty(stoken))
            {
                stoken = ValidateToken(stoken, signKey);
            }
            return stoken;
        }

        private string Decrypt(string cipherText, byte[] Password)
        {
            //Convert base 64 text to bytes
            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            //We will derieve our Key and Vectore based on following 
            //password and a random salt value, 13 bytes in size.
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
                new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 
            0x64, 0x76, 0x65, 0x64, 0x65, 0x76});
            byte[] decryptedData = Decrypt(cipherBytes,
                pdb.GetBytes(32), pdb.GetBytes(16));

            //Converting unicode string from decrypted data
            return Encoding.Unicode.GetString(decryptedData);
        }

        private byte[] Decrypt(byte[] cipherData, byte[] Key, byte[] IV)
        {
            byte[] decryptedData;
            //Create stream for decryption
            using (MemoryStream ms = new MemoryStream())
            {
                //Create Rijndael object with key and vector
                using (Rijndael alg = Rijndael.Create())
                {
                    alg.Key = Key;
                    alg.IV = IV;
                    //Forming cryptostream to link with data stream.
                    using (CryptoStream cs = new CryptoStream(ms,
                        alg.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        //Write all data to stream.
                        cs.Write(cipherData, 0, cipherData.Length);
                    }
                    decryptedData = ms.ToArray();
                }
            }
            return decryptedData;
        }

        private string Encrypt(string clearText, byte[] Password)
        {
            //Convert text to bytes
            byte[] clearBytes =
              System.Text.Encoding.Unicode.GetBytes(clearText);

            //We will derieve our Key and Vectore based on following 
            //password and a random salt value, 13 bytes in size.
            PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password,
                new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 
            0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76});

            byte[] encryptedData = Encrypt(clearBytes,
                     pdb.GetBytes(32), pdb.GetBytes(16));

            return Convert.ToBase64String(encryptedData);
        }

        private byte[] Encrypt(byte[] clearData, byte[] Key, byte[] IV)
        {
            byte[] encryptedData;
            //Create stream for encryption
            using (MemoryStream ms = new MemoryStream())
            {
                //Create Rijndael object with key and vector
                using (Rijndael alg = Rijndael.Create())
                {
                    alg.Key = Key;
                    alg.IV = IV;
                    //Forming cryptostream to link with data stream.
                    using (CryptoStream cs = new CryptoStream(ms,
                       alg.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        //Write all data to stream.
                        cs.Write(clearData, 0, clearData.Length);
                    }
                    encryptedData = ms.ToArray();
                }
            }

            return encryptedData;
        }

       private string getTimestamp()
        {
            DateTime refTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan ts = DateTime.UtcNow - refTime;
            return ((uint)ts.TotalSeconds).ToString();
        }

       private string ValidateToken(string token, byte[] signKey)
       {
           if (string.IsNullOrEmpty(token))
               return null;
           

           string[] s = { "&sign=" };
           string[] bodyAndSig = token.Split(s, StringSplitOptions.None);

           if (bodyAndSig.Length != 2)
               return null;


           byte[] sig = DecodeandConvertToBase8(bodyAndSig[1]);

           if (sig == null)
               return null;

           byte[] sig2 = SignToken(bodyAndSig[0], signKey);

           if (sig2 == null)
               return null;

           if (sig.Length == sig2.Length)
           {
               for (int i = 0; i < sig.Length; i++)
               {
                   if (sig[i] != sig2[i]) { return null; }
               }

               return token;
           }
           return null;
       }

       private byte[] SignToken(string token, byte[] signKey)
       {
           if (signKey == null || signKey.Length == 0)
           {
               throw new InvalidOperationException("Error: SignToken: Secret key was not set. Aborting.");
           }

           if (string.IsNullOrEmpty(token))
           {
               return null;
           }

           using (HashAlgorithm hashAlg = new HMACSHA256(signKey))
           {
               byte[] data = Encoding.Default.GetBytes(token);
               byte[] hash = hashAlg.ComputeHash(data);
               return hash;
           }
       }

       private byte[] derive(string secret, string prefix)
       {
           using (HashAlgorithm hashAlg = HashAlgorithm.Create("SHA256"))
           {
               const int keyLength = 16;
               byte[] data = Encoding.Default.GetBytes(prefix + secret); 
               byte[] hashOutput = hashAlg.ComputeHash(data);
               byte[] byteKey = new byte[keyLength];
               Array.Copy(hashOutput, byteKey, keyLength);
               return byteKey;
           }
       }

       private byte[] DecodeandConvertToBase8(string s)
       {
           byte[] b = null;
           if (s == null) { return b; }
           s = HttpUtility.UrlDecode(s);

           try
           {
               b = Convert.FromBase64String(s);
           }
           catch (Exception e)
           {
               Console.Write(e);
               return null;
           }
           return b;
       }

       private string EncodeandConvertToBase64(byte[] s)
       {
           string b = null, encoded = null;
           if (s == null) { return b; }
           try
           {
               b = Convert.ToBase64String(s);
               encoded = HttpUtility.UrlEncode(b);
           }
           catch (Exception)
           {
               return null;
           }
           return encoded;
       }

       private NameValueCollection parse(string input)
       {
           if (string.IsNullOrEmpty(input))
           {
               return null;
           }

           NameValueCollection pairs = new NameValueCollection();

           string[] kvs = input.Split(new Char[] { '&' });
           foreach (string kv in kvs)
           {
               int separator = kv.IndexOf('=');

               if ((separator == -1) || (separator == kv.Length))
               {
                   continue;
               }

               pairs[kv.Substring(0, separator)] = kv.Substring(separator + 1);
           }

           return pairs;
       }

    }

    
    public class SafeTokenUser
    {
        public SafeTokenUser(string timestamp, string name, string appcontext, string token)
        {
            setTimestamp(timestamp);
            setName(name);
            setAppContext(appcontext);
            setToken(token);
        }

        DateTime timestamp;

        /// <summary>
        ///  Returns the timestamp as obtained from the SSO token.
        /// </summary>
        public DateTime Timestamp { get { return timestamp; } }

        /// <summary>
        /// Sets the Unix timestamp.
        /// </summary>
        /// <param name="timestamp"></param>
        private void setTimestamp(string timestamp)
        {
            if (string.IsNullOrEmpty(timestamp))
            {
                throw new ArgumentException("Error: User: Null timestamp in token.");
            }

            int timestampInt;

            try
            {
                timestampInt = Convert.ToInt32(timestamp);
            }
            catch (Exception)
            {
                throw new ArgumentException("Error: User: Invalid timestamp: "
                                            + timestamp);
            }

            DateTime refTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            this.timestamp = refTime.AddSeconds(timestampInt);
        }

        string name;

        /// <summary>
        /// Returns the pairwise unique ID for the user.
        /// </summary>
        public string Name { get { return name; } }

        /// <summary>
        /// Sets the pairwise unique ID for the user.
        /// </summary>
        /// <param name="id">User id</param>
        private void setName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Error: User: Null name in token.");
            }

            Regex re = new Regex(@"^\w+$");
            if (!re.IsMatch(name))
            {
                throw new ArgumentException("Error: User: Invalid name: " + name);
            }

            this.name = name;
        }

     

        string appcontext;

        /// <summary>
        /// Returns the application context that was originally passed
        /// to the sign-in request, if any.
        /// </summary>
        public string AppContext { get { return appcontext; } }

        /// <summary>
        /// Sets the the Application context.
        /// </summary>
        /// <param name="context"></param>
        private void setAppContext(string appcontext)
        {
            this.appcontext = appcontext;
        }

        string token;

        /// <summary>
        /// Returns the encrypted Web Authentication token containing 
        /// the UID. This can be cached in a cookie and the UID can be
        /// retrieved by calling the ProcessToken method.
        /// </summary>
        public string Token { get { return token; } }

        /// <summary>
        /// Sets the the User token.
        /// </summary>
        /// <param name="token"></param>
        private void setToken(string token)
        {
            this.token = token;
        }
    }
}
