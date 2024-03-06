using System;
using System.Collections.Generic;
using Build.Schema.Thunderstore.API;

namespace Build.Schema.Converters;

public class ThunderstorePackageVersionListingIndexJsonConverter : EntriesDictionaryJsonConverter<Dictionary<Version, ThunderstorePackageVersionListing>, Version, ThunderstorePackageVersionListing>
{
    public override Version KeyForValue(ThunderstorePackageVersionListing value) => value.Version;
}
