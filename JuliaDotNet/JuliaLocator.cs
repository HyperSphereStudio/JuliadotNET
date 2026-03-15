using System.Text.RegularExpressions;

namespace JuliaDotNet;

public readonly record struct JuliaVersion : IComparable<JuliaVersion>
{
    public int Major { get; init; }
    public int Minor { get; init; }
    public int Patch { get; init; }

    public JuliaVersion(int major, int minor, int patch)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }
    
    public static bool TryParse(string input, out JuliaVersion version) {
        version = default;
        if (string.IsNullOrWhiteSpace(input)) return false;

        // Clean up common prefixes if they exist
        var clean = input.Replace("julia-", "").Split('+')[0];
        var parts = clean.Split('.');

        if (parts.Length < 2) return false;

        if (int.TryParse(parts[0], out int major) &&
            int.TryParse(parts[1], out int minor))
        {
            int patch = parts.Length > 2 && int.TryParse(parts[2], out int p) ? p : 0;
            version = new JuliaVersion(major, minor, patch);
            return true;
        }

        return false;
    }

    public int CompareTo(JuliaVersion other)
    {
        int majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0) return majorComparison;
        
        int minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0) return minorComparison;
        
        return Patch.CompareTo(other.Patch);
    }

    public override string ToString() => $"{Major}.{Minor}.{Patch}";

    // Operator overloads for clean logic in your Compare delegate
    public static bool operator >(JuliaVersion a, JuliaVersion b) => a.CompareTo(b) > 0;
    public static bool operator <(JuliaVersion a, JuliaVersion b) => a.CompareTo(b) < 0;
    public static bool operator >=(JuliaVersion a, JuliaVersion b) => a.CompareTo(b) >= 0;
    public static bool operator <=(JuliaVersion a, JuliaVersion b) => a.CompareTo(b) <= 0;
}

public static class JuliaLocator {
    public delegate int JuliaCompare(string path, JuliaVersion version);
    
    public static string? GetJuliaPath(JuliaCompare compare) {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".julia", "juliaup");
      
        if (!Directory.Exists(path))
            return null;

        string? bestMatch = null;
        int bestMatchVal = 0;
        var versionRegex = new Regex(@"julia-(?<ver>\d+\.\d+\.\d+)");
        
        foreach (var f in Directory.EnumerateDirectories(path)) {
     
            var versionStr = versionRegex.Match(f);
            if (!versionStr.Success)
                continue;
            
            if(!JuliaVersion.TryParse(versionStr.Groups["ver"].Value, out var vers))
                throw new Exception($"Unable to parse Julia version: {versionStr}");
            
            var k = compare(f, vers);
            if (bestMatchVal >= k) continue;
            bestMatch = f;
            bestMatchVal = k;
        }

        return bestMatch;
    }
}