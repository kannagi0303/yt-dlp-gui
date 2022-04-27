using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Libs.Yaml {
    public static class Yaml {
        public static T Open<T>(string path) where T : new() {
            if (File.Exists(path)) {
                try {
                    using (var yaml = File.OpenText(path)) {
                        var deserializer = new DeserializerBuilder()
                            .IgnoreUnmatchedProperties()
                            .Build();
                        return deserializer.Deserialize<T>(yaml);
                    }
                } catch (YamlException e) {
                    //Debug.WriteLine(e.Message);
                }
            }
            return new T();
        }
        public static string Serialize(object obj) {
            var s = new SerializerBuilder()
                .WithTypeInspector(inner => new CommentGatheringTypeInspector(inner))
                .WithEmissionPhaseObjectGraphVisitor(args => new SkipEmptyObjectGraphVisitor(args.InnerVisitor))
                .WithEmissionPhaseObjectGraphVisitor(args => new CommentsObjectGraphVisitor(args.InnerVisitor))
                .Build();
            return s.Serialize(obj);
        }
        public static void Save(string path, object graph) {
            FileInfo info = new FileInfo(path);
            Directory.CreateDirectory(info.DirectoryName);
            using (var yaml = new StreamWriter(info.FullName, false, Encoding.UTF8)) {
                var s = new SerializerBuilder()
                    .WithTypeInspector(inner => new CommentGatheringTypeInspector(inner))
                    .WithEmissionPhaseObjectGraphVisitor(args => new SkipEmptyObjectGraphVisitor(args.InnerVisitor))
                    .WithEmissionPhaseObjectGraphVisitor(args => new CommentsObjectGraphVisitor(args.InnerVisitor))
                    .Build();
                s.Serialize(yaml, graph);
            }
        }
        public static void Load(this IYamlConfig obj, string path) {
            MethodInfo mi = typeof(Yaml).GetMethod(nameof(Yaml.Open)); //方法
            if (mi.IsGenericMethod) {
                //泛型呼叫
                var func = mi.MakeGenericMethod(new[] { obj.GetType() });
                var data = func.Invoke(null, new[] { path }) as IYamlConfig; //参数
                Util.PropertyCopy(data, obj);
                obj._YAMLPATH = path;
            }
        }
        public static void Save(this IYamlConfig obj, string path = "") {
            var filename = string.IsNullOrWhiteSpace(path) ? obj._YAMLPATH : path;
            if (!string.IsNullOrWhiteSpace(filename)) {
                Save(filename, obj);
            }
        }
    }
    public class IYamlConfig {
        [YamlIgnore] public string _YAMLPATH { get; set; }
    }
}
