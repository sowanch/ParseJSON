using CsvHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace ParseJSON.Controllers
{
    public class HomeController : Controller
    {
        string featureSets = ConfigurationManager.AppSettings["featureSets"];
        string outputFolder = ConfigurationManager.AppSettings["outputFolder"];
        string dbaseName = ConfigurationManager.AppSettings["dbaseName"];

        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";

            
            return View();
        }

        [HttpPost]
        public ActionResult zipUp(string Email, HttpPostedFileBase file)
        {
            ViewBag.Title = "Home Page";

            if (file.ContentLength > 0)
            {
                var fileName = Path.GetFileName(file.FileName);
                var path = Path.Combine(Server.MapPath("~/App_Data/uploads"), fileName);
                file.SaveAs(path);


                string folderName = @"" + outputFolder;

                System.IO.Directory.CreateDirectory(folderName);

                string filename = path;

                const string sql = "select data from Surveys;";
                var conn = new SQLiteConnection("Data Source=" + filename);
                try
                {
                    conn.Open();
                    DataSet ds = new DataSet();
                    var da = new SQLiteDataAdapter(sql, conn);
                    da.Fill(ds);
                    int rowNumber = 0;

                    //Delete pre-existing files in the folder
                    string[] filePaths = Directory.GetDirectories(folderName);
                    foreach (string filePath in filePaths)
                        Directory.Delete(filePath, true);

                    Thread.Sleep(3000);

                    foreach (DataRow pRow in ds.Tables[0].Rows)
                    {
                        // To create a string that specifies the path to a subfolder under your 
                        // top-level folder, add a name for the subfolder to folderName.
                        string pathString = System.IO.Path.Combine(folderName, "SubFolder" + rowNumber.ToString());
                        System.IO.Directory.CreateDirectory(pathString);

                        
                        var jObj = JsonConvert.DeserializeObject(pRow[0].ToString()) as JObject;

                        //Create initial csv for the region
                        var initialWriter = new StreamWriter(System.IO.Path.Combine(pathString, jObj.First.Path.Trim() + ".csv"), false);
                        createCSV(initialWriter, jObj.First.First);


                        var elements = featureSets.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                        string[] words = featureSets.Split(',');
                        int wordCount;
                        wordCount = 0;

                        

                        foreach (string word in elements)
                        {
                            string jina = word.Trim();
                            JObject realData = JsonConvert.DeserializeObject(jObj.First.First.ToString()) as JObject;
                            var fdate = JObject.Parse(jObj.First.First.ToString())[jina];
                            if (fdate != null)
                                if (fdate.HasValues)
                                {
                                    JArray features = (JArray)fdate;

                                    if (!(features is null))
                                    {

                                        var writer = new StreamWriter(System.IO.Path.Combine(pathString, word.Trim() + "_" + wordCount.ToString() + ".csv"), false);

                                        var csv = new CsvWriter(writer);
                                        var headerNames = new List<string>();

                                        foreach (JProperty obj in features[0])
                                        {
                                            csv.WriteField(obj.Name);
                                            headerNames.Add(obj.Name);
                                        }

                                        csv.NextRecord();


                                        foreach (var obj in features)
                                        {
                                            int i = 0;
                                            var objectNames = new List<string>();

                                            foreach (JProperty prop in obj)
                                            {
                                                objectNames.Add(prop.Name);
                                            }

                                            foreach (string header in headerNames)
                                            {
                                                if (objectNames.Contains(header, StringComparer.OrdinalIgnoreCase))
                                                {
                                                    foreach (JProperty prop in obj)
                                                    {
                                                        if (prop.Name == header)
                                                            csv.WriteField(prop.Value.ToString());
                                                        continue;
                                                    }
                                                }
                                                else
                                                {
                                                    csv.WriteField("");
                                                }
                                            }

                                            csv.NextRecord();
                                        }

                                        writer.Close();
                                        writer.Dispose();
                                    }
                                    wordCount++;
                                }
                            
                        }

                        rowNumber++;

                    }

                    conn.Close();

                    System.IO.File.Delete(path);

                }
                catch (Exception yg)
                {

                }

                string zipPath = @"" + dbaseName + DateTime.Now.ToString("yyyyMMddHHmmss") + ".zip";

                ZipFile.CreateFromDirectory(folderName, zipPath, CompressionLevel.Fastest, true);

                using (MailMessage mm = new MailMessage("lndungo@gmail.com", Email))
                {
                    mm.Subject = "Extracted CSVs from SQLite";
                    mm.Body = "Please find the CSVs extracted";

                    Attachment at = new Attachment(zipPath);
                    mm.Attachments.Add(at);

                    mm.IsBodyHtml = false;
                    SmtpClient smtp = new SmtpClient();
                    smtp.Host = "smtp.gmail.com";
                    smtp.EnableSsl = true;
                    NetworkCredential NetworkCred = new NetworkCredential("lndungo@gmail.com", "wambuila22");
                    smtp.UseDefaultCredentials = true;
                    smtp.Credentials = NetworkCred;
                    smtp.Port = 587;
                    smtp.Send(mm);
                    ViewBag.Message = "Email sent.";
                }

                Thread.Sleep(3000);

            }


            return View();
        }

        private static void createCSV(StreamWriter writer, JToken features)
        {
            var csv = new CsvWriter(writer);
            var headerNames = new List<string>();

            foreach (JProperty obj in features)
            {
                csv.WriteField(obj.Name);
                headerNames.Add(obj.Name);
            }

            csv.NextRecord();


            foreach (var rowLine in features)
            {
                int i = 0;
                var objectNames = new List<string>();

                foreach (var prop in rowLine)
                {
                    if(prop.ToString() != null) {
                        string csValue;
                        if (prop.ToString().Length > 30000) { 
                            csValue = prop.ToString().Substring(0, 30000);
                        }
                        else
                        {
                            csValue = prop.ToString();
                        }
                        csv.WriteField(csValue);
                    }
                    else
                    {
                        csv.WriteField("");
                    }
                }
            }

            csv.NextRecord();

            writer.Close();
            writer.Dispose();
        }
    }
}
