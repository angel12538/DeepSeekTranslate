using System.Collections.Generic;
using XUnity.AutoTranslator.Plugin.Core.Endpoints;

namespace DeepSeekTranslate.Tests
{
    public class TestInitializationContext : IInitializationContext
    {
        private readonly Dictionary<string, object> _settings;

        public TestInitializationContext(string sourceLanguage, string destinationLanguage, Dictionary<string, object> settings = null)
        {
            SourceLanguage = sourceLanguage;
            DestinationLanguage = destinationLanguage;
            _settings = settings ?? new Dictionary<string, object>();
        }

        public string PluginDirectory => ".";

        public string SourceLanguage { get; }

        public string DestinationLanguage { get; }

        public T GetOrCreateSetting<T>(string section, string key, T defaultValue)
        {
            var dictKey = section + "." + key;
            if (_settings.TryGetValue(dictKey, out var value))
            {
                return (T)value;
            }
            return defaultValue;
        }

        public T GetOrCreateSetting<T>(string section, string key)
        {
            return GetOrCreateSetting(section, key, default(T));
        }

        public void SetSetting<T>(string section, string key, T value)
        {
            _settings[section + "." + key] = value;
        }

        public void DisableCertificateChecksFor(params string[] hosts)
        {
        }
    }
}
