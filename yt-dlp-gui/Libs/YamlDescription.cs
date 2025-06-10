using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace yt_dlp_gui.Libs
{
    public class YamlDescription : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return true;
        }

        public object ReadYaml(IParser parser, Type type)
        {
            throw new NotImplementedException();
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type)
        {
            var pds = TypeDescriptor.GetProperties(value);
            emitter.Emit(new MappingStart(null, null, false, MappingStyle.Block));
            foreach (PropertyDescriptor pd in pds)
            {
                if (pd.IsBrowsable && pd.Name != "Configs")
                {
                    var description = pd.Description;
                    if (!string.IsNullOrEmpty(description))
                    {
                        emitter.Emit(new Comment(description.Replace("\r\n", "\r\n# "), false));
                    }
                    emitter.Emit(new Scalar(null, pd.Name));
                    var propertyValue = pd.GetValue(value);

                    // Manually handle nested objects or collections if necessary
                    // For simple values, this might suffice.
                    // This uses the default serializer's logic to write the value.
                    var serializer = new Serializer();
                    var writer = new System.IO.StringWriter();
                    serializer.Serialize(writer, propertyValue);
                    emitter.Emit(new Scalar(null, writer.ToString().Trim()));
                }
            }
            emitter.Emit(new MappingEnd());
        }
    }
}
