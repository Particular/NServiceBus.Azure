<Query Kind="Program">
  <NuGetReference>NuGet.PackageManagement</NuGetReference>
  <NuGetReference>NuGet.Protocol.Core.v3</NuGetReference>
  <NuGetReference>YamlDotNet</NuGetReference>
  <Namespace>NuGet.Common</Namespace>
  <Namespace>NuGet.Configuration</Namespace>
  <Namespace>NuGet.Packaging</Namespace>
  <Namespace>NuGet.Packaging.Core</Namespace>
  <Namespace>NuGet.Protocol</Namespace>
  <Namespace>NuGet.Protocol.Core.Types</Namespace>
  <Namespace>NuGet.Versioning</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>YamlDotNet.Serialization</Namespace>
</Query>

async Task Main()
{
	// older versions drag this one in as a dependency, but we can ignore it in the output
    var endOfLifePackages = new List<string>
    {
        { "NServiceBus.Azure" }
    };

    var source = "https://api.nuget.org/v3/index.json";
    var componentsPath = Path.Combine(Util.CurrentQuery.Location, @"components.yaml");
    var outputPath = Path.Combine(Util.CurrentQuery.Location, @"dependencies.md");

    var utcTomorrow = DateTime.UtcNow.Date.AddDays(1);
    var logger = new Logger();

    var packageMetadata = await new SourceRepository(new PackageSource(source), Repository.Provider.GetCoreV3()).GetResourceAsync<PackageMetadataResource>();

    var downstreamPackages =
        (await Task.WhenAll(GetComponents(componentsPath)
		    .SelectMany(component => component.NugetOrder
                .Select(packageId =>
                    new
                    {
                        Id = packageId,
                        Category = component.Category,
                    }))
		    .Distinct()
			.Where(package => !endOfLifePackages.Contains(package.Id))
            .Select(async package =>
                new Package
                {
                    Id = package.Id,
                    Category = package.Category,
                    Versions = await packageMetadata.GetVersions(package.Id, logger, true)
                })))
        .OrderBy(package => package.Id)
        .ToList();

    foreach (var package in downstreamPackages)
    {
        package.Dump(utcTomorrow);
    }

    using (var output = new StreamWriter(outputPath, append: false))
    {
        output.Write(downstreamPackages, utcTomorrow, null, true);
    }
}

public static class TextWriterExtensions
{
    public static void Write(this TextWriter output, Package package, DateTimeOffset utcTomorrow, DateTimeOffset? earliest, bool force) =>
        output.Write(
            package.Versions,
            utcTomorrow,
            earliest,
            force,
            () =>
            {
                output.WriteLine($"### {package.Id}");
                output.WriteLine();
            });

    public static void Write(this TextWriter output, IEnumerable<Package> packages, DateTimeOffset utcTomorrow, DateTimeOffset? earliest, bool force)
    {
        foreach (ComponentCategory category in Enum.GetValues(typeof(ComponentCategory)))
        {
            var categoryHeadingWritten = false;

            foreach (var package in packages.Where(package => package.Category == category))
            {
                var packageHeadingWritten = false;

                output.Write(
                    package.Versions,
                    utcTomorrow,
                    earliest,
                    force,
                    () =>
                    {
                        if (!categoryHeadingWritten)
                        {
                            output.WriteLine($"### {category} packages");
                            output.WriteLine();
                            categoryHeadingWritten = true;
                        }
                    },
                    () =>
                    {
                        if (!packageHeadingWritten)
                        {
                            output.WriteLine($"#### {package.Id}");
                            output.WriteLine();
                            packageHeadingWritten = true;
                        }
                    });
            }
        }
    }

    private static void Write(this TextWriter output, List<Version> versions, DateTimeOffset utcTomorrow, DateTimeOffset? earliest, bool force, params Action[] writeHeadings)
    {
        var relevantVersions = versions
            .Where(version => !earliest.HasValue || (!version.PatchingEnd.HasValue || version.PatchingEnd.Value >= earliest.Value))
            .ToList();

        if (!force && !relevantVersions.Any())
        {
            return;
        }

        foreach (var writeHeading in writeHeadings)
        {
            writeHeading();
        }

        if (!relevantVersions.Any())
        {
            output.WriteLine($"No versions released{(earliest.HasValue ? $" since {earliest.Value.ToString("yyyy-MMM-dd")}" : "")}.");
            output.WriteLine();

            return;
        }

        output.WriteLine("| Version   | Released       | Dependencies      |");
        output.WriteLine("|:---------:|:--------------:|:-----------------:|");

        foreach (var version in relevantVersions.OrderByDescending(version => version.First.Identity.Version))
        {
			if(version.Dependencies.Count() == 0)
			{
				output.Write($"| ");
	            output.Write($"{version.First.Identity.Version.ToMinorString()}".PadRight(9));
	            output.Write($" | ");
	            output.Write($"{version.First.Published.Value.UtcDateTime.Date.ToString("yyyy-MM-dd")}".PadRight(14));
	            output.Write($" | ");
	            output.Write($"".PadRight(17));
	            output.WriteLine($" |");
			}
			else{
				var first = true;
				foreach (var dependency in version.Dependencies)
	            {
					var id = (first) ? version.First.Identity.Version.ToMinorString() : "";
					var pub = (first) ? version.First.Published.Value.UtcDateTime.Date.ToString("yyyy-MM-dd") : "";
				    output.Write($"| ");
		            output.Write($"{id}".PadRight(9));
		            output.Write($" | ");
		            output.Write($"{pub}".PadRight(14));
		            output.Write($" | ");
		            output.Write($"{ dependency.Id } { dependency.VersionRange }".PadRight(17));
		            output.WriteLine($" |");
					
					first = false;
				}
			}
        }

        output.WriteLine();
    }
}

