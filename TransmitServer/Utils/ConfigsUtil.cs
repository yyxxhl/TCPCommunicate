using System.Collections.Generic;
using System.Configuration;

namespace TransmitServer.Utils
{
    public class ConfigsUtil
    {
        private static ConfigsUtil _configsUtil;
        private static Dictionary<string, string> _dic;
        private ConfigsUtil()
        {
            InitConfigs();
        }

        private void InitConfigs()
        {
            if (_dic == null)
            {
                _dic = new Dictionary<string, string>();
                var settings = ConfigurationManager.AppSettings;
                if (settings != null && settings.Count > 0)
                {
                    foreach (var item in settings.AllKeys)
                    {
                        if (!_dic.ContainsKey(item))
                        {
                            _dic.Add(item.ToUpper(), settings.Get(item));
                        }
                    }
                }
            }
        }

        public static ConfigsUtil Instance
        {
            get
            {
                if (_configsUtil == null)
                {
                    _configsUtil = new ConfigsUtil();
                }
                return _configsUtil;
            }
        }

        public string GetConfig(string key)
        {
            string val = string.Empty;
            if (_dic == null)
            {
                return val;
            }
            key = key.ToUpper();
            if (_dic.ContainsKey(key))
            {
                return _dic[key];
            }
            return val;
        }

        public int GetIntConfig(string key)
        {
            string str = GetConfig(key);
            int val = 0;
            int.TryParse(str, out val);
            return val;
        }
    }
}
