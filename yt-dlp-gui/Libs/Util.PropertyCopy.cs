namespace Libs {
    public partial class Util {
        public static void PropertyCopy<TParent, TChild>(TParent from, TChild to) {
            var parentProperties = from.GetType().GetProperties();
            var childProperties = to.GetType().GetProperties();

            foreach (var parentProperty in parentProperties) {
                foreach (var childProperty in childProperties) {
                    if (parentProperty.Name == childProperty.Name && parentProperty.PropertyType == childProperty.PropertyType) {
                        childProperty.SetValue(to, parentProperty.GetValue(from));
                        break;
                    }
                }
            }
        }
    }
}