public static class PackageMetadataResourceExtensions
{
    public static async Task<List<Version>> GetVersions(this PackageMetadataResource resource, string packageId, ILogger logger, bool excludeOwnDependencies)
    {
        var minors = (await resource.GetMetadataAsync(packageId, true, false, logger, CancellationToken.None))
            .OrderBy(package => package.Identity.Version)
            .GroupBy(package => new { package.Identity.Version.Major, package.Identity.Version.Minor })
            .Select(group => new { First = group.First(), Last = group.Last()  })
            .ToList();

        var missingPublishedDate = minors.Where(package => !package.First.Published.HasValue).ToList();

        if (missingPublishedDate.Any())
        {
            throw new Exception($"These {packageId} packages have no published date: {string.Join(", ", missingPublishedDate.Select(minor => minor.First.Identity.Version))}");
        }

        return minors
            .Select(minor =>
            {                
                return new Version
                {
                    First = minor.First,
                    Last = minor.Last,
					Dependencies = minor.Last?.DependencySets.FirstOrDefault(s => s.TargetFramework.Framework == "Any")?.Packages.Where(p => excludeOwnDependencies ? !p.Id.Contains("NServiceBus") : true),
                };
            })
            .OrderBy(version => version.First.Identity)
            .ToList();
    }
}

public static class PackageExtensions
{
    public static void Dump(this Package package, DateTimeOffset utcTomorrow)
    {
        package.Id.Dump("Package");

        package.Versions
           .OrderByDescending(version => version.First.Identity.Version.Major)
           .ThenByDescending(version => version.First.Identity.Version.Minor)
           .Select(version => new
           {
               Package = version.ToString(),
               Published = version.First.Published?.UtcDateTime.Date.ToString("yyyy-MM-dd"),
               Dependencies = version.Dependencies.Select(d => new { d.Id, d.VersionRange })
           })
           .Dump("Versions");
    }
}

public static class PackageSearchMetadataExtensions
{
    public static string ToMinorString(this IPackageSearchMetadata package) =>
        package == null ? null : $"{package.Identity.Id} {package.Identity.Version.ToMinorString()}";
}

public static class NugetVersionExtensions
{
    public static string ToMinorString(this NuGetVersion version) => $"{version.Major}.{version.Minor}.x";
}

static IEnumerable<SerializationComponent> GetComponents(string path)
{
    List<SerializationComponent> components;
    using (var reader = File.OpenText(path))
    {
        components = new Deserializer().Deserialize<List<SerializationComponent>>(reader);
    }

    return components
        .Where(component => component.UsesNuget && component.SupportLevel == SupportLevel.Regular);
}

public class Package
{
    public string Id { get; set; }
    public ComponentCategory Category { get; set; }
    public List<Version> Versions { get; set; }
}

public class Version
{
    public IPackageSearchMetadata First { get; set; }
    public IPackageSearchMetadata Last { get; set; }
    public Version BoundedBy { get; set; }
    public Version ExtendedBy { get; set; }
    public DateTime? PatchingEnd { get; set; }
    public string PatchingEndReason { get; set; }
    public override string ToString() => this.First.ToMinorString();
	public IEnumerable<PackageDependency> Dependencies { get;set; }
}

public class SerializationComponent
{
    public SerializationComponent()
    {
        this.UsesNuget = true;
        this.SupportLevel = SupportLevel.Regular;
        this.Category = ComponentCategory.Other;
        this.NugetOrder = new List<string>();
    }

    public string Name { get; set; }
    public string Description { get; set; }
    public string Key { get; set; }
    public string DocoUrl { get; set; }
    public string ProjectUrl { get; set; }
    public string LicenseUrl { get; set; }
    public string GitHubOwner { get; set; }
    public bool UsesNuget { get; set; }
    public SupportLevel SupportLevel { get; set; }
    public ComponentCategory Category { get; set; }
    public List<string> NugetOrder { get; set; }
}

public enum SupportLevel
{
    Regular,
    Labs,
    Community,
}

public enum ComponentCategory
{
    Core,
    Transport,
    Persistence,
    Serializer,
    Container,
    Logger,
    Databus,
    Host,
    Other,
}

public class Logger : ILogger
{
    public void LogDebug(string data) { }
    public void LogVerbose(string data) { }
    public void LogInformation(string data) { }
    public void LogMinimal(string data) => $"INFO: {data}".Dump();
    public void LogWarning(string data) => $"WARNING: {data}".Dump();
    public void LogError(string data) => $"ERROR: {data}".Dump();
    public void LogInformationSummary(string data) => data.Dump("Information summary");
    public void LogErrorSummary(string data) => data.Dump("Error summary");
}