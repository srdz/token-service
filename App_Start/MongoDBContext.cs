using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TokenWindowsService.App_Start
{
    public class MongoDBContext
    {
        public IMongoDatabase database;

        public MongoDBContext()
        {
            // internal server - deciphera
            string url = "mongodb://...";

            var mongoClient = new MongoClient(url);
            database = mongoClient.GetDatabase("CustomerMaster");

            //if (!string.IsNullOrEmpty(url))
            //{
            //    int pos = url.LastIndexOf("/") + 1;
            //    var server_database = url.Substring(pos, url.Length - pos);

            //    var mongoClient = new MongoClient(url);
            //    database = mongoClient.GetDatabase(server_database);
            //}
        }
    }
}