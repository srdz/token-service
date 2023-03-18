using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace TokenWindowsService.Models
{
    public class LoginOAuthModel
    {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }
        public int id { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string[] role { get; set; }
    }
}