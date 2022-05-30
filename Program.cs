using System.Net;
using System.Text;
using MongoDB.Bson;
using Newtonsoft.Json;
using VersineResponse;
using VersineUser;

namespace users;

class HttpServer
{
    private static HttpListener? listener;

    private static async Task HandleIncomingConnections(EasyMango.EasyMango userDatabase, EasyMango.EasyMango postDatabase, WebToken.WebToken jwt,
        string doorUrl)
    {
        while (true)
        {
            // Will wait here until we hear from a connection
            HttpListenerContext ctx = await listener?.GetContextAsync()!;

            // Peel out the requests and response objects
            HttpListenerRequest req = ctx.Request;
            HttpListenerResponse resp = ctx.Response;

            // Print out some info about the request
            Console.WriteLine(req.HttpMethod);
            Console.WriteLine(req.Url?.ToString());
            Console.WriteLine(req.UserHostName);
            Console.WriteLine(req.UserAgent);

            // Lex the url into an array
            string[] reqUrlArray = (req.Url?.AbsolutePath ?? "").Trim('/').Split('/');

            // Public profile
            if (req.HttpMethod == "GET" && reqUrlArray.Length == 2 && reqUrlArray[0] == "user")
            {
                string username = reqUrlArray[1];

                string userid = userDatabase.GetSingleDatabaseEntry("username", username, out BsonDocument userBson)
                    ? userBson.GetElement("_id").Value.AsObjectId.ToString()
                    : "";

                if (!string.Equals(userid, ""))
                {
                    Dictionary<string, string> data = new Dictionary<string, string>
                    {
                        { "id", userid },
                        { "avatar", userBson.GetElement("avatar").Value.AsString },
                        { "bio", userBson.GetElement("bio").Value.AsString },
                        { "banner", userBson.GetElement("banner").Value.AsString },
                        { "color", userBson.GetElement("color").Value.AsString }
                    };

                    string jsonData = JsonConvert.SerializeObject(data);


                    Response.Success(resp, "Profile provided", jsonData);
                }
                else
                {
                    Response.Fail(resp, "user not found");
                }
            }
            // Private profile
            else if (req.HttpMethod == "POST" && req.Url?.AbsolutePath == "/profile")
            {
                StreamReader reader = new StreamReader(req.InputStream);
                string bodyString = await reader.ReadToEndAsync();
                dynamic body = JsonConvert.DeserializeObject(bodyString)!;

                string token;
                try
                {
                    token = ((string)body.username).Trim();
                }
                catch
                {
                    token = "";
                }

                string userid = jwt.GetIdFromToken(token);

                if (!string.Equals(userid, ""))
                {
                    if (userDatabase.GetSingleDatabaseEntry("_id", new BsonObjectId(new ObjectId(userid)),
                            out BsonDocument userBson))
                    {
                        Dictionary<string, string> data = new Dictionary<string, string>
                        {
                            { "id", userid },
                            { "ticket", userBson.GetElement("ticket").Value.AsString },
                            { "ticketCount", userBson.GetElement("ticketCount").Value.AsInt32.ToString() },
                            { "avatar", userBson.GetElement("avatar").Value.AsString },
                            { "bio", userBson.GetElement("bio").Value.AsString },
                            { "banner", userBson.GetElement("banner").Value.AsString },
                            { "color", userBson.GetElement("color").Value.AsString }
                        };

                        string jsonData = JsonConvert.SerializeObject(data);


                        Response.Success(resp, "Profile provided", jsonData);
                    }
                    else
                    {
                        Response.Fail(resp, "user no longer exists");
                    }
                }
                else
                {
                    Response.Fail(resp, "invalid token");
                }
            }
            else if (req.HttpMethod == "POST" && req.Url?.AbsolutePath == "/deleteUser")
            {
                StreamReader reader = new StreamReader(req.InputStream);
                string bodyString = await reader.ReadToEndAsync();
                dynamic body = JsonConvert.DeserializeObject(bodyString)!;

                string token;
                string password;
                try
                {
                    token = ((string)body.username).Trim();
                    password = ((string)body.password).Trim();
                }
                catch
                {
                    token = "";
                    password = "";
                }

                string userid = jwt.GetIdFromToken(token);

                if (!string.Equals(userid, ""))
                {
                    if (userDatabase.GetSingleDatabaseEntry("_id", new BsonObjectId(new ObjectId(userid)),
                            out BsonDocument userBson))
                    {
                        HttpClient client = new HttpClient();

                        Dictionary<string, string> login = new Dictionary<string, string>
                        {
                            { "username", userBson.GetElement("username").Value.AsString },
                            { "password", password }
                        };

                        string requestBody = JsonConvert.SerializeObject(login);
                        var httpContent = new StringContent(requestBody, Encoding.UTF8, "application/json");
                        var result = await client.PostAsync(doorUrl + "/login", httpContent);
                        string bodystr = await result.Content.ReadAsStringAsync();
                        dynamic json = JsonConvert.DeserializeObject(bodystr)!;
                        if ((string)json.status == "success")
                        {
                            if (userDatabase.RemoveSingleDatabaseEntry("_id", new BsonObjectId(new ObjectId(userid))))
                            {
                                // TODO : remove all posts, friends, friend requests and circles of the user
                                Response.Success(resp, "user successfully deleted", "");
                            }
                            else
                            {
                                Response.Fail(resp, "an error occured, please try again later");
                            }
                        }
                        else
                        {
                            Response.Fail(resp, "wrong password");
                        }
                    }
                    else
                    {
                        Response.Fail(resp, "user no longer exists");
                    }
                }
                else
                {
                    Response.Fail(resp, "invalid token");
                }
            }
            else if (req.HttpMethod == "POST" && req.Url?.AbsolutePath == "/editBio")
            {
                StreamReader reader = new StreamReader(req.InputStream);
                string bodyString = await reader.ReadToEndAsync();
                dynamic body = JsonConvert.DeserializeObject(bodyString)!;

                string token;
                string bio;
                try
                {
                    token = ((string)body.username).Trim();
                }
                catch
                {
                    token = "";
                }

                try
                {
                    bio = ((string)body.username).Trim();
                }
                catch
                {
                    bio = "";
                }

                string userid = jwt.GetIdFromToken(token);

                if (!string.Equals(userid, ""))
                {
                    if (userDatabase.GetSingleDatabaseEntry("_id", new BsonObjectId(new ObjectId(userid)),
                            out BsonDocument userBson))
                    {
                        User user = new User(userBson)
                        {
                            bio = bio
                        };

                        if (userDatabase.ReplaceSingleDatabaseEntry("_id", userid, user.ToBson()))
                        {
                            Response.Success(resp, "user bio changed", bio);
                        }
                        else
                        {
                            Response.Fail(resp, "an error occured, please try again later");
                        }
                    }
                    else
                    {
                        Response.Fail(resp, "user no longer exists");
                    }
                }
                else
                {
                    Response.Fail(resp, "invalid token");
                }
            }
            else if (req.HttpMethod == "POST" && req.Url?.AbsolutePath == "/editUsername")
            {
                StreamReader reader = new StreamReader(req.InputStream);
                string bodyString = await reader.ReadToEndAsync();
                dynamic body = JsonConvert.DeserializeObject(bodyString)!;

                string token;
                string username;
                try
                {
                    token = ((string)body.username).Trim();
                    username = ((string)body.username).Trim();
                }
                catch
                {
                    token = "";
                    username = "";
                }
                
                string userid = jwt.GetIdFromToken(token);

                if (!string.Equals(userid, "") && !string.Equals(username, ""))
                {
                    if (!userDatabase.GetSingleDatabaseEntry("username", username, out BsonDocument nonExistentUser))
                    {
                        BsonObjectId userObjectId = new BsonObjectId(new ObjectId(userid));               
                        if (userDatabase.GetSingleDatabaseEntry("_id", userObjectId,
                                out BsonDocument userBson))
                        {
                            User user = new User(userBson)
                            {
                                username = username
                            };
                            if (userDatabase.ReplaceSingleDatabaseEntry("_id",userObjectId,user.ToBson()))
                            {
                                Response.Success(resp, "username changed", username);
                            }
                            else
                            {
                                Response.Fail(resp, "an error occured, please try again later");
                            }
                        }
                        else
                        {
                            Response.Fail(resp, "user no longer exists");
                        }
                    }
                    else
                    {
                        Response.Fail(resp, "username taken");
                    }
                }
                else
                {
                    Response.Fail(resp, "invalid body");
                }
            }
            else if (req.HttpMethod == "POST" && (req.Url?.AbsolutePath == "/requestFriend" ||
                                                  req.Url?.AbsolutePath == "/deleteRequest"))
            {
                StreamReader reader = new StreamReader(req.InputStream);
                string bodyString = await reader.ReadToEndAsync();
                dynamic body = JsonConvert.DeserializeObject(bodyString)!;

                string webToken;
                string requestId;
                try
                {
                    webToken = ((string)body.username).Trim();
                    requestId = ((string)body.password).Trim();
                }
                catch
                {
                    webToken = "";
                    requestId = "";
                }


                string id = jwt.GetIdFromToken(webToken);
                if (string.Equals(id, ""))
                {
                    Response.Fail(resp, "invalid token");
                }
                else
                {
                    BsonObjectId userId = new BsonObjectId(new ObjectId(id));
                    BsonObjectId friendId = new BsonObjectId(new ObjectId(requestId));

                    if (userDatabase.GetSingleDatabaseEntry("_id", userId,
                            out BsonDocument userBsonDocument))
                    {
                        if (userDatabase.GetSingleDatabaseEntry("_id", friendId,
                                out BsonDocument requestedUserBsonDocument))
                        {
                            User user = new User(userBsonDocument);
                            User requestedUser = new User(requestedUserBsonDocument);

                            if (req.Url?.AbsolutePath == "/requestFriend")
                            {
                                if (user.incomingFriendRequests.Contains(friendId) ||
                                    requestedUser.outgoingFriendRequests.Contains(userId))
                                {
                                    user.friends.Add(friendId);
                                    requestedUser.friends.Add(userId);

                                    user.outgoingFriendRequests.Remove(friendId);

                                    user.incomingFriendRequests.Remove(friendId);

                                    requestedUser.outgoingFriendRequests.Remove(userId);

                                    requestedUser.incomingFriendRequests.Remove(userId);
                                }
                                else
                                {
                                    if (!user.outgoingFriendRequests.Contains(friendId))
                                    {
                                        user.outgoingFriendRequests.Add(friendId);
                                    }

                                    if (!requestedUser.incomingFriendRequests.Contains(userId))
                                    {
                                        requestedUser.incomingFriendRequests.Add(userId);
                                    }
                                }
                            }
                            else
                            {
                                user.outgoingFriendRequests.Remove(friendId);

                                requestedUser.incomingFriendRequests.Remove(userId);
                            }

                            if (userDatabase.ReplaceSingleDatabaseEntry("_id", userId, user.ToBson()) &&
                                userDatabase.ReplaceSingleDatabaseEntry("_id", friendId, user.ToBson()))
                            {
                                Response.Success(resp, "success", "");
                            }
                            else
                            {
                                Response.Fail(resp, "an error occured, please try again later");
                            }
                        }
                        else
                        {
                            Response.Fail(resp, "requested user doesn't exist");
                        }
                    }
                    else
                    {
                        Response.Fail(resp, "user no longer exists");
                    }
                }
            }
            else
            {
                Response.Fail(resp, "404");
            }
            resp.Close();
        }
    }

