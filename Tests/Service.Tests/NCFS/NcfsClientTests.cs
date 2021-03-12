using Moq;
using NUnit.Framework;
using Service.Configuration;
using Service.NCFS;
using Flurl.Http.Testing;
using Glasswall.Core.Engine.Messaging;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Service.StoreMessages.Enums;

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

            [TestCase(NcfsDecision.Block)]
            [TestCase(NcfsDecision.Relay)]
            [TestCase(NcfsDecision.Replace)]
            public async Task Successful_Call_To_The_API_Returns_Correct_Decision(NcfsDecision expectedDecision)
            {
                // Arrange
                var expectedBase64 = "Expected Replacement";

                _httpTest.RespondWithJson(new { base64Replacement = expectedBase64 }, headers: new Dictionary<string, string>() { { "ncfs-decision", expectedDecision.ToString() } });

                // Act
                var result = await _client.GetOutcome("base64", FileType.Doc);

                // Assert
                Assert.That(result.NcfsDecision, Is.EqualTo(expectedDecision));
            }

            [Test]
            public async Task Successful_Call_To_The_API_Returns_Base64_Replacement()
            {
                // Arrange
                var expectedDecision = NcfsDecision.Replace;
                var expectedBase64 = "Expected Replacement";

                _httpTest.RespondWithJson(new { base64Replacement = expectedBase64 }, headers: new Dictionary<string, string>() { { "ncfs-decision", expectedDecision.ToString() } });

                // Act
                var result = await _client.GetOutcome("base64", FileType.Doc);

                // Assert
                Assert.That(result.Base64Replacement, Is.EqualTo(expectedBase64));
            }

            [Test]
            public async Task Successful_Call_To_The_API_Returns_Replacement_MimeType()
            {
                // Arrange
                var expectedDecision = NcfsDecision.Replace;
                var expectedBase64 = "Expected Replacement";
                var expectedMimeType = "text/json";

                _httpTest.RespondWithJson(new { base64Replacement = expectedBase64 }, headers: new Dictionary<string, string>() { { "ncfs-decision", expectedDecision.ToString() }, { "ncfs-replacement-mimetype", expectedMimeType } });

                // Act
                var result = await _client.GetOutcome("base64", FileType.Doc);

                // Assert
                Assert.That(result.ReplacementMimeType, Is.EqualTo(expectedMimeType));
            }

            [TestCase("NCFS-DECISION", "NCFS-REPLACEMENT-MIMETYPE")]
            [TestCase("ncfs-decision", "ncfs-replacement-mimetype")]
            [TestCase("Ncfs-Decision", "Ncfs-Replacement-Mimetype")]
            public async Task Headers_Are_Case_Insensitive(string decisionKey, string mimetypeKey)
            {
                // Arrange
                var expectedDecision = NcfsDecision.Replace;
                var expectedBase64 = "Expected Replacement";
                var expectedMimeType = "text/json";

                _httpTest.RespondWithJson(new { base64Replacement = expectedBase64 }, headers: new Dictionary<string, string>() { { decisionKey, expectedDecision.ToString() }, { mimetypeKey, expectedMimeType } });

                // Act
                var result = await _client.GetOutcome("base64", FileType.Doc);

                // Assert
                Assert.That(result.NcfsDecision, Is.EqualTo(expectedDecision));
                Assert.That(result.ReplacementMimeType, Is.EqualTo(expectedMimeType));
            }

            [TestCase("replace", NcfsDecision.Replace)]
            [TestCase("relay", NcfsDecision.Relay)]
            [TestCase("block", NcfsDecision.Block)]
            public async Task Decision_Enum_Is_Case_Insensitive(string returned, NcfsDecision expected)
            {
                // Arrange
                var expectedBase64 = "Expected Replacement";
                var expectedMimeType = "text/json";

                _httpTest.RespondWithJson(new { base64Replacement = expectedBase64 }, headers: new Dictionary<string, string>() { { "ncfs-decision", returned }, { "ncfs-replacement-mimetype", expectedMimeType } });

                // Act
                var result = await _client.GetOutcome("base64", FileType.Doc);

                // Assert
                Assert.That(result.NcfsDecision, Is.EqualTo(expected));
            }

            [Test]
            public async Task Block_Is_Returned_When_Cannot_Parse_Returned_Decision()
            {
                // Arrange
                var expectedBase64 = "Expected Replacement";
                var expectedDecision = "I AM NOT A CORRECT DECISION";

                _httpTest.RespondWithJson(new { base64Replacement = expectedBase64 }, headers: new Dictionary<string, string>() { { "ncfs-decision", expectedDecision.ToString() } });

                // Act
                var result = await _client.GetOutcome("base64", FileType.Doc);

                // Assert
                Assert.That(result.NcfsDecision, Is.EqualTo(NcfsDecision.Block));
            }

            [Test]
            public async Task Unsuccessful_Call_To_The_API_Returns_Failed_Outcome()
            {
                // Arrange
                var expectedDecision = NcfsDecision.Block;

                _httpTest.RespondWith("error", 500);

                // Act
                var result = await _client.GetOutcome("base64", FileType.Doc);

                // Assert
                Assert.That(result.NcfsDecision, Is.EqualTo(expectedDecision));
            }
        }
    }
}
