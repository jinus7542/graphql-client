using System;
using System.Text.RegularExpressions;
using LitJson;

namespace GW
{
    public static class Extensions
    {
        public static string ReplaceFirst(this string src, string oldValue, string newValue)
        {
            var idx = src.IndexOf(oldValue);
            return src.Remove(idx, oldValue.Length).Insert(idx, newValue);
        }

        public static string ReplaceLast(this string src, string oldValue, string newValue)
        {
            var idx = src.LastIndexOf(oldValue);
            return src.Remove(idx, oldValue.Length).Insert(idx, newValue);
        }

        public static string Build(this string query, object[] param = null)
        {
            if (null != param)
            {
                var pattern = "[$]\\w+";
                var rg = new Regex(pattern);
                var matched = rg.Matches(query);
                if (matched.Count != param.Length)
                {
                    throw new Exception($"parameter validation failed. {matched.Count} {param.Length}");
                }
                var i = 0;
                foreach (var match in matched)
                {
                    var value = JsonMapper.ToJson(new { anonymous_param_ = param[i++] });
                    value = value.ReplaceFirst($"{{\"anonymous_param_\":", "");
                    value = value.ReplaceLast("}", "");
                    query = query.ReplaceFirst(match.ToString(), value.ToString());
                }
            }
            return query;
        }
    }
}
