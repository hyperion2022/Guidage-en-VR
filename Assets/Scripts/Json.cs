using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public delegate string ToJson<T>(T value);

public class Json {
    public static string ListToJson<T>(IEnumerable<T> list, ToJson<T> convert) {
        return $"[{string.Join(',', list.Select(k => convert(k)))}]";
    }
    public static string DictToJson<K, V>(IDictionary<K, V> dict, ToJson<K> convertK, ToJson<V> convertV) {
        return $"{{{string.Join(',', dict.Select(p => convertK(p.Key) + ":" + convertV(p.Value)))}}}";
    }

    public static string AnyToJson(object any) {
        if (any is string s) {
            return StringToJson(s);
        }
        else if (any is Vector4 v) {
            return $"[{v.x}, {v.y}, {v.z}, {v.w}]";
        }
        throw new NotImplementedException();
    }

    public static string StringToJson(string v) => "\"" + v + "\"";

    public static Dict DictWriter => new Dict();
    public class Dict {
        private List<string> content = new List<string>();
        public Dict Field(string key, string value) {
            content.Add(StringToJson(key) + ":" + value);
            return this;
        }
        public string ToJson() => $"{{{string.Join(',', content)}}}";
    }
}