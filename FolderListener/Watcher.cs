using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SqlClient;
using System.Configuration;

namespace FolderListener
{
    public class Watcher
    {
        static string pdfType = "";
        static string debugFlag = ConfigurationManager.AppSettings["Debug"].ToString();

        public void Run(string[] args)
        {

            // Create a new FileSystemWatcher and set its properties.
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = args[0];
            /* Watch for changes in LastAccess and LastWrite times, and
               the renaming of files or directories. */
            watcher.NotifyFilter =  NotifyFilters.FileName ;
            // Only watch text files.
            watcher.Filter = "*.pdf";

            // Add event handlers.
            //watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            //watcher.Deleted += new FileSystemEventHandler(OnChanged);
            //watcher.Renamed += new RenamedEventHandler(OnRenamed);

            // Begin watching.
            watcher.EnableRaisingEvents = true;

            // Wait for the user to quit the program.
            Console.WriteLine("Press \'q\' to quit the sample.");
            while (Console.Read() != 'q') ;
        }

        // Define the event handlers. 
        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
            Console.WriteLine("Create XML File for OCR for file name " + e.Name);
            CreateXMLMapDoc(e.Name, e.FullPath);
        }

        private static void OnRenamed(object source, RenamedEventArgs e)
        {
            // Specify what is done when a file is renamed.
            Console.WriteLine("File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
        }

 
        private static void CreateXMLMapDoc(string fileName, string pathName)
        {

            // file name breaks down as follows:
            // sample file name: 13I_015761ACCT_V1
            // meanings and positions:
            // first 2 characters are the year
            // next 1 character is one of the following, which indicate which return types to use:
            //      "I" - Individual - 1040
            //      "C" - Corporate - 1120
            //      "S" - Corporate - 1120S
            //      "P" - Partnership - ????
            // the next 6-9 characters are the client ID
            //      --> these 6-9 characters will be followed by "ACCT", "GOVT" or "CLNT"
            //      --> the value that we must look for in the file name is a parameter
            //      --> passed in and placed into the pdfType variable.
            //      the "_V1" at the end will be ignored. AWD has indicated that this value is ALWAYS V1

            string clientId = "";
            string year = "";
            string returnType = "";
            string documentName = "Form ";
            string outputFileName = PathNameNoFileName(pathName, fileName) + FileNameNoExt(fileName) + ".xml";
            string pdfTypeForDocName = "";

            if (fileName.IndexOf("ACCT") > -1) { pdfType = "ACCT"; }
            if (fileName.IndexOf("GOVT") > -1) { pdfType = "GOVT"; }
            if (fileName.IndexOf("CLNT") > -1) { pdfType = "CLNT"; }

            switch (pdfType)
            {
                case "ACCT":
                    pdfTypeForDocName = "Accounting Copy";
                    break;
                case "GOVT":
                    pdfTypeForDocName = "Government Copy";
                    break;
                case "CLNT":
                    pdfTypeForDocName = "Client Copy";
                    break;
                default:
                    pdfTypeForDocName = "Other Copy";
                    break;
            }



            year = "20" + fileName.Substring(0, 2);
            
            int clientIdLength = fileName.IndexOf(pdfType);

            clientId = fileName.Substring(4, clientIdLength - 4);
            if (clientId.IndexOf(".") == -1)
            {
                clientId = clientId + ".0";
            }
            switch (fileName.Substring(2, 1))
            {
                case "I":
                    returnType = "1040";
                    break;
                case "C":
                    returnType = "1120";
                   break;
                case "S":
                    returnType = "1102S";
                    break;
                case "P":
                    returnType = "1120S";
                   break;
                default:
                    returnType = "1040";
                    break;
            }

            documentName += returnType + " - " + pdfTypeForDocName;
            string clientName = ClientName(clientId);

            StringBuilder sb = new StringBuilder();
            sb.Append(@"<?xml version=""1.0"" encoding=""ISO-8859-1""?>");
            sb.Append(@"<document>");
            sb.Append(@"<client><![CDATA[");
            sb.Append(clientName);
            sb.Append(@"]]></client>");
            sb.Append(@"<year>");
            sb.Append(year);
            sb.Append(@"</year>");
            sb.Append(@"<returntype>");
            sb.Append(returnType);
            sb.Append(@"</returntype>");
            sb.Append(@"<name>");
            sb.Append(documentName);
            sb.Append(@"</name>");
            sb.Append(@"</document>");

            if (debugFlag == "1")
            {
                Console.WriteLine(sb);
            }

            File.WriteAllText(outputFileName, sb.ToString());
        }

        private static string PathNameNoFileName(string pathName, string fileName)
        {
            int fileNamePos = pathName.IndexOf(fileName);
            return pathName.Substring(0, fileNamePos);
        }

        private static string FileNameNoExt(string fileName)
        {

            int dotPos = fileName.LastIndexOf(".");
            return fileName.Substring(0, dotPos);

        }

        private static string ClientName(string clientId)
        {
            string retValue = " No Selection - 0.0";        // set the original value, which will be used if the client ID is not found in the database

            var connectionString = ConfigurationManager.ConnectionStrings["AWD"].ConnectionString;
            try
            {
                using (SqlConnection myConnection = new SqlConnection(connectionString))
                {
                    // build the sql query
                    string oString = "Select * from [vwClientAccounts] where ClientNo=@clientNo";
                    SqlCommand oCmd = new SqlCommand(oString, myConnection);
                    oCmd.Parameters.AddWithValue("@clientNo", clientId);
                    myConnection.Open();
                    if (debugFlag == "1")
                    {
                        Console.WriteLine(oCmd.CommandText);
                        Console.WriteLine(oCmd.Parameters[0].Value);
                    }
                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        if (debugFlag == "1")
                        {
                            Console.WriteLine(oReader.HasRows.ToString());
                        }
                        while (oReader.Read())
                        {
                            retValue = oReader["ClientName"].ToString();
                        }

                        myConnection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            return retValue;

        }
    }
}
