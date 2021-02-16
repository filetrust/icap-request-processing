using Moq;
using NUnit.Framework;
using Service.Configuration;
using Service.NCFS;
using Flurl.Http.Testing;
using Glasswall.Core.Engine.Messaging;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Service.Tests.NCFS
{
    public class NcfsClientTests
    {
        public class GetOutcomeMethod : NcfsClientTests
        {
            private Mock<IFileProcessorConfig> _mockConfig;
            private Mock<ILogger<NcfsClient>> _mockLogger;

            private NcfsClient _client;

            private HttpTest _httpTest;

            [SetUp]
            public void Setup()
            {
                _mockConfig = new Mock<IFileProcessorConfig>();
                _mockLogger = new Mock<ILogger<NcfsClient>> ();
                _mockConfig.SetupGet(s => s.NcfsRoutingUrl).Returns("http://localroast");

                _client = new NcfsClient(_mockConfig.Object, _mockLogger.Object);

                _httpTest = new HttpTest();
            }

            [TearDown]
            public void TearDown()
            {
                _httpTest.Dispose();
            }

            [Test]
            public async Task Successful_Call_To_The_API_Returns_Correct_Outcome()
            {
                // Arrange
                var expectedBase64 = "Expected Replacement";
                var expectedDecision = FileOutcome.Replace;

                _httpTest.RespondWithJson(new { base64Replacement = expectedBase64 }, headers: new Dictionary<string, string>() { { "ncfs-decision", expectedDecision } });

                // Act
                var result = await _client.GetOutcome("base64", FileType.Doc);

                // Assert
                Assert.That(result.NcfsDecision, Is.EqualTo(expectedDecision));
                Assert.That(result.Base64Replacement, Is.EqualTo(expectedBase64));
            }

            [Test]
            public async Task Unsuccessful_Call_To_The_API_Returns_Failed_Outcome()
            {
                // Arrange
                var expectedDecision = FileOutcome.Failed;

                _httpTest.RespondWith("error", 500);

                // Act
                var result = await _client.GetOutcome("base64", FileType.Doc);

                // Assert
                Assert.That(result.NcfsDecision, Is.EqualTo(expectedDecision));
            }
        }
    }
}
