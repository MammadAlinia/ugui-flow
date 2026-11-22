using NUnit.Framework;
using UnityEditor.PackageManager;

namespace UguiFlow.Tests
{
    public sealed class PackageSmokeTests
    {
        private const string FlowPackageId = "com.mammadalinia.ugui-flow";
        private const string UguiPackageId = "com.unity.ugui";

        [Test]
        public void UnityRegistersTheEmbeddedFlowPackage()
        {
            PackageInfo package = PackageInfo.FindForAssetPath($"Packages/{FlowPackageId}/package.json");

            Assert.That(package, Is.Not.Null);
            Assert.That(package.name, Is.EqualTo(FlowPackageId));
            Assert.That(package.displayName, Is.EqualTo("uGUI Flow"));
            Assert.That(package.source, Is.EqualTo(PackageSource.Embedded));
            Assert.That(package.dependencies, Has.Some.Matches<DependencyInfo>(
                dependency => dependency.name == UguiPackageId && dependency.version == "2.0.0"));
        }

        [Test]
        public void UnityResolvesUguiVersionTwo()
        {
            PackageInfo package = PackageInfo.FindForAssetPath($"Packages/{UguiPackageId}/package.json");

            Assert.That(package, Is.Not.Null);
            Assert.That(package.name, Is.EqualTo(UguiPackageId));
            Assert.That(package.version, Is.EqualTo("2.0.0"));
        }
    }
}
