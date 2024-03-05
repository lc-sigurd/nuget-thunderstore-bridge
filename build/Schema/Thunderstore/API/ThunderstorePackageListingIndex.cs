using System.Collections.Generic;
using Build.Schema.Converters;
using Newtonsoft.Json;

namespace Build.Schema.Thunderstore.API;

[JsonConverter(typeof(ThunderstorePackageIndexJsonConverter))]
public class ThunderstorePackageListingIndex : Dictionary<(string @namespace, string name), ThunderstorePackageListing>;
