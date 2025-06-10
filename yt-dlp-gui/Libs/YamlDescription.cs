using System;
using System.ComponentModel;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;

namespace yt_dlp_gui.Libs
{
    public class YamlDescription : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            // This converter can apply to any type with properties.
            return true;
        }

        public object ReadYaml(IParser parser, Type type, ObjectDeserializer nestedObjectDeserializer)
        {
            // We are not using this converter for reading YAML, so we can leave it unimplemented.
            throw new NotImplementedException();
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer nestedObjectSerializer)
        {
            if (value == null) return;

            var pds = TypeDescriptor.GetProperties(value);
            emitter.Emit(new MappingStart(null, null, false, MappingStyle.Block));

            foreach (PropertyDescriptor pd in pds)
            {
                if (pd.IsBrowsable && pd.Name != "Configs")
                {
                    var description = pd.Description;
                    if (!string.IsNullOrEmpty(description))
                    {
                        // Emit each line of the description as a separate comment
                        foreach (var line in description.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
                        {
                            emitter.Emit(new Comment($" {line}", false));
                        }
                    }

                    // Emit the property name
                    emitter.Emit(new Scalar(pd.Name));

                    // Emit the property value using the nested serializer
                    var propertyValue = pd.GetValue(value);
                    nestedObjectSerializer(propertyValue, pd.PropertyType);
                }
            }
            emitter.Emit(new MappingEnd());
        }
    }
}
