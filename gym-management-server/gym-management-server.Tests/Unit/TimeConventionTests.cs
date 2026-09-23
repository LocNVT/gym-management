using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    /// <summary>
    /// Pins the project's time convention: everything is stored in UTC.
    ///
    /// This exists because the convention was broken once and nothing noticed.
    /// <c>CheckInService.CreateAsync</c> stamped manual check-ins with
    /// <c>DateTime.Now</c> while the fingerprint path used <c>DateTime.UtcNow</c>, so the
    /// CheckIns table held two clocks seven hours apart and every report over it was wrong
    /// for half its rows. A unit test of either path in isolation passes; only a rule about
    /// the whole codebase catches it.
    /// </summary>
    public class TimeConventionTests
    {
        // DateTime.Now / DateTime.Today read the SERVER's local clock, which is a deployment
        // detail. Use DateTime.UtcNow and convert for display.
        private static readonly Regex LocalClock =
            new(@"\bDateTime\s*\.\s*(Now|Today)\b", RegexOptions.Compiled);

        [Fact]
        public void No_source_file_reads_the_server_local_clock()
        {
            var offenders = ServerSourceFiles()
                .Select(path => (path, text: File.ReadAllText(path)))
                .Where(f => LocalClock.IsMatch(f.text))
                .Select(f => Path.GetFileName(f.path))
                .OrderBy(name => name)
                .ToList();

            Assert.True(
                offenders.Count == 0,
                "Everything is stored in UTC. These files read the server's local clock via " +
                "DateTime.Now or DateTime.Today — use DateTime.UtcNow instead and convert only " +
                "for display: " + string.Join(", ", offenders));
        }

        [Fact]
        public void The_convention_test_is_actually_reading_source_files()
        {
            // Without this, a broken path would make the rule above pass vacuously forever.
            var files = ServerSourceFiles();

            Assert.True(files.Count > 20, $"Expected to scan the server project's sources, found {files.Count}.");
            Assert.Contains(files, f => Path.GetFileName(f) == "Program.cs");
        }

        /// <summary>
        /// Every .cs file the server project owns, excluding generated output and EF migrations
        /// (migration designer files embed timestamps we do not control).
        /// </summary>
        private static List<string> ServerSourceFiles()
        {
            var projectDir = LocateServerProject();

            return Directory
                .EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                .Where(path =>
                {
                    var relative = Path.GetRelativePath(projectDir, path);
                    return !relative.StartsWith("obj" + Path.DirectorySeparatorChar)
                        && !relative.StartsWith("bin" + Path.DirectorySeparatorChar)
                        && !relative.StartsWith("Migrations" + Path.DirectorySeparatorChar);
                })
                .ToList();
        }

        private static string LocateServerProject()
        {
            // Walk up from the test assembly (…/gym-management-server.Tests/bin/Debug/net8.0)
            // until we find the sibling server project. Fail loudly rather than silently
            // scanning nothing.
            var dir = new DirectoryInfo(AppContext.BaseDirectory);

            while (dir is not null)
            {
                var candidate = Path.Combine(dir.FullName, "gym-management-server");
                if (File.Exists(Path.Combine(candidate, "gym-management-server.csproj")))
                    return candidate;

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException(
                $"Could not locate the server project above {AppContext.BaseDirectory}. " +
                "The time-convention rule cannot run, so it must not report success.");
        }
    }
}
