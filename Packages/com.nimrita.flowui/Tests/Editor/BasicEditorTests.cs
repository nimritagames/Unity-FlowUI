using NUnit.Framework;
using Nimrita.FlowUI;

namespace Nimrita.FlowUI.Tests.Editor
{
    /// <summary>
    /// Basic editor tests for Flow UI System.
    /// These tests run in the Unity Editor (EditMode).
    /// </summary>
    public class BasicEditorTests
    {
        [Test]
        public void PackageExists()
        {
            // This is a simple placeholder test to verify the test framework works
            Assert.IsTrue(true, "Package test framework is working");
        }

        [Test]
        public void UIManagerTypeExists()
        {
            // Verify the UIManager type can be found
            var uiManagerType = typeof(UIManager);
            Assert.IsNotNull(uiManagerType, "UIManager type should exist");
        }
    }
}
