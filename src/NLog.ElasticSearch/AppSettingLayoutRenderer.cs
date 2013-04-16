using System.Configuration;
using System.Linq;
using System.Text;
using NLog.Config;
using NLog.LayoutRenderers;

namespace NLog.ElasticSearch
{
    [LayoutRenderer("AppSetting")]
    public sealed class AppSettingLayoutRenderer : LayoutRenderer
    {
        [RequiredParameter]
        public string Key { get; set; }

        public string Suffix { get; set; }

        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            if (!string.IsNullOrEmpty(this.Suffix))
            {
                var suffixParts = this.Suffix.Split(',');
                for (var i = suffixParts.Length; i > 0; i--)
                {
                    var keyBuilder = new StringBuilder(this.Key, 140);
                    foreach (var part in suffixParts.Take(i))
                    {
                        var partValue = ConfigurationManager.AppSettings[part.Trim()];
                        if (!string.IsNullOrEmpty(partValue))
                        {
                            keyBuilder.Append(".");
                            keyBuilder.Append(partValue);
                        }
                    }
                    var value = ConfigurationManager.AppSettings[keyBuilder.ToString()];
                    if (!string.IsNullOrEmpty(value))
                    {
                        builder.Append(value);
                        return;
                    }
                }
            }

            builder.Append(ConfigurationManager.AppSettings[this.Key]);
        }
    }
}
