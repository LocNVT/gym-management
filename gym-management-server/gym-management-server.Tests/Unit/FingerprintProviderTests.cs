using System;
using System.Collections.Generic;
using System.Linq;
using gym_management_server.Fingerprints;
using gym_management_server.Fingerprints.Providers;
using Xunit;

namespace gym_management_server.Tests.Unit
{
    public class FingerprintProviderTests
    {
        private static byte[] Template(params byte[] bytes) => bytes;

        [Fact]
        public void Mock_Identify_returns_match_for_identical_template()
        {
            var provider = new MockFingerprintProvider();
            var memberId = Guid.NewGuid();
            var templateId = Guid.NewGuid();
            var tmpl = Template(1, 2, 3, 4, 5);

            var candidates = new List<FingerprintCandidate> { new(memberId, templateId, tmpl) };
            var result = provider.Identify(tmpl, candidates);

            Assert.True(result.Matched);
            Assert.Equal(memberId, result.MemberId);
            Assert.Equal(templateId, result.TemplateId);
            Assert.Equal(100, result.Score);
        }

        [Fact]
        public void Mock_Identify_returns_no_match_below_threshold()
        {
            var provider = new MockFingerprintProvider();
            var candidates = new List<FingerprintCandidate>
            {
                new(Guid.NewGuid(), Guid.NewGuid(), Template(9, 9, 9, 9, 9))
            };

            var result = provider.Identify(Template(1, 2, 3, 4, 5), candidates);

            Assert.False(result.Matched);
            Assert.True(result.Score < MockFingerprintProvider.MatchThreshold);
        }

        [Fact]
        public void Mock_Identify_picks_best_candidate()
        {
            var provider = new MockFingerprintProvider();
            var probe = Template(1, 2, 3, 4, 5);

            var poor = new FingerprintCandidate(Guid.NewGuid(), Guid.NewGuid(), Template(1, 0, 0, 0, 0));
            var best = new FingerprintCandidate(Guid.NewGuid(), Guid.NewGuid(), Template(1, 2, 3, 4, 0));

            var result = provider.Identify(probe, new List<FingerprintCandidate> { poor, best });

            Assert.True(result.Matched);
            Assert.Equal(best.MemberId, result.MemberId);
        }

        [Fact]
        public void Mock_Identify_empty_candidates_is_no_match()
        {
            var provider = new MockFingerprintProvider();
            var result = provider.Identify(Template(1, 2, 3), new List<FingerprintCandidate>());
            Assert.False(result.Matched);
            Assert.Equal(0, result.Score);
        }

        [Fact]
        public void Factory_resolves_by_vendor_case_insensitively()
        {
            var factory = new FingerprintProviderFactory(new IFingerprintProvider[]
            {
                new MockFingerprintProvider(),
                new ZkTecoFingerprintProvider()
            });

            Assert.IsType<MockFingerprintProvider>(factory.GetProvider("mock"));
            Assert.IsType<ZkTecoFingerprintProvider>(factory.GetProvider("ZKTECO"));
        }

        [Fact]
        public void Factory_throws_for_unknown_vendor()
        {
            var factory = new FingerprintProviderFactory(new IFingerprintProvider[] { new MockFingerprintProvider() });
            Assert.Throws<NotSupportedException>(() => factory.GetProvider("Nonexistent"));
        }

        [Theory]
        [InlineData("ZKTeco")]
        [InlineData("Suprema")]
        [InlineData("DigitalPersona")]
        public void Vendor_stubs_throw_not_implemented(string vendor)
        {
            var factory = new FingerprintProviderFactory(new IFingerprintProvider[]
            {
                new ZkTecoFingerprintProvider(),
                new SupremaFingerprintProvider(),
                new DigitalPersonaFingerprintProvider()
            });

            var provider = factory.GetProvider(vendor);
            Assert.Throws<NotImplementedException>(() => provider.CreateTemplate(new byte[] { 1 }));
            Assert.Throws<NotImplementedException>(() =>
                provider.Identify(new byte[] { 1 }, new List<FingerprintCandidate>()));
        }
    }
}
