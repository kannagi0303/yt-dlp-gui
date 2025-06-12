using System;
using System.ComponentModel;
using YamlDotNet.Core;
using YamlDotNet.Core.Events; // Required for MappingStart, MappingEnd, Scalar, Comment
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories; // Although ObjectDeserializer and ObjectSerializer are used, ObjectFactories might not be strictly needed for this specific converter's WriteYaml.

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
            // Or, if default deserialization is desired when this converter is registered:
            // return nestedObjectDeserializer(typeof(object)); // This might not be correct, depends on usage.
            // Safest is to throw if it's not meant to be used for reading.
            throw new NotImplementedException("This converter is only for writing YAML with comments.");
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer nestedObjectSerializer)
        {
            if (value == null) return;

            var pds = TypeDescriptor.GetProperties(value);
            emitter.Emit(new MappingStart(null, null, false, MappingStyle.Block));

            foreach (System.ComponentModel.PropertyDescriptor pd in pds)
            {
                // Original code had: pd.IsBrowsable && pd.Name != "Configs"
                // Let's keep that logic. If "Configs" is a specific property to skip.
                if (pd.IsBrowsable && pd.Name != "Configs")
                {
                    var description = pd.Description;
                    if (!string.IsNullOrEmpty(description))
                    {
                        // Emit each line of the description as a separate comment
                        foreach (var line in description.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)) // Escaped backslashes for string literal
                        {
                            emitter.Emit(new Comment($" {line}", false));
                        }
                    }

                    // Emit the property name
                    emitter.Emit(new Scalar(pd.Name));

                    // Emit the property value using the nested serializer
                    var propertyValue = pd.GetValue(value);
                    // Pass the actual type of the property for correct serialization
                    nestedObjectSerializer(propertyValue, pd.PropertyType);
                }
            }
            emitter.Emit(new MappingEnd());
        }
    }
}
