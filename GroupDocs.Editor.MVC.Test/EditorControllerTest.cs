using GroupDocs.Editor.MVC.Products.Common.Entity.Web;
using GroupDocs.Editor.MVC.Products.Editor.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;

namespace NUnitTestProject1
{
    public class EditorControllerTest
    {
        Mock<IConfigurationSection> mockServerConfSection;
        Mock<IConfigurationSection> mockCommonConfSection;
        Mock<IConfiguration> mockConfig;
        Mock<PostedDataEntity> mockPostedData;
        EditorApiController controller;

        [SetUp]
        public void Setup()
        {
            mockServerConfSection = new Mock<IConfigurationSection>();
            mockServerConfSection.SetupGet(m => m[It.Is<string>(s => s == "HttpPort")]).Returns("8080");

            mockCommonConfSection = new Mock<IConfigurationSection>();
            mockCommonConfSection.SetupGet(m => m[It.Is<string>(s => s == "IsPageSelector")]).Returns("true");
            mockCommonConfSection.SetupGet(m => m[It.Is<string>(s => s == "IsDownload")]).Returns("true");
            mockCommonConfSection.SetupGet(m => m[It.Is<string>(s => s == "IsUpload")]).Returns("true");
            mockCommonConfSection.SetupGet(m => m[It.Is<string>(s => s == "IsPrint")]).Returns("true");
            mockCommonConfSection.SetupGet(m => m[It.Is<string>(s => s == "IsBrowse")]).Returns("true");
            mockCommonConfSection.SetupGet(m => m[It.Is<string>(s => s == "IsRewrite")]).Returns("true");

            mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(a => a.GetSection(It.Is<string>(s => s == "ServerConfiguration"))).Returns(mockServerConfSection.Object);
            mockConfig.Setup(a => a.GetSection(It.Is<string>(s => s == "CommonConfiguration"))).Returns(mockCommonConfSection.Object);

            mockPostedData = new Mock<PostedDataEntity>();
            controller = new EditorApiController(mockConfig.Object);
        }

        [Test]
        public void LoadFileTree_ReturnsOkActionResult()
        {
            // Arrange


            // Act
            var result = controller.loadFileTree(mockPostedData.Object);
            var okResult = result as OkObjectResult;

            // Assert
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
        }
    }
}