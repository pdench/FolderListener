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
        static string debugFlag = ConfigurationManager.AppSettings["Debug"].ToString();
        static string errorFolder = ConfigurationManager.AppSettings["ErrorFolder"].ToString();

        public void Run(string[] args)
        {

            // make sure that the error folder ends with "\"; will throw an error if not
            if (errorFolder.EndsWith("\\"))
            {
                errorFolder += "\\";
            }

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
            try
            {
                CreateXMLMapDoc(e.Name, e.FullPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                System.IO.File.Move(e.FullPath, errorFolder + e.Name);
            }
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
            // the next 6-9 characters are the client ID
            //      --> these 6-9 characters will be followed by "ACCT", "GOVT" or "CLNT"
            //      --> the value that we must look for in the file name is a parameter
            //      --> passed in and placed into the pdfType variable.
            //      the "_V1" at the end will be ignored. AWD has indicated that this value is a version number that is not needed in m-files
            //      any data that appears after the version number and before the ".pdf" in the file name will be added to the document name.
            //      this data is likely the shareholder name for a Schedule K1.

            string pdfType = "";
            string clientId = "";
            string year = "";
            string returnType = "";
            string documentName = "Form ";
            string outputFileName = PathNameNoFileName(pathName, fileName) + FileNameNoExt(fileName) + ".xml";
            string pdfTypeForDocName = "";
            int clientIdLength = 0;
            bool isExtension = false;

            fileName = fileName.ToUpper();
            // looking for data between the version number
            int pdfLocation = fileName.IndexOf(".PDF");
            int versionLocation = fileName.IndexOf("_V") + 3;       // add 3 to move beyond the version number in the string
            string shareholderName = fileName.Substring(versionLocation, pdfLocation - versionLocation);       // the "+4" adds the ".pdf" back to get the right position

            if (fileName.IndexOf("ACCT") > -1) { pdfType = "ACCT"; }
            if (fileName.IndexOf("GOVT") > -1) { pdfType = "GOVT"; }
            if (fileName.IndexOf("CLNT") > -1) { pdfType = "CLNT"; }
            if (fileName.IndexOf("K1") > -1) { pdfType = "K1"; }

            if (debugFlag == "1")
            {
                Console.WriteLine("PDF Type: " + pdfType);
            }

            if (debugFlag == "1")
            {
                Console.WriteLine("Version Pos: " + fileName.IndexOf("_V").ToString());
            }

            if (pdfType == "")        // these are the extensions
            {
                clientIdLength = fileName.IndexOf("_V");
                isExtension = true;
                if (fileName.Substring(2, 1) == "I")
                {
                    returnType = "4868";
                }
                else
                {
                    returnType = "7004";
                }
            }
            else
            {
                clientIdLength = fileName.IndexOf(pdfType);
            }

            year = "20" + fileName.Substring(0, 2);

            clientId = fileName.Substring(4, clientIdLength - 4);
            if (clientId.IndexOf(".") == -1)
            {
                clientId = clientId + ".0";
            }

            if (!isExtension)           // if this is an extension, we have already set the return type
            {
                switch (fileName.Substring(2, 1))
                {
                    case "I":
                        returnType = "1040";
                        break;
                    case "C":
                        returnType = "1120";
                        break;
                    case "S":
                        returnType = "1120S";
                        break;
                    case "P":
                        returnType = "1065";
                        break;
                    case "X":
                        returnType = "990";
                        break;
                    case "F":
                        returnType = "1041";
                        break;
                    case "K":
                        returnType = "5500";
                        break;
                    case "Y":
                        returnType = "709";
                        break;
                    default:
                        returnType = "1040";
                        break;
                }
            }

            switch (pdfType)
            {
                case "ACCT":
                    pdfTypeForDocName = " - Office";
                    break;
                case "GOVT":
                    pdfTypeForDocName = " - Filing";
                    break;
                case "CLNT":
                    pdfTypeForDocName = " - Client";
                    break;
                case "K1":
                    pdfTypeForDocName = "";
                    returnType = "K1";
                   break;
                default:
                    pdfTypeForDocName = " - Extension";
                    break;
            }

            Client cl = new Client();
            cl = ClientName(clientId);

            documentName = year + " - Form " + returnType;
            if (cl.FYE != "12")
            {
                int fyeYear = Convert.ToInt16(year) + 1;
                documentName += " (FYE " + cl.FYE + "/" + fyeYear.ToString() + ")";
            }
            if (isExtension)
            {
                documentName += " Extension";
            }

            documentName += " - " + cl.ClientName;

            if (pdfTypeForDocName != "")
            {
                documentName += pdfTypeForDocName + " Copy";
            }

            if (shareholderName != "") 
            {
                documentName += " - " + shareholderName.Trim();
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(@"<?xml version=""1.0"" encoding=""ISO-8859-1""?>");
            sb.Append(@"<document>");
            sb.Append(@"<client><![CDATA[");
            sb.Append(cl.ClientName);
            sb.Append(@"]]></client>");
            sb.Append(@"<year>");
            sb.Append(year);
            sb.Append(@"</year>");
            sb.Append(@"<returntype>");
            sb.Append(returnType);
            sb.Append(@"</returntype>");
            sb.Append(@"<name><![CDATA[");
            sb.Append(documentName);
            sb.Append(@"]]></name>");
            sb.Append(@"</document>");

            if (debugFlag == "1")
            {
                Console.WriteLine(sb);
            }

            File.WriteAllText(outputFileName, sb.ToString());
        }

        private static string PathNameNoFileName(string pathName, string fileName)
        {
            if (debugFlag == "1")
            {
                Console.WriteLine(pathName);
                Console.WriteLine(fileName);
                Console.WriteLine(pathName.IndexOf(fileName).ToString());
            }
            int fileNamePos = pathName.IndexOf(fileName);
            return pathName.Substring(0, fileNamePos);
        }

        private static string FileNameNoExt(string fileName)
        {

            Console.WriteLine(fileName);
            Console.WriteLine(fileName.LastIndexOf(".").ToString());
            int dotPos = fileName.LastIndexOf(".");
            return fileName.Substring(0, dotPos);

        }

        private static Client ClientName(string clientId)
        {
            Client thisClient = new Client();

            string clientName = " No Selection - 0.0";        // set the original value, which will be used if the client ID is not found in the database
            string fye = "";

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
                            clientName = oReader["ClientName"].ToString();
                            fye = oReader["fye"].ToString();
                        }

                        myConnection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            thisClient.ClientName = clientName;
            thisClient.FYE = fye;
            return thisClient;

        }
    }
}
