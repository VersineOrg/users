using System;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace users;

public class ResponseFormat
    {
        public String status { get; set; }
        public String message { get; set; }
        public string? data { get; set; }
    }

    public class Response
    {
        public static void Success(HttpListenerResponse resp, string message, string? data)
        {
            ResponseFormat response = new ResponseFormat
            {
                status = "success",
                message = message,
                data = data
            };
            string jsonString = JsonConvert.SerializeObject(response);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonString);

            try
            {
                resp.ContentLength64 = buffer.LongLength;
                resp.ContentType = "application/json";
                resp.ContentEncoding = Encoding.UTF8;
                resp.OutputStream.Write(buffer, 0, buffer.Length);

            }
            catch
            {
                // ignored
            }
        }

        public static void Fail(HttpListenerResponse resp, string message)
        {
            ResponseFormat response = new ResponseFormat
            {
                status = "fail",
                message = message
            };
            string jsonString = JsonConvert.SerializeObject(response);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonString);

            try
            {
                resp.ContentLength64 = buffer.LongLength;
                resp.ContentType = "application/json";
                resp.ContentEncoding = Encoding.UTF8;
                resp.OutputStream.Write(buffer, 0, buffer.Length);
            }
            catch
            {
                // ignored
            }
        }

        public static string BuildData(string username, string Avatar, string bio)
        {
            JObject data =
                new JObject(
                    new JProperty("user",
                        new JObject(
                            new JProperty("name", username),
                            new JProperty("Avatar", Avatar),
                            new JProperty("bio", bio))));
            return data.ToString();
        }
    }