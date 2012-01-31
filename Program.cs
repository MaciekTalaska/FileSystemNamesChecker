using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace FSNC
{
    internal enum EntryType
    {
        File,
        Directory
    }


    class SeekInvalidPathChars
    {
        private static string about = "fsnc (File System Names Checker) - by Maciek Talaska (2009)" + System.Environment.NewLine +
                                      "simple utility that scans specified directory (or whole drive)" + System.Environment.NewLine +
                                      "and search for files or directories with illegal chars in their names" + System.Environment.NewLine +
                                      System.Environment.NewLine +
                                      "usage: " + System.Environment.NewLine +
                                     @"fsnc e:\  - scans whole E:\ drive" + System.Environment.NewLine +
                                      "fsnc e:\\projects\\ - scans \"projects\" directory (with subfolders) on drive E:\\ " + System.Environment.NewLine;


        private string startpath;
        private char[] invalidPathChars;
        private char[] invalidFileChars;
        List<string> reportLines;
        ulong invalidCounter;
        char[] newlinedelimiter;

        private string filelistcommand = @"DIR {0}\ /A:-D /B";
        private string dirlistcommand  = @"DIR {0}\ /A:D /B";

        public SeekInvalidPathChars(string initialPath)
        {
            startpath = initialPath;

            invalidPathChars = Path.GetInvalidPathChars();
            invalidFileChars = Path.GetInvalidFileNameChars();
            reportLines = new List<string>();
            invalidCounter = 0;
            newlinedelimiter = System.Environment.NewLine.ToCharArray();
        }

        private void GetSystemEntries( DirectoryInfo di, EntryType requestedEntryType )
        {
            string command = requestedEntryType == EntryType.Directory ? dirlistcommand : filelistcommand;

            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
            psi.FileName = "cmd.exe";
            psi.Arguments = "/C " + string.Format(command, di.FullName );
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = false;
            System.Diagnostics.Process p = System.Diagnostics.Process.Start(psi);
            
            string temp = p.StandardOutput.ReadToEnd();

            string[] entries = temp.Split(newlinedelimiter, StringSplitOptions.RemoveEmptyEntries);
            if (requestedEntryType == EntryType.Directory)
                CheckNames(di, entries, invalidPathChars);
            else
                CheckNames(di, entries, invalidFileChars);
        }

        private void CheckNames( DirectoryInfo di, string[] entries, char[] invalidChars )
        {
            StringBuilder sb = new StringBuilder();
            
            foreach (string s in entries)
            {
                if (s.IndexOfAny(invalidChars) == -1)
                    continue;
                sb.AppendLine(string.Format("invalid entry: \"{0}\" in \"{1}\"", s, di.FullName));
                ++invalidCounter;
            }
            if (sb.Length > 0)
                reportLines.Add(sb.ToString());
        }


        private void RecurseDirectories( DirectoryInfo root )
        {
            // files
            try
            {
                FileInfo[] files = root.GetFiles();
            }
            catch (System.UnauthorizedAccessException)
            { 
            }
            catch
            {
                GetSystemEntries(root, EntryType.File);
            }
            
            // directories
            try
            {
                DirectoryInfo[] directories = root.GetDirectories();
                foreach (DirectoryInfo di in directories)
                    RecurseDirectories(di);
            }
            catch
            {
                GetSystemEntries(root, EntryType.Directory);
            }
        }

        public void Seek()
        {
            DirectoryInfo di = new DirectoryInfo(startpath);
            if ( (!di.Exists) )
            {
                Console.WriteLine("invalid startup path");
                Environment.Exit(-1);
            }

            RecurseDirectories( di );
        }

        public void PrintToConsole()
        {
            if ((reportLines.Count == 0) || (invalidCounter == 0))
            {
                Console.WriteLine("no invalid names found in {0}", startpath);
                return;
            }

            Console.WriteLine("{0} invalid file(s) / directory(ies) found",invalidCounter);
            foreach (string s in reportLines)
            {
                Console.WriteLine(s);
            }
        }


        static void Main(string[] args)
        {

            if ((args.Length == 0) || (string.IsNullOrEmpty(args[0])))
            {
                Console.WriteLine(about);
                Console.WriteLine("No startup path parameter specified... exiting");
                Environment.Exit(-1);
            }

            SeekInvalidPathChars seeker = new SeekInvalidPathChars(args[0]);
            seeker.Seek();
            seeker.PrintToConsole();
        }
    }
}
