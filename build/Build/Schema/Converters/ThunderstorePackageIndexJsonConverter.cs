using Build.Schema.Thunderstore.API;

namespace Build.Schema.Converters;

public class ThunderstorePackageIndexJsonConverter : EntriesDictionaryJsonConverter<ThunderstorePackageListingIndex, (string @namespace, string name), ThunderstorePackageListing>
{
    public override (string @namespace, string name) KeyForValue(ThunderstorePackageListing value) => (value.Namespace, value.Name);
}
