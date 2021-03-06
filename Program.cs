using System.Globalization;
using System.Net;
using System.Text;
using door;
using MongoDB.Bson;
using Newtonsoft.Json;
using VersineResponse;
using VersineUser;

namespace users;

class HttpServer
{
    private static HttpListener? listener;

    private static async Task HandleIncomingConnections(EasyMango.EasyMango userDatabase,
        EasyMango.EasyMango postDatabase, WebToken.WebToken jwt,
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
            if (req.HttpMethod == "POST" && reqUrlArray.Length == 2 && reqUrlArray[0] == "user")
            { 
                StreamReader reader = new StreamReader(req.InputStream);
                string bodyString = await reader.ReadToEndAsync();
                dynamic body;
                try
                {
                    body = JsonConvert.DeserializeObject(bodyString)!;
                }
                catch
                {
                    Response.Fail(resp, "bad request");
                    resp.Close();
                    continue;
                }

                string token;
                try
                {
                    token = ((string) body.token).Trim();
                }
                catch
                {
                    token = "";
                }

                string requestingUserid = jwt.GetIdFromToken(token);

                if (!string.Equals(requestingUserid, ""))
                {
                    if (userDatabase.GetSingleDatabaseEntry("_id", new BsonObjectId(new ObjectId(requestingUserid)),
                            out BsonDocument requestingUserBson))
                    {
                        string username = reqUrlArray[1];

                        string userid =
                            userDatabase.GetSingleDatabaseEntry("username", username, out BsonDocument userBson)
                                ? userBson.GetElement("_id").Value.AsObjectId.ToString()
                                : "";

                        if (!string.Equals(userid, ""))
                        {
                            User user = new User(userBson);
                            Dictionary<string, string> data = new Dictionary<string, string>
                            {
                                {"id", userid},
                                {"username", user.username},
                                {"avatar", user.avatar},
                                {"bio", user.bio},
                                {"banner", user.banner},
                                {"color", user.color}
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
                else
                {
                    Response.Fail(resp, "user not found");
                }
            }
            // Public profile by id
            else if (req.HttpMethod == "POST" && reqUrlArray.Length == 2 && reqUrlArray[0] == "userById")
            {
                StreamReader reader = new StreamReader(req.InputStream);
                string bodyString = await reader.ReadToEndAsync();
                dynamic body;
                try
                {
                    body = JsonConvert.DeserializeObject(bodyString)!;
                }
                catch
                {
                    Response.Fail(resp, "bad request");
                    resp.Close();
                    continue;
                }

                string token;
                try
                {
                    token = ((string) body.token).Trim();
                }
                catch
                {
                    token = "";
                }

                string requestingUserid = jwt.GetIdFromToken(token);

                if (!string.Equals(requestingUserid, ""))
                {
                    if (userDatabase.GetSingleDatabaseEntry("_id", new BsonObjectId(new ObjectId(requestingUserid)),
                            out BsonDocument requestingUserBson))
                    {
                        string requestedUserId = reqUrlArray[1];

                        string userid =
                            userDatabase.GetSingleDatabaseEntry("_id", new BsonObjectId (new ObjectId(requestedUserId)), out BsonDocument userBson)
                                ? userBson.GetElement("_id").Value.AsObjectId.ToString()
                                : "";

                        if (!string.Equals(userid, ""))
                        {
                            User user = new User(userBson);
                            Dictionary<string, string> data = new Dictionary<string, string>
                            {
                                {"id", userid},
                                {"username", user.username},
                                {"avatar", user.avatar},
                                {"bio", user.bio},
                                {"banner", user.banner},
                                {"color", user.color}
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
                dynamic body;
                try
                {
                    body = JsonConvert.DeserializeObject(bodyString)!;
                }
                catch
                {
                    Response.Fail(resp, "bad request");
                    resp.Close();
                    continue;
                }

                string token;
                try
                {
                    token = ((string) body.token).Trim();
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

                        User user = new User(userBson);

                        string stringified(List<BsonObjectId> list)
                        {
                            var stringifiedFriendArray = "[";
                            foreach (BsonObjectId element in list)
                            {
                                stringifiedFriendArray += '"';
                                stringifiedFriendArray += element.ToString();
                                stringifiedFriendArray += '"';
                                stringifiedFriendArray += ',';
                            }
                            stringifiedFriendArray += ']';

                            return stringifiedFriendArray;
                        }

                        Dictionary<string, string> data = new Dictionary<string, string>
                        {
                            {"id", userid},
                            {"ticket", user.ticket},
                            {"ticketCount", user.ticketCount.ToString()},
                            {"avatar", user.avatar},
                            {"bio", user.bio},
                            {"banner", user.banner},
                            {"color", user.color},
                            {"friends", stringified(user.friends)},
                            {"incomingFriendRequests",stringified(user.incomingFriendRequests)},
                            {"outgoingFriendRequests",stringified(user.outgoingFriendRequests)}
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
                dynamic body;
                try
                {
                    body = JsonConvert.DeserializeObject(bodyString)!;
                }
                catch
                {
                    Response.Fail(resp, "bad request");
                    resp.Close();
                    continue;
                }

                string token;
                string password;
                try
                {
                    token = ((string) body.token).Trim();
                    password = ((string) body.password).Trim();
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
                            {"username", userBson.GetElement("username").Value.AsString},
                            {"password", password}
                        };

                        string requestBody = JsonConvert.SerializeObject(login);
                        var httpContent = new StringContent(requestBody, Encoding.UTF8, "application/json");
                        var result = await client.PostAsync(doorUrl + "/login", httpContent);
                        string bodystr = await result.Content.ReadAsStringAsync();
                        dynamic json = JsonConvert.DeserializeObject(bodystr)!;
                        if ((string) json.status == "success")
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
            else if (req.HttpMethod == "POST" && req.Url?.AbsolutePath == "/editUser")
            {
                StreamReader reader = new StreamReader(req.InputStream);
                string bodyString = await reader.ReadToEndAsync();
                dynamic body;
                try
                {
                    body = JsonConvert.DeserializeObject(bodyString)!;
                }
                catch
                {
                    Response.Fail(resp, "bad request");
                    resp.Close();
                    continue;
                }

                string token;
                string bio;
                string color;
                string avatar;
                string banner;
                try
                {
                    token = ((string) body.token).Trim();
                }
                catch
                {
                    token = "";
                }

                try
                {
                    bio = ((string) body.bio).Trim();
                }
                catch
                {
                    bio = "";
                }

                try
                {
                    color = ((string) body.color).Trim();
                }
                catch
                {
                    color = "";
                }

                try
                {
                    avatar = ((string) body.avatar).Trim();
                }
                catch
                {
                    avatar = "";
                }

                try
                {
                    banner = ((string) body.banner).Trim();
                }
                catch
                {
                    banner = "";
                }

                string userid = jwt.GetIdFromToken(token);

                if (!string.Equals(userid, ""))
                {
                    if (userDatabase.GetSingleDatabaseEntry("_id", new BsonObjectId(new ObjectId(userid)),
                            out BsonDocument userBson))
                    {
                        // Verifies color is a valid color
                        if (color.Length >1)
                        {
                            if (!((color[0] == '#' && color.Length == 7 && int.TryParse(color.Substring(1),
                                    NumberStyles.HexNumber, null, out Int32 temp)) || string.Equals(color, "")))
                            {
                                color = userBson.GetElement("color").Value.AsString;
                            }
                        }
                        else
                        {
                            color = userBson.GetElement("color").Value.AsString;
                        }

                        // TODO: store avatar image and put link in avatar variable
                        // TODO: store banner image and put link in banner variable

                        User user = new User(userBson);
                        user.bio = bio;
                        user.color = color;
                        user.avatar = avatar;
                        user.banner = banner;
                        user.avatar = avatar;
                        user.banner = banner;
                        
                        if (userDatabase.ReplaceSingleDatabaseEntry("_id", new BsonObjectId(new ObjectId(userid)), user.ToBson()))
                        {
                            Response.Success(resp, "user modified", bio);
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
                dynamic body;
                try
                {
                    body = JsonConvert.DeserializeObject(bodyString)!;
                }
                catch
                {
                    Response.Fail(resp, "bad request");
                    resp.Close();
                    continue;
                }

                string token;
                string username;
                try
                {
                    token = ((string) body.token).Trim();
                    username = ((string) body.username).Trim();
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
                            if (userDatabase.ReplaceSingleDatabaseEntry("_id", userObjectId, user.ToBson()))
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
            else if (req.HttpMethod == "POST" && req.Url?.AbsolutePath == "/editPassword")
            {
                StreamReader reader = new StreamReader(req.InputStream);
                string bodyString = await reader.ReadToEndAsync();
                dynamic body;
                try
                {
                    body = JsonConvert.DeserializeObject(bodyString)!;
                }
                catch
                {
                    Response.Fail(resp, "bad request");
                    resp.Close();
                    continue;
                }

                string token;
                string password;
                string newPassword;
                try
                {
                    token = ((string) body.token).Trim();
                    password = ((string) body.password).Trim();
                    newPassword = ((string) body.newPassword).Trim();
                }
                catch
                {
                    token = "";
                    password = "";
                    newPassword = "";
                }

                string userid = jwt.GetIdFromToken(token);
                if (!string.Equals(userid, ""))
                {
                    if (!string.Equals(newPassword, ""))
                    {
                        if (userDatabase.GetSingleDatabaseEntry("_id", new BsonObjectId(new ObjectId(userid)),
                                out BsonDocument userBson))
                        {
                            HttpClient client = new HttpClient();

                            Dictionary<string, string> login = new Dictionary<string, string>
                            {
                                {"username", userBson.GetElement("username").Value.AsString},
                                {"password", password}
                            };

                            string requestBody = JsonConvert.SerializeObject(login);
                            var httpContent = new StringContent(requestBody, Encoding.UTF8, "application/json");
                            var result = await client.PostAsync(doorUrl + "/login", httpContent);
                            string bodystr = await result.Content.ReadAsStringAsync();
                            dynamic json = JsonConvert.DeserializeObject(bodystr)!;
                            if ((string) json.status == "success")
                            {
                                User user = new User(userBson)
                                {
                                    password = HashTools.HashString(newPassword, userid)
                                };

                                userDatabase.ReplaceSingleDatabaseEntry("_id", new BsonObjectId(new ObjectId(userid)),
                                    user.ToBson());
                                Response.Success(resp, "user password modified", "");
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
                        Response.Fail(resp, "empty password");
                    }
                }
                else
                {
                    Response.Fail(resp, "invalid token");
                }
            }
            else if (req.HttpMethod == "POST" && (req.Url?.AbsolutePath == "/requestFriend" ||
                                                  req.Url?.AbsolutePath == "/deleteRequest"))
            {
                StreamReader reader = new StreamReader(req.InputStream);
                string bodyString = await reader.ReadToEndAsync();
                dynamic body;
                try
                {
                    body = JsonConvert.DeserializeObject(bodyString)!;
                }
                catch
                {
                    Response.Fail(resp, "bad request");
                    resp.Close();
                    continue;
                }
                
                string token;
                string requesteduserid;
                
                try
                {
                    token = ((string) body.token).Trim();
                    requesteduserid = ((string) body.requesteduserid).Trim();
                }
                catch
                {
                    token = "";
                    requesteduserid = "";
                }
                //string requestId;
                
                string id = jwt.GetIdFromToken(token);
                if (string.Equals(id, ""))
                {
                    Response.Fail(resp, "invalid token");
                }
                else
                {
                    
                    BsonObjectId userId = new BsonObjectId(new ObjectId(id));
                    BsonObjectId requestedid = new BsonObjectId(new ObjectId(requesteduserid));
                    if (userDatabase.GetSingleDatabaseEntry("_id", userId,
                            out BsonDocument userBsonDocument))
                    {
                        if (userDatabase.GetSingleDatabaseEntry("_id", requestedid,
                                out BsonDocument requestedUserBsonDocument))
                        {
                            User user = new User(userBsonDocument);
                            User requestedUser = new User(requestedUserBsonDocument);
                            BsonObjectId requestedUserId = requestedUserBsonDocument.GetElement("_id").Value.AsObjectId;

                            if (req.Url?.AbsolutePath == "/requestFriend")
                            {
                                if (user.incomingFriendRequests.Contains(requestedUserId) ||
                                    requestedUser.outgoingFriendRequests.Contains(userId))
                                {
                                    user.friends.Add(requestedUserId);
                                    requestedUser.friends.Add(userId);

                                    user.outgoingFriendRequests.Remove(requestedUserId);

                                    user.incomingFriendRequests.Remove(requestedUserId);

                                    requestedUser.outgoingFriendRequests.Remove(userId);

                                    requestedUser.incomingFriendRequests.Remove(userId);
                                }
                                else
                                {
                                    if (!user.friends.Contains(requestedUserId))
                                    {
                                        if (!user.outgoingFriendRequests.Contains(requestedUserId))
                                        {
                                            user.outgoingFriendRequests.Add(requestedUserId);
                                        }
                                        else
                                        {
                                            Response.Fail(resp, "You already have an outgoing friend request for this user!");
                                        }

                                        if (!requestedUser.incomingFriendRequests.Contains(userId))
                                        {
                                            requestedUser.incomingFriendRequests.Add(userId);
                                        }
                                        else
                                        {
                                            Response.Fail(resp, "This user already have a friend request from you!");
                                        }
                                    }
                                    else
                                    {
                                        Response.Fail(resp, "This user is already your friend!");
                                    }
                                }
                            }
                            else
                            {
                                user.outgoingFriendRequests.Remove(requestedUserId);
                                user.friends.Remove(requestedUserId);
                                
                                requestedUser.incomingFriendRequests.Remove(userId);
                                requestedUser.friends.Remove(userId);
                            }

                            if (userDatabase.ReplaceSingleDatabaseEntry("_id", userId, user.ToBson()) &&
                                userDatabase.ReplaceSingleDatabaseEntry("_id", requestedUserId, requestedUser.ToBson()))
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
            else if (req.HttpMethod == "GET" && req.Url?.AbsolutePath == "/health")
            {
                Response.Success(resp, "service up", "");
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
        EasyMango.EasyMango userDatabase =
            new EasyMango.EasyMango(connectionString, userDatabaseNAme, userCollectionName);
        EasyMango.EasyMango postDatabase =
            new EasyMango.EasyMango(connectionString, postDatabaseNAme, postCollectionName);

        // Handle requests
        Task listenTask = HandleIncomingConnections(userDatabase, postDatabase, jwt, doorUrl);
        listenTask.GetAwaiter().GetResult();

        // Close the listener
        listener.Close();
    }
}