    public static void Main(string[] args)
    {
        // Load config file
        IConfigurationRoot config =
            new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddEnvironmentVariables()
                .Build();

        // Get values from config file
        string connectionString = config.GetValue<string>("connectionString");
        string userDatabaseNAme = config.GetValue<string>("userDatabaseNAme");
        string userCollectionName = config.GetValue<string>("userCollectionName");
        string postDatabaseNAme = config.GetValue<string>("postDatabaseNAme");
        string postCollectionName = config.GetValue<string>("postCollectionName");
        string secretKey = config.GetValue<string>("secretKey");
        string doorUrl = config.GetValue<string>("doorUrl");
        uint expireDelay = config.GetValue<uint>("expireDelay");

        // Create a Http server and start listening for incoming connections
        string url = "http://*:" + config.GetValue<String>("Port") + "/";
        listener = new HttpListener();
        listener.Prefixes.Add(url);
        listener.Start();
        Console.WriteLine("Listening for connections on {0}", url);

        // Json web token
        WebToken.WebToken jwt = new WebToken.WebToken(secretKey, expireDelay);


        // Create EasyMango databases
        EasyMango.EasyMango userDatabase = new EasyMango.EasyMango(connectionString, userDatabaseNAme, userCollectionName);
        EasyMango.EasyMango postDatabase = new EasyMango.EasyMango(connectionString, postDatabaseNAme, postCollectionName);

        // Handle requests
        Task listenTask = HandleIncomingConnections(userDatabase,postDatabase, jwt, doorUrl);
        listenTask.GetAwaiter().GetResult();

        // Close the listener
        listener.Close();
    }
}
