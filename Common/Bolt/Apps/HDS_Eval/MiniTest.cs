using System;

using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.IO;
using System.Text;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Files;

using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using HomeOS.Hub.Common.Bolt.DataStore;

using System.Diagnostics;
using Amazon.S3.Model;
using Amazon.S3;

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace HomeOS.Hub.Common.Bolt.Apps.Eval
{  
    public class MiniTest
    {
        [DllImport("kernel32", SetLastError = true)]
        static extern unsafe SafeFileHandle CreateFile(
            string FileName,           // file name
            uint DesiredAccess,        // access mode
            uint ShareMode,            // share mode
            IntPtr SecurityAttributes, // Security Attr
            uint CreationDisposition,  // how to create
            uint FlagsAndAttributes,   // file attributes
            IntPtr hTemplate // template file  
            );

        public MiniTest()
        {

        }

        ~MiniTest()
        {
            GC.Collect();
        }
        
        public void Destroy()
        {
            GC.Collect();
        }


        static void Main_Mini(string[] args)
        {
                        
            // Populate the keys and the values
            Random random = new Random(DateTime.Now.Millisecond);
            Byte[] val = new Byte[10000];
            random.NextBytes(val);

            for (int loop = 0; loop < 1; ++loop)
            {

                System.IO.File.Delete("E:\\DiskRaw");
                /*
                // http://stackoverflow.com/questions/5916673/how-to-do-non-cached-file-writes-in-c-sharp-winform-app
                // http://support.microsoft.com/kb/99794
                // File Caching - http://msdn.microsoft.com/en-us/library/windows/desktop/aa364218%28v=vs.85%29.aspx
                const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
                SafeFileHandle handle = CreateFile("E:\\DiskRaw",
                                            (uint)FileAccess.Write,
                                            (uint)FileShare.None,
                                            IntPtr.Zero,
                                            (uint)FileMode.Open,
                                             FILE_FLAG_NO_BUFFERING,
                                            IntPtr.Zero);

                // unfortunately this throws a runtime exception - i think the blocksize is messed up
                var fout = new FileStream(handle, FileAccess.ReadWrite, 1024 * 512, false);
                */
                FileStream fout = new FileStream("E:\\DiskRaw", FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
                fout.Seek(0, SeekOrigin.End);
                BinaryWriter fs_bw = new BinaryWriter(fout);

                Stopwatch watch = new Stopwatch();
                watch.Start();
                for (int i = 0; i < 100000; ++i)
                {
                    //fs_bw.BaseStream.Seek(0, SeekOrigin.End);
                    //fs_bw.Write(StreamFactory.NowUtc());
                    fs_bw.Write(val);
                }
                fout.Flush(true); // MAGIC! - http://msdn.microsoft.com/en-us/library/ee474552.aspx
                fs_bw.Close();
                watch.Stop();
                Console.WriteLine("Time to write data (ms) = " + watch.ElapsedMilliseconds);
            }
            Console.ReadKey();
        }
    }
}