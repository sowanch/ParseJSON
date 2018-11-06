using CsvHelper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ParseJSON.Controllers
{
    public class ValuesController : ApiController
    {
        string featureSets = ConfigurationManager.AppSettings["featureSets"];
        string ficha = ConfigurationManager.AppSettings["ficha"];
        string outputFolder = ConfigurationManager.AppSettings["outputFolder"];
        string dbaseName = ConfigurationManager.AppSettings["dbaseName"];
        // GET api/values
        public IEnumerable<string> Get()
        {
            string folderName = @"" + outputFolder;

            System.IO.Directory.CreateDirectory(folderName);

            // Create a file name for the file you want to create. 
            string fileName = System.IO.Path.GetRandomFileName();

            // This example uses a random string for the name, but you also can specify
            // a particular name.
            //string fileName = "MyNewFile.txt";

            // Use Combine again to add the file name to the path.
            //pathString = System.IO.Path.Combine(pathString, fileName);

            string filename = dbaseName;

            const string sql = "select data from Surveys;";
            var conn = new SQLiteConnection("Data Source=" + filename);
            try
            {
                conn.Open();
                DataSet ds = new DataSet();
                var da = new SQLiteDataAdapter(sql, conn);
                da.Fill(ds);
                int rowNumber = 0;

                foreach (DataRow pRow in ds.Tables[0].Rows)
                {
                    // To create a string that specifies the path to a subfolder under your 
                    // top-level folder, add a name for the subfolder to folderName.
                    string pathString = System.IO.Path.Combine(folderName, "SubFolder" + rowNumber.ToString());
                    System.IO.Directory.CreateDirectory(pathString);

                    var jObj = JsonConvert.DeserializeObject(pRow[0].ToString()) as JObject;

                    //JObject o1 = JObject.Parse(File.ReadAllText(@"h:\pangash.json"));

                    var elements = featureSets.Split(new[]{ ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                    string[] words = featureSets.Split(',');
                    int wordCount;
                    wordCount = 0;

                    //JArray features = (JArray)jObj.First.First[ficha];

                    foreach (string word in elements)
                    {
                        string jina = word.Trim();
                        JObject realData = JsonConvert.DeserializeObject(jObj.First.First.ToString()) as JObject;
                        var fdate = JObject.Parse(jObj.First.First.ToString())[jina];
                        if (fdate.HasValues)
                        {
                            JArray features = (JArray)fdate;

                            if (!(features is null))
                            {

                                //var writer = new StreamWriter(@"h:\" + word.Trim() + "_" + wordCount.ToString() + ".csv", false);
                                //,,pathString = System.IO.Path.Combine(pathString, fileName);

                                var writer = new StreamWriter(System.IO.Path.Combine(pathString, word.Trim() + "_" + wordCount.ToString() + ".csv"), false);

                                var csv = new CsvWriter(writer);

                                foreach (JProperty obj in features[0])
                                {
                                    csv.WriteField(obj.Name);
                                }

                                csv.NextRecord();

                                foreach (var obj in features)
                                {
                                    foreach (JProperty prop in obj)
                                    {
                                        csv.WriteField(prop.Value.ToString());
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

            }
            catch (Exception yg)
            {

            }

            //using (var csv = new CsvWriter(writer))
            //{



            //    writer.Flush();
            //    stream.Position = 0;

            //    reader.ReadToEnd();

            //    using (Stream s = File.Create(@"h:\pangizo.csv"))
            //    {
            //        reader.CopyTo(s);
            //    }


            //}

            // read JSON directly from a file
            string startPath = @"c:\example\start";
            string zipPath = @"c:\example\result.zip";
            string extractPath = @"c:\example\extract";

            ZipFile.CreateFromDirectory(folderName, @"H:\GISData\result.zip", CompressionLevel.Fastest, true);

            /*using (MemoryStream ms = new MemoryStream())
            {
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    var zipArchiveEntry = archive.CreateEntry("file1.txt", CompressionLevel.Fastest);
                    using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(file1, 0, file1.Length);
                    zipArchiveEntry = archive.CreateEntry("file2.txt", CompressionLevel.Fastest);
                    using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(file2, 0, file2.Length);
                }
                return File(ms.ToArray(), "application/zip", "Archive.zip");
            }*/

            //ZipFile.ExtractToDirectory(zipPath, extractPath);
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }

        public static DataTable jsonStringToTable(string jsonContent)
        {
            DataTable dt = JsonConvert.DeserializeObject<DataTable>(jsonContent);
            return dt;
        }

        public static string jsonToCSV(string jsonContent, string delimiter)
        {
            StringWriter csvString = new StringWriter();
            using (var csv = new CsvWriter(csvString))
            {
                csv.Configuration.Delimiter = delimiter;

                using (var dt = jsonStringToTable(jsonContent))
                {
                    foreach (DataColumn column in dt.Columns)
                    {
                        csv.WriteField(column.ColumnName);
                    }
                    csv.NextRecord();

                    foreach (DataRow row in dt.Rows)
                    {
                        for (var i = 0; i < dt.Columns.Count; i++)
                        {
                            csv.WriteField(row[i]);
                        }
                        csv.NextRecord();
                    }
                }
            }
            return csvString.ToString();
        }
    }
}
