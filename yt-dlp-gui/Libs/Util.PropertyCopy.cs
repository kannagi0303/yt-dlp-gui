namespace Libs {
    public partial class Util {
        public static void PropertyCopy<TParent, TChild>(TParent from, TChild to) {
            var parentProperties = from.GetType().GetProperties();
            var childProperties = to.GetType().GetProperties();

            foreach (var parentProperty in parentProperties) {
                foreach (var childProperty in childProperties) {
                    if (parentProperty.Name == childProperty.Name && parentProperty.PropertyType == childProperty.PropertyType) {
                        if (childProperty.CanWrite) {
                            var value = parentProperty.GetValue(from);
                            // Optional: Add check here if 'value' is null and childProperty.PropertyType is non-nullable value type
                            // However, for reference types, SetValue should handle nullable reference type mismatches if target expects non-null.
                            childProperty.SetValue(to, value);
                        }
                        break;
                    }
                }
            }
        }
    }
}
