using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using users;

namespace users;
class HttpServer
    {
        public static BsonDocument ChangeBson(dynamic changes, BsonDocument userinfo)
        {
            User user = new User(userinfo);
            if (changes.username != null)
            {
                user.username = changes.username;
            }
            if (changes.bio != null)
            {
                user.bio = changes.bio;
            }
            if (changes.avatar != null)
            {
                user.avatar = changes.avatar;
            }
            if (changes.banner != null)
            {
                user.banner = changes.banner;
            }
            if (changes.color != null)
            {
                user.color = changes.color;
            }
            return user.ToBson();
        }
        public static BsonDocument friend(dynamic newfriend, dynamic friendtorm, BsonDocument userinfo)
        {
            User user = new User(userinfo);
            if (newfriend != null)
            {
                user.friends.Add(newfriend);
            }

            if (friendtorm != null)
            {
                user.friends.Remove(friendtorm);
            }
            return user.ToBson();
        }
        public static HttpListener? Listener;

        public static async Task HandleIncomingConnections(IConfigurationRoot config, EasyMango.EasyMango database)
        {
            while (true)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await Listener?.GetContextAsync()!;

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                // Print out some info about the request
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.Url?.ToString());
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);

                List<Token> Lexed = Lexer.Lex(req.Url?.AbsolutePath);

                if (Lexed.Count == 2)
                {
                    if (req.HttpMethod == "POST" &&  Lexed[0].Str == "user")
                    {
                        StreamReader reader = new StreamReader(req.InputStream);
                        string bodyString = await reader.ReadToEndAsync();
                        dynamic body = JsonConvert.DeserializeObject(bodyString)!;

                        string user = ((string) body.token).Trim() ?? "";
                        string userid = WebToken.GetIdFromToken(user);
                        
                        if (database.GetSingleDatabaseEntry("_id", new BsonObjectId(userid), out BsonDocument UserBson))
                        {
                            switch (Lexed[1].Str)
                            {
                                case "edit":
                                    BsonDocument editedbson = ChangeBson(body.changed, UserBson);
                                    if (database.ReplaceSingleDatabaseEntry("_id", new BsonObjectId(userid),editedbson))
                                    {
                                        Response.Success(resp, "user edited",editedbson.ToString());
                                    }
                                    else
                                    {
                                        Response.Fail(resp,"user not found");
                                    }
                                    break;
                                case "addfriend":
                                    BsonDocument addedfriendbson = friend(body.friend, null, UserBson);
                                    if (database.ReplaceSingleDatabaseEntry("_id", new BsonObjectId(userid), addedfriendbson))
                                    {
                                        Response.Success(resp, "friend added", addedfriendbson.ToString());
                                    }
                                    else
                                    {
                                        Response.Fail(resp,"user not found");
                                    }
                                    break;
                                case "removefriend":
                                    BsonDocument removedfriend = friend(null, body.friendtorm, UserBson);
                                    if (database.ReplaceSingleDatabaseEntry("_id", new BsonObjectId(userid), removedfriend))
                                    {
                                        Response.Success(resp, "friend removed", removedfriend.ToString());
                                    }
                                    else
                                    {
                                        Response.Fail(resp,"user not found");
                                    }
                                    break;
                                case "delete":
                                    if (database.RemoveSingleDatabaseEntry("_id",new BsonObjectId(userid)))
                                    {
                                        Response.Success(resp,"user deleted","we good");
                                    }
                                    else
                                    {
                                        Response.Fail(resp,"user to delete not found");
                                    }
                                    break;
                                default:
                                    Response.Fail(resp, "invalid body");
                                    break;
                            }
                        }
                        else
                        {
                            Response.Fail(resp, "user not found");
                        }
                    }
                    else
                    {
                        Response.Fail(resp, "404");
                    }
                }
                resp.Close();
            }
        }
        
        public static void Main(string[] args)
        {
            // Build the configuration for the env variables
            IConfigurationRoot config =
                new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", true)
                    .AddEnvironmentVariables()
                    .Build();

            // Create a Http server and start listening for incoming connections
            string url = "http://*:" + config.GetValue<String>("Port") + "/";
            Listener = new HttpListener();
            Listener.Prefixes.Add(url);
            Listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            string connectionString = config.GetValue<String>("connectionString");
            string databaseNAme = config.GetValue<String>("databaseName");
            string collectionName = config.GetValue<String>("collectionName");


            // Create a new EasyMango database
            EasyMango.EasyMango database = new EasyMango.EasyMango(connectionString, databaseNAme, collectionName);

            // Handle requests
            Task listenTask = HandleIncomingConnections(config, database);
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            Listener.Close();
        }
    }