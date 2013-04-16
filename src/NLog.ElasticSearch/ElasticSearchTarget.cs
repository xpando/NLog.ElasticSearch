using System;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog.Common;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace NLog.ElasticSearch
{
    [Target("ElasticSearch")]
    public sealed class ElasticSearchTarget : Target
    {
        [RequiredParameter]
        public Layout Url { get; set; }

        [RequiredParameter]
        public Layout Layout { get; set; }

        protected override void Write(AsyncLogEventInfo info)
        {
            try
            {
                var url = new Uri(this.Url.Render(info.LogEvent));
                var layout = this.Layout.Render(info.LogEvent);
                var json = JObject.Parse(layout).ToString(); // make sure the json is valid
                var client = new WebClient();

                client.Headers.Add(HttpRequestHeader.ContentType, "application/json");

                UploadStringCompletedEventHandler cb = null;
                cb = (s, e) =>
                {
                    if (cb != null)
                        client.UploadStringCompleted -= cb;

                    if (e.Error != null)
                    {
                        if (e.Error is WebException)
                        {
                            var we = e.Error as WebException;
                            try
                            {
                                var result = JObject.Load(new JsonTextReader(new StreamReader(we.Response.GetResponseStream())));
                                var error = result.GetValue("error");
                                if (error != null)
                                {
                                    info.Continuation(new Exception(result.ToString(), e.Error));
                                    return;
                                }
                            }
                            catch (Exception) { info.Continuation(new Exception("Failed to send log event to ElasticSearch", e.Error)); }
                        }

                        info.Continuation(e.Error);

                        return;
                    }

                    info.Continuation(null);
                };

                client.UploadStringCompleted += cb;
                client.UploadStringAsync(url, "PUT", json);
            }
            catch (Exception ex)
            {
                info.Continuation(ex);
            }
        }

        protected override void Write(LogEventInfo logEvent)
        {
            throw new NotImplementedException();
        }
    }
}
